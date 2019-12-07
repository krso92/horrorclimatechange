//#define PI_CREATE_ASSET_MENUS

using System;
using System.Collections.Generic;
using Sisus.Attributes;
using Sisus.OdinSerializer;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class FieldValueOverrides<T> : ISerializationCallbackReceiver
	{
		[NonSerialized, ShowInInspector]
		private Dictionary<string, object> valueOverrides = new Dictionary<string, object>(10);

		[SerializeField, HideInInspector]
		private byte[] serializedData;
		[SerializeField, HideInInspector]
		private List<Object> serializedObjectsReferences;

		public void OnValueChanged(IDrawer changed, object value)
		{
			var memberInfo = changed.MemberInfo;
			if(memberInfo != null)
			{
				// TO DO: Handle attributes?
				valueOverrides[memberInfo.FullPath] = value;
			}
		}
		
		public void OnBeforeSerialize()
		{
			serializedData = SerializationUtility.SerializeValue(valueOverrides, DataFormat.Binary, out serializedObjectsReferences);
		}

		public void OnAfterDeserialize()
		{
			if(serializedData != null)
			{
				valueOverrides = SerializationUtility.DeserializeValue<Dictionary<string, object>>(serializedData, DataFormat.Binary, serializedObjectsReferences);
			}
		}
	}
}