#if UNITY_EDITOR
using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(MeshFilter), false, true)]
	public class MeshFilterDrawer : CustomEditorComponentDrawer
	{
		// calling this instantiates the mesh in edit mode
		private static readonly string[] DontDisplayMembers = { "mesh" };

		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Vertical;
			}
		}

		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				return 41f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 54f;
		}

		/// <inheritdoc/>
		protected override string[] NeverDisplayMembers()
		{
			return DontDisplayMembers;
		}
	}
}
#endif