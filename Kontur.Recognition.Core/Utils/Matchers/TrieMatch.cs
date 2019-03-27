namespace Kontur.Recognition.Utils.Matchers
{
	/// <summary>
	/// Represents result of matching with TrieMatcher
	/// </summary>
	public class TrieMatch
	{
		/// <summary>
		/// Starting position in source string where the match is detected
		/// </summary>
		public int Position { get; private set; }

		/// <summary>
		/// The detected value
		/// </summary>
		public string Value { get; private set; }

		public TrieMatch(int position, string value)
		{
			Position = position;
			Value = value;
		}
	}
}