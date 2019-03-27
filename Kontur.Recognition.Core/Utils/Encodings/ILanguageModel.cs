namespace Kontur.Recognition.Utils.Encodings
{
	/// <summary>
	/// Language model interface which provide a common way to validate
	/// a text against language specific rules
	/// </summary>
	public interface ILanguageModel
	{
		/// <summary>
		/// Determines whether the given text comforms to the specific language model.
		/// Returns a value between 0 (the text does not comply with the rules of the model)
		/// and 1 (the full compliance with the model)
		/// </summary>
		/// <param name="text">The text to analyse</param>
		/// <param name="ignoreCase">Whether text should be processed with no regards to letter case</param>
		/// <param name="applyModelFilter">Whether only the characters which correspond to the model should be taken into account</param>
		/// <returns></returns>
		double GetCompliance(string text, bool ignoreCase, bool applyModelFilter);
	}
}