using JetBrains.Annotations;
using Sisus.Attributes;
using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class StyledTextDrawer : TextDrawer
	{
		public GUIStyle guiStyle;

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(string);
			}
		}

		/// <inheritdoc/>
		public override string Value
		{
			get
			{
				return guiStyle != null ? guiStyle.name : "";
			}
		}

		/// <inheritdoc/>
		public override bool SetValue(object newValue)
		{
			CantSetValueError();
			return false;
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(string setValue, bool applyToField, bool updateMembers)
		{
			CantSetValueError();
			return false;
		}

		/// <inheritdoc/>
		public override object GetValue(int index)
		{
			return Value;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="guiStyle"> The starting cached value of the drawer. </param>
		/// <param name="text"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static StyledTextDrawer Create([NotNull]GUIStyle guiStyle, string text, [CanBeNull]IParentDrawer parent, LinkedMemberInfo memberInfo = null, GUIContent label = null, bool readOnly = false)
		{
			StyledTextDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new StyledTextDrawer();
			}
			result.Setup(guiStyle, text, typeof(string), memberInfo, parent, label, readOnly, memberInfo != null && memberInfo.GetAttribute<TextAreaAttribute>() != null, memberInfo != null && memberInfo.GetAttribute<DelayedAttribute>() != null);
			result.LateSetup();
			return result;
		}

		private static void CantSetValueError() { Debug.LogError("ReadOnly value can't be changed"); }
		
		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively() { }

		/// <inheritdoc/>
		protected override void ApplyValueToField() { }

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard() { CantSetValueError(); }

		/// <inheritdoc/>
		protected override void DoReset() { CantSetValueError(); }

		/// <inheritdoc/>
		protected override void OnValidate() { }

		/// <inheritdoc />
		public override string DrawControlVisuals(Rect position, string inputValue)
		{
			if(inputValue == null)
			{
				GUI.Label(position, "null");
				return null;
			}

			GUI.Label(position, inputValue, guiStyle);
			return inputValue;
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			//no mouseover effects, since field is not editable
		}

		/// <inheritdoc />
		public void SetupInterface(object attribute, object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var parametersProvider = attribute as IDrawerSetupDataProvider;
			var parameters = parametersProvider.GetSetupParameters();
			var setGuiStyle = Inspector.Preferences.GetStyle((string)parameters[0]);

			string text = setValue as string;
			if(text == null)
			{
				text = StringUtils.ToString(setValue);
			}

			Setup(setGuiStyle, text, setValueType, setMemberInfo, setParent, setLabel, setReadOnly, setMemberInfo != null && setMemberInfo.GetAttribute<TextAreaAttribute>() != null, setMemberInfo != null && setMemberInfo.GetAttribute<DelayedAttribute>() != null);
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var parametersProvider = setMemberInfo.GetAttribute<IDrawerSetupDataProvider>();
			var parameters = parametersProvider.GetSetupParameters();
			var setGuiStyle = Inspector.Preferences.GetStyle((string)parameters[0]);
		
			string text = setValue as string;
			if(text == null)
			{
				text = StringUtils.ToString(setValue);
			}
			Setup(setGuiStyle, text, setValueType, setMemberInfo, setParent, setLabel, setReadOnly, setMemberInfo != null && setMemberInfo.GetAttribute<TextAreaAttribute>() != null, setMemberInfo != null && setMemberInfo.GetAttribute<DelayedAttribute>() != null);
		}

		/// <inheritdoc />
		protected sealed override void Setup(string setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly, bool setTextArea, bool setDelayed)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		protected virtual void Setup(GUIStyle setGuiStyle, string setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly, bool setTextArea, bool setDelayed)
		{
			#if DEV_MODE
			Debug.Log("setGuiStyle="+ setGuiStyle.name+ "setValue= "+StringUtils.ToString(setValue)+", setLabel="+StringUtils.ToString(setLabel));
			#endif

			guiStyle = setGuiStyle;
			base.Setup(setValue, setMemberInfo, setParent, setLabel, setReadOnly, setTextArea, setDelayed);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			ParentDrawerUtility.AddMenuItemsFromContextMenuAttribute(GetValues(), ref menu);
			
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc />
		protected override string GetRandomValue()
		{
			return Value;
		}

		/// <inheritdoc />
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}
	}
}