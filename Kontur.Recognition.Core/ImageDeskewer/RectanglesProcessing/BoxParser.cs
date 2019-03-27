using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kontur.Recognition.ImageDeskewer.RectanglesProcessing
{
    /// <summary>
    /// Find clasters of black points on binary image
    /// </summary>
    class BoxParser
    {
        private readonly int[] dx = { 1, 1, 1, 0, 0, -1, -1, -1 },
                  dy = { 0, 1, -1, -1, 1, 0, 1, -1 };

        private readonly Queue<Point> queue = new Queue<Point>();
        private readonly bool[,] image;
        private readonly int height, width;

        /// <summary>
        /// Bounding Boxes on image
        /// </summary>
        public Rectangle[] Boxes { get; private set; }

        /// <summary>
        /// Find clasters of black points on binary image
        /// </summary>
        /// <param name="binaryImage">Binary image. True - black, false - white</param>
        public BoxParser(bool[,] binaryImage)
        {

            image = binaryImage.Clone() as bool[,];
            height = binaryImage.GetUpperBound(1) + 1;
            width = binaryImage.GetUpperBound(0) + 1;
            Boxes = FindBoxes();
        }

        /// <summary>
        /// Find bounding Boxes with BFS
        /// </summary>
        /// <returns>Array of bounding Boxes</returns>
        private Rectangle[] FindBoxes()
        {
            var result = new List<Rectangle>();

            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    if (image[x, y])
                    {
                        queue.Enqueue(new Point(x, y));
                        image[x, y] = false;
                        var box = FindBoundingBox();
                        result.Add(box);
                    }
                }
            return result.ToArray();
        }

        /// <summary>
        /// Make bfs currentPoint first points in queue until it find bounding box
        /// </summary>
        /// <returns>Bounding box</returns>
        private Rectangle FindBoundingBox()
        {
            int minX = width, minY = height, maxX = 0, maxY = 0;
            while (queue.Any())
            {
                var now = queue.Dequeue();
                
                if (now.X < minX)
                    minX = now.X;
                if (now.X > maxX)
                    maxX = now.X;
                if (now.Y < minY)
                    minY = now.Y;
                if (now.Y > maxY)
                    maxY = now.Y;
                
                Mark(now);
                
            }
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Mark current and neighbour points
        /// </summary>
        /// <param name="currentPoint">Current point</param>
        private void Mark(Point currentPoint)
        {
            for (int k = 0; k < 8; ++k)
            {

                int x = currentPoint.X + dx[k];
                int y = currentPoint.Y + dy[k];
                if (x < 0 || y < 0 || x >= width || y >= height)
                    break;
                if (image[x, y])
                {
                    queue.Enqueue(new Point(x, y));
                    image[x, y] = false;
                }
            }
        }
    }
}