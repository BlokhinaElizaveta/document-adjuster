using Kontur.Recognition.GeometryModel;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Api
{
	/// <summary>
	/// Holds results of OCR recognition. Depending on capabilities of OCR engine resulting model can be built relative to original image (not deskewed)
	/// or relative to deskewed image. In latter case information on transformation which was used to deskew image is provided. 
	/// </summary>
	public class RecognitionResult
	{
		// Holds geometric model which was produced by OCR engine (either deskewed or not)
		private TextGeometryModel geometryModel;
		// Whether stored model is a deskewed one
		private bool isDeskewed;
		// When storing deskewed model, this field holds deskew transformation parameters
		private DeskewParameters deskewParameters;

		public TextGeometryModel GeometryModel { get { return geometryModel; } }
		public bool IsDeskewed { get { return isDeskewed; } }
		public DeskewParameters DeskewParameters { get { return deskewParameters; } }

		/// <summary>
		/// Creates new instance of recognition results assuming that results are deskewed with given transformation
		/// </summary>
		/// <param name="geometryModel">The model to store</param>
		/// <param name="deskewParameters">The set of deskew transformation parameters</param>
		public RecognitionResult([NotNull] TextGeometryModel geometryModel, [NotNull] DeskewParameters deskewParameters)
		{
			this.geometryModel = geometryModel;
			this.deskewParameters = deskewParameters;
			isDeskewed = true;
		}

		/// <summary>
		/// Creates new instance of recognition results assuming that results are not deskewed
		/// </summary>
		/// <param name="geometryModel"></param>
		public RecognitionResult(TextGeometryModel geometryModel)
		{
			this.geometryModel = geometryModel;
			deskewParameters = null;
			isDeskewed = false;
		}
	}
}