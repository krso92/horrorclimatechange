using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	[Serializable]
	public class GameObjectHeaderDrawer
	{
		#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
		private static readonly GUIContent OpenPrefabButtonLabel = new GUIContent("Open In Prefab Mode", "Open in Prefab Mode for full editing support.");
		#endif
		
		[SerializeField]
		private GameObject[] targets;

		#if UNITY_EDITOR
		[SerializeField]
		private Editor editor;
		#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
		private bool isPrefab;
		#endif
		#endif
		
		public void SetTargets(GameObject[] setTargets)
		{
			targets = setTargets;

			#if UNITY_EDITOR
			Editors.GetEditor(ref editor, targets, null, true);

			#if UNITY_2018_3_OR_NEWER
			isPrefab = targets.Length == 1 && targets[0].IsPrefab();
			#endif
			#endif
		}

		public void Draw(Rect position)
		{
			#if UNITY_EDITOR
			if(Platform.EditorMode)
			{
				EditorGUIDrawer.AssetHeader(position, editor);

				#if UNITY_2018_3_OR_NEWER
				if(isPrefab)
				{
					position.y += position.height - DrawGUI.SingleLineHeight;
					position.height = DrawGUI.SingleLineHeight;

					// UPDATE: even if prefab is being drawn in grayed out color
					// due to being inactive, draw the open prefab button without
					// being grayed out, to make it clear that it remains usable.
					var guiColorWas = GUI.color;
					GUI.color = Color.white;
					
					if(DrawGUI.Editor.Button(position, OpenPrefabButtonLabel, InspectorPreferences.Styles.Toolbar))
					{
						DrawGUI.UseEvent();
						GameObjectDrawer.OpenPrefab(targets[0]);
					}

					GUI.color = guiColorWas;
				}
				#endif

				return;
			}
			#endif
			DrawGUI.Runtime.GameObjectHeader(position, targets[0]);
		}

		public void ResetState()
		{
			targets = null;

			#if UNITY_EDITOR
			if(!ReferenceEquals(editor, null))
			{
				Editors.Dispose(ref editor);
			}
			#endif
		}

		public void OnProjectOrHierarchyChanged(GameObject[] setTargets)
		{
			targets = setTargets;

			#if UNITY_EDITOR
			if(editor == null || Editors.DisposeIfInvalid(ref editor))
			{
				Editors.GetEditor(ref editor, targets);
			}
			#endif
		}
	}
}