using System;

namespace Kontur.Recognition.SpreadSheetModel
{
	public interface ICellFormatsProvider
	{
		/// <summary>
		/// Returns date which corresponds to value 0 of cell value
		/// </summary>
		DateTime NullDate { get; }

		/// <summary>
		/// Maximal number of decimal digits for default cell format
		/// </summary>
		int StandardMaxDecimals { get; }

		/// <summary>
		/// Retrieves format by given format ID
		/// </summary>
		/// <param name="formatId">ID of format to retrieve</param>
		/// <returns></returns>
		CellFormat GetFormat(int formatId);
	}
}