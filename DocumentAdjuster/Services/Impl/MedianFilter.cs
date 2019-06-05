using System.Collections.Generic;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal class MedianFilter : IMedianFilter
    {
        public KrecImage Apply(KrecImage image, int radius)
        {
            var width = image.Width;
            var height = image.Height;
            var result = new KrecImage(image, new byte[image.ImageData.Length]);
            var size = radius * 2 + 1;

            for (var lineIdx = radius; lineIdx < height - radius; lineIdx++)
            {
                for (var counter = radius; counter < width - radius; counter++)
                {
                    var neighborhood = new List<byte>();
                    for (var i = 0; i < size; i++)
                    {
                        for (var j = 0; j < size; j++)
                        {
                            var y = lineIdx + i - radius;
                            var sourceIdx = y * image.BytesPerLine + counter;
                            neighborhood.Add(image.ImageData[sourceIdx + j - radius]);
                        }
                    }

                    neighborhood.Sort();
                    var median = neighborhood[neighborhood.Count / 2];
                    result.ImageData[lineIdx * image.BytesPerLine + counter] = median;
                }
            }

            return result;
        }
    }
}
