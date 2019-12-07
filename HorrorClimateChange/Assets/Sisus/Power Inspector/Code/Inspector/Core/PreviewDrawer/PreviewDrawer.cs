//#define DEBUG_SET_TARGETS
//#define DEBUG_RESIZING
//#define DEBUG_RESIZING_DETAILED
//#define DEBUG_MINIMIZED
//#define DEBUG_DISPOSE
//#define DEBUG_UPDATE_ACTIVE_PREVIEWS
//#define DEBUG_TOOLBAR

using System;
#if DEV_MODE
using System.Linq;
#endif
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class PreviewDrawer
	{
		private const float MinInspectorHeightWithoutPreviewArea = 131f;

		#if UNITY_2019_3_OR_NEWER
		private const float HeaderHeight = 22f;
		#else
		private const float HeaderHeight = 17f;
		#endif

		#if UNITY_2019_3_OR_NEWER
		private const float PreviewSettingsToolbarHeight = 20f;
		#else
		private const float PreviewSettingsToolbarHeight = 17f;
		#endif
		
		private const float AssetLabelAndAssetBundleEditorFullHeight = HeaderHeight + AssetLabelAndAssetBundleEditorBodyHeight;
		private const float AssetLabelEditorFullHeight = HeaderHeight + AssetLabelEditorBodyHeight;
		private const float AssetBundleEditorFullHeight = HeaderHeight + AssetBundleEditorBodyHeight;

		private const float AssetLabelAndAssetBundleEditorBodyHeight = AssetBundleEditorBodyHeight + AssetLabelEditorBodyHeight;

		#if UNITY_2019_3_OR_NEWER
		private const float AssetLabelEditorBodyHeight = 30f;
		#else
		private const float AssetLabelEditorBodyHeight = 28f;
		#endif

		private const float AssetBundleEditorBodyHeight = 18f + AssetBundleGUIBodyYPadding;

		private const float InitialHeightVisualPreviews = 160f;
		private const float InitialHeightNonVisualPreviews = 79f;
		
		private const float MinHeight = HeaderHeight;

		private const float AssetLabelButtonXOffset = 5f;

		private const float AssetLabelButtonYOffset = 5f;
		
		private const float AssetLabelButtonHeight = 15f;

		private const float AssetBundleGUIBodyXPadding = 4f;
		
		#if UNITY_2019_3_OR_NEWER
		private const float AssetBundleGUIBodyYPadding = 2f;
		#else
		private const float AssetBundleGUIBodyYPadding = 2f;
		#endif

		#if UNITY_EDITOR
		private static readonly GUIContent AssetBundleAndLabelsEditorTitle = new GUIContent("Asset Labels");
		private static readonly GUIContent AssetLabelsEditorTitle = new GUIContent("Asset Labels");
		private static readonly GUIContent AssetBundleEditorTitle = new GUIContent("Asset Bundle");
		#endif

		[SerializeField]
		private bool hidden;
		[SerializeField]
		private bool minimized;
		[SerializeField]
		private bool showAssetLabelEditor;
		[SerializeField]
		private bool showAssetBundleEditor;
		[SerializeField]
		private bool resizable;		
		[SerializeField]
		private bool hasVisualPreview;

		[SerializeField]
		private readonly List<IPreviewableWrapper> activePreviews = new List<IPreviewableWrapper>(3);

		[SerializeField]
		private List<IPreviewableWrapper> allPreviews = new List<IPreviewableWrapper>(3);

		[SerializeField]
		private int selectedIndex;
		
		[SerializeField]
		private bool hasPreviews;
		
		[SerializeField]
		private float heightWasBeforeMinimized;

		[SerializeField]
		private float currentHeight = MinHeight;
		
		[SerializeField]
		private float preferredInitialHeight = MinHeight;
					
		[SerializeField]
		private bool hasPreviewSettingsToolbar;

		#if UNITY_EDITOR
		[SerializeField]
		private readonly AssetBundleGUIDrawer assetBundleGUIDrawer;
		#endif

		private bool hasPreviewSettingsToolbarDetermined;
		private bool heightIsForCurrentTargets;
		private bool resizing;
		private bool mousePressedDownOverAHeader;
		private bool mousePressedDownOverSelectedHeader;
		private bool cursorMovedAfterMouseDown;
		private bool requiresConstantRepaint;
		private float requiresConstantRepaintLastUpdated;
		private float activePreviewsLastUpdated;

		private readonly IInspector inspector;
		
		#if UNITY_EDITOR
		private static Dictionary<string, float> assetLabelsMenuBuiltFromLabels;
		private static List<PopupMenuItem> assetLabelMenuItems = new List<PopupMenuItem>(100);
		private static Dictionary<string, PopupMenuItem> assetLabelMenuGroupsByLabel = new Dictionary<string, PopupMenuItem>(0);
		private static Dictionary<string, PopupMenuItem> assetLabelMenuItemsByLabel = new Dictionary<string, PopupMenuItem>(100);
		private static List<PopupMenuItem> tickedMenuItems = new List<PopupMenuItem>();
		#endif

		private bool cursorLeftHeaderBoundsAfterSelected;	
		
		public float Height
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				var expectedResult = GetCurrentHeightUpdated();
				if(!currentHeight.Equals(expectedResult))
				{
					// NOTE: blacklisting Height to prevent infinite loop, with GetFullStateInfo calling Height recursively!
					Debug.LogError("PreviewArea currentHeight ("+currentHeight+") != GetCurrentHeightUpdated() "+expectedResult+"!\n"+DebugUtility.GetFullStateInfo(this, "Height"));
				}
				#endif

				// Temp until can can make event-based updating be reliable enough that this isn't needed.
				UpdateCurrentHeight();

				return currentHeight;
			}
		}

		public bool HasPreviews
		{
			get
			{
				if(Platform.Time > activePreviewsLastUpdated + 0.1f)
				{
					UpdateActivePreviews();
				}
				return hasPreviews;
			}
		}

		private bool Hidden
		{
			get
			{
				if(hidden)
				{
					return true;
				}
				float availableSpace = inspector.State.WindowRect.height - MinInspectorHeightWithoutPreviewArea;
				return availableSpace < (minimized ? MinHeight : GetMinOpenHeight());
			}
		}

		public PreviewDrawer(IInspector parentInspector)
		{
			inspector = parentInspector;
			
			#if UNITY_EDITOR
			assetBundleGUIDrawer = new AssetBundleGUIDrawer();
			#endif

			Setup();
		}

		public PreviewDrawer(IInspector parentInspector, PreviewDrawer copySettingsFrom)
		{
			inspector = parentInspector;

			#if UNITY_EDITOR
			assetBundleGUIDrawer = new AssetBundleGUIDrawer();
			#endif

			Setup();

			hidden = copySettingsFrom.hidden;
			minimized = copySettingsFrom.minimized;
			selectedIndex = copySettingsFrom.selectedIndex;
			currentHeight = copySettingsFrom.currentHeight;
			heightWasBeforeMinimized = copySettingsFrom.heightWasBeforeMinimized;
		}

		private void Setup()
		{
			inspector.State.OnHeightChanged -= OnInspectorHeightChanged;
			inspector.State.OnHeightChanged += OnInspectorHeightChanged;

			heightWasBeforeMinimized = inspector.State.WindowRect.width;
		}
		
		public void ResetState()
		{
			#if UNITY_EDITOR
			assetBundleGUIDrawer.ResetState();
			#endif
			ClearPreviews();
			selectedIndex = 0;
			currentHeight = 0f;
			heightWasBeforeMinimized = 0f;
		}

		private void ClearPreviews()
		{
			#if UNITY_EDITOR
			for(int n = allPreviews.Count - 1; n >= 0; n--)
			{
				try
				{
					#if DEV_MODE && DEBUG_DISPOSE
					Debug.Log("Calling Dispose for preview "+StringUtils.TypeToString(allPreviews[n]));
					#endif

					var preview = allPreviews[n];
					Previews.Dispose(ref preview);
				}
				// An ArgumentException can occur here when Dispose gets called
				// for AssetImporterEditor of an asset that was just destroyed
				catch(Exception e)
				{
					Debug.LogWarning(e);
				}
			}
			#endif

			activePreviews.Clear();
			allPreviews.Clear();
			hasPreviews = false;
			mousePressedDownOverAHeader = false;
			mousePressedDownOverSelectedHeader = false;
			cursorMovedAfterMouseDown = false;
			heightIsForCurrentTargets = false;
		}
		
		public void SetTargets(Object[] setTargets, IDrawer drawer)
		{
			#if UNITY_EDITOR
			assetBundleGUIDrawer.SetAssets(setTargets);
			#endif
			
			ClearPreviews();
			drawer.AddPreviewWrappers(ref allPreviews);

			allPreviews.ForEach((prev)=>Debug.Assert(prev.StateIsValid, prev.ToString()+".StateIsValid: FALSE"));

			UpdateActivePreviews();

			selectedIndex = 0;

			hasPreviews = activePreviews.Count > 0;

			if(setTargets.Length == 0 || setTargets[0].IsSceneObject())
			{
				showAssetBundleEditor = false;
				showAssetLabelEditor = false;
			}
			else
			{
				#if UNITY_EDITOR
				showAssetBundleEditor = setTargets[0].GetType() != typeof(MonoScript);
				showAssetLabelEditor = true;
				#else
				showAssetBundleEditor = false;
				showAssetLabelEditor = false;
				#endif
			}

			hidden = !hasPreviews && !showAssetBundleEditor && !showAssetLabelEditor;
			resizable = hasPreviews || showAssetLabelEditor || showAssetBundleEditor;
			hasPreviewSettingsToolbar = hasPreviews;
			hasPreviewSettingsToolbarDetermined = !hasPreviewSettingsToolbar;

			UpdateHasVisualPreview();
			UpdateMinimizedAndSelectedForActivePreviews();

			UpdateCurrentHeight();

			inspector.RefreshView();

			#if DEV_MODE && DEBUG_SET_TARGETS
			Debug.Log(StringUtils.ToColorizedString("SetTargets(", StringUtils.ToString(setTargets), ") with showAssetLabelEditor=", showAssetLabelEditor, ", showAssetBundleEditor=", showAssetBundleEditor, ", Hidden=", Hidden, ", resizable=", resizable, ", currentHeight=", currentHeight, ", hasPreviews=", hasPreviews, ", activePreviews=", StringUtils.TypesToString(activePreviews)));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!showAssetBundleEditor || showAssetLabelEditor); // case where asset bundle editor is shown but asset labels editor is not shown shouldn't currently be supported
			Debug.Assert(!resizing);
			#endif
		}

		private void UpdateActivePreviews()
		{
			#if DEV_MODE && DEBUG_UPDATE_ACTIVE_PREVIEWS
			var activeWas = activePreviews.ToArray();
			#endif
			
			activePreviewsLastUpdated = Platform.Time;

			activePreviews.Clear();
			for(int n = 0, count = allPreviews.Count; n < count; n++)
			{
				var preview = allPreviews[n];
				if(preview.HasPreviewGUI())
				{
					activePreviews.Add(preview);
				}
				else if(n == selectedIndex)
				{
					heightIsForCurrentTargets = false;
				}
			}

			int activeCount = activePreviews.Count;
			if(activeCount == 0)
			{
				hasPreviews = false;
				SetSelectedIndex(0);
			}
			else
			{
				hasPreviews = true;
				SetSelectedIndex(Mathf.Clamp(selectedIndex, 0, activeCount - 1));
			}

			#if DEV_MODE && DEBUG_UPDATE_ACTIVE_PREVIEWS
			if(!activeWas.ContentsMatch(activePreviews))
			{
				#if DEV_MODE
				Debug.Log(StringUtils.ToColorizedString("Active Changed from ", activeWas.Length, " to ", activeCount, "/", allPreviews.Count, "\nfrom:\n", StringUtils.ToString(activePreviews, "\n"), "\n\nto:\n", StringUtils.ToString(activePreviews, "\n")));
				#endif
			}
			#endif
		}

		private void UpdateRequiresContantRepaint()
		{
			requiresConstantRepaintLastUpdated = Platform.Time;

			if(hasVisualPreview)
			{
				requiresConstantRepaint = true;
			}
			else
			{
				requiresConstantRepaint = activePreviews.Count > selectedIndex && activePreviews[selectedIndex].RequiresConstantRepaint;
			}
		}

		private void SetSelectedIndex(int setSelectedIndex)
		{
			#if DEV_MODE
			Debug.Assert(setSelectedIndex >= 0);
			Debug.Assert(setSelectedIndex < activePreviews.Count || activePreviews.Count == 0);
			#endif
			
			if(selectedIndex != setSelectedIndex)
			{
				selectedIndex = setSelectedIndex;
				hasPreviewSettingsToolbar = true;
				hasPreviewSettingsToolbarDetermined = false;
				heightIsForCurrentTargets = false;

				UpdateHasVisualPreview();

				UpdateCurrentHeight();
			}
		}

		private void UpdateHasVisualPreview()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(selectedIndex >= 0, StringUtils.ToString(selectedIndex));
			#endif
			
			hasVisualPreview = activePreviews.Count > selectedIndex && HasVisualPreview(activePreviews[selectedIndex]);
		}

		private bool HasVisualPreview(IPreviewableWrapper previewable)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(selectedIndex >= 0, StringUtils.ToString(selectedIndex));
			#endif
			
			if(activePreviews.Count <= selectedIndex)
			{
				return false;
			}

			#if UNITY_EDITOR
			var type = previewable.Targets[0].GetType();
			return type == typeof(TextureImporter) || type == typeof(VideoClipImporter) || type == typeof(AudioImporter) || type == typeof(ModelImporter) || type == typeof(GameObject) || type == typeof(AnimationClip);
			#else
			return false;
			#endif
		}

		private void UpdateMinimizedAndSelectedForActivePreviews()
		{
			switch(UserSettings.ShowPreviewArea)
			{
				case ShowPreviewArea.Minimized:
					SetMinimized(true);
					break;
				case ShowPreviewArea.Dynamic:
					for(int n = activePreviews.Count - 1; n >= 0; n--)
					{
						if(HasVisualPreview(activePreviews[n]))
						{
							SetSelectedIndex(n);
							SetMinimized(false);
							return;
						}

						#if DEV_MODE && DEBUG_MINIMIZED
						Debug.Log("PreviewDrawer - activePreviews["+n+"] "+activePreviews[n].Targets[0].GetType().Name+": not important enough for expanding in Dynamic mode...");
						#endif
					}
					#if DEV_MODE && DEBUG_MINIMIZED
					Debug.Log("PreviewDrawer - activePreviews contained nothing  important enough for expanding in Dynamic mode:\n"+StringUtils.TypesToString(activePreviews, "\n"));
					#endif
					hasVisualPreview = false;
					SetMinimized(true);
					return;
			}
		}
		
		private void UpdateCurrentHeight()
		{
			SetCurrentHeight(GetCurrentHeightUpdated());
		}

		private float GetCurrentHeightUpdated()
		{
			bool heightWasForCurrentTargets = heightIsForCurrentTargets;
			heightIsForCurrentTargets = true;

			if(Hidden)
			{
				return 0f;
			}

			if(minimized)
			{
				return MinHeight;
			}
			
			// free resizing is only allowed when there the view has previews
			if(hasPreviews)
			{
				if(!heightWasForCurrentTargets || currentHeight <= GetMinOpenHeight())
				{
					if(resizing)
					{
						return MinHeight;
					}
					
					UpdatePreferredInitialHeight();
					if(preferredInitialHeight >= heightWasBeforeMinimized)
					{
						return preferredInitialHeight;
					}
					return heightWasBeforeMinimized;
				}
				return currentHeight;
			}
			
			#if UNITY_EDITOR
			if(showAssetBundleEditor)
			{
				return showAssetLabelEditor ? AssetLabelAndAssetBundleEditorFullHeight : AssetBundleEditorFullHeight;
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(showAssetLabelEditor, "UpdateCurrentHeight called with invalid state!");
			#endif

			return AssetLabelEditorFullHeight;
		}

		private float GetMinOpenHeight()
		{
			if(hasPreviews)
			{
				float result = HeaderHeight;
				if(hasPreviewSettingsToolbar)
				{
					result += PreviewSettingsToolbarHeight;
				}
				#if UNITY_EDITOR
				if(showAssetLabelEditor)
				{
					result += AssetLabelEditorBodyHeight;
				}
				if(showAssetBundleEditor)
				{
					result += AssetBundleEditorBodyHeight;
				}
				#endif
				return result;
			}

			#if UNITY_EDITOR
			if(showAssetBundleEditor)
			{
				if(showAssetLabelEditor)
				{
					return AssetLabelAndAssetBundleEditorFullHeight;
				}
				return AssetBundleEditorFullHeight;
			}
			if(showAssetLabelEditor)
			{
				return AssetLabelEditorFullHeight;
			}
			#endif
			return MinHeight;
		}

		private void UpdatePreferredInitialHeight()
		{
			if(hasVisualPreview)
			{
				preferredInitialHeight = InitialHeightVisualPreviews;
				
				#if DEV_MODE && DEBUG_RESIZING
				Debug.Log(StringUtils.ToColorizedString("preferredInitialHeight = ", preferredInitialHeight+" (from inspector width)"));
				#endif
			}
			else if(hasPreviews)
			{
				preferredInitialHeight = InitialHeightNonVisualPreviews;

				if(hasPreviewSettingsToolbar)
				{
					preferredInitialHeight += PreviewSettingsToolbarHeight;
				}

				#if UNITY_EDITOR
				if(showAssetLabelEditor)
				{
					preferredInitialHeight += AssetLabelEditorBodyHeight;
				}
				if(showAssetBundleEditor)
				{
					preferredInitialHeight += AssetBundleEditorBodyHeight;
				}
				#endif

				#if DEV_MODE && DEBUG_RESIZING
				Debug.Log(StringUtils.ToColorizedString("preferredInitialHeight = ", preferredInitialHeight+" (hasPreviews=", true, ", showAssetLabelEditor=", showAssetLabelEditor, ", showAssetBundleEditor=", showAssetBundleEditor, ")"));
				#endif
			}
			#if UNITY_EDITOR
			else if(showAssetBundleEditor)
			{
				if(showAssetLabelEditor)
				{
					preferredInitialHeight = AssetLabelAndAssetBundleEditorFullHeight;
					#if DEV_MODE && DEBUG_RESIZING
					Debug.Log(StringUtils.ToColorizedString("preferredInitialHeight = ", preferredInitialHeight+" (hasPreviews=", false, ", showAssetLabelEditor=", true, ", showAssetBundleEditor=", true, ")"));
					#endif
				}
				else
				{
					preferredInitialHeight = AssetBundleEditorFullHeight;
					#if DEV_MODE && DEBUG_RESIZING
					Debug.Log(StringUtils.ToColorizedString("preferredInitialHeight = ", preferredInitialHeight+" (hasPreviews=", false, ", showAssetLabelEditor=", false, ", showAssetBundleEditor=", true, ")"));
					#endif
				}
			}
			else if(showAssetLabelEditor)
			{
				preferredInitialHeight = AssetLabelEditorFullHeight;
				#if DEV_MODE && DEBUG_RESIZING
				Debug.Log(StringUtils.ToColorizedString("preferredInitialHeight = ", preferredInitialHeight+" (hasPreviews=", false, ", showAssetLabelEditor=", true, ", showAssetBundleEditor=", false, ")"));
				#endif
			}
			#endif
			else
			{
				preferredInitialHeight = MinHeight;
				#if DEV_MODE && DEBUG_RESIZING
				Debug.Log(StringUtils.ToColorizedString("preferredInitialHeight = ", preferredInitialHeight+" (hasPreviews=", false, ", showAssetLabelEditor=", false, ", showAssetBundleEditor=", false, ")"));
				#endif
			}
		}
		
		private void OnInspectorHeightChanged(float inspectorHeight)
		{
			if(minimized || Hidden)
			{
				return;
			}

			if(currentHeight == 0f)
			{
				UpdateCurrentHeight();
			}

			SetCurrentHeight(currentHeight);
		}

		private void SetCurrentHeight(float setHeight)
		{
			if(setHeight <= 0f)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(Hidden, "SetCurrentHeight("+setHeight+") called but Hidden was false.");
				#endif
				setHeight = 0f;
			}
			else if(setHeight <= MinHeight)
			{
				setHeight = MinHeight;
				if(!resizing)
				{
					SetMinimized(true);
				}
			}
			else
			{
				float maxHeight = inspector.State.WindowRect.height - MinInspectorHeightWithoutPreviewArea;
				if(setHeight > maxHeight)
				{
					setHeight = maxHeight;
				}

				if(hasPreviews && !resizing)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!minimized);
					Debug.Assert(!Hidden);
					#endif

					#if DEV_MODE
					if(!heightWasBeforeMinimized.Equals(setHeight)) { Debug.Log("heightWasBeforeMinimized = "+setHeight); }
					#endif

					heightWasBeforeMinimized = setHeight;
				}
			}
			
			if(!currentHeight.Equals(setHeight))
			{
				currentHeight = setHeight;
			}
		}
		
		public void Draw(Rect position)
		{
			if(Hidden)
			{
				return;
			}

			#if DEV_MODE && UNITY_EDITOR
			HandleDevOnlyContextMenu(position);
			#endif

			#if UNITY_EDITOR
			if(!hasPreviews)
			{
				DrawWithoutPreviews(position);
				return;
			}
			#endif
			
			int count = activePreviews.Count;

			if(selectedIndex < 0 || selectedIndex >= count)
			{
				Debug.LogError("selectedIndex ("+ selectedIndex + ") was less than zero or more or equal to count ("+count+")");
				return;
			}

			inspector.NowDrawingPart = InspectorPart.PreviewArea;

			var activePreview = activePreviews[selectedIndex];
			
			int visibleCount = count;
			
			var e = Event.current;
			var eventType = e.rawType;
			bool isRepaint = eventType == EventType.Repaint;
			var mousePosition = e.mousePosition;
			
			var fullPosition = position;
			var assetBundleAndOrLabelEditorRect = position;
			assetBundleAndOrLabelEditorRect.y += position.height;
			assetBundleAndOrLabelEditorRect.height = 0f;

			#if UNITY_EDITOR
			if(showAssetBundleEditor)
			{
				position.height -= AssetBundleEditorBodyHeight;
				assetBundleAndOrLabelEditorRect.y -= AssetBundleEditorBodyHeight;
				assetBundleAndOrLabelEditorRect.height = AssetBundleEditorBodyHeight;
			}
			if(showAssetLabelEditor)
			{
				position.height -= AssetLabelEditorBodyHeight;
				assetBundleAndOrLabelEditorRect.y -= AssetLabelEditorBodyHeight;
				assetBundleAndOrLabelEditorRect.height += AssetLabelEditorBodyHeight;
			}
			#endif
			
			// only draw asset label / asset bundle editors if there's enough space to draw them fully
			if(position.height <= HeaderHeight + HeaderHeight)
			{
				position = fullPosition;
				assetBundleAndOrLabelEditorRect.height = 0f;
			}
			
			GUILayout.BeginArea(position);
			{
				float maxWidth = (float)Screen.width / visibleCount;
				
				position.y = 0f;
				position.height = 18f;
				
				DrawGUI.Active.ColorRect(position, Color.black);

				var headerStyle = visibleCount >= 3 ? InspectorPreferences.Styles.PreToolbarCompact : InspectorPreferences.Styles.PreToolbar;
				
				GUILayout.BeginVertical();
				{
					GUILayout.BeginHorizontal(GUILayout.Height(18f));
					{
						for(int n = 0; n < count; n++)
						{
							var preview = activePreviews[n];

							bool isSelected = selectedIndex == n;

							GUI.contentColor = isSelected ? Color.white : Color.black;

							string text;
							try
							{ 
								text = preview.GetPreviewTitle().text;
							}
							catch(MissingReferenceException exception)
							{
								Debug.LogError(exception);
								ResetState();
								GUIUtility.ExitGUI();
								return;
							}

							// GUILayout.Button seems to consume mouse down events, making it hard to detect when a mouse button
							// was pressed down over its draw rect, which is why RepeatButton is used here instead.
							bool mouseDownOverHeader = GUILayout.RepeatButton(text, headerStyle, GUILayout.Width(maxWidth));

							var headerRect = GUILayoutUtility.GetLastRect();
							bool mouseovered = headerRect.Contains(e.mousePosition);
							if(mouseovered && inspector.Manager.MouseDownInfo.IsClick)
							{
								#if DEV_MODE
								if(!mousePressedDownOverAHeader) { Debug.LogError(StringUtils.ToColorizedString(ToString()+" Event=", e, ", eventType=", eventType, ", e.type=", e.type, ", e.rawType=", e.rawType,", e.button=", e.button, ", hotControl=", GUIUtility.hotControl)); }
								#endif
								mouseDownOverHeader = true;
							}
							
							bool drawDragBar;
							if(isSelected)
							{
								if(mousePressedDownOverSelectedHeader || resizing)
								{
									drawDragBar = true;
								}
								else if(mousePressedDownOverAHeader)
								{
									drawDragBar = false;
								}
								else if(mouseovered)
								{
									drawDragBar = cursorLeftHeaderBoundsAfterSelected;
								}
								else
								{
									drawDragBar = false;

									if(eventType == EventType.MouseMove)
									{
										cursorLeftHeaderBoundsAfterSelected = true;
									}
								}
							}
							else
							{
								drawDragBar = false;
							}

							if(drawDragBar)
							{
								var rectBelowCursor = headerRect;
								rectBelowCursor.width = 10f;
								var mouseX = Event.current.mousePosition.x - 5f;
								if(mouseX > headerRect.x)
								{
									if(mouseX < headerRect.xMax - 5f)
									{
										rectBelowCursor.x = mouseX;
									}
									else
									{
										rectBelowCursor.x = headerRect.xMax - 5f;
									}
								}
								GUI.Label(rectBelowCursor, GUIContent.none, InspectorPreferences.Styles.PreToolbar);
								rectBelowCursor.y += 5f;
								GUI.Label(rectBelowCursor, GUIContent.none, InspectorPreferences.Styles.DragHandle);

								// make the drag bar follow the cursor smoothly
								// optimization: could also only call this when the cursor moves
								GUI.changed = true;
							}
							
							if(mouseDownOverHeader)
							{
								if(e.isMouse)
								{
									DrawGUI.Use(e);
								}
								if(!mousePressedDownOverAHeader)
								{
									cursorLeftHeaderBoundsAfterSelected = isSelected;
									mousePressedDownOverAHeader = true;
									mousePressedDownOverSelectedHeader = isSelected;
									cursorMovedAfterMouseDown = false;

									SetSelectedIndex(n);
									activePreview = activePreviews[n];
								}
							}
						
							GUILayout.Space(1f);
						}
					}
					GUILayout.EndHorizontal();

					if(hasPreviewSettingsToolbar)
					{
						GUILayout.BeginHorizontal(InspectorPreferences.Styles.PreToolbar, GUILayout.MaxHeight(PreviewSettingsToolbarHeight));
						{
							GUILayout.FlexibleSpace();

							activePreview.OnPreviewSettings();

							if(!hasPreviewSettingsToolbarDetermined && eventType == EventType.Repaint)
							{
								var lastRect = GUILayoutUtility.GetLastRect();
								hasPreviewSettingsToolbar = lastRect.height.Equals(PreviewSettingsToolbarHeight);
								hasPreviewSettingsToolbarDetermined = true;
							
								#if DEV_MODE && DEBUG_TOOLBAR
								Debug.Log(StringUtils.ToColorizedString("hasPreviewSettingsToolbar="+hasPreviewSettingsToolbar, ", GetLastRect=", lastRect, ", activePreview=", activePreview.ToString(), ", Height=", GetCurrentHeightUpdated(), ", MinOpenHeight=", GetMinOpenHeight()));
								#endif

								UpdateCurrentHeight();
							}
						}
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndVertical();
				
				var previewRect = GUILayoutUtility.GetRect(0f, 10240f, 64f, 10240f);
				
				if(mousePressedDownOverAHeader && resizable)
				{
					var mouseDownInfo = inspector.Manager.MouseDownInfo;
					if(!mouseDownInfo.MouseButtonIsDown)
					{
						mousePressedDownOverAHeader = false;
						#if DEV_MODE && DEBUG_RESIZING
						Debug.Log("resizing = "+StringUtils.False);
						#endif
						resizing = false;
						if(mousePressedDownOverSelectedHeader)
						{
							if(!cursorMovedAfterMouseDown)
							{
								SetMinimized(!minimized);
							}
							else if(currentHeight <= MinHeight)
							{
								SetMinimized(true);
							}
							else
							{
								heightWasBeforeMinimized = currentHeight;
							}
							mousePressedDownOverSelectedHeader = false;
						}
						cursorMovedAfterMouseDown = false;
					}
					else if(inspector.Manager.MouseDownInfo.CursorMovedAfterMouseDown)
					{
						cursorMovedAfterMouseDown = true;
						#if DEV_MODE && DEBUG_RESIZING_DETAILED
						Debug.Log("resizing = "+StringUtils.True);
						#endif
						resizing = true;
						SetMinimized(false);
					}
				}
				else
				{
					#if DEV_MODE && DEBUG_RESIZING
					if(resizing) { Debug.Log("resizing = "+StringUtils.False); }
					#endif
					resizing = false;
				}

				switch(eventType)
				{
					case EventType.Repaint:
						InspectorPreferences.Styles.PreBackground.Draw(previewRect, false, false, false, false);
						break;
					case EventType.MouseUp:
						mousePressedDownOverAHeader = false;
						mousePressedDownOverSelectedHeader = false;
						cursorMovedAfterMouseDown = false;
						break;
				}

				var colWas = GUI.skin.label.normal.textColor;
				GUI.skin.label.normal.textColor = Color.white;
				activePreview.DrawPreview(previewRect);
				GUI.skin.label.normal.textColor = colWas;
			}
			GUILayout.EndArea();

			if(resizing)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(resizable);
				#endif

				#if DEV_MODE && DEBUG_RESIZING_DETAILED
				{
					float inspectorHeight = inspector.State.WindowRect.height;
					float setHeight = inspectorHeight - mousePosition.y;
					Debug.Log(StringUtils.ToColorizedString("resizing - setHeight="+ setHeight+" e =", e, ", heightWasBeforeMinimized=", heightWasBeforeMinimized, ", mousePos.y=", mousePosition.y, ", CanRequestLocalPosition=", Cursor.CanRequestLocalPosition, ", inspectorHeight=", inspectorHeight));
				}
				#endif

				if(isRepaint && Cursor.CanRequestLocalPosition)
				{
					DrawGUI.Active.SetCursor(MouseCursor.ResizeVertical);

					float inspectorHeight = inspector.State.WindowRect.height;
					float maxHeight = inspectorHeight - MinInspectorHeightWithoutPreviewArea;
					float setHeight = inspectorHeight - mousePosition.y;
					if(!currentHeight.Equals(setHeight) && setHeight >= MinHeight && setHeight <= maxHeight)
					{
						SetCurrentHeight(setHeight);
						GUI.changed = true;
					}
				}
				else if(eventType == EventType.MouseDrag)
				{
					GUI.changed = true;
				}
			}

			#if UNITY_EDITOR
			if(assetBundleAndOrLabelEditorRect.height > 0f)
			{
				DrawGUI.Active.ColorRect(assetBundleAndOrLabelEditorRect, inspector.Preferences.theme.AssetHeaderBackground);
				
				if(showAssetBundleEditor)
				{
					if(showAssetLabelEditor)
					{
						GUIContent[] assetLabels;
						GUIContent[] assetLabelsOnlyOnSomeTargets;
						GetAssetLabels(out assetLabels, out assetLabelsOnlyOnSomeTargets);
						DrawAssetLabelAndBundleGUIBody(assetBundleAndOrLabelEditorRect, assetLabels, assetLabelsOnlyOnSomeTargets);
					}
					else
					{
						DrawAssetBundleGUIBody(assetBundleAndOrLabelEditorRect);
					}
				}
				else if(showAssetLabelEditor)
				{
					GUIContent[] assetLabels;
					GUIContent[] assetLabelsOnlyOnSomeTargets;
					GetAssetLabels(out assetLabels, out assetLabelsOnlyOnSomeTargets);
					DrawAssetLabelGUIBody(assetBundleAndOrLabelEditorRect, assetLabels, assetLabelsOnlyOnSomeTargets);
				}
			}
			#endif
			
			InspectorUtility.ActiveInspector.NowDrawingPart = InspectorPart.Other;
		}

		#if DEV_MODE && UNITY_EDITOR
		private void HandleDevOnlyContextMenu(Rect position)
		{
			var e = Event.current;
			var eventType = e.rawType;
			if(position.Contains(e.mousePosition) && (eventType == EventType.ContextClick || (eventType == EventType.MouseDown && e.button == 1)))
			{
				OpenDevOnlyContextMenu();	
			}
		}

		private void OpenDevOnlyContextMenu()
		{
			var devMenu = Menu.Create();
			devMenu.Add("List Previews", ()=>
			{
				Debug.Log("Active Previews:\n"+StringUtils.ToString(activePreviews, "\n")+"\n\nAll Previews:\n"+StringUtils.ToString(allPreviews));
			});

			devMenu.Add("Edit PreviewDrawer.cs", ()=>
			{
				var script = FileUtility.FindScriptFile(GetType());
				if(script != null)
				{
					AssetDatabase.OpenAsset(script);
				}
				else
				{
					Debug.LogError("FileUtility.FindScriptFilepath could not find file "+GetType().Name+".cs");
				}
			});
			devMenu.Open();
		}
		#endif

		#if UNITY_EDITOR
		private void DrawWithoutPreviews(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(showAssetBundleEditor || showAssetLabelEditor);
			Debug.Assert(!hasPreviews);
			Debug.Assert(!Hidden);
			Debug.Assert(position.height == Height);
			#endif
			
			if(showAssetBundleEditor)
			{
				if(showAssetLabelEditor)
				{
					DrawAssetLabelAndBundleGUI(position);
					return;
				}

				DrawAssetBundleGUI(position);
				return;
			}

			if(showAssetLabelEditor)
			{
				DrawAssetLabelGUI(position);
			}
		}
		
		private void DrawAssetLabelAndBundleGUI(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(showAssetBundleEditor);
			Debug.Assert(showAssetLabelEditor);
			Debug.Assert(!hasPreviews);
			Debug.Assert(position.height == Height);
			Debug.Assert(position.height > 0f);

			if(!minimized)
			{
				if(hasPreviewSettingsToolbar)
				{
					Debug.Assert(position.height == AssetLabelAndAssetBundleEditorFullHeight + PreviewSettingsToolbarHeight, "position.height "+ position.height + " != "+ AssetLabelAndAssetBundleEditorFullHeight);
				}
				else
				{
					Debug.Assert(position.height == AssetLabelAndAssetBundleEditorFullHeight, "position.height " + position.height + " != " + AssetLabelAndAssetBundleEditorFullHeight);
				}
			}
			#endif

			GUIContent[] assetLabels;
			GUIContent[] assetLabelsOnlyOnSomeTargets;
			GetAssetLabels(out assetLabels, out assetLabelsOnlyOnSomeTargets);
			int assetLabelCount = assetLabels == null ? 0 : assetLabels.Length + assetLabelsOnlyOnSomeTargets.Length;
			var bodyRect = DrawHeader(position, AssetBundleAndLabelsEditorTitle, assetLabelCount);

			if(minimized)
			{
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(bodyRect.height > 0f, "bodyRect.height was 0f: "+ bodyRect);
			#endif

			DrawBodyBackground(bodyRect);
			DrawAssetLabelAndBundleGUIBody(bodyRect, assetLabels, assetLabelsOnlyOnSomeTargets);
		}

		private void DrawBodyBackground(Rect position)
		{
			GUI.Label(position, "", InspectorPreferences.Styles.PreBackground);
		}

		private void DrawAssetLabelAndBundleGUIBody(Rect bodyRect, GUIContent[] assetLabels, GUIContent[] assetLabelsOnlyOnSomeTargets)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(showAssetBundleEditor);
			Debug.Assert(showAssetLabelEditor);
			Debug.Assert(!minimized);
			Debug.Assert(!Hidden);
			Debug.Assert(bodyRect.height == AssetLabelAndAssetBundleEditorBodyHeight, "bodyRect.height " + bodyRect.height + " != " + AssetLabelAndAssetBundleEditorBodyHeight);
			#endif
			
			#if UNITY_2019_3_OR_NEWER
			DrawGUI.Active.ColorRect(bodyRect, inspector.Preferences.theme.AssetHeaderBackground);
			#endif

			var assetLabelGUIRect = bodyRect;
			assetLabelGUIRect.height = AssetLabelEditorBodyHeight;
			DrawAssetLabelGUIBody(assetLabelGUIRect, assetLabels, assetLabelsOnlyOnSomeTargets);

			var assetBundleGUIRect = bodyRect;
			assetBundleGUIRect.y += assetLabelGUIRect.height;
			assetBundleGUIRect.height -= assetLabelGUIRect.height;

			DrawAssetBundleGUIBody(assetBundleGUIRect);
		}
		
		private void DrawAssetBundleGUI(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(showAssetBundleEditor);
			Debug.Assert(!showAssetLabelEditor);
			Debug.Assert(!hasPreviews);
			Debug.Assert(position.height == Height);

			if(hasPreviewSettingsToolbar)
			{
				Debug.Assert(position.height == AssetBundleEditorFullHeight + PreviewSettingsToolbarHeight, "position.height " + position.height + " != " + AssetBundleEditorFullHeight);
			}
			else
			{
				Debug.Assert(position.height == AssetBundleEditorFullHeight, "position.height " + position.height + " != " + AssetBundleEditorFullHeight);
			}
			#endif

			var bodyRect = DrawBasicHeader(position, AssetBundleEditorTitle);
			if(!minimized)
			{
				DrawBodyBackground(bodyRect);
				DrawAssetBundleGUIBody(bodyRect);
			}
		}

		private void DrawAssetBundleGUIBody(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(showAssetBundleEditor);
			Debug.Assert(position.height == AssetBundleEditorBodyHeight);
			#endif

			position.x += AssetBundleGUIBodyXPadding;
			position.width -= AssetBundleGUIBodyXPadding + AssetBundleGUIBodyXPadding;
			position.height -= AssetBundleGUIBodyYPadding;

			GUILayout.BeginArea(position);
			{
				assetBundleGUIDrawer.Draw();
			}
			GUILayout.EndArea();
		}
		
		private void DrawAssetLabelGUI(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!showAssetBundleEditor);
			Debug.Assert(showAssetLabelEditor);
			Debug.Assert(!hasPreviews);
			Debug.Assert(position.height == Height);

			if(hasPreviewSettingsToolbar)
			{
				Debug.Assert(position.height == AssetLabelEditorFullHeight + PreviewSettingsToolbarHeight, "position.height " + position.height + " != " + AssetLabelEditorFullHeight);
			}
			else
			{
				Debug.Assert(position.height == AssetLabelEditorFullHeight, "position.height " + position.height + " != " + AssetLabelEditorFullHeight);
			}
			#endif

			GUIContent[] assetLabels;
			GUIContent[] assetLabelsOnlyOnSomeTargets;
			GetAssetLabels(out assetLabels, out assetLabelsOnlyOnSomeTargets);
			int assetLabelCount = assetLabels == null ? 0 : assetLabels.Length + assetLabelsOnlyOnSomeTargets.Length;
			var bodyRect = DrawHeader(position, AssetLabelsEditorTitle, assetLabelCount);
			if(!minimized)
			{
				DrawBodyBackground(bodyRect);
				DrawAssetLabelGUIBody(bodyRect, assetLabels, assetLabelsOnlyOnSomeTargets);
			}
		}

		private void DrawAssetLabelGUIBody(Rect position, GUIContent[] assetLabels, GUIContent[] assetLabelsOnlyOnSomeTargets)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(showAssetLabelEditor);
			Debug.Assert(position.height == AssetLabelEditorBodyHeight);
			#endif

			position.x += AssetLabelButtonXOffset;
			position.y += AssetLabelButtonYOffset;			
			position.height = AssetLabelButtonHeight;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position.yMax <= position.y + AssetLabelEditorBodyHeight);
			#endif
			
			var assetLabelStyle = GUI.skin.GetStyle("AssetLabel");
			
			if(assetLabels != null)
			{
				for(int n = 0, labelCount = assetLabels.Length; n < labelCount; n++)
				{
					var assetLabel = assetLabels[n];
					var size = assetLabelStyle.CalcSize(assetLabel);
					position.width = size.x;

					if(position.x + position.width > Screen.width - 26f)
					{
						break;
					}

					if(GUI.Button(position, assetLabel, InspectorPreferences.Styles.AssetLabel))
					{
						var menu = Menu.Create();
						menu.Add("Remove Label", ()=>
						{
							var targets = inspector.State.drawers[0].UnityObjects;
							AssetLabels.Remove(targets, assetLabel.text);
						});
						ContextMenuUtility.Open(menu, true, inspector, InspectorPart.PreviewArea, null, Part.AssetLabelsButton);
					}

					position.x += size.x + 2f;
				}
			}

			if(assetLabelsOnlyOnSomeTargets != null)
			{
				//draw asset labels found only on some targets in greyed out color
				var guiColor = GUI.color;
				guiColor.a = 0.5f;
				GUI.color = guiColor;

				for(int n = 0, labelCount = assetLabelsOnlyOnSomeTargets.Length; n < labelCount; n++)
				{
					var assetLabel = assetLabelsOnlyOnSomeTargets[n];
					var size = assetLabelStyle.CalcSize(assetLabel);
					position.width = size.x;

					if(position.x + position.width > Screen.width - 26f)
					{
						break;
					}

					if(GUI.Button(position, assetLabel, InspectorPreferences.Styles.AssetLabel))
					{
						var menu = Menu.Create();
						menu.Add("Add For All", ()=>
						{
							var targets = inspector.State.drawers[0].UnityObjects;
							AssetLabels.Add(targets, assetLabel.text);
						});
						menu.Add("Remove Label", ()=>
						{
							var targets = inspector.State.drawers[0].UnityObjects;
							AssetLabels.Remove(targets, assetLabel.text);
						});
						ContextMenuUtility.Open(menu, true, inspector, InspectorPart.PreviewArea, null, Part.AssetLabel);
					}

					position.x += size.x + 2f;
				}

				guiColor.a = 1f;
				GUI.color = guiColor;
			}
			
			// TO DO: draw labels found only on some of multi-selected targets
			// allow adding or removing label via menu that opens on click

			position.width = 16f;
			position.x = Screen.width - 26f;
			
			if(DrawGUI.Active.MouseDownButton(position, GUIContent.none, "AssetLabel Icon"))
			{
				#if UNITY_EDITOR
				OpenAssetLabelsContextMenu(position, assetLabels);
				#endif
			}
		}

		private void GetAssetLabels(out GUIContent[] assetLabels, out GUIContent[] assetLabelsOnlyOnSomeTargets)
		{
			if(!showAssetLabelEditor)
			{
				assetLabels = null;
				assetLabelsOnlyOnSomeTargets = null;
				return;
			}

			var asset = inspector.State.drawers[0] as IAssetDrawer;
			if(asset != null)
			{
				assetLabels = asset.AssetLabels;
				assetLabelsOnlyOnSomeTargets = asset.AssetLabelsOnlyOnSomeTargets;
			}
			else 
			{
				#if DEV_MODE
				var state = inspector.State;
				Debug.LogWarning("drawers[0] was not IAssetDrawer. state.assetMode="+state.assetMode+", inspected="+(state.drawers == null ? "ArrayNull" : state.drawers.Length == 0 ? "Empty" : state.drawers[0] == null ? "ContentNull" : state.drawers[0].Name));
				#endif

				assetLabels = null;
				assetLabelsOnlyOnSomeTargets = null;
			}
		}

		/// <summary>
		/// Draws header with asset label count display, for use with asset label editor
		/// or asset label + asset bundle editor combo.
		/// </summary>
		/// <param name="position"> The position at which to draw the header. </param>
		/// <param name="label"> Label for header </param>
		/// <param name="assetLabelCount"> Number of asset labels on targets. </param>
		/// <returns> Remaining Rect for the body. </returns>
		private Rect DrawHeader(Rect position, GUIContent label, int assetLabelCount)
		{
			var bottomBarRect = DrawBasicHeader(position, label);

			if(assetLabelCount > 0)
			{
				#if DEV_MODE
				// for testing purposes, to see how label display works with large numbers
				if(Event.current.control){ assetLabelCount = 99; }
				#endif

				var countRect = position;
				countRect.width = assetLabelCount > 9 ? 27f : 19f;
				countRect.x = Screen.width - countRect.width - 6f;
				countRect.height = AssetLabelButtonHeight;
				const float yOffset = (HeaderHeight - AssetLabelButtonHeight) / 2;
				countRect.y += yOffset;


				GUI.Label(countRect, StringUtils.ToString(assetLabelCount), InspectorPreferences.Styles.AssetLabel);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(minimized || bottomBarRect.height > 0f, "bottomBarRect.height was 0f: " + bottomBarRect);
			#endif

			return bottomBarRect;
		}

		/// <summary>
		/// Draws basic header for use with asset label or asset bundle editor,
		/// without anything fancy like asset label count display.
		/// </summary>
		/// <param name="position"> The position at which to draw the header. </param>
		/// <param name="label"> Label for header </param>
		/// <returns> Rect for the bottom bar. </returns>
		private Rect DrawBasicHeader(Rect position, GUIContent label)
		{
			var headerRect = position;
			headerRect.height = HeaderHeight;

			GUI.DrawTexture(headerRect, InspectorUtility.Preferences.graphics.objectHeaderBg);

			var bottomBarRect = position;
			bottomBarRect.height = position.height - HeaderHeight;
			bottomBarRect.y += HeaderHeight;

			if(DrawGUI.Active.Button(headerRect, label, InspectorPreferences.Styles.PreToolbar) && resizable)
			{
				SetMinimized(!minimized);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(minimized || position.height > 0f);
			#endif

			return bottomBarRect;
		}
		
		#if UNITY_EDITOR
		private void OpenAssetLabelsContextMenu(Rect openPosition, GUIContent[] assetLabels)
		{
			//Could also scan through all assets in project to generate the list, if don't want to use reflection
			var labelsDictionary = typeof(AssetDatabase).InvokeMember("GetAllLabels", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod, null, null, null) as Dictionary<string, float>;

			if(assetLabelsMenuBuiltFromLabels != labelsDictionary)
			{
				assetLabelsMenuBuiltFromLabels = labelsDictionary;

				assetLabelMenuItems.Clear();
				assetLabelMenuGroupsByLabel.Clear();
				assetLabelMenuItemsByLabel.Clear();

				int lastLabelIndex = assetLabels == null ? -1 : assetLabels.Length - 1;

				foreach(var assetLabel in labelsDictionary)
				{
					var menuItemAssetLabel = assetLabel.Key;
					var item = PopupMenuUtility.BuildPopupMenuItemWithLabel(ref assetLabelMenuItems, ref assetLabelMenuGroupsByLabel, ref assetLabelMenuItemsByLabel, menuItemAssetLabel, null, menuItemAssetLabel, "", null);

					for(int n = lastLabelIndex; n >= 0; n--)
					{
						if(string.Equals(assetLabels[n].text, menuItemAssetLabel, StringComparison.Ordinal))
						{
							tickedMenuItems.Add(item);
							break;
						}
					}
				}
			}

			#if DEV_MODE
			Debug.Log("Openings asset label menu with "+assetLabelMenuItems.Count+" items");
			#endif

			PopupMenuManager.Open(inspector, assetLabelMenuItems, assetLabelMenuGroupsByLabel, assetLabelMenuItemsByLabel, tickedMenuItems, true, openPosition, OnPopupMenuItemClicked, OnPopupMenuClosed, new GUIContent("Asset Labels"), null);
		}
		#endif

		private void OnPopupMenuItemClicked(PopupMenuItem clickedItem)
		{
			var asset = (IAssetDrawer)inspector.State.drawers[0];
			var assetLabels = asset.AssetLabels;

			string menuItemAssetLabel = clickedItem.label;
			bool hasLabel = false;
			for(int l = assetLabels.Length - 1; l >= 0; l--)
			{
				if(string.Equals(assetLabels[l].text, menuItemAssetLabel, StringComparison.Ordinal))
				{
					hasLabel = true;
					break;
				}
			}

			if(hasLabel)
			{
				AssetLabels.Remove(asset.UnityObjects, menuItemAssetLabel);
			}
			else
			{
				AssetLabels.Add(asset.UnityObjects, menuItemAssetLabel);
			}
		}

		private void OnPopupMenuClosed() { }
		#endif

		private void SetMinimized(bool setIsMinimized)
		{
			if(minimized != setIsMinimized)
			{
				#if DEV_MODE && DEBUG_MINIMIZED
				Debug.Log(StringUtils.ToColorizedString("minimized = ", setIsMinimized, ", with resizing=", resizing, ", preferredInitialHeight=", preferredInitialHeight, ", hasPreviews=", hasPreviews, ", showAssetLabelEditor=", showAssetLabelEditor, ", showAssetBundleEditor=", showAssetBundleEditor));
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(resizable || setIsMinimized, "SetMinimized(" + StringUtils.False + ") called but resizable "+StringUtils.False);
				#endif

				minimized = setIsMinimized;
				if(setIsMinimized)
				{
					SetCurrentHeight(MinHeight);
				}
				else if(!resizing)
				{
					UpdatePreferredInitialHeight();
					if(heightWasBeforeMinimized <= preferredInitialHeight)
					{
						heightWasBeforeMinimized = preferredInitialHeight;
					}

					SetCurrentHeight(heightWasBeforeMinimized);
				}

				inspector.RefreshView();
				UpdateCurrentHeight();
			}
		}
		
		/// <summary>
		/// Editor targets can become null when hierarchy or assets changes.
		/// </summary>
		public void OnProjectOrHierarchyChanged()
		{
			RemovePreviewsWithInvalidStates();
		}

		public void ReloadPreviewInstances()
		{
			if(!hasVisualPreview)
			{
				#if DEV_MODE
				if(activePreviews.Count > 0) { Debug.LogWarning("Ignoring ReloadPreviewInstances call because hasVisualPreview="+StringUtils.False+" with allPreviews:\n"+StringUtils.ToString(activePreviews, "\n")+"\n\nallPreviewTargets:\n"+StringUtils.ToString(activePreviews.Select(preview=>preview.Targets[0].GetType()).ToArray(), "\n")); }
				#endif
				return;
			}

			for(int n = allPreviews.Count - 1; n >= 0; n--)
			{
				allPreviews[n].ReloadPreviewInstances();
			}
		}

		public bool RequiresConstantRepaint()
		{
			if(hidden || minimized || !HasPreviews)
			{
				return false;
			}

			if(Platform.Time > requiresConstantRepaintLastUpdated + 0.1f)
			{
				UpdateRequiresContantRepaint();
			}

			return requiresConstantRepaint;
		}

		private void RemovePreviewsWithInvalidStates()
		{
			bool removed = false;

			for(int n = allPreviews.Count - 1; n >= 0; n--)
			{
				var preview = allPreviews[n];
				if(!preview.StateIsValid)
				{
					#if DEV_MODE
					Debug.LogWarning("OnHierarchyChanged - Removing PreviewDrawer with invalid state: "+StringUtils.TypeToString(preview));
					#endif

					try
					{
						#if DEV_MODE && DEBUG_DISPOSE
						Debug.Log("OnHierarchyChanged - Calling Dispose for preview "+StringUtils.TypeToString(activePreviews[n]));
						#endif

						Previews.Dispose(ref preview);
					}
					catch(Exception e)
					{
						Debug.LogWarning(e);
					}
					allPreviews.RemoveAt(n);
					removed = true;
				}
			}

			if(removed)
			{
				UpdateActivePreviews();
			}
		}
	}
}