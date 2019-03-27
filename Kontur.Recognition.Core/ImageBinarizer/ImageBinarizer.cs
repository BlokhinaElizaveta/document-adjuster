using Kontur.Recognition.ImageCore;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.ImageBinarizer
{
	public static class ImageBinarizer
	{
		public static KrecImage Binarize([NotNull] this KrecImage image)
		{
			return WhiteLocalThresholdBinarizer.DefaultInstance().BinarizeImage(image);
		}
	}
}
