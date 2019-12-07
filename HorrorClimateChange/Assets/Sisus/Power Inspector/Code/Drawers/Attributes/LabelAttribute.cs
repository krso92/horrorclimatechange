using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that specifies that its target should be shown in Power Inspector as a button.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method), MeansImplicitUse(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
	public class LabelAttribute : ShowInInspectorAttribute, IUseDrawer, IDrawerSetupDataProvider
	{
		[NotNull]
		public readonly string guiStyle;

		public LabelAttribute()
		{
			guiStyle = "";
		}

		public LabelAttribute([NotNull]string setGuiStyle)
		{
			guiStyle = setGuiStyle;
		}

		/// <inheritdoc />
		public Type GetDrawerType([NotNull] Type attributeHolderType, [NotNull] Type defaultDrawerTypeForAttributeHolder)
		{
			return typeof(StyledTextDrawer);
		}

		/// <inheritdoc />
		public object[] GetSetupParameters()
		{
			return new[] { guiStyle };
		}
	}
}