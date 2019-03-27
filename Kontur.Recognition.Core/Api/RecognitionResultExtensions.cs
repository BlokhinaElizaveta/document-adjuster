using Kontur.Recognition.GeometryModel;

namespace Kontur.Recognition.Api
{
	public static class RecognitionResultExtensions
	{
		/// <summary>
		/// Performs deskew procedure on given recognition results if needed
		/// </summary>
		/// <param name="source">The results of OCR to process</param>
		/// <returns>Recognition results which are guaranteed to be deskewed</returns>
		public static RecognitionResult Deskew(this RecognitionResult source)
		{
			if (source.IsDeskewed)
			{
				return source;
			}

			var deskewParams = source.GeometryModel.DetectDeskewParameters();
			var modelDeskewed = source.GeometryModel.DeskewModel(deskewParams);
			modelDeskewed = modelDeskewed.RemoveEmptyElements();
			return new RecognitionResult(modelDeskewed, deskewParams);
		}

		/// <summary>
		/// Scales given recognition results by specified factor
		/// </summary>
		/// <param name="source">The results of OCR to process</param>
		/// <param name="scaleFactor"></param>
		/// <returns>Recognition results which are guaranteed to be deskewed</returns>
		public static RecognitionResult Scale(this RecognitionResult source, double scaleFactor)
		{
			var targetModel = source.GeometryModel.ScaleModel(scaleFactor);
			var deskewParameters = source.DeskewParameters;
			if (deskewParameters != null)
			{
				var sourceBoundingBoxScaled = DeskewProcessor.ScaleBoundingBox(new BoundingBox(0, 0, deskewParameters.SourceWidth, deskewParameters.SourceHeight), scaleFactor);
				deskewParameters = new DeskewParameters(sourceBoundingBoxScaled.Width, sourceBoundingBoxScaled.Height, deskewParameters.AngleRadians);
				return new RecognitionResult(targetModel, deskewParameters);
			}
			return new RecognitionResult(targetModel);
		}


		/// <summary>
		/// Restores geometry model related to source image by given recognition results
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static TextGeometryModel SourceModel(this RecognitionResult source)
		{
			if (!source.IsDeskewed)
			{
				return source.GeometryModel;
			}
			var deskewParams = source.DeskewParameters;
			var transformRev = deskewParams.Transform.Reverse();
			var sourceModel = source.GeometryModel.RotateModel(transformRev, deskewParams.SourceWidth, deskewParams.SourceHeight);
			return sourceModel;
		}
	}
}