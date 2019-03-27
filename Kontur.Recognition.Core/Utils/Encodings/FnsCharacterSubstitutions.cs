namespace Kontur.Recognition.Utils.Encodings
{
	/// <summary>
	/// The substitution which is found in FNS produced documents (there are some embedded fonts 
	/// without an information on how to map them to Unicode). This substitution helps to fix 
	/// character content extracted from those documents.
	/// </summary>
	public static class FnsCharacterSubstitutions
	{
		/// <summary>
		/// The substitution to fix encoding in FNS produced PDF documents
		/// </summary>
		public static readonly CharacterSubstitution FnsSubstitution1 = BuildFNsSubstitution1();

		private static CharacterSubstitution BuildFNsSubstitution1()
		{
			var result = new CharacterSubstitution();
			result.MapRange('\u0003', ' ', 33); // punctuation characters and digits
			result.MapRange('\u023A', 'À', 32);
			result.MapRange('\u025A', 'à', 32);
			result.MapChar('\u028B', '¹');
			return result;
		}
	}
}