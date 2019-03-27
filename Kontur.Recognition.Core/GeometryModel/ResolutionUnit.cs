namespace Kontur.Recognition.GeometryModel
{
	/// <summary>
	/// Defines possible resolution unit. Image model contains resolution information in this units
	/// </summary>
	public enum ResolutionUnit
	{
		/// <summary>
		/// Resolution unit is not known
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Resolution unit is pixels per inch
		/// </summary>
		PixelsPerInch = 1,

		/// <summary>
		/// Resolution unit is pixels per centimeter
		/// </summary>
		PixelsPerCentimeter = 2
	}
}