using System;

namespace Kontur.Recognition.SpreadSheetModel
{
	[Flags]
	public enum NumberFormat
	{
		All = 0,
		Defined = 1,
		Date = 2,
		Time = 4,
		Currency = 8,
		Number = 16,
		Scientific = 32,
		Fraction = 64,
		Percent = 128,
		Text = 256,
		Datetime = 6,
		Logical = 1024,
		Undefined = 2048
	}
}