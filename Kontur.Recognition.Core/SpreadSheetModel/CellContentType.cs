namespace Kontur.Recognition.SpreadSheetModel
{
	public enum CellContentType
	{
		// Cell is empty (no content)
		Empty,
		// Cell contains number-based information (use Value member and format information to decode real information)
		Value,
		// Cell contains static text (can be read with either Formula or Text properties)
		Text,
		// Cell contains calculatable expression (can be read with Formula property)
		Formula
	}
}