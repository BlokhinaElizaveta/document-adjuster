using System.Collections.Generic;

namespace DocumentAdjuster.Services
{
    internal class MedianFilterService:IMedianFilterService
    {
        public int[,] Apply(int[,] image, int radius)
        {
            var width = image.GetLength(0);
            var height = image.GetLength(1);
            var result = new int[width, height];
            var size = radius * 2 + 1;

            for (var x = radius; x < width - radius; x++)
            {
                for (var y = radius; y < height - radius; y++)
                {
                    var neighborhood = new List<int>();
                    for (var i = 0; i < size; i++)
                    {
                        for (var j = 0; j < size; j++)
                            neighborhood.Add(image[x + j - radius, y + i - radius]);
                    }

                    neighborhood.Sort();
                    var median = neighborhood[neighborhood.Count / 2];
                    result[x, y] = median;
                }                 
            }

            return result;
        }
    }
}
