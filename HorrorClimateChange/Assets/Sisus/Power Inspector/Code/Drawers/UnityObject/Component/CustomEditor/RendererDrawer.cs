#if UNITY_EDITOR
using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(Renderer), true, true)]
	public class RendererDrawer : CustomEditorComponentDrawer
	{
		// calling this instantiates the materials in edit mode
		private static readonly string[] DontDisplayMembers = { "material", "materials" };

		/// <inheritdoc/>
		protected override string[] NeverDisplayMembers()
		{
			return DontDisplayMembers;
		}
	}
}
#endif