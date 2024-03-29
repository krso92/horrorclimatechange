﻿using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that can be added to a field, property, method or class to specify which drawer it should use.
	/// 
	/// This can be useful when a certain drawer is reused for multiple different things, or if you cannot directly
	/// modify an existing drawer to change its target by add a DrawerFor* attribute to it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class UseDrawerAttribute : TargetableAttribute, IUseDrawer, IDrawerSetupDataProvider
	{
		private static readonly object[] noParameters = new object[0];

		/// <summary>
		/// Type of the drawer to use.
		/// </summary>
		[NotNull]
		public readonly Type drawerType;

		/// <summary>
		/// Parameters that can be used during Setup phase of a drawer that was selected using this attribute.
		/// </summary>
		[NotNull]
		public readonly object[] setupParameters;

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified drawer type in the inspector.
		/// </summary>
		/// <param name="setDrawerType"> Type of drawer class to use for drawing the attribute holder. This can not be null. </param>
		public UseDrawerAttribute([NotNull]Type setDrawerType) : base()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(setDrawerType != null, "UseDrawerAttribute parameter \"setDrawerType\" value was null. This is not supported.");
			#endif

			drawerType = setDrawerType;
			setupParameters = noParameters;
		}

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified drawer type in the inspector.
		/// </summary>
		/// <param name="setDrawerType"> Type of drawer class to use for drawing the attribute holder. This can not be null. </param>
		/// <param name="attributeTarget"></param>
		public UseDrawerAttribute([NotNull]Type setDrawerType, Target attributeTarget) : base(attributeTarget)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(setDrawerType != null, "UseDrawerAttribute parameter \"setDrawerType\" value was null. This is not supported.");
			#endif

			drawerType = setDrawerType;
			setupParameters = noParameters;
		}

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified drawer type in the inspector.
		/// </summary>
		/// <param name="setDrawerType"> Type of drawer class to use for drawing the attribute holder. This can not be null. </param>
		/// <param name="setSetupParameters"> Parameter values for use during the Setup phase of the drawer. If no additional parameters are provided for the drawer, this should be a zero-size array. This can not be null. </param>
		public UseDrawerAttribute([NotNull]Type setDrawerType, [NotNull]params object[] setSetupParameters)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(setDrawerType != null, "UseDrawerAttribute parameter \"setDrawerType\" value was null. This is not supported.");
			UnityEngine.Debug.Assert(setSetupParameters != null, "UseDrawerAttribute parameter \"setSetupParameters\" value was null. This is not supported, you should use an empty array instead.");
			#endif

			drawerType = setDrawerType;
			setupParameters = setSetupParameters;
		}

		/// <inheritdoc/>
		public object[] GetSetupParameters()
		{
			return setupParameters;
		}

		/// <inheritdoc/>
		public Type GetDrawerType(Type attributeHolderType, Type defaultDrawerTypeForAttributeHolder)
		{
			return drawerType;
		}
	}
}