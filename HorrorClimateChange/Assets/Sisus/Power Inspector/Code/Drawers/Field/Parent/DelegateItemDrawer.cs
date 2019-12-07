using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(Delegate), false, true)]
	public class DelegateItemDrawer : ParentFieldDrawer<Delegate>
	{
		private static readonly List<MethodInfo> ReusedMethodsList = new List<MethodInfo>();

		private Type delegateType;
		private MethodInfo[] methodOptions;
		private string[] methodOptionNames;
		
		private Rect nullTogglePosition;
		private Rect objectReferenceFieldPosition;
		private Rect instanceTypePopupFieldPosition;
		private Rect methodPopupFieldPosition;

		private int controls;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		protected override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return true;
			}
		}
		
		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return delegateType;
			}
		}

		private bool IsNull
		{
			get
			{
				return Value == null;
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="type"> Type of the delegate. Can not be null. </param>
		/// <param name="parent"> The parent drawers of this member. Can be null. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True if drawer should be read only. </param>
		/// <returns> The newly-created instance. </returns>
		public static DelegateItemDrawer Create(Delegate value, [NotNull]Type type, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool setReadOnly)
		{
			DelegateItemDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result =  new DelegateItemDrawer();
			}
			result.Setup(value, type, null, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var delegateValue = (Delegate)setValue;
			Setup(delegateValue, setValueType, null, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup([CanBeNull]Delegate setValue, [NotNull]Type setValueType, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly)
		{
			delegateType = setValueType;
			OnDelegateValueChanged(setValue);
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			memberBuildList.Add(memberInfo);
		}
		
		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			var value = Value;
			if(value == null)
			{
				DrawerArrayPool.Resize(ref members, 2);
				members[0] = TypeDrawer.Create(null, null, this, GUIContentPool.Empty(), ReadOnly);
				members[0].OnValueChanged += OnTargetTypeChanged;
				members[1] = ObjectReferenceDrawer.Create(null, Types.UnityObject, this, GUIContent.none, true, ReadOnly);
				members[1].OnValueChanged += OnUnityObjectTargetChanged;
			}
			else
			{
				var target = value.Target;
				bool hasTarget = target != null;
				Object unityObject;
				bool isUnityObject;
				bool isAnonymous;
				string methodName;
				Type targetType;
				int methodIndex;
				MethodInfo method;

				if(hasTarget)
				{
					targetType = target.GetType();
					unityObject = target as Object;
					isUnityObject = unityObject != null;
					var bindingFlags = (isUnityObject ? BindingFlags.Instance : BindingFlags.Static) | BindingFlags.Public | BindingFlags.NonPublic;
					methodOptions = targetType.GetMethods(bindingFlags);
					int methodCount = methodOptions.Length;
					
					var parameterlessMethods = new List<MethodInfo>(methodCount);
					for(int n = methodCount - 1; n >= 0; n--)
					{
						method = methodOptions[n];
						if(method.GetParameters().Length == 0)
						{
							parameterlessMethods.Add(method);
						}
					}

					method = value.Method;

					methodIndex = parameterlessMethods.IndexOf(method);
					if(methodIndex == -1)
					{
						parameterlessMethods.Insert(0, method);
					}

					methodOptions = parameterlessMethods.ToArray();
					methodCount = parameterlessMethods.Count;
					ArrayPool<string>.Resize(ref methodOptionNames, methodCount);
					for(int n = methodCount - 1; n >= 0; n--)
					{
						methodOptionNames[n] = methodOptions[n].Name;
					}

					methodName = method.Name;
					isAnonymous = methodName[0] == '<';

					if(isAnonymous)
					{
						string methodOrigin = methodName.Substring(1,methodName.IndexOf('>')-1);
						methodName = string.Concat("Anonymous Method (", methodOrigin, ")");
					}
				}
				else
				{
					targetType = null;
					methodIndex = 0;

					method = value.Method;
					if(method == null)
					{
						methodName = "{ }";
						unityObject = null;
						isUnityObject = false;
						isAnonymous = false;
					}
					else
					{
						methodName = method.Name;
						unityObject = null;
						isUnityObject = false;
						isAnonymous = methodName[0] == '<';
					}
				}

				if(Array.IndexOf(methodOptionNames, methodName) == -1)
				{
					methodOptions = methodOptions.InsertAt(0, method);
					methodOptionNames = methodOptionNames.InsertAt(0, methodName);
				}

				#if DEV_MODE
				Debug.Log(Msg(ToString()+".DoBuildMembers with target=", target, ", type=", targetType, ", isUnityObject=", isUnityObject, ", methodName=", methodName, ", isAnonymous=", isAnonymous+", methodNames=", StringUtils.ToString(methodOptionNames)));
				#endif

				if(isUnityObject)
				{
					DrawerArrayPool.Resize(ref members, 2);
					members[0] = ObjectReferenceDrawer.Create(unityObject, unityObject.GetType(), this, GUIContentPool.Empty(), true, ReadOnly);
					members[0].OnValueChanged += OnUnityObjectTargetChanged;
					members[1] = PopupMenuDrawer.Create(methodIndex, methodOptionNames, null, this, GUIContentPool.Empty(), ReadOnly);
					members[1].OnValueChanged += OnSelectedMethodChanged;
				}
				else
				{
					DrawerArrayPool.Resize(ref members, 3);
					members[0] = NullToggleDrawer.Create(OnNullToggleButtonClicked, this, ReadOnly);
					members[1] = TypeDrawer.Create(targetType, null, this, GUIContentPool.Empty(), ReadOnly);
					members[1].OnValueChanged += OnTargetTypeChanged;
					members[2] = PopupMenuDrawer.Create(methodIndex, methodOptionNames, null, this, GUIContentPool.Empty(), ReadOnly);
					members[2].OnValueChanged += OnSelectedMethodChanged;
				}
			}
		}

		private void OnNullToggleButtonClicked()
		{
			#if DEV_MODE
			Debug.Log(ToString()+".OnNullToggleButtonClicked with IsNull="+StringUtils.ToColorizedString(IsNull));
			#endif

			Value = null;
		}

		private void OnTargetTypeChanged(IDrawer changed, object type)
		{
			OnTargetTypeChanged(type as Type);
		}

		private void OnTargetTypeChanged(Type type)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnTargetTypeChanged(" + StringUtils.ToString(type)+")");
			#endif

			UpdateMethodOptions(type);
			int count = methodOptions.Length;
			MethodInfo method;
			if(count == 0)
			{
				Value = null;
				return;
			}
			
			method = methodOptions[0];
			if(method == null)
			{
				if(count == 1)
				{
					Value = null;
					return;
				}
				method = methodOptions[1];
			}

			#if DEV_MODE
			Debug.Log("CreateDelegate(" + StringUtils.ToString(type)+", null, \"" + method + "\")");
			#endif
			Value = Delegate.CreateDelegate(type, null, method);
		}

		private void OnUnityObjectTargetChanged(IDrawer changed, object target)
		{
			OnUnityObjectTargetChanged(target as Object);
		}

		private void OnUnityObjectTargetChanged(Object target)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnUnityObjectTargetChanged(" + StringUtils.ToString(target) +")");
			#endif

			if(target == null)
			{
				Value = null;
			}
			else
			{
				var targetType = target.GetType();
				
				UpdateMethodOptions(targetType);
				int count = methodOptions.Length;
				if(count == 0)
				{
					Value = null;
				}
				else
				{
					var method = methodOptions[0];
					Value = CreateDelegate(target, method);
				}
			}
		}

		[CanBeNull]
		private Delegate CreateDelegate(object target, MethodInfo method)
		{
			return Delegate.CreateDelegate(delegateType, target, method);
		}

		private void OnSelectedMethodChanged(IDrawer changed, object methodIndex)
		{
			OnSelectedMethodChanged((int)methodIndex);
		}

		private void OnSelectedMethodChanged(int methodIndex)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnSelectedMethodChanged(" + StringUtils.ToString(methodIndex) +")");
			#endif

			if(methodIndex < 0 || methodIndex > methodOptions.Length)
			{
				Value = null;
			}
			else
			{
				var value = Value;
				var target = value.Target;
				Value = CreateDelegate(target, methodOptions[methodIndex]);
			}
		}

		private void UpdateMethodOptions(Type targetType)
		{
			GetMethodOptions(targetType, delegateType, ref methodOptions, ref methodOptionNames);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(methodOptions.Length == methodOptionNames.Length);
			#endif
		}
		
		private static void GetMethodOptions(Type targetType, Type delegateType, ref MethodInfo[] methodOptions, ref string[] methodOptionNames)
		{
			if(targetType == null)
			{
				ArrayPool<MethodInfo>.ToZeroSizeArray(ref methodOptions);
				ArrayPool<string>.ToZeroSizeArray(ref methodOptionNames);
				return;
			}

			Type delegateReturnType;
			ParameterInfo[] delegateParameters;
			DelegateUtility.GetDelegateInfo(delegateType, out delegateReturnType, out delegateParameters);
			
			var isUnityObject = targetType.IsUnityObject();
			var bindingFlags = (isUnityObject ? BindingFlags.Instance : BindingFlags.Static) | BindingFlags.Public | BindingFlags.NonPublic;
			methodOptions = targetType.GetMethods(bindingFlags);
			int methodCount = methodOptions.Length;
			var validMethods = ReusedMethodsList;
			for(int n = methodCount - 1; n >= 0; n--)
			{
				var method = methodOptions[n];
				if(method.MethodSignatureMatchesDelegate(delegateReturnType, delegateParameters))
				{
					validMethods.Add(method);
				}
			}
			methodOptions = validMethods.ToArray();
			validMethods.Clear();
			methodCount = methodOptions.Length;
			ArrayPool<string>.Resize(ref methodOptionNames, methodCount);
			for(int n = methodCount - 1; n >= 0; n--)
			{
				methodOptionNames[n] = methodOptions[n].Name;
			}

			#if DEV_MODE
			Debug.Log(StringUtils.ToString(targetType) + ".GetMethodOptions results:\n"+StringUtils.ToString(methodOptionNames, "\n"));
			#endif
		}

		#if UNITY_EDITOR
		/// <inheritdoc/>
		public override bool Draw(Rect position)
		{
			var backgroundRect = position;
			backgroundRect.x = DrawGUI.InspectorWidth - DrawGUI.MinControlFieldWidth + DrawGUI.MiddlePadding - 1f;
			backgroundRect.width = DrawGUI.MinControlFieldWidth - DrawGUI.RightPadding - DrawGUI.MiddlePadding - 1f;
			backgroundRect.y += 1f;
			backgroundRect.height -= 2f;
			return base.Draw(position);
		}
		#endif
		
		/// <inheritdoc />
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			return true;
		}

		private static string GetTooltip(ParameterInfo[] parameterInfos)
		{
			int count = parameterInfos.Length;
			if(count <= 0)
			{
				return "";
			}

			var sb = StringBuilderPool.Create();
			sb.Append('<');
			for(int n = 0; n < count; n++)
			{
				if(n != 0)
				{
					sb.Append(',');
				}
				sb.Append(parameterInfos[n].ParameterType.Name);
			}
			sb.Append('>');

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		/// <inheritdoc />
		public override bool DrawBodySingleRow(Rect position)
		{
			switch(controls)
			{
				case 0:
					return ParentDrawerUtility.DrawBodySingleRow(this, instanceTypePopupFieldPosition, objectReferenceFieldPosition);
				case 1:
					return ParentDrawerUtility.DrawBodySingleRow(this, objectReferenceFieldPosition, methodPopupFieldPosition);
				case 2:
					return ParentDrawerUtility.DrawBodySingleRow(this, nullTogglePosition, instanceTypePopupFieldPosition, methodPopupFieldPosition);
				default:
					return false;
			}
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			var value = Value;

			bool isNull = IsNull;

			if(isNull)
			{
				controls = 0;

				instanceTypePopupFieldPosition = bodyLastDrawPosition;
				instanceTypePopupFieldPosition.width = (bodyLastDrawPosition.width - 3f) * 0.5f;

				objectReferenceFieldPosition = instanceTypePopupFieldPosition;
				objectReferenceFieldPosition.x = objectReferenceFieldPosition.x + 3f + instanceTypePopupFieldPosition.width;
			}
			else if(value.Target is Object)
			{
				controls = 1;

				objectReferenceFieldPosition = bodyLastDrawPosition;
				objectReferenceFieldPosition.width = (bodyLastDrawPosition.width - 3f) * 0.5f;

				methodPopupFieldPosition = objectReferenceFieldPosition;
				methodPopupFieldPosition.x = methodPopupFieldPosition.x + 3f + objectReferenceFieldPosition.width;
			}
			else
			{
				controls = 2;

				nullTogglePosition = bodyLastDrawPosition;
				nullTogglePosition.width = 16f;

				instanceTypePopupFieldPosition = bodyLastDrawPosition;
				instanceTypePopupFieldPosition.x += nullTogglePosition.width;
				instanceTypePopupFieldPosition.width = (instanceTypePopupFieldPosition.width - nullTogglePosition.width - 3f) * 0.5f;

				methodPopupFieldPosition = instanceTypePopupFieldPosition;
				methodPopupFieldPosition.x += instanceTypePopupFieldPosition.width;
			}
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!IsNull)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Invoke", Invoke);
			}
			
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void Invoke()
		{
			Value.DynamicInvoke(null);
		}

		/// <inheritdoc/>
		public override object DefaultValue()
		{
			return null;
		}

		/// <inheritdoc/>
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			OnDelegateValueChanged(Value);
			base.OnCachedValueChanged(applyToField, updateMembers);
		}

		private void OnDelegateValueChanged(Delegate value)
		{
			if(value == null)
			{
				UpdateMethodOptions(null);
				Label = GUIContentPool.Create("null");
			}
			else
			{
				var target = value.Target;
				UpdateMethodOptions(target != null ? target.GetType() : null);

				// support anonymous methods
				var method = value.Method;
				if(Array.IndexOf(methodOptions, method) == -1)
				{
					methodOptions = methodOptions.Add(method);
					methodOptionNames = methodOptionNames.Add(method.Name);
				}
				Label = GUIContentPool.Create("Delegate");
			}
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			methodOptions = null;
			methodOptionNames = null;
			
			base.Dispose();
		}

		/// <inheritdoc/>
		protected override Delegate GetCopyOfValue(Delegate source)
		{
			return source; // cloning delegates not yet supported
		}
	}
}