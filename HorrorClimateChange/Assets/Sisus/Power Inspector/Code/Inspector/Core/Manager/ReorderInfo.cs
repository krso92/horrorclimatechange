//#define DEBUG_DRAG_STARTED

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class ReorderInfo
	{
		private IReorderable control;
		private IReorderableParent parent;
		private int controlIndexInParent = -1;

		private readonly ReorderDropTarget mouseoveredDropTarget = new ReorderDropTarget();
		
		public IReorderable Drawer
		{
			get
			{
				return control;
			}
		}

		public IReorderableParent Parent
		{
			get
			{
				return parent;
			}
		}

		public ReorderDropTarget MouseoveredDropTarget
		{
			get
			{
				return mouseoveredDropTarget;
			}
		}

		public int MemberIndex
		{
			get
			{
				return controlIndexInParent;
			}
		}
		
		public void OnReorderableDragStarted(IReorderable reorderedControl, IReorderableParent reorderedControlParent, IInspector inspector)
		{
			#if DEV_MODE && DEBUG_DRAG_STARTED
			Debug.Log("OnReorderableDragStarted(control=" + StringUtils.ToString(reorderedControl) +", parent="+ StringUtils.ToString(reorderedControlParent) +")");
			#endif

			control = reorderedControl;
			parent = reorderedControlParent;
			controlIndexInParent = Array.IndexOf(parent.Members, control);

			mouseoveredDropTarget.OnReorderableDragStarted(inspector, control);
			
			reorderedControlParent.OnMemberReorderingStarted(reorderedControl);
		}

		/// <summary>
		/// This should be called every time a new dragging of UnityEngine.Object references starts,
		/// or when the cursor moves over a new inspector during a drag.
		/// </summary>
		/// <param name="mouseoveredInspector"> The inspector over which the dragging is now taking place. </param>
		/// <param name="draggedObjects"> Dragged object references. </param>
		public void OnUnityObjectDragOverInspectorStarted(IInspector mouseoveredInspector, Object[] draggedObjects)
		{
			#if DEV_MODE && DEBUG_DRAG_STARTED
			Debug.Log("OnUnityObjectDragOverInspectorStarted(inspector="+ StringUtils.ToString(mouseoveredInspector) + ", dragged=" + StringUtils.ToString(draggedObjects) + ")");
			#endif
			
			#if DEV_MODE
			Debug.Assert(mouseoveredInspector != null || (control == null && parent == null));
			#endif

			mouseoveredDropTarget.OnUnityObjectDragOverInspectorStarted(mouseoveredInspector, draggedObjects);
		}
		
		public void OnMouseoveredInspectorChanged(IInspector newlyMouseoveredInspector)
		{
			mouseoveredDropTarget.OnDropTargetInspectorChanged(newlyMouseoveredInspector, control, DrawGUI.Active.DragAndDropObjectReferences);
		}

		public void OnCursorMovedOrInspectorLayoutChanged()
		{
			mouseoveredDropTarget.OnCursorMovedOrInspectorLayoutChanged();
		}

		public void Clear()
		{
			if(control != null)
			{
				parent.OnMemberReorderingEnded(control);
				control = null;
				parent = null;
				controlIndexInParent = -1;
				mouseoveredDropTarget.Clear();
			}
		}
	}
}