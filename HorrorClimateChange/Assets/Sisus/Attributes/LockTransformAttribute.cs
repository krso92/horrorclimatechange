using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// When added before a component class causes the transform component to be locked to default state for all GameObjects that contain the component with this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class LockTransformAttribute : Attribute, IComponentModifiedCallbackReceiver<Transform>
	{
		/// <inheritdoc/>
		public void OnComponentAdded(Component attributeHolder, Transform addedComponent)
		{
			addedComponent.hideFlags = HideFlags.NotEditable;
			addedComponent.localPosition = Vector3.zero;
			addedComponent.localEulerAngles = Vector3.zero;
			addedComponent.localScale = Vector3.one;
		}

		/// <inheritdoc/>
		public void OnComponentModified(Component attributeHolder, Transform modifiedComponent)
		{
			if(modifiedComponent.localPosition != Vector3.zero || modifiedComponent.localEulerAngles != Vector3.zero || modifiedComponent.localScale != Vector3.one || modifiedComponent.hideFlags != HideFlags.NotEditable)
			{
				Debug.LogWarning(attributeHolder.GetType().Name + " requires that " + modifiedComponent.GetType().Name + " remains at default state.");

				modifiedComponent.hideFlags = HideFlags.NotEditable;
				modifiedComponent.localPosition = Vector3.zero;
				modifiedComponent.localEulerAngles = Vector3.zero;
				modifiedComponent.localScale = Vector3.one;
			}
		}
	}
}