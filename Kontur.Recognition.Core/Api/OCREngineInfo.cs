using System;

namespace Kontur.Recognition.Api
{
	public class OCREngineInfo
	{
		/// <summary>
		/// Name of OCR engine
		/// </summary>
		public string EngineName { get; set; }

		/// <summary>
		/// How many pages can be recognized with this engine
		/// </summary>
		public int VolumePagesRemaining { get; set; }

		/// <summary>
		/// Total number of pages assigned to this engine in accordance with current license
		/// </summary>
		public int VolumePagesTotal { get; set; }

		/// <summary>
		/// Date of license expiration (engine can not be used after that date)
		/// </summary>
		public DateTime LicenseEndDate { get; set; }

		/// <summary>
		/// String identifier of the license used by the engine
		/// </summary>
		public string LicenseID{ get; set; }

		public override string ToString()
		{
			return string.Format("EngineName: {0}, VolumePagesRemaining: {1}, LicenseEndDate: {2}, LicenseID: {3}", EngineName, VolumePagesRemaining, LicenseEndDate, LicenseID);
		}
	}
}