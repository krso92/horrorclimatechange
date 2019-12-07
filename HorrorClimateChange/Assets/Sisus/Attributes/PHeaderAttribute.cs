using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Like Unity's built-in HeaderAttribute but supports targeting of properties and methods in addition to fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class PHeaderAttribute : HeaderAttribute
	{
		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		/// <param name="header">The header text.</param>
		public PHeaderAttribute(string header) : base(header) { }

		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		/// <param name="headerLines">The lines of text for the header.</param>
		public PHeaderAttribute(params string[] headerLines) : base(string.Join("\n", headerLines)) { }

		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		/// <param name="fontSize">Specify the fontSize to use for the header.</param>
		public PHeaderAttribute(string header, int fontSize) : base(string.Concat("<size=", fontSize, ">", header, "</size>")) { }
		
		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		/// <param name="color">Specify the font color to use for the header. For example "red" or "#ff0000ff".</param>
		public PHeaderAttribute(string header, string color) : base(string.Concat("<color=", color, ">", header, "</color>")) { }

		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		///  <param name="fontSize">Specify the fontSize to use for the header.</param>
		/// <param name="color">Specify the font color to use for the header. For example "red" or "#ff0000ff".</param>
		public PHeaderAttribute(string header, int fontSize, string color) : base(string.Concat("<size=", fontSize, "><color=", color, ">", header, "</color></size>")) { }
	}
}