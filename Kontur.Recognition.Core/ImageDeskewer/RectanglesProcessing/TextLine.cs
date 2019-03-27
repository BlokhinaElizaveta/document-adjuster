using System;
using System.Drawing;
using System.Linq;

namespace Kontur.Recognition.ImageDeskewer.RectanglesProcessing
{
    /// <summary>
    /// Contains line of chars and box, that bounds it
    /// </summary>
    [Serializable]
    internal class TextLine
    {
		/// <summary>
		/// Chars in line
		/// </summary>
		public Rectangle[] Chars { get; private set; }
		/// <summary>
		/// Rectangle that bound all chars in line
		/// </summary>
		public Rectangle BoundingRect { get; private set; }

        /// <summary>
        /// Array of chars
        /// </summary>
        /// <param name="chars"></param>
        public TextLine(Rectangle[] chars)
        {
            Chars = chars;
			BoundingRect = Union(chars);
        }

        /// <summary>
        /// Single char
        /// </summary>
        /// <param name="ch"></param>
        public TextLine(Rectangle ch)
        {
            Chars = new[] { ch };
            BoundingRect = ch;
        }

        /// <summary>
        /// Add char to text line
        /// </summary>
        /// <param name="ch">Added char</param>
        public void AddChar(Rectangle ch)
        {
            Chars = Chars.Union(new[] { ch }).ToArray();
            BoundingRect = Union(BoundingRect, ch);
        }

		/// <summary>
		/// Find union of two rectangles
		/// </summary>
		/// <returns></returns>
		private static Rectangle Union(Rectangle[] rectangles)
		{
			var minX = int.MaxValue;
			var minY = int.MaxValue;
			var maxX = int.MinValue;
			var maxY = int.MinValue;

			foreach (var rectangle in rectangles)
			{
				minX = Math.Min(minX, rectangle.Left);
				minY = Math.Min(minY, rectangle.Top);
				maxX = Math.Max(maxX, rectangle.Right);
				maxY = Math.Max(maxY, rectangle.Bottom);
			}
			return new Rectangle(minX, minY, maxX - minX, maxY - minY);
		}


        /// <summary>
        /// Find union of two rectangles
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        private static Rectangle Union(Rectangle r1, Rectangle r2)
        {
            var x = Math.Min(r1.X, r2.X);
            var y = Math.Min(r1.Y, r2.Y);
            var w = Math.Max(r1.Right, r2.Right) - x;
            var h = Math.Max(r1.Bottom, r2.Bottom) - y;
            return new Rectangle(x, y, w, h);
        }
    }
}
