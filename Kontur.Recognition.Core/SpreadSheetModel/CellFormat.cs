namespace Kontur.Recognition.SpreadSheetModel
{
	/// <summary>
	/// Encapsulates information about cell formatting
	/// </summary>
	public class CellFormat
	{
		/// <summary>
		/// Unique integer identifier of format
		/// </summary>
		private readonly int formatId;
		/// <summary>
		/// Format string
		/// </summary>
		private readonly string formatString;
		/// <summary>
		/// Flags which determine real datatype of cell (text, date, time, number etc.)
		/// </summary>
		private readonly NumberFormat formatTypeFlags;

		public NumberFormat FormatTypeFlags
		{
			get { return formatTypeFlags; }
		}

		public int FormatId 
		{ 
			get { return formatId; }
		}

		public string FormatString
		{
			get { return formatString; }
		}

		public CellFormat(int formatId, string formatString, NumberFormat formatTypeFlags)
		{
			this.formatId = formatId;
			this.formatString = formatString;
			this.formatTypeFlags = formatTypeFlags;
		}

		public override string ToString()
		{
			return string.Format("Key: {0}, FormatString: {1}, FormatTypeFlags: {2}", formatId, formatString, formatTypeFlags);
		}
	}
}