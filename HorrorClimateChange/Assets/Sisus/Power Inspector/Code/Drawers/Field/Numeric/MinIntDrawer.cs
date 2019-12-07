using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public sealed class MinIntDrawer : NumericDrawer<int>
	{
		private int min;

		public static MinIntDrawer Create(int value, int min, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			MinIntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MinIntDrawer();
			}
			result.Setup(value, min, memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		/// <inheritdoc/>
		protected sealed override void Setup(int setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		private void Setup(int setValue, int setMin, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			min = setMin;
			Setup(setValue < min ? min : setValue, typeof(int), setMemberInfo, setParent, setLabel, setReadOnly);
			if(string.IsNullOrEmpty(label.tooltip))
			{
				label.tooltip = string.Concat("Value greater than or equal to ", StringUtils.ToString(min));
			}
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(int setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(setValue < min ? min : setValue, applyToField, updateMembers);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref int inputValue, int inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Max(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity), min);
		}

		/// <inheritdoc />
		public override int DrawControlVisuals(Rect position, int value)
		{
			return DrawGUI.Active.MinIntField(controlLastDrawPosition, value, min);
		}

		/// <inheritdoc />
		protected override int GetRandomValue()
		{
			return UnityEngine.Random.Range(min, int.MaxValue);
		}
	}
}