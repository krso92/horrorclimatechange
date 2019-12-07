using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Like Unity's built-in SpaceAttribute but supports targeting of properties and methods in addition to fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class PSpaceAttribute : SpaceAttribute
	{
		/// <summary> Use this DecoratorDrawer to add some spacing in the Inspector. </summary>
		public PSpaceAttribute() : base() { }

		/// <summary> Use this DecoratorDrawer to add some spacing in the Inspector. </summary>
		/// <param name="height"> The spacing in pixels. </param>
		public PSpaceAttribute(float height) : base(height) { }
	}
}