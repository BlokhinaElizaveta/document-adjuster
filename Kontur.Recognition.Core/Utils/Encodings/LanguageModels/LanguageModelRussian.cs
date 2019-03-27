namespace Kontur.Recognition.Utils.Encodings.LanguageModels
{
	/// <summary>
	/// The frequency based model of Russian language
	/// </summary>
	public class LanguageModelRussian : LanguageModel
	{
		/// <summary>
		/// Predefined model instance
		/// </summary>
		public static readonly LanguageModelRussian Instance = new LanguageModelRussian();

		private LanguageModelRussian() : base(MakeLowerCaseFrequencies(), 0.025)
		{
		}

		private static CharCounters MakeLowerCaseFrequencies()
		{
			var result = new CharCounters();
			result.AddChar('а', 7998);
			result.AddChar('б', 1592);
			result.AddChar('в', 4533);
			result.AddChar('г', 1687);
			result.AddChar('д', 2977);
			result.AddChar('е', 8483);
			result.AddChar('ё', 13);
			result.AddChar('ж', 940);
			result.AddChar('з', 1641);
			result.AddChar('и', 7367);
			result.AddChar('й', 1208);
			result.AddChar('к', 3486);
			result.AddChar('л', 4343);
			result.AddChar('м', 3203);
			result.AddChar('н', 6700);
			result.AddChar('о', 10983);
			result.AddChar('п', 2804);
			result.AddChar('р', 4746);
			result.AddChar('с', 5473);
			result.AddChar('т', 6318);
			result.AddChar('у', 2615);
			result.AddChar('ф', 267);
			result.AddChar('х', 966);
			result.AddChar('ц', 486);
			result.AddChar('ч', 1450);
			result.AddChar('ш', 718);
			result.AddChar('щ', 361);
			result.AddChar('ъ', 37);
			result.AddChar('ы', 1898);
			result.AddChar('ь', 1735);
			result.AddChar('э', 331);
			result.AddChar('ю', 639);
			result.AddChar('я', 2001);
			return result;
		}
	}
}