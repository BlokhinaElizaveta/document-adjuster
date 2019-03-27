using System;
using Kontur.Recognition.ImageCore;

namespace Kontur.Recognition.ImageBinarizer
{

	/// <summary>
	/// This class implements algorithm for fast adaptive binarization.
	/// </summary>
	public class WhiteLocalThresholdBinarizer
    {
		// These default values were chosen according to experimental data
		private const int radiusChangeStep = 5;
		private const double defaultBias = 1.1;
		private const int defaultWindowSize = 25;
		private const double defaultLowVarianceThreshold = 169;

		/// <summary>
		/// Size of window which is used to calculate local threshold for binarization
		/// </summary>
		private readonly int windowSize;

		/// <summary>
		/// Threshold bias (as we assume that background contains more points than foreground)
		/// </summary>
        private readonly double bias;

		/// <summary>
		/// The threshold of contrast (to avoid binarization of areas with too little difference between foreground and background)
		/// </summary>
        private readonly double lowVarianceThreshold;

	    /// <summary>
	    /// Creates an instance of binarizer with optimal parameters (calculated in comparative tests)
	    /// </summary>
	    /// <returns></returns>
	    public static WhiteLocalThresholdBinarizer DefaultInstance()
	    {
		    return new WhiteLocalThresholdBinarizer(1.1, 25, 13);
	    }

		/// <summary>
		/// Creates an instance of adaptive image binarizer with specified processing parameters
		/// </summary>
		/// <param name="bias">The bias to compensate difference in number of foreground and background 
		/// points while searching for a threshold between black and white</param>
		/// <param name="windowSize">The size of a window in which the points will be analized to choose 
		/// black/white color threshold</param>
		/// <param name="contrastThreshold">Specifies the threshold for color difference
		/// which is considered enough to separate black and white colors</param>
		public WhiteLocalThresholdBinarizer(double? bias, int? windowSize, int? contrastThreshold = null)
        {
			this.bias = bias ?? defaultBias;
            this.windowSize = (windowSize ?? defaultWindowSize) | 1;
            lowVarianceThreshold = contrastThreshold.HasValue ? (contrastThreshold.Value * contrastThreshold.Value) : defaultLowVarianceThreshold;
        }

        /// <summary>
        /// Creates a new binarized copy of the given image
        /// </summary>
        /// <param name="sourceImage">The image to process</param>
        /// <returns></returns>
        public KrecImage BinarizeImage(KrecImage sourceImage)
        {
	        var image = sourceImage.ToGrayscaled(true);

			int bytesPerLine = image.BytesPerLine;
            int width = image.Width;
            int height = image.Height;
	        byte[] imageData = image.ImageData;
			byte[] resultData = new byte[imageData.Length];
            int widthM1 = width - 1;
            int heightM1 = height - 1;
            int maxRadius = Math.Min(width/2, height/2);

            int radius = windowSize / 2;

            uint[,] integralCopy = GetIntegralCopy(image);
            uint[,] squareCopy = GetSquareCopy(image);

            for (int y = 0; y < height; y++)
            {
				var localRadius = radius;
				var minY = (y - radius) <= 0 ? 0 : y - radius;
                var maxY = (y + radius) >= height ? heightM1 : y + radius;
                for (int x = 0; x < width; x++)
                {
                    var minX = (x - radius) <= 0 ? 0 : x - radius;
                    var maxX = (x + radius) >= width ? widthM1 : x + radius;
                    var integralMean = GetRectangleMean(minX, minY, maxX, maxY, integralCopy);
                    var variance = GetRectangleMean(minX, minY, maxX, maxY, squareCopy) - integralMean * integralMean;
                    double mean;
	                if (variance > lowVarianceThreshold)
	                {
						// Scanned area contains enough points (both from background and foreground) to make decision on binarization threshold
		                localRadius = radius;
		                mean = integralMean;
	                }
	                else
	                {
						// Scanned area is to be extended as there is not enough points of different brightness
		                var localMinX = minX;
		                var localMaxX = maxX;
		                var localMinY = minY;
		                var localMaxY = maxY;
		                while (variance < lowVarianceThreshold && localRadius < maxRadius)
		                {
			                localRadius += radiusChangeStep;
			                localMinX = (x - localRadius) <= 0 ? 0 : x - localRadius;
			                localMaxX = (x + localRadius) >= width ? widthM1 : x + localRadius;
			                localMinY = (y - localRadius) <= 0 ? 0 : y - localRadius;
			                localMaxY = (y + localRadius) >= height ? heightM1 : y + localRadius;
			                integralMean = GetRectangleMean(localMinX, localMinY, localMaxX, localMaxY, integralCopy);
			                variance = GetRectangleMean(localMinX, localMinY, localMaxX, localMaxY, squareCopy) -
			                           integralMean*integralMean;
		                }
		                mean = GetRectangleMean(localMinX, localMinY, localMaxX, localMaxY, integralCopy);
		                localRadius -= 2*radiusChangeStep;
	                }
	                var index = y* bytesPerLine + x;
	                resultData[index] = (byte)((imageData[index] * bias < mean) ? 0 : 255);
                }
			}
			return new KrecImage(image, resultData);
        }

		/// <summary>
		/// Builds integral matrix to claculate sums by given rectangular range
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
	    private static uint[,] GetIntegralCopy(KrecImage image)
        {
            var width = image.Width;
            var height = image.Height;
	        var bytesPerLine = image.BytesPerLine;
	        var imageData = image.ImageData;
            var numArray = new uint[height + 1, width + 1];
            for (var y = 0; y < height; y++)
            {
                uint lineCurrentSum = 0U;
                for (var x = 0; x < width; x++)
                {
					lineCurrentSum += imageData[y * bytesPerLine + x];
                    numArray[y + 1, x + 1] = numArray[y, x + 1] + lineCurrentSum;
                }
            }
            return numArray;
        }

		/// <summary>
		/// Builds integral matrix to claculate sums of squares by given rectangular range
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		private static uint[,] GetSquareCopy(KrecImage image)
        {
			var width = image.Width;
			var height = image.Height;
	        var bytesPerLine = image.BytesPerLine;
			var imageData = image.ImageData;
			var numArray = new uint[height + 1, width + 1];
			for (var y = 0; y < height; y++)
            {
                uint lineCurrentSum = 0U;
                for (var x = 0; x < width; x++)
                {
					uint curElem = imageData[y * bytesPerLine + x];
                    lineCurrentSum += curElem * curElem;
                    numArray[y + 1, x + 1] = lineCurrentSum + numArray[y, x + 1];
                }
            }
            return numArray;
        }

	    private static double GetRectangleMean(int x1, int y1, int x2, int y2, uint[,] integralCopy)
        {
            x2++;
            y2++;
            return (integralCopy[y2, x2] + integralCopy[y1, x1] - integralCopy[y2, x1] - integralCopy[y1, x2]) / (double)((x2 - x1) * (y2 - y1));
        }

        public override string ToString()
        {
            return "WhiteLocalThreshold: bias = " + bias + ", window = " + windowSize;
        }
    }
}
