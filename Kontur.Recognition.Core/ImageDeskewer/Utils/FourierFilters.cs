using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Kontur.Recognition.ImageDeskewer.Utils
{
    /// <summary>
    /// Filters to modify image in frequency domain
    /// </summary>
    static class FourierFilters
    {
        /// <summary>
        /// Two-dimension fourier transform
        /// </summary>
        /// <param name="matrix">Square matrix with side with power of to size</param>
        /// <param name="width">Width of side of matrix</param>
        /// <param name="inverse">Is it inverse fourier transform?</param>
        /// <returns>Transformed image</returns>
        public static Complex[][] TwoDFft(Complex[][] matrix, int width, bool inverse)
        {
            Parallel.For(0, width, i =>
            {
                matrix[i] = Fft(matrix[i], inverse);
            });


            TransposeSquareMatrix(matrix);

            Parallel.For(0, width, i =>
            {
                matrix[i] = Fft(matrix[i], inverse);
            });


            TransposeSquareMatrix(matrix);

            return matrix;
        }

        /// <summary>
        /// Low Pass Filter
        /// </summary>
        /// <param name="fourierResult">Fourier transformed image</param>
        /// <returns></returns>
        public static Complex[][] LowPassFilter(Complex[][] fourierResult)
        {
            const int cutTreashold = 520;
            int width = fourierResult.Count();
            int center = width/2;

            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int dx = i - width / 2;
                    int dy = j - width / 2;

                    var dist = Math.Sqrt(center * center + center * center) - Math.Sqrt(dx * dx + dy * dy);

                    var mult = ButterworthFunction(dist, cutTreashold);
                    fourierResult[i][j] *= mult;
                }
            }
            return fourierResult;
        }

        /// <summary>
        /// Filter angles that doesn't multiplies Pi/2
        /// </summary>
        /// <param name="fourierResult">Fourier transformed image</param>
        /// <returns>Filtered image on frequency domain</returns>
        public static Complex[][] AnglesFilter(Complex[][] fourierResult)
        {
            const int cutTreashold = 64;
            int width = fourierResult.Count();
            int center = width / 2;
            const int maxDist = 10;

            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int dx = i - width / 2;
                    int dy = j - width / 2;

                    var dist = Math.Min(Math.Abs(dx) - maxDist, Math.Abs(dy) - maxDist);
                    if (Math.Sqrt(dx * dx + dy * dy) > center)
                        continue;

                    var mult = ButterworthFunction(dist, cutTreashold);
                    fourierResult[i][j] *= mult;
                }
            }
            return fourierResult;
        }

        /// <summary>
        /// Rude Hight Pass Filter
        /// </summary>
        /// <param name="fourierResult">Fourier transformed image</param>
        /// <returns>Filtered image on frequency domain</returns>
        public static Complex[][] HightPassFilter(Complex[][] fourierResult)
        {
            var magnitudes = fourierResult.SelectMany(r => r.Select(c => c.Magnitude)).ToArray();
            var avg = magnitudes.Average();
            int width = fourierResult.Count();
            const int windowSize = 10;

            for (int i = -windowSize / 2; i < windowSize / 2; ++i)
            {
                for (int j = -windowSize / 2; j < windowSize / 2; ++j)
                {
                    fourierResult[width / 2 + i][width / 2 + j] = avg;
                }
            }

            return fourierResult;
        }

        /// <summary>
        /// Convert image colors to complexes and center it for Fourier transform
        /// </summary>
        /// <param name="colors">Colors to transform</param>
        /// <param name="width">Image width</param>
        /// <returns>Complex for transformation</returns>
        public static Complex[][] ConvertToCenteringComplex(byte[] colors, int width)
        {
            var result = new Complex[width][];
            for (int i = 0; i < width; ++i)
                result[i] = new Complex[width];

            for (int y = 0; y < width; ++y)
            {
                int row = y * width;
                for (int x = 0; x < width; ++x)
                {
                    result[x][y] = colors[row + x];
                    if ((y + x) % 2 != 0)
                        result[x][y] *= -1;
                }
            }
            return result;
        }

        /// <summary>
        /// Convert centered complexes after inverse Fourier transform to  image colors
        /// </summary>
        /// <param name="transformResults">Results of inverse Fourier transform</param>
        /// <returns></returns>
        public static byte[] ConvertFromCenteringComplex(Complex[][] transformResults)
        {
            int width = transformResults.Count();
            var result = new byte[width * width];
            for (int y = 0; y < width; ++y)
            {
                int row = y * width;
                for (int x = 0; x < width; ++x)
                {
                    if ((y + x) % 2 != 0)
                        transformResults[x][y] *= -1;
                    result[row + x] = (byte)Math.Min(transformResults[x][y].Real + 0.5, 255);
                }
            }
            return result;
        }

        private static Complex[] Fft(Complex[] points, bool invert = false)
        {
            var outComplex = points;
            int n = outComplex.Count();

            for (int i = 1, j = 0; i < n; ++i)
            {
                int bit = n >> 1;
                for (; j >= bit; bit >>= 1)
                    j -= bit;
                j += bit;
                if (i < j)
                {
                    Complex swap = outComplex[i];
                    outComplex[i] = outComplex[j];
                    outComplex[j] = swap;
                }
            }

            double exp = invert ? -2 * Math.PI : 2 * Math.PI;
            for (int len = 2; len <= n; len <<= 1)  //Пересчёт углов заранее не даёт прироста в скорости
            {
                double ang = exp / len;
                var wlen = new Complex(Math.Cos(ang), Math.Sin(ang));
                int len2 = len >> 1;
                for (int i = 0; i < n; i += len)
                {
                    Complex w = 1;
                    for (int j = 0, ind1 = len2 + i; j < len2; ++j, ++ind1)
                    {
                        Complex u = outComplex[i + j], v = outComplex[ind1] * w;
                        outComplex[i + j] = (u + v);
                        outComplex[ind1] = (u - v);
                        w *= wlen;
                    }
                }
            }


            if (invert)
            {
                for (int i=0; i<n; ++i)
                    outComplex[i] /= n;
            }
            return outComplex;
        }

        private static void TransposeSquareMatrix<T>(T[][] matrix)
        {
            int width = matrix.Count();
            for (int i = 0; i < width; ++i)
            {
                for (int j = i + 1; j < width; ++j)
                {
                    T swap = matrix[j][i];
                    matrix[j][i] = matrix[i][j];
                    matrix[i][j] = swap;
                }
            }
        }

        private static double ButterworthFunction(double dist, double sliceDist)
        {
            if (dist < sliceDist)
                return 1;
            const int exp = 2 * 2;
            return 1 / Math.Pow(1 + dist / sliceDist, exp);
        }

    }
}
