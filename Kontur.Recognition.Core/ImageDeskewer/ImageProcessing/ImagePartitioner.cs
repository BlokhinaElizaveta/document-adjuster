using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kontur.Recognition.ImageDeskewer.ImageProcessing
{
    /// <summary>
    /// Find disjoint parts that divides by monotone lines
    /// </summary>
    static class ImagePartitioner
    {

        #region Unused in current version
        /// <summary>
        /// Get image partitioning by monotone lines. Unused in this version.
        /// </summary>
        /// <param name="binary">Binary image</param>
        /// <param name="maxStepSize">Max parts side size</param>
        /// <returns>Rectangles with parts of image</returns>
        public static Rectangle[] GetImagePartitioning(bool[,] binary, int maxStepSize)
        {
            int width = binary.GetUpperBound(0) + 1;
            int height = binary.GetUpperBound(1) + 1;
            var resultPartitioning = new List<Rectangle>();
            var horizontalLines = FindHorizontalSeparatingLines(binary);

            var verticalPartitioning = GetImagePartitioningByVertical(horizontalLines, maxStepSize, new Rectangle(0, 0, width, height));
            foreach (var part in verticalPartitioning)
            {
                var vertLines = FindVerticalSeparatingLines(binary, part.Top, part.Bottom);
                var horizontalPartitioning = GetImagePartitioningByHorizontal(vertLines, maxStepSize, part);
                resultPartitioning.AddRange(horizontalPartitioning);

            }

            return resultPartitioning.ToArray();
        }


        private static Rectangle[] FindHorizontalSeparatingLines(bool[,] binary, int startX = 0, int endX = int.MaxValue)   //TODO добавить для кусочно-монотонных линий
        {
            var separatingLines = new List<Rectangle>();
            int height = binary.GetUpperBound(1) + 1;
            int width = Math.Min(binary.GetUpperBound(0) + 1, endX);

            int y = 0;
            while (y < height)
            {
                var separatingLine = FindHorizontalMonotoneLine(binary, startX, y, width, height);

                if (separatingLine == null)
                {
                    ++y;
                    continue;
                }

                separatingLines.Add((Rectangle)separatingLine);
                y = separatingLine.Value.Y + separatingLine.Value.Height;
            }
            

            return separatingLines.ToArray();
        }

        private static Rectangle[] GetImagePartitioningByVertical(Rectangle[] horizontalSeparatingLines, int maxStepSize, Rectangle place)
        {
            int count = horizontalSeparatingLines.Count();
            var result = new List<Rectangle>();

            if (count == 0)
            {
                result.Add(place);
                return result.ToArray();
            }

            int startY = 0;
            if (horizontalSeparatingLines[0].Y == place.Y)  //отсекаем пустые области сверху и снизу листа
                startY = horizontalSeparatingLines[0].Bottom;

            for (int i = 0; i < count - 1; ++i)
            {
                if (horizontalSeparatingLines[i + 1].Top - startY > maxStepSize && horizontalSeparatingLines[i].Bottom - startY != 0)
                {
                    result.Add(new Rectangle(0, startY, place.Width, horizontalSeparatingLines[i].Top - startY));
                    startY = horizontalSeparatingLines[i].Bottom;
                }
            }

            if (horizontalSeparatingLines[count - 1].Bottom == place.Height)
                result.Add(new Rectangle(place.X, startY, place.Width, horizontalSeparatingLines[count - 1].Top - startY));
            else
                result.Add(new Rectangle(place.X, startY, place.Width, place.Height - startY));

            return result.ToArray();
        }

        private static Rectangle[] FindVerticalSeparatingLines(bool[,] binary, int startY = 0, int endY = int.MaxValue)   //TODO добавить для кусочно-монотонных линий
        {
            var separatingLines = new List<Rectangle>();
            int height = Math.Min(binary.GetUpperBound(1) + 1, endY);
            int width = binary.GetUpperBound(0) + 1;

            int x = 0;
            while (x < width)
            {
                var separatingLine = FindVerticalMonotoneLine(binary, x, startY, width, height);

                if (separatingLine == null)
                {
                    ++x;
                    continue;
                }

                separatingLines.Add((Rectangle)separatingLine);
                x = separatingLine.Value.X + separatingLine.Value.Width;
            }

            return separatingLines.ToArray();
        }

        private static Rectangle[] GetImagePartitioningByHorizontal(Rectangle[] verticalSeparatingLines, int maxStepSize, Rectangle place)
        {
            int count = verticalSeparatingLines.Count();
            var result = new List<Rectangle>();

            if (count == 0)
            {
                result.Add(place);
                return result.ToArray();
            }

            int startX = 0;
            if (verticalSeparatingLines[0].X == place.X)  //отсекаем пустые области сверху и снизу листа
                startX = verticalSeparatingLines[0].Right;


            for (int i = 0; i < count - 1; ++i)
            {
                if (verticalSeparatingLines[i + 1].Left - startX > maxStepSize && verticalSeparatingLines[i].Right - startX != 0)
                {
                    result.Add(new Rectangle(startX, place.Y, verticalSeparatingLines[i].Left - startX, place.Height));
                    startX = verticalSeparatingLines[i].Right;
                }
            }

            if (verticalSeparatingLines[count - 1].Right == place.Width)
            {
                if (count == 1)
                    return result.ToArray();
                result.Add(new Rectangle(startX, place.Y, verticalSeparatingLines[count - 1].Left - startX, place.Height));
            }
            else
                result.Add(new Rectangle(startX, place.Y, place.Width - startX, place.Height));

            return result.ToArray();
        }
        #endregion

        /// <summary>
        /// Get meaning part of image
        /// </summary>
        /// <param name="binary">Binary image</param>
        /// <returns>Rectangle which bounds meaning part of image</returns>
        public static Rectangle GetImageWithoutBorders(bool[,] binary)
        {
            int width = binary.GetUpperBound(0) + 1;
            int height = binary.GetUpperBound(1) + 1;

            var leftBorder = FindVerticalMonotoneLine(binary, 0, 0, width, height) ?? new Rectangle(0, 0, 0, height);
	        if (leftBorder.Right == width)
		        throw new BadImageFormatException("White image");
            var rightBorder = FindRightVerticalMonotoneLine(binary, width - 1, 0, width, height) ?? new Rectangle(width - 1, 0, 0, height);

            var topBorder = FindHorizontalMonotoneLine(binary, leftBorder.Right, 0, rightBorder.Left - leftBorder.Right, height) ??
                            new Rectangle(leftBorder.Right, 0, rightBorder.Left - leftBorder.Right, 0);

            var bottomBorder = FindBottomHorizontalMonotoneLine(binary, leftBorder.Right, height - 1, rightBorder.Left - leftBorder.Right, height) ??
                new Rectangle(leftBorder.Right, height - 1, rightBorder.Left - leftBorder.Right, 0);

	        if (bottomBorder.Top == 0)
		        throw new BadImageFormatException("White image");

			return new Rectangle(leftBorder.Right, topBorder.Bottom, rightBorder.Left - leftBorder.Right + 1, bottomBorder.Top - topBorder.Bottom + 1);
        }

        private static Rectangle? FindVerticalMonotoneLine(bool[,] binary, int startX, int startY, int width, int height)
        {
            int lineWidth = 0;
            int endX = startX + width;
            int endY = startY + height;
            for (int x = startX; x < endX; ++x)
            {
                bool lineColor = binary[x, startY];
                for (int y = startY + 1; y < endY; ++y)
                {
                    if (binary[x, y] == lineColor)
                        continue;

                    if (lineWidth != 0)
                        return new Rectangle(x - lineWidth, startY, lineWidth, endY - startY);

                    return null;
                }
                ++lineWidth;
            }
            if (lineWidth != 0)
                return new Rectangle(endX - lineWidth, startY, lineWidth, endY - startY);

            return null;
        }

        private static Rectangle? FindHorizontalMonotoneLine(bool[,] binary, int startX, int startY, int width, int height)
        {
            int lineHeight = 0;
            int endX = startX + width;
            int endY = startY + height;
            for (int y = startY; y < endY; ++y)
            {
                bool lineColor = binary[startX, y];
                for (int x = startX + 1; x < endX; ++x)
                {
                    if (binary[x, y] == lineColor)
                        continue;

                    if (lineHeight != 0)
                        return new Rectangle(startX, y - lineHeight, endX - startX, lineHeight);

                    return null;
                }

                ++lineHeight;
            }
            if (lineHeight != 0)
                return new Rectangle(startX, endY - lineHeight, endX - startX, lineHeight);

            return null;
        }


        private static Rectangle? FindRightVerticalMonotoneLine(bool[,] binary, int startX, int startY, int width, int height)
        {
            int lineWidth = 0;
            int endX = startX - width;
            int endY = startY + height;
            for (int x = startX; x > endX; --x)
            {
                bool lineColor = binary[x, startY];
                for (int y = startY + 1; y < endY; ++y)
                {
                    if (binary[x, y] == lineColor)
                        continue;

                    if (lineWidth != 0)
                        return new Rectangle(x, startY, lineWidth, endY - startY);

                    return null;
                }
                ++lineWidth;
            }
            if (lineWidth != 0)
                return new Rectangle(endX + 1, startY, lineWidth, endY - startY);

            return null;
        }

        private static Rectangle? FindBottomHorizontalMonotoneLine(bool[,] binary, int startX, int startY, int width, int height)
        {
            int lineHeight = 0;
            int endX = startX + width;
            int endY = startY - height;
            for (int y = startY; y > endY; --y)
            {
                bool lineColor = binary[startX, y];
                for (int x = startX + 1; x < endX; ++x)
                {
                    if (binary[x, y] == lineColor)
                        continue;

                    if (lineHeight != 0)
                        return new Rectangle(startX, y, endX - startX, lineHeight);

                    return null;
                }

                ++lineHeight;
            }
            if (lineHeight != 0)
                return new Rectangle(startX, endY + 1, endX - startX, lineHeight);

            return null;
        }
    }
}
