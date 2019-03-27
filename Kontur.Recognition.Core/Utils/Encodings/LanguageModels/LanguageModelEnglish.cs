namespace Kontur.Recognition.Utils.Encodings.LanguageModels
{
	/// <summary>
	/// The frequency based model of English language
	/// </summary>
	public class LanguageModelEnglish : LanguageModel
	{
		/// <summary>
		/// Predefined model instance
		/// </summary>
		public static readonly LanguageModelEnglish Instance = new LanguageModelEnglish();

		// TODO: calculate relative frequency of capital letter in English
		// currently the value of 0.025 is taken the same as for Russian
		private LanguageModelEnglish() : base(MakeFrequencies(), 0.025)
		{
		}

		private static CharCounters MakeFrequencies()
		{
			var result = new CharCounters();
			result.AddChar('a', 8167);
			result.AddChar('b', 1492);
			result.AddChar('c', 2782);
			result.AddChar('d', 4253);
			result.AddChar('e', 12702);
			result.AddChar('f', 2228);
			result.AddChar('g', 2015);
			result.AddChar('h', 6094);
			result.AddChar('i', 6966);
			result.AddChar('j', 153);
			result.AddChar('k', 772);
			result.AddChar('l', 4025);
			result.AddChar('m', 2406);
			result.AddChar('n', 6749);
			result.AddChar('o', 7507);
			result.AddChar('p', 1929);
			result.AddChar('q', 95);
			result.AddChar('r', 5987);
			result.AddChar('s', 6327);
			result.AddChar('t', 9056);
			result.AddChar('u', 2758);
			result.AddChar('v', 978);
			result.AddChar('w', 2360);
			result.AddChar('x', 150);
			result.AddChar('y', 1974);
			result.AddChar('z', 74);
			return result;
		}
	}
}