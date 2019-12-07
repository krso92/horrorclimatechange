#define DEBUG_APPLY_VALUE
//#define DEBUG_DRAG_MOUSEOVER
//#define DEBUG_UNAPPLIED_CHANGES
//#define DEBUG_OBJECT_PICKER
//#define DEBUG_KEYBOARD_INPUT

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Drawer representing UnityEngine.Object references, including Components, GameObject and various assets.
	/// </summary>
	[Serializable, DrawerForField(typeof(Object), true, true)]
	public class ObjectReferenceDrawer : PrefixControlComboDrawer<Object>
	{
		private static List<PopupMenuItem> generatedMenuItems = new List<PopupMenuItem>(30);
		private static Dictionary<string, PopupMenuItem> generatedGroupsByLabel = new Dictionary<string, PopupMenuItem>(10);
		private static Dictionary<string, PopupMenuItem> generatedItemsByLabel = new Dictionary<string, PopupMenuItem>(20);

		private static readonly List<Component> GetComponents = new List<Component>();
		private static readonly List<Object> FindObjects = new List<Object>();

		private Type type;
		private bool allowSceneObjects = true;
		private bool hasUnappliedChanges;
		private Object valueUnapplied;

		private bool objectFieldMouseovered;
		private bool objectPickerButtonMouseovered;

		private bool listeningForObjectPickerClosed;

		private bool usingEyedropper;
		private bool viewWasLockedWhenStartedUsingEyedropper;
		private Object[] selectionWasWhenStartedUsingEyedropper;

		/// <inheritdoc/>
		public override Part MouseoveredPart
		{
			get
			{
				return objectPickerButtonMouseovered ? Part.Picker : base.MouseoveredPart;
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return type;
			}
		}

		/// <summary>
		/// Gets the draw position and dimensions of the drag-n-drop region of the control.
		/// This is the control's bounds, without the object picker icon.
		/// </summary>
		/// <value> The object field position. </value>
		private Rect DragNDropAreaPosition
		{
			get
			{
				var rect = ControlPosition;
				rect.width -= 18f;
				return rect;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="type"> The type constraint for the UnityEngine.Objects that can be dragged to the field. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="allowSceneObjects"> True if can assing scene objects to field, in addition to assets. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ObjectReferenceDrawer Create(Object value, Type type, IParentDrawer parent, GUIContent label, bool allowSceneObjects, bool readOnly)
		{
			ObjectReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ObjectReferenceDrawer();
			}
			result.Setup(value, type, null, parent, label, allowSceneObjects, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. </param>
		/// <param name="type"> The type constraint for the UnityEngine.Objects that can be dragged to the field. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="allowSceneObjects"> True if can assing scene objects to field, in addition to assets. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ObjectReferenceDrawer Create(Object value, [CanBeNull]LinkedMemberInfo memberInfo, Type type, IParentDrawer parent, GUIContent label, bool allowSceneObjects, bool readOnly)
		{
			ObjectReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ObjectReferenceDrawer();
			}
			result.Setup(value, type, memberInfo, parent, label, allowSceneObjects, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo">
		/// LinkedMemberInfo for the field, property or parameter that the created drawer
		/// represent. If null, all UnityEngine.Object types can be assigned to the field.
		/// </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="allowSceneObjects"> True if can assing scene objects to field, in addition to assets. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ObjectReferenceDrawer Create(Object value, [CanBeNull]LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool allowSceneObjects, bool readOnly)
		{
			ObjectReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ObjectReferenceDrawer();
			}
			result.Setup(value, null, memberInfo, parent, label, allowSceneObjects, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Object)setValue, setValueType, setMemberInfo, setParent, setLabel, true, setReadOnly);
		}

		/// <inheritdoc/>
		protected sealed override void Setup(Object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method");
		}

		private void Setup([CanBeNull]Object setValue, [CanBeNull]Type setValueType, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, GUIContent setLabel, bool setAllowSceneObjects, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!hasUnappliedChanges, ToString(setLabel, setMemberInfo)+".Setup - hasUnappliedChanges was "+StringUtils.True);
			#endif

			#if DEV_MODE
			Debug.Log("ObjectReferenceDrawer.Setup called with setLabel="+StringUtils.ToString(setLabel));
			#endif

			if(setValueType == null)
			{
				if(setMemberInfo != null)
				{
					setValueType = setMemberInfo.Type;
					if(!setValueType.IsUnityObject())
					{
						setValueType = Types.UnityObject;
					}
				}
				else
				{
					setValueType = Types.UnityObject;
				}
			}
			else if(!setValueType.IsUnityObject())
			{
				setValueType = Types.UnityObject;
			}
			else if(!setAllowSceneObjects)
			{
				setAllowSceneObjects = setValueType == Types.UnityObject || setValueType.IsGameObject() || setValueType.IsComponent() || setValueType.IsInterface;
			}

			#if DEV_MODE
			Debug.Assert(setValueType != null);
			Debug.Assert(setValueType.IsUnityObject());
			if(setMemberInfo != null)
			{
				Debug.Assert(setMemberInfo.Type != null, ToString(setLabel, setMemberInfo)+" fieldInfo.Type was null "+ setMemberInfo);
				Debug.Assert(setMemberInfo.Type.IsInterface || setMemberInfo.Type.IsAssignableFrom(setValueType), ToString(setLabel, setMemberInfo) + " fieldInfo " + StringUtils.ToString(setMemberInfo.Type) + " not assignable from type parameter " + StringUtils.ToString(setValueType));
			}
			#endif
			
			allowSceneObjects = setAllowSceneObjects;

			type = setValueType;
			
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			valueUnapplied = Value;
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(Object setValue, bool applyToField, bool updateMembers)
		{
			if(setValue != null && !type.IsAssignableFrom(setValue.GetType()))
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".Value.set type "+ StringUtils.ToString(type) + " not assignable from "+ StringUtils.TypeToString(setValue));
				#endif
				return false;
			}

			#if DEV_MODE
			Debug.Assert(setValue == null || memberInfo == null || memberInfo.Type.IsAssignableFrom(setValue.GetType()));
			#endif
			
			valueUnapplied = setValue;
			SetHasUnappliedChanges(false);

			return base.DoSetValue(setValue, applyToField, updateMembers);
		}

		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			//Don't update cached values while object picker is open
			if(ObjectPicker.IsOpen)
			{
				//Debug.Log("ObjectPickerIsOpen...");
				return;
			}

			//Don't update cached values while values picked using object picker
			//haven't been applied yet
			if(hasUnappliedChanges)
			{
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();
			valueUnapplied = Value;
		}
		
		/// <summary>
		/// Just Draw the control with current value and return changes made to the value via the control,
		/// without fancy features like data validation color coding
		/// </summary>
		public override Object DrawControlVisuals(Rect position, Object inputValue)
		{
			#if UNITY_EDITOR
			if(ObjectPicker.IsOpen)
			{
				if(hasUnappliedChanges)
				{
					var color = InspectorUtility.Preferences.theme.ControlUnappliedChangesTint;
					color.a = 1f;
					DrawGUI.DrawMouseoverEffect(DragNDropAreaPosition, color);
				}
			}
			#endif

			if(usingEyedropper)
			{
				DrawGUI.Active.SetCursor(MouseCursor.ArrowPlus);
			}

			if(PopupMenuManager.IsOpen && PopupMenuManager.LastmenuOpenedForDrawer == this)
			{
				var selected = PopupMenu.SelectedItem;
				if(selected != null && !selected.IsGroup && Value != selected.IdentifyingObject as Object)
				{
					var color = InspectorUtility.Preferences.theme.ControlUnappliedChangesTint;
					color.a = 1f;
					DrawGUI.DrawMouseoverEffect(DragNDropAreaPosition, color);
				}
			}

			var setValueUnapplied = DrawGUI.Active.ObjectField(position, valueUnapplied, Type, allowSceneObjects);
			
			if(setValueUnapplied != valueUnapplied)
			{
				#if DEV_MODE && DEBUG_OBJECT_PICKER
				Debug.Log(Msg("valueUnapplied = ", setValueUnapplied," (was: ", valueUnapplied, ") with inputValue=", inputValue, ", Value=", Value));
				#endif
				valueUnapplied = setValueUnapplied;
				SetHasUnappliedChanges(inputValue != valueUnapplied);
			}
			
			#if UNITY_EDITOR
			if(ObjectPicker.IsOpen)
			{
				//don't apply changes while the object picker is open
				//so that we can e.g. revert back to previous value when escape is pressed
				return inputValue;
			}
			#endif

			if(hasUnappliedChanges)
			{
				#if UNITY_EDITOR
				// try to figure out whether should apply the value last selected via the Object Picker
				// or if should discard it and revert to previous value (e.g. because escape was pressed)
				// this is tough, because there can be a number of Event calls (Layout, Repaint) before
				// the Escape, Return or KeypadEnter event gets through here. Two other ways to apply
				// the value is by double clicking an entry in the object picker, or by click off-screen
				// of the object picker to close it.
				var inspector = InspectorUtility.ActiveInspector;
				inspector.RefreshView();
				
				// re-focus the EditorWindow that was used to open the Object Picker - it always loses
				// focus to the Object Picker window when it's opened.
				if(!inspector.InspectorDrawer.HasFocus)
				{
					inspector.InspectorDrawer.FocusWindow();
				}

				switch(Event.current.keyCode)
				{
					//the object picker was closed using the esc key: discard the value
					case KeyCode.Escape:
						DiscardUnappliedChanges();
						return inputValue;
					//the object was picked using enter or return key: apply the value
					case KeyCode.KeypadEnter:
					case KeyCode.Return:
						ApplyUnappliedChanges();
						return inputValue;
				}

				//if no other applicable KeyCodes were detected until the next time the mouse was moved, then it's
				//safe to assume that the user closed the object picker either by double clicking an object in the view
				//or by clicking off-window and thus causing the window to close. In both instances the value should be applied.
				if(Event.current.isMouse)
				{
					ApplyUnappliedChanges();
					return inputValue;
				}

				//handle value being changed via drag n drop
				var lastInput = DrawGUI.LastInputEvent();
				if(lastInput != null && lastInput.type == EventType.DragPerform)
				#endif
				{
					ApplyUnappliedChanges();
				}
			}
			
			return inputValue;
		}

		private void StartUsingEyeDropper()
		{
			var inspector = Inspector;
			inspector.Manager.MouseDownInfo.onMouseUp += OnMouseUpWhileUsingEyedropper;
			inspector.InspectorDrawer.SelectionManager.OnNextSelectionChanged(OnSelectionChangedWhileUsingEyedropper);
			usingEyedropper = true;
			viewWasLockedWhenStartedUsingEyedropper = inspector.State.ViewIsLocked;
			selectionWasWhenStartedUsingEyedropper = inspector.InspectorDrawer.SelectionManager.Selected;
			inspector.State.ViewIsLocked = true;
		}

		private void OnMouseUpWhileUsingEyedropper(IDrawer mouseDownOverDrawer, bool isClick)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnMouseUpWhileUsingEyedropper");
			#endif

			Inspector.Manager.MouseDownInfo.onMouseUp -= OnMouseUpWhileUsingEyedropper;
			usingEyedropper = false;
		}

		private void OnSelectionChangedWhileUsingEyedropper(Object[] selected)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnSelectionChangedWhileUsingEyedropper");
			#endif

			if(!usingEyedropper)
			{
				return;
			}

			AssignFromUserProvidedRootObjects(selected);
			//StopUsingEyeDropper();
		}

		/// <inheritdoc/>
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnDeselectedInternal reason="+ reason+ ", losingFocusTo="+StringUtils.ToString(losingFocusTo));
			#endif

			if(usingEyedropper)
			{
				if(losingFocusTo == null)
				{
					OnNextLayout(()=>Select(ReasonSelectionChanged.OtherClicked));
				}
				else
				{
					AssignFromUserProvidedRootObjects(losingFocusTo.UnityObjects);
					StopUsingEyeDropper();
				}
			}

			base.OnDeselectedInternal(reason, losingFocusTo);
		}

		private void StopUsingEyeDropper()
		{
			if(!usingEyedropper)
			{
				return;
			}

			#if DEV_MODE
			if(Inspector == null) { Debug.Log(ToString() + ".StopUsingEyeDropper with Inspector=null"); }
			else{ Debug.Log(ToString()+ ".StopUsingEyeDropper with inspected="+ StringUtils.ToString(Inspector.State.inspected)+", Selected="+ StringUtils.ToString(Inspector.InspectorDrawer.SelectionManager.Selected) +", SelectedWas="+ StringUtils.ToString(selectionWasWhenStartedUsingEyedropper)); }
			#endif

			usingEyedropper = false;
			var inspector = Inspector;
			if(inspector != null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(inspector.State.ViewIsLocked);
				#endif

				inspector.Manager.MouseDownInfo.onMouseUp -= OnMouseUpWhileUsingEyedropper;
				inspector.InspectorDrawer.SelectionManager.CancelOnNextSelectionChanged(OnSelectionChangedWhileUsingEyedropper);

				inspector.Select(selectionWasWhenStartedUsingEyedropper);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(inspector.InspectorDrawer.SelectionManager.Selected.ContentsMatch(selectionWasWhenStartedUsingEyedropper));
				#endif

				inspector.State.ViewIsLocked = viewWasLockedWhenStartedUsingEyedropper;
			}
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(GetType().Name + ".OnKeyboardInputGiven(" + inputEvent.keyCode + ") with DrawGUI.EditingTextField=" + DrawGUI.EditingTextField);
			#endif

			switch(inputEvent.keyCode)
			{
				case KeyCode.Escape:
					if(usingEyedropper)
					{
						StopUsingEyeDropper();
					}
					else if(hasUnappliedChanges)
					{
						#if DEV_MODE && DEBUG_APPLY_VALUE
						Debug.Log(GetType().Name+" - Discarding unapplied value "+ StringUtils.TypeToString(valueUnapplied) + " because esc was pressed");
						#endif

						DiscardUnappliedChanges();
						GUI.changed = true;
					}
					return true;
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if(hasUnappliedChanges)
					{
						#if DEV_MODE && DEBUG_APPLY_VALUE
						Debug.Log(GetType().Name+" - Applying value "+ StringUtils.TypeToString(valueUnapplied) + " because return or enter was pressed");
						#endif

						ApplyUnappliedChanges();
						GUI.changed = true;
					}
					return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		public override void OnMiddleClick(Event inputEvent)
		{
			if(Value != null)
			{
				DrawGUI.Use(inputEvent);
				Peek();
			}
		}

		/// <inheritdoc/>
		public override void CopyToClipboard()
		{
			if(MixedContent)
			{
				try
				{
					Clipboard.CopyObjectReferences(Values, type);
					SendCopyToClipboardMessage();
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogWarning(e);
				#else
				catch
				{
				#endif
					SendCopyToClipboardMessage();
				}
				return;
			}
			try
			{
				Clipboard.CopyObjectReference(Value, type);
				SendCopyToClipboardMessage();
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				SendCopyToClipboardMessage();
			}
		}

		/// <inheritdoc/>
		public override void CopyToClipboard(int index)
		{
			try
			{
				Clipboard.CopyObjectReference(GetValue(index) as Object, Type);
				SendCopyToClipboardMessage();
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				SendCopyToClipboardMessage();
			}
		}

		/// <inheritdoc/>
		protected override bool CanPasteFromClipboard()
		{
			return Clipboard.HasObjectReference() && Clipboard.CanPasteAs(Type);
		}

		/// <inheritdoc />
		protected override void DoPasteFromClipboard()
		{
			if(Clipboard.HasObjectReference())
			{
				Value = Clipboard.PasteObjectReference(type);
			}
		}

		/// <inheritdoc/>
		protected override string GetPasteFromClipboardMessage()
		{
			return "Pasted reference{0}.";
		}
		
		/// <inheritdoc />
		public override void OnMouseover()
		{
			var objFieldRect = DragNDropAreaPosition;
			if(objFieldRect.Contains(Cursor.LocalPosition))
			{
				DrawGUI.DrawMouseoverEffect(objFieldRect, localDrawAreaOffset);
			}
			else if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel && MouseOverPart == PrefixedControlPart.Prefix)
			{
				DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		public override void DrawFilterHighlight(SearchFilter filter, Color color)
		{
			if(lastPassedFilterTestType == FilterTestType.Value)
			{
				DrawGUI.DrawControlFilteringEffect(DragNDropAreaPosition, color, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			if(!CanAcceptDrag(mouseDownInfo.MouseDownOverDrawer, dragAndDropObjectReferences))
			{
				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Rejected;
				return;
			}

			DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Generic;

			if(Event.current.type == EventType.DragExited || Event.current.type == EventType.DragPerform)
			{
				if(AssignFromUserProvidedRootObjects(dragAndDropObjectReferences))
				{
					return;
				}
			}
			
			var objFieldRect = DragNDropAreaPosition;
			if(objFieldRect.Contains(Cursor.LocalPosition))
			{
				DrawGUI.DrawMouseoverEffect(objFieldRect, localDrawAreaOffset);
			}
			else if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel && MouseOverPart == PrefixedControlPart.Prefix)
			{
				DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
			}
		}

		private bool AssignFromUserProvidedRootObjects(Object[] userProvidedRootObjects)
		{
			var acceptableObjects = AcceptableDragNDropSubjects(userProvidedRootObjects);
			int count = acceptableObjects.Length;
			if(count == 1)
			{
				DrawGUI.Use(Event.current);
				Inspector.Manager.MouseDownInfo.Clear();
				Value =  acceptableObjects[0];
				StopUsingEyeDropper();
				return true;
			}

			if(count > 1)
			{
				DrawGUI.Use(Event.current);
				Inspector.Manager.MouseDownInfo.Clear();

				var menu = new List<PopupMenuItem>(count);
				for(int n = 0; n < count; n++)
				{
					var dropped = acceptableObjects[n];
					var typeOfDropped = dropped.GetType();
					menu.Add(PopupMenuItem.Item(dropped, typeOfDropped, StringUtils.ToStringSansNamespace(typeOfDropped), typeOfDropped.Namespace, null, MenuItemValueType.UnityObject));
				}
				string menuLabel = acceptableObjects.Length == 1 ? acceptableObjects[0].name : "Select "+StringUtils.ToStringSansNamespace(type);
				PopupMenuManager.Open(Inspector, menu, controlLastDrawPosition, OnSelectTargetSubTargetMenuItemClicked, OnSelectTargetSubTargetMenuClosed, menuLabel, this);
				// Don't stop using eye dropper yet, so that the inspector lock remains effective.
				// Stop using in OnSelectTargetSubTargetMenuClosed instead.
				return true;
			}

			StopUsingEyeDropper();
			return false;
		}

		private void OnSelectTargetSubTargetMenuItemClicked(PopupMenuItem item)
		{
			Value = item.IdentifyingObject as Object;
		}

		private void OnSelectTargetSubTargetMenuClosed()
		{
			StopUsingEyeDropper();
		}

		private bool CanAcceptDrag(IDrawer mouseDownOverControl, Object[] dragAndDropObjectReferences)
		{
			if(mouseDownOverControl == this)
			{
				return false;
			}

			if(dragAndDropObjectReferences.Length < 1)
			{
				return false;
			}
			
			var mousePos = Cursor.LocalPosition;
			if(!DragNDropAreaPosition.Contains(mousePos) && !labelLastDrawPosition.Contains(mousePos))
			{
				return false;
			}

			var dragged = dragAndDropObjectReferences[0];
			var draggedGo = dragged as GameObject;
			if(draggedGo != null)
			{
				if(type == Types.GameObject || type == Types.UnityObject)
				{
					return true;
				}

				if(Types.Component.IsAssignableFrom(type))
				{
					return draggedGo.GetComponent(type) != null;
				}
				return false;
			}

			return type.IsInstanceOfType(dragged);
		}

		private Object[] AcceptableDragNDropSubjects(Object[] dragAndDropObjectReferences)
		{
			if(dragAndDropObjectReferences.Length == 0)
			{
				return dragAndDropObjectReferences;
			}
			
			var dragged = dragAndDropObjectReferences[0];
			var draggedGo = dragged as GameObject;
			if(draggedGo != null)
			{
				if(type == Types.GameObject)
				{
					return dragAndDropObjectReferences;
				}
				if(type == Types.UnityObject)
				{
					draggedGo.GetComponents(Types.Component, GetComponents);
					GetComponents.Insert(0, null);
					int count = GetComponents.Count;
					var result = ArrayPool<Object>.Create(count);
					result[0] = draggedGo;
					for(int n = count - 1; n >= 1; n--)
					{
						result[n] = GetComponents[n];
					}
					GetComponents.Clear();
					return result;
				}
				if(Types.Component.IsAssignableFrom(type))
				{
					return draggedGo.GetComponents(type);
				}
				return ArrayPool<Object>.ZeroSizeArray;
			}

			if(type.IsInstanceOfType(dragged))
			{
				return ArrayExtensions.TempUnityObjectArray(dragged);
			}
			return ArrayPool<Object>.ZeroSizeArray;
		}

		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			#if DEV_MODE
			Debug.Log(GetType().Name+ ".OnRightClick with objectFieldMouseovered="+ objectFieldMouseovered);
			#endif

			if(objectFieldMouseovered && !ReadOnly)
			{
				DrawGUI.Use(inputEvent);
				DisplayTargetSelectMenu();
				return true;
			}
			else if(objectPickerButtonMouseovered)
			{
				DrawGUI.Use(inputEvent);
				StartUsingEyeDropper();
				return true;
			}

			return base.OnRightClick(inputEvent);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(Value != null)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Peek", Peek);
				menu.Add("Ping", Ping);

				#if UNITY_EDITOR
				if(Value.IsSceneObject())
				{
					menu.AddSeparator();
					menu.Add("Show In Scene View", ShowInSceneView);
				}
				#endif
			}

			menu.AddSeparator();
			menu.Add("Eyedropper Tool", StartUsingEyeDropper);

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		#if UNITY_EDITOR
		private void ShowInSceneView()
		{
			var sceneViews = Resources.FindObjectsOfTypeAll<SceneView>();
			if(sceneViews.Length > 0)
			{
				var sceneView = sceneViews[0];
				sceneView.AlignViewToObject(Value.Transform());
			}
		}
		#endif

		/// <inheritdoc/>
		public override void OnDrag(Event inputEvent)
		{
			base.OnDrag(inputEvent);

			if(Inspector.Manager.MouseDownInfo.CursorMovedAfterMouseDown && !DrawGUI.IsUnityObjectDrag && Event.current.type == EventType.MouseDrag)
			{
				DrawGUI.Active.DragAndDropObjectReferences = Values;
			}
		}


		/// <inheritdoc/>
		protected override void OnControlClicked(Event inputEvent)
		{
			if(objectPickerButtonMouseovered)
			{
				//what to do here?
				//save current selection for later restoring?
				//deselect inspector?
				//at least don't focus control field!
				return;
			}

			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
			FocusControlField();
			
			// While this is not necessary for pinging to take place (Unity already does this internally),
			// what it does is handle bringing the Project / Hierarchy window to front if it's a background tab.
			DrawGUI.Active.PingObject(Value);

			#if UNITY_EDITOR
			var value = Value;
			if(value != null)
			{
				InspectorUtility.ActiveInspector.ScrollToShow(value);
			}
			#else
			DisplayTargetSelectMenu();
			#endif
		}

		/// <inheritdoc/>
		protected override Object GetRandomValue()
		{
			var type = Type;

			#if UNITY_EDITOR
			if(!allowSceneObjects || Random.Range(0, 2) == 0)
			{
				var allAssetGuids = AssetDatabase.FindAssets(StringUtils.Concat("t:", Type.Name));
				int assetCount = allAssetGuids.Length;
				if(assetCount > 0)
				{
					
					for(int n = 0; n < 10; n++)
					{
						var guid = allAssetGuids[Random.Range(0, assetCount)];
						var assetPath = AssetDatabase.GUIDToAssetPath(guid);
						var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
						if(type.IsAssignableFrom(asset.GetType()))
						{
							return asset;
						}
					}
				}
				return null;
			}
			#endif
			
			var allSceneObjects = Object.FindObjectsOfType(Type);
			int objCount = allSceneObjects.Length;
			if(objCount > 0)
			{
				for(int n = 0; n < 10; n++)
				{
					var obj = allSceneObjects[Random.Range(0, objCount)];
					if(type.IsAssignableFrom(obj.GetType()))
					{
						return obj;
					}
				}
			}
			return null;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			StopUsingEyeDropper();

			ApplyUnappliedChanges();
			
			#if DEV_MODE
			Debug.Assert(!hasUnappliedChanges, ToString()+".Dispose - hasUnappliedChanges was true!");
			#endif

			if(listeningForObjectPickerClosed)
			{
				#if DEV_MODE && DEBUG_OBJECT_PICKER
				Debug.Log("listeningForObjectPickerClosed = "+StringUtils.False);
				#endif
				listeningForObjectPickerClosed = false;
				ObjectPicker.OnClosed -= OnObjectPickerClosedWithUnappliedChanges;
			}
			
			base.Dispose();
		}

		/// <inheritdoc />
		protected override bool TryGetSingleValueVisualizedInInspector(out object visualizedValue)
		{
			if(hasUnappliedChanges)
			{
				visualizedValue = valueUnapplied;
				return true;
			}

			return base.TryGetSingleValueVisualizedInInspector(out visualizedValue);
		}

		/// <inheritdoc/>
		protected override Object GetCopyOfValue(Object source)
		{
			return source;
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);
			if(MouseOverPart == PrefixedControlPart.Control)
			{
				objectFieldMouseovered = DragNDropAreaPosition.MouseIsOver();
				objectPickerButtonMouseovered = !objectFieldMouseovered;
			}
			else
			{
				objectFieldMouseovered = false;
				objectPickerButtonMouseovered = false;
			}
		}

		private void SetHasUnappliedChanges(bool setHasUnappliedChanges)
		{
			if(hasUnappliedChanges != setHasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log("SetHasUnappliedChanges("+StringUtils.ToColorizedString(setHasUnappliedChanges)+ ") with Value="+ StringUtils.ToColorizedString(Value)+ ", valueUnapplied=" + StringUtils.ToColorizedString(valueUnapplied));
				#endif

				hasUnappliedChanges = setHasUnappliedChanges;
				
				if(hasUnappliedChanges)
				{
					if(ObjectPicker.IsOpen && !listeningForObjectPickerClosed)
					{
						#if DEV_MODE && DEBUG_OBJECT_PICKER
						Debug.Log("listeningForObjectPickerClosed = "+StringUtils.True);
						#endif
						listeningForObjectPickerClosed = true;
						ObjectPicker.OnClosed += OnObjectPickerClosedWithUnappliedChanges;
					}
				}
				else
				{
					if(listeningForObjectPickerClosed)
					{
						#if DEV_MODE && DEBUG_OBJECT_PICKER
						Debug.Log("listeningForObjectPickerClosed = "+StringUtils.False);
						#endif
						listeningForObjectPickerClosed = false;
						ObjectPicker.OnClosed -= OnObjectPickerClosedWithUnappliedChanges;
					}
				}
			}
		}
		
		private void OnObjectPickerClosedWithUnappliedChanges(Object initialObject, Object selectedObject, bool wasCancelled)
		{
			#if DEV_MODE && DEBUG_OBJECT_PICKER
			Debug.Log(Msg("OnObjectPickerClosedWithUnappliedChanges(initial=", initialObject, ", selected=", selectedObject, ", wasCancelled=", wasCancelled,") - hasUnappliedChanges=" + StringUtils.ToColorizedString(hasUnappliedChanges)+ ", Value="+ StringUtils.ToColorizedString(Value)+ ", valueUnapplied=" + StringUtils.ToColorizedString(valueUnapplied)));
			#endif

			Select(ReasonSelectionChanged.GainedFocus);

			if(listeningForObjectPickerClosed)
			{
				#if DEV_MODE && DEBUG_OBJECT_PICKER
				Debug.Log("listeningForObjectPickerClosed = "+StringUtils.False);
				#endif
				listeningForObjectPickerClosed = false;
				ObjectPicker.OnClosed -= OnObjectPickerClosedWithUnappliedChanges;
			}

			if(wasCancelled)
			{
				DiscardUnappliedChanges();
			}
			else
			{
				ApplyUnappliedChanges();
			}
		}

		private void Peek()
		{
			var inspector = InspectorUtility.ActiveInspector;
			if(inspector != null && inspector.InspectorDrawer.CanSplitView)
			{
				var splittableDrawer = (ISplittableInspectorDrawer)inspector.InspectorDrawer;
				inspector.OnNextLayout(() => splittableDrawer.ShowInSplitView(Value));
			}
		}

		private void Ping()
		{
			DrawGUI.Ping(Value);
		}
		
		private void DisplayTargetSelectMenu()
		{
			generatedMenuItems.Clear();
			generatedGroupsByLabel.Clear();
			generatedItemsByLabel.Clear();

			var currentValue = Value;
			Transform currentValueTransform;

			GameObject prefabRoot;
			bool isAsset;

			// if field has a current value, generate menu
			// in relation to said target value
			if(currentValue != null)
			{
				currentValueTransform = currentValue.Transform();
				if(currentValueTransform == null)
				{
					prefabRoot = null;
					isAsset = true;
				}
				else if(currentValueTransform.IsPrefab())
				{
					#if !UNITY_EDITOR
					prefabRoot = currentValueTransform.gameObject;
					#elif UNITY_2018_2_OR_NEWER
					prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(currentValueTransform.gameObject));
					#else
					prefabRoot = PrefabUtility.FindPrefabRoot(currentValueTransform.gameObject);
					#endif
					isAsset = true;
				}
				else
				{
					prefabRoot = null;
					isAsset = false;
				}
			}
			// if field has no current value, generate menu
			// in relation to gameObject which holds the field
			else
			{
				currentValueTransform = null;

				#if UNITY_EDITOR
				var target = UnityObject;
				var gameObject = target == null ? null : target.GameObject();

				// if field resides in a prefab populate the menu with all objects in prefab
				if(gameObject != null && gameObject.IsPrefab())
				{
					#if UNITY_2018_2_OR_NEWER
					prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(gameObject));
					#else
					prefabRoot = PrefabUtility.FindPrefabRoot(gameObject);
					#endif
					isAsset = true;
				}
				else
				#endif
				{
					// if field has no value and the field does not reside inside a prefab,
					// set root to null, as we'll populate the menu with all targets in assets / hierarchy
					prefabRoot = null;
					isAsset = !allowSceneObjects;
				}
			}
			
			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString("DisplayTargetSelectMenu with isAsset=", isAsset, ", rootGameObject=", prefabRoot, ", type=", type));
			#endif

			string selectItemAtPath = null;

			var menuLabel = GUIContentPool.Empty();

			// if this member resides in a prefab target, then we get all
			// GameObjects / Components in children of the prefab.
			if(prefabRoot != null)
			{
				// add all GameObjects and Components in children of the prefab
				selectItemAtPath = PopupMenuUtility.BuildPopupMenuItemForObjectsInChildren(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, prefabRoot, type, currentValue);
				menuLabel.text = prefabRoot.name;
			}
			else
			{
				if(!isAsset)
				{
					menuLabel.text = "Hierarchy";

					if(type == Types.GameObject)
					{
						for(int s = 0, scount = SceneManager.sceneCount;  s < scount; s++)
						{
							var scene = SceneManager.GetSceneAt(s);
							var gos = scene.GetAllGameObjects();
							for(int g = 0, count = gos.Length; g < count; g++)
							{
								PopupMenuUtility.BuildPopupMenuItemForGameObject(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, gos[g]);
							}
						}
					}
					else if(type == Types.Transform || type == Types.RectTransform)
					{
						for(int s = 0, scount = SceneManager.sceneCount;  s < scount; s++)
						{
							var scene = SceneManager.GetSceneAt(s);
							var gos = scene.GetAllGameObjects();
							for(int g = 0, count = gos.Length; g < count; g++)
							{
								PopupMenuUtility.BuildPopupMenuItemForTransform(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, gos[g].transform);
							}
						}
					}
					else if(type == Types.UnityObject)
					{
						for(int s = 0, scount = SceneManager.sceneCount;  s < scount; s++)
						{
							var scene = SceneManager.GetSceneAt(s);
							var gos = scene.GetAllGameObjects();
							for(int g = 0, count = gos.Length; g < count; g++)
							{
								PopupMenuUtility.BuildPopupMenuItemForGameObjectAndItsComponents(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, gos[g]);
							}
						}
					}
					else if(type.IsInterface)
					{
						type.FindObjectsImplementingInterface(GetComponents, FindObjects);
						GetComponents.Sort(SortComponentsByHierarchyOrder.Instance);
						for(int n = 0, count = GetComponents.Count; n < count; n++)
						{
							var comp = GetComponents[n];
							string hierarchyPath = string.Concat(comp.transform.GetHierarchyPath(), "/", StringUtils.ToStringSansNamespace(comp.GetType()));
							PopupMenuUtility.BuildPopupMenuItemWithLabel(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, comp, hierarchyPath, MenuItemValueType.UnityObject);
						}
						for(int n = 0, count = FindObjects.Count; n < count; n++)
						{
							var obj = FindObjects[n];
							string assetPath = string.Concat(obj.HierarchyOrAssetPath(), "/", StringUtils.ToStringSansNamespace(obj.GetType()));
							PopupMenuUtility.BuildPopupMenuItemWithLabel(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, obj, assetPath, MenuItemValueType.UnityObject);
						}
					}
					else if(type.IsComponent())
					{
						var comps = Object.FindObjectsOfType(type) as Component[];
						Array.Sort(comps, SortComponentsByHierarchyOrder.Instance);
						for(int n = 0, count = comps.Length; n < count; n++)
						{
							var comp = comps[n];
							string hierarchyPath = string.Concat(comp.transform.GetHierarchyPath(), "/", StringUtils.ToStringSansNamespace(comp.GetType()));
							PopupMenuUtility.BuildPopupMenuItemWithLabel(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, comp, hierarchyPath, MenuItemValueType.UnityObject);
						}
					}
					else //ScriptableObject, Asset etc.
					{
						var objs = Object.FindObjectsOfType(type);
						for(int n = 0, count = objs.Length; n < count; n++)
						{
							var obj = objs[n];
							PopupMenuUtility.BuildPopupMenuItemWithLabel(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, obj, obj.name, MenuItemValueType.UnityObject);
						}
					}

					if(currentValueTransform != null)
					{
						selectItemAtPath = currentValueTransform.GetHierarchyPath();
						if(!type.IsGameObject())
						{
							selectItemAtPath = string.Concat(selectItemAtPath, "/", currentValue.GetType().Name);
						}
					}
				}
				#if UNITY_EDITOR
				//an asset
				else
				{
					var currentOrBaseType = currentValue != null ? currentValue.GetType() : type;

					// If type is GameObject or Component, just list all prefabs without Components.
					// The user can then use the right click menu again to further select which Component to use.
					// Alternatively could also immediately pop a new menu open to let the user define the
					// Object inside said Prefab in OnPopupMenuClosed.
					if(currentOrBaseType == Types.GameObject || currentOrBaseType.IsComponent())
					{
						menuLabel.text = "Prefabs";
						var gameObjects = AssetDatabase.FindAssets("t:GameObject");
						int count = gameObjects.Length;
						if(count > 0)
						{
							for(int n = 0; n < count; n++)
							{
								var assetGuid = gameObjects[n];
								string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

								// remove "Assets/" from the beginning, and ".prefab" from the end
								string assetPathShortened = assetPath.Substring(7, assetPath.Length - 14);
								PopupMenuUtility.BuildPopupMenuItemWithLabel(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, assetPath, type, assetPathShortened, "", MenuItemValueType.AssetPath);
							}
						}
					}
					else
					{
						//menuLabel.text = "Assets";
						menuLabel.text = type.Name + " Assets";

						var assets = AssetDatabase.FindAssets(string.Concat("t:", type.Name));
						
						int count = assets.Length;
						if(type.IsSealed)
						{
							for(int n = 0; n < count; n++)
							{
								var assetGuid = assets[n];
								string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

								// remove "Assets/" from the beginning
								string assetPathShortened = assetPath.Substring(7);
								PopupMenuUtility.BuildPopupMenuItemWithLabel(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, assetPath, type, assetPathShortened, "", MenuItemValueType.AssetPath);
							}
						}
						else
						{
							for(int n = 0; n < count; n++)
							{
								var assetGuid = assets[n];
								string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

								// remove "Assets/" from the beginning
								string assetPathShortened = assetPath.Substring(7);

								var loaded = AssetDatabase.LoadAssetAtPath(assetPath, type);
								var assetType = loaded.GetType();
								EditorUtility.UnloadUnusedAssetsImmediate();
								PopupMenuUtility.BuildPopupMenuItemWithLabel(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel, assetPath, assetType, assetPathShortened, "", MenuItemValueType.AssetPath);
							}
						}

						if(currentValue != null)
						{
							var assetPath = AssetDatabase.GetAssetPath(currentValue);
							selectItemAtPath = assetPath.Substring(7);
						}
					}
				}
				#endif
			}
			
			if(generatedMenuItems.Count > 0)
			{
				var unrollPosition = DragNDropAreaPosition;
				unrollPosition.height = PopupMenu.TotalMaxHeightWithNavigationBar;
				PopupMenuManager.Open(Inspector, generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, DragNDropAreaPosition, OnPopupMenuItemClicked, OnPopupMenuClosed, menuLabel, this);

				if(selectItemAtPath != null)
				{
					#if DEV_MODE
					Debug.Log("selectItemAtPath: "+ selectItemAtPath);
					#endif
					PopupMenuManager.SelectItem(selectItemAtPath);
				}
			}
		}

		private void OnPopupMenuItemClicked(PopupMenuItem item)
		{
			Value = item.IdentifyingObject as Object;
		}

		private void OnPopupMenuClosed()
		{
			Select(ReasonSelectionChanged.Initialization);
		}
		
		/// <summary>
		/// If value selected via object picker is still unapplied, apply it now
		/// </summary>
		private void ApplyUnappliedChanges()
		{
			if(hasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".ApplyUnappliedChanges with valueUnapplied=", valueUnapplied, ", Value=", Value, "  - Event=", StringUtils.ToString(Event.current) + ", KeyCode=" + (Event.current == null ? KeyCode.None : Event.current.keyCode) + ", button=" + (Event.current == null ? -1 : Event.current.button)));
				#endif

				Value = valueUnapplied;
			}
			#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
			else { Debug.Log(StringUtils.ToColorizedString(ToString(), ".ApplyUnappliedChanges with valueUnapplied=", valueUnapplied, ", Value=", Value, "  - Event=", StringUtils.ToString(Event.current) + ", KeyCode=" + (Event.current == null ? KeyCode.None : Event.current.keyCode) + ", button=" + (Event.current == null ? -1 : Event.current.button))); }
			#endif
		}

		/// <summary>
		/// If value selected via object picker is still unapplied, discard it now
		/// and keep the previously selected value
		/// </summary>
		private void DiscardUnappliedChanges()
		{
			if(hasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log("DiscardUnappliedChanges - Event=" + StringUtils.ToString(Event.current) + ", KeyCode=" + Event.current.keyCode + ", button=" + Event.current.button);
				#endif

				valueUnapplied = Value;
				SetHasUnappliedChanges(false);
			}
		}
	}
}