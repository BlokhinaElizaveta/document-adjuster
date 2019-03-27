using System;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Api
{
	/// <summary>
	/// Describes generic interface of optical recognition engine
	/// </summary>
	public interface IOCREngine : IDisposable
	{
		/// <summary>
		///  Performs recognition of single image (must be in image format like jpeg, png etc.)
		/// </summary>
		/// <param name="pathToImageFile">The file to process</param>
		/// <param name="featuresToEnable">Specifies features to enable for OCR engine</param>
		/// <returns>The recognised data</returns>
		RecognitionResult RecognizeImage([NotNull] string pathToImageFile, OCRFeatures featuresToEnable);

		/// <summary>
		/// Returns the unique identifier of OCR engine
		/// </summary>
		/// <returns></returns>
		string GetEngineId();

		/// <summary>
		/// Returns the name of OCR engine
		/// </summary>
		/// <returns></returns>
		string GetName();

		/// <summary>
		/// Returns meta information about OCR engine
		/// </summary>
		/// <returns></returns>
		OCREngineInfo GetEngineInfo();
	}
}