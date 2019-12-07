namespace Sisus
{
	public enum FieldVisibililty
	{
		/// <summary>
		/// Only show properties explicitly exposed with attributes
		/// like EditorBrowsable, Browsable(true), SerializeField or ShowInInspector
		/// </summary>
		Default = 0,

		/// <summary>
		/// All public fields are shown, unless explicitly hidden
		/// with attributes like HideInInspector, even if non-serialized.
		/// </summary>
		AllPublic = 1,

		/// <summary>
		/// All public and non-public fields are shown unless explicitly hidden
		/// with attributes like HideInInspector.
		/// </summary>
		All = 2
	}
}