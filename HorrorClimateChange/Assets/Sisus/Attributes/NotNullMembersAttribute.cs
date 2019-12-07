using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that specifies that any members of the collection that follows it should never be null.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
	public class NotNullMembersAttribute : ValueValidatorAttribute
	{
		public NotNullMembersAttribute() : base(Target.Members) { }

		/// <inheritdoc/>
		public override bool Validate(object value)
		{
			return value != null;
		}
	}
}