using System;
using System.Collections.Generic;
using System.Text;
using Sisus.Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class UnityObjectJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType.IsUnityObject();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			string serializedString;
			if(value == null)
			{
				serializedString = "null";
			}
			else
			{
				List<Object> objectReferences = null;
				serializedString = PrettySerializer.SerializeUnityObject(value as Object, ref objectReferences);
			}

			writer.WriteRawValue(serializedString);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string stringData = reader.Value as string;
			if(string.IsNullOrEmpty(stringData) || string.Equals(stringData, "null", StringComparison.Ordinal))
			{
				return null;
			}

			var unityObjectToOverwrite = existingValue as Object;
			PrettySerializer.DeserializeUnityObject(stringData, unityObjectToOverwrite);
			return unityObjectToOverwrite;
		}
	}
}