using System;

namespace Kontur.Recognition.Api
{
	[Flags]
	public enum OCRFeatures
	{
		/// <summary>
		/// Means no special features to activate (provides maximal speed)
		/// </summary>
		NotSet = 0,
		/// <summary>
		/// Means enabling tables recognition (slows down processing)
		/// </summary>
		ExtractTables = 1,
		/// <summary>
		/// Means gaining more quality by mean of slowing down processing
		/// </summary>
		HighQuality = 2,
		/// <summary>
		/// Enable preprocessing step to remove stamps (of blue color)
		/// </summary>
		StampsRemoval = 4
	}
}