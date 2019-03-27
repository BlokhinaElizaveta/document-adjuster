namespace Kontur.Recognition.ImageProcessing
{
	/// <summary>
	/// Pixel compression specified by type.
	/// </summary>
	public enum CompressionType
	{
		/// <summary>
		/// No compression
		/// </summary>
		None = 0, 

		/// <summary>
		/// JPEG compression type
		/// </summary>
		Jpeg = 1,

		/// <summary>
		/// Lzw compression type (lossless)
		/// </summary>
		Lzw = 2
	}
}
