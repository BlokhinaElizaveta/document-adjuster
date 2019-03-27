using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	public class GMWord : GMElement
	{
		/// <summary>
		/// Text of this word
		/// </summary>
		[NotNull]
		public string Text { get; set; }

		/// <summary>
		/// If the word is obtained directly from document, accuracy is set to 100. If the word was 
		/// obtained from OCR, accuracy is set to mean value of accuracy of all recognized characters.
		/// This value allows to estimate whether word is reliable or not. Accuracy must be in range from 0 to 100 inclusive.
		/// </summary>
		public int Accuracy { get; private set; }

		public GMWord([NotNull] BoundingBox boundingBox, [NotNull] string text, int accuracy) 
			: base(boundingBox)
		{
			Text = text;
			Accuracy = accuracy;
		}

		public override string ToString()
		{
			return string.Format("\"{0}\" at {1}", Text, BoundingBox);
		}
	}
}