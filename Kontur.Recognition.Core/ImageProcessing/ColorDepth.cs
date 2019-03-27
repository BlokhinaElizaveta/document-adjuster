namespace Kontur.Recognition.ImageProcessing
{
	/// <summary>
	/// Defines possible image depth in bits per color channel. 
	/// </summary>
	public enum ColorDepth
	{
		/// <summary>
		/// Depth is unknown
		/// </summary>
		Unknown = 0, 
		/// <summary>
		/// One bit per channel
		/// </summary>
		Depth1Bit = 1,
		/// <summary>
		/// Eight bits per channel
		/// </summary>
		Depth8Bit = 2,
		/// <summary>
		/// Sixteen bits per channel
		/// </summary>
		Depth16Bit = 3
	}
}