using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class ReorderableParentDrawerUtility
	{
		public const float DraggedObjectGapHeight = 3f;

		/// <summary> Gets zero-based drop target index at point, where 0 is right below the parent's header (i.e. above the first member, if any). </summary>
		/// <param name="parent"> The parent whose drop target index at the given point we want to get. This cannot be null. </param>
		/// <param name="point"> The point for which we want to find the drop target index. </param>
		/// <param name="reoderedMemberIsDrawnWithOthers">
		/// True if member currently being reordered is drawn as normal among along with other members, false if member is not drawn.
		/// </param>
		/// <returns> The drop target index at point. </returns>
		public static int GetDropTargetIndexAtPoint([NotNull]IReorderableParent parent, Vector2 point, bool reoderedMemberIsDrawnWithOthers)
		{
			//if folded, not a valid drop target
			if(!parent.Unfolded)
			{
				return -1;
			}

			Rect dropRect = parent.FirstReorderableDropTargetRect;
			
			int index = parent.FirstVisibleCollectionMemberIndex;

			//if no visible collection members (e.g. collection size is zero)
			//still check if point is over FirstReorderableDropTargetRect
			if(index == -1)
			{
				return dropRect.Contains(point) ? 0 : -1;
			}

			var visibleMembers = parent.VisibleMembers;
			var reordering = InspectorUtility.ActiveManager.MouseDownInfo.Reordering.Drawer;
			
			int lastMemberIndex = parent.LastVisibleCollectionMemberIndex;
			for(; index <= lastMemberIndex; index++)
			{
				if(dropRect.Contains(point))
				{
					return index;
				}
				var member = visibleMembers[index];
				if(reordering != member || reoderedMemberIsDrawnWithOthers)
				{
					dropRect.y += member.Height;
				}
			}

			//check drop rect below last member
			if(dropRect.Contains(point))
			{
				return index;
			}
			return -1;
		}
		
		public static float CalculateHeight(IReorderableParent parent)
		{
			float unfoldedness = parent.Unfoldedness;

			if(unfoldedness <= 0f)
			{
				return parent.HeaderHeight;
			}

			float membersHeight = 0f;
			var visibleMembers = parent.VisibleMembers;
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				membersHeight += visibleMembers[n].Height;
			}

			var mouseDownInfo = InspectorUtility.ActiveManager.MouseDownInfo;
			if(mouseDownInfo.NowReordering)
			{
				var reordering = mouseDownInfo.Reordering;
				var source = reordering.Parent;
				var mouseovered = reordering.MouseoveredDropTarget;
				var target = mouseovered.Parent;
				
				if(source == parent)
				{
					if(target != parent)
					{
						membersHeight += DraggedObjectGapHeight - reordering.Drawer.Height;
					}
					else if(reordering.MemberIndex != mouseovered.MemberIndex)
					{
						membersHeight += DraggedObjectGapHeight;
					}
				}
				else if(target == parent)
				{
					membersHeight += reordering.Drawer.Height;
				}
			}

			return parent.HeaderHeight + membersHeight * unfoldedness;
		}
	}
}