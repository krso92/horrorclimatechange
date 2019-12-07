using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public sealed class MaxIntDrawer : NumericDrawer<int>
	{
		private int max;

		public static MaxIntDrawer Create(int value, int max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			MaxIntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MaxIntDrawer();
			}
			result.Setup(value, max, memberInfo, parent, label, setReadOnly);
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

		private void Setup(int value, int setMax, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			max = setMax;
			Setup(value > max ? max : value, typeof(int), setMemberInfo, setParent, setLabel, setReadOnly);

			if(string.IsNullOrEmpty(label.tooltip))
			{
				label.tooltip = string.Concat("Value smaller than or equal to ", StringUtils.ToString(max));
			}
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(int setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(setValue > max ? max : setValue, applyToField, updateMembers);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref int inputValue, int inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Min(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity), max);
		}

		/// <inheritdoc />
		public override int DrawControlVisuals(Rect position, int value)
		{
			return DrawGUI.Active.MaxIntField(controlLastDrawPosition, value, max);
		}

		/// <inheritdoc />
		protected override int GetRandomValue()
		{
			return UnityEngine.Random.Range(int.MinValue, max);
		}
	}
}