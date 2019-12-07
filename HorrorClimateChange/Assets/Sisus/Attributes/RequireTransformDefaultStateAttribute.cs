using System;
using UnityEngine;

namespace Sisus.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class RequireTransformDefaultStateAttribute : Attribute, IComponentModifiedCallbackReceiver<Transform>
	{
		/// <inheritdoc/>
		public void OnComponentAdded(Component attributeHolder, Transform addedComponent)
		{
			addedComponent.localPosition = Vector3.zero;
			addedComponent.localEulerAngles = Vector3.zero;
			addedComponent.localScale = Vector3.one;
		}

		/// <inheritdoc/>
		public void OnComponentModified(Component attributeHolder, Transform modifiedComponent)
		{
			if(modifiedComponent.localPosition != Vector3.zero || modifiedComponent.localEulerAngles != Vector3.zero || modifiedComponent.localScale != Vector3.one)
			{
				#if UNITY_EDITOR
				UnityEditor.EditorGUIUtility.editingTextField = false;
				#endif

				Debug.LogWarning(attributeHolder.GetType().Name + " requires that " + modifiedComponent.GetType().Name + " remains at default state.");

				modifiedComponent.localPosition = Vector3.zero;
				modifiedComponent.localEulerAngles = Vector3.zero;
				modifiedComponent.localScale = Vector3.one;
			}
		}
	}
}