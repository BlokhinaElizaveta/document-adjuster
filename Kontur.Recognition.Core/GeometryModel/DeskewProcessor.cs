using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kontur.Recognition.GeometryModel
{
	/// <summary>
	/// Holds parameters of desckew transformation 
	/// Coordinates are represented in PDF page units (usually 1/72 of inch) multiplied to 1000000 (to use integer arithmetics)
	/// </summary>
	public static class DeskewProcessor
	{
		public static DeskewParameters DetectDeskewParameters(this TextGeometryModel textGeometryModel)
		{
			var anglesDetected = new List<double>();
			foreach (var line in textGeometryModel.Lines())
			{
				ProcessLine(line, anglesDetected);
			}
			var pageBBox = textGeometryModel.PageBox;
			var deskewAngle = CalculateDeskewAngle(anglesDetected);
			return new DeskewParameters(pageBBox.XMax, pageBBox.YMax, deskewAngle);
		}

		#region DeskewAngleDetectionLogic
		private static void ProcessLine(GMLine line, ICollection<double> anglesDetected)
		{
			var suitableWords = line.Words().Where(IsWordValidForAngleDetection).ToList();

			// To detect direction of given line we have to have at least two words in the line
			if (suitableWords.Count < 2)
			{
				return;
			}

			//var firstWord = suitableWords[0];
			double sum = 0;
			for (var idx = 1; idx < suitableWords.Count; idx++)
			{
				//var secondWord = suitableWords[idx];
				//var angle = DetectAngle(firstWord, secondWord);
				var angle = DetectAngle(suitableWords[idx - 1], suitableWords[idx]);
				sum += angle;
				//Console.Out.WriteLine("Angle: {0:N4}", Angles.RadToDeg(angle));
				anglesDetected.Add(angle);
			}
			//var avAngle = sum / (suitableWords.Count - 1);
			// Console.Out.WriteLine("Average angle: {0:N4}", Angles.RadToDeg(avAngle));
		}

		/// <summary>
		/// Set of "good" characters (which do not have elements below base line)
		/// </summary>
		private static readonly Regex validWordPattern = new Regex(@"^[0-9a-fhik-orxzA-Zàáâãå¸æçèéêëìíîïñòõ÷øúûüýþÿÀÁÂÃÅ¨ÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕ×ØÚÛÜÝÞß\.\+\-\ \""\'“”:’«»\%]+$");
		/// <summary>
		/// Set of "bad" characters (which in certain fonts may have elements below base line)
		/// </summary>
		private static readonly Regex invalidWordPattern = new Regex(@"^.*[\|\(\)äðóôöù\,ÄÖÙ\,_‚/\[\];]+.*$");

		private static bool IsWordValidForAngleDetection(GMWord word)
		{
			var result = validWordPattern.IsMatch(word.Text);
//			if (!result)
//			{
//				if (!invalidWordPattern.IsMatch(word.Text) && word.Text.Trim().Length > 0)
//				{
//					Console.Out.WriteLine("Rejected word: \"{0}\"", word.Text);
//				}
//			}
			return result;
		}

		/// <summary>
		/// Assuming that firstWord and secondWord are representing bounding boxes for words of the same line 
		/// (edges of bounding boxes are aligned to X and Y axes) whis method detects angle between 
		/// line and positive direction of X-axis.
		/// Angle is detected by direction of line connecting the centers of given bounding rectangles
		/// </summary>
		/// <param name="firstWord"></param>
		/// <param name="secondWord"></param>
		/// <returns>Angle in radians</returns>
		private static double DetectAngle(GMWord firstWord, GMWord secondWord)
		{
			var firstBBox = firstWord.BoundingBox;
			var secondBBox = secondWord.BoundingBox;

			// Page coordinates have origin in top-left corner, 
			// positive direction of Y-axis is oriented to down, 
			// positive direction of X-axis is oriented to right
			Func<BoundingBox, int> selectorX = (bbox => (bbox.XMin + bbox.XMax));
			Func<BoundingBox, int> selectorY = (bbox => (bbox.YMin + bbox.YMax));

			// Here are coordinates of vector of text direction
			var dirX = selectorX(secondBBox) - selectorX(firstBBox);
			var dirY = selectorY(secondBBox) - selectorY(firstBBox);

			dirY = -dirY; // To invert Y-axis direction
			double angle = (dirX >= 0) ? 0 : (dirY > 0) ? Math.PI : -Math.PI;
			if (dirX < 0)
			{
				dirX = -dirX;
				dirY = -dirY;
			}

			if (-dirX <= dirY && dirY <= dirX)
			{
				angle += Math.Atan(dirY / (double)dirX);
			}
			else if (dirY > 0)
			{
				angle += Math.PI / 2 - Math.Atan(dirX / (double)dirY);
			}
			else
			{
				angle += -Math.PI / 2 + Math.Atan(dirX / (double)dirY);
			}

			return angle;
		}

		/// <summary>
		/// Alternative approach to angle detection: this method uses line connecting those vertices of word's bounding rectangles which
		/// are the closest to base line of text.
		/// </summary>
		/// <param name="firstWord"></param>
		/// <param name="secondWord"></param>
		/// <returns>Angle in radians</returns>
		private static double DetectAngle3(GMWord firstWord, GMWord secondWord)
		{
			var firstBBox = firstWord.BoundingBox;
			var secondBBox = secondWord.BoundingBox;
			Func<BoundingBox, int> selectorX;
			Func<BoundingBox, int> selectorY;
			// Page coordinates have origin in top-left corner, 
			// positive direction of Y-axis is oriented to down, 
			// positive direction of X-axis is oriented to right
			if (secondBBox.XMax < firstBBox.XMax)
			{
				// Page is rotated upside-down (words in line go from right to left)
				if (firstBBox.YMin >= secondBBox.YMin)
				{
					// Text goes from down-right corner to upper-left corner
					selectorX = (bbox => bbox.XMin);
					selectorY = (bbox => bbox.YMin);
				}
				else
				{
					// Text goes from upper-right corner to down-left corner
					selectorX = (bbox => bbox.XMax);
					selectorY = (bbox => bbox.YMin);
				}
			}
			else
			{
				// Page is properly rotated (words in linego from left to right)
				if (firstBBox.YMax >= secondBBox.YMax)
				{
					// Text goes from down-left corner to upper-right corner
					selectorX = (bbox => bbox.XMin);
					selectorY = (bbox => bbox.YMax);
				}
				else
				{
					// Text goes from upper-left corner to down-right corner
					selectorX = (bbox => bbox.XMax);
					selectorY = (bbox => bbox.YMax);
				}
			}
			// Here are coordinates of vector of text direction
			var dirX = selectorX(secondBBox) - selectorX(firstBBox);
			var dirY = selectorY(secondBBox) - selectorY(firstBBox);
			dirY = -dirY;
			var dirLength = Math.Sqrt(dirX * dirX + dirY * dirY);
			//Console.Out.WriteLine("{0:N4}:{1:N4}", dirX / dirLength, dirY / dirLength);
			double angle = 0;
			if (dirX < 0)
			{
				if (dirY > 0)
				{
					angle = Math.PI;
				}
				else
				{
					angle = -Math.PI;
				}
				dirX = -dirX;
				dirY = -dirY;
			}
			if (-dirX <= dirY && dirY <= dirX)
			{
				angle += Math.Atan(dirY / (double)dirX);
			}
			else if (dirY > 0)
			{
				angle += Math.PI / 2 - Math.Atan(dirX / (double)dirY);
			}
			else
			{
				angle += -Math.PI / 2 + Math.Atan(dirX / (double)dirY);
			}
			return angle;
		}

		private const double angleMultiplier = 50 * 180 / Math.PI;

		/// <summary>
		/// Performs calculation of deskew angle based on distribution of detected angles. Returns the angle by which the image is rotated relative to normal orientation
		/// (i.e. to align the image in such a way that text lines are horizontal you need to negate the returned angle and rotate the image by that angle)
		/// </summary>
		/// <param name="anglesDetected"></param>
		/// <returns></returns>
		private static double CalculateDeskewAngle(ICollection<double> anglesDetected)
		{
			if (anglesDetected.Count == 0)
			{
				return 0;
			}
			var histogram = new Dictionary<int, int>();
			var maxCount = 0;
			foreach (var angle in anglesDetected)
			{
				var angleRounded = (int)Math.Round(angle * angleMultiplier);
				int currentCount;
				if (histogram.TryGetValue(angleRounded, out currentCount))
				{
					currentCount++;
					histogram[angleRounded] = currentCount;
					if (maxCount < currentCount)
					{
						maxCount = currentCount;
					}
				}
				else
				{
					currentCount = 1;
					histogram.Add(angleRounded, currentCount);
				}
			}
			var lowFilterBound = (int)Math.Round(maxCount * 0.8);
			var meanAngle = 0;
			var count = 0;
			foreach (var entry in histogram.Where(entry => (entry.Value >= lowFilterBound)))
			{
				count += entry.Value;
				meanAngle += entry.Key * entry.Value;
				//Console.Out.WriteLine("Dominating angle: {0:N4}, count: {1}", Angles.RadToDeg(entry.Key / angleMultiplier), entry.Value);
			}
			var result = (meanAngle / angleMultiplier) / count;

			//Console.Out.WriteLine("Mean angle: {0:N4}", Angles.RadToDeg(result));
			return result;
		}
#endregion

		/// <summary>
		/// Transforms given model by rotating it by specified set of parameters. If the set of parameters is not given (null) then it will be calculated
		/// in such a way that a resulting model contains text lines aligned strictly horizontally.
		/// </summary>
		/// <param name="model">The model to transform</param>
		/// <param name="deskewParameters">Set of transformation parameters (or null, if transformation should be determined automatically)</param>
		/// <returns>The transformed model</returns>
		public static TextGeometryModel DeskewModel(this TextGeometryModel model, DeskewParameters deskewParameters = null)
		{
			var desckewParams = deskewParameters ?? DetectDeskewParameters(model);
			var transform = desckewParams.Transform;
			// Console.Out.WriteLine(desckewParams);

			var targetModel = new TextGeometryModel(new BoundingBox(0, 0, desckewParams.TargetWidth, desckewParams.TargetHeight), model.GridUnit);
			ModelGeometryTransformer.TransformModelGeometry(model, targetModel,
				box => DeskewBoundingBox(box, transform));

			return targetModel;
		}

		private static BoundingBox DeskewBoundingBox(BoundingBox src, IsometricTransform transform)
		{
			var cosAngle = Math.Abs(transform.CosAngle);
			var sinAngle = Math.Abs(transform.SinAngle);
			if (cosAngle > sinAngle)
			{
				var tmp = sinAngle;
				sinAngle = cosAngle;
				cosAngle = tmp;
			}

			var sin2Angle = 2 * sinAngle * cosAngle;
			var sin2AngleSq = sin2Angle * sin2Angle;
			var cos2AngleSq = 1 - sin2AngleSq;

			var points = new List<Point>
			{
				transform.Transform(new Point(src.XMin, src.YMin)),
				transform.Transform(new Point(src.XMin, src.YMax)),
				transform.Transform(new Point(src.XMax, src.YMax)),
				transform.Transform(new Point(src.XMax, src.YMin)),
			};
			var xMin = points.Select(point => point.x).Min();
			var xMax = points.Select(point => point.x).Max();
			var yMin = points.Select(point => point.y).Min();
			var yMax = points.Select(point => point.y).Max();

			// Here we have a rectangle that is a bounding one for original bounding rectangle rotated
			// The problem here is that this rectangle is not the bounding one for the word itself (due to rotation)
			// so we have to updte it (if possible) to reduce its size
			var W = xMax - xMin;
			var H = yMax - yMin;
			var xCnt = (xMin + xMax) / 2;
			var yCnt = (yMin + yMax) / 2;

			var w = (W - sin2Angle * H) / cos2AngleSq;
			var h = (H - sin2Angle * W) / cos2AngleSq;

			// According to formulas, values of w and h should be positive 
			// but under some circumstances (line contains words in fonts of different sizes) they can become negative (as desckew formulas 
			// are obtained under assumption that line contains words of the same vertical size). So in this case we just make the values non-negative
			// (although the result will not be quite good - according to the resulting sizes words will be higher than the line itself)
			w = Math.Abs(w);
			h = Math.Abs(h);

			// New height and width are suitable for the case when rotation angle was chosen correctly to deskew the model
			// (i.e. the words have been aligned horizontally). Unfortunately, the can be the cases when the angle is a wrong one 
			// and words are not aligned properly. In such cases we have to limit new bounding rectangle dimensions by the ones obtained
			// by rotation of original bounding rectangle (without any correction). Otherwise there is a risk that new bounding rectangle 
			// will lay outside of the page (and its coordinates get negative)

			if (w > W)
			{
				w = W;
			}

			if (h > H)
			{
				h = H;
			}

			var w2 = w / 2;
			var h2 = h / 2;
			return new BoundingBox(Floor(xCnt - w2), Floor(yCnt - h2), Ceiling(xCnt + w2), Ceiling(yCnt +h2));
		}

		/// <summary>
		/// Rotates given model by given isomorphic transform. 
		/// Bounding box for an object of the model is transformed in such a way that it is still the minimal rectangle with sides parallel to coordinate axes contaning original box rotated.
		/// </summary>
		/// <param name="model">The model to process</param>
		/// <param name="transform">The transform to apply</param>
		/// <param name="targetWidth">Target model width</param>
		/// <param name="targetHeight">Target model height</param>
		/// <returns></returns>
		public static TextGeometryModel RotateModel(this TextGeometryModel model, IsometricTransform transform, int targetWidth, int targetHeight)
		{
			var targetModel = new TextGeometryModel(new BoundingBox(0, 0, targetWidth, targetHeight), model.GridUnit);
			ModelGeometryTransformer.TransformModelGeometry(model, targetModel,
				box => RotateBoundingBox(box, transform));
			return targetModel;
		}

		private static BoundingBox RotateBoundingBox(BoundingBox src, IsometricTransform transform)
		{
			var points = new List<Point>
			{
				transform.Transform(new Point(src.XMin, src.YMin)),
				transform.Transform(new Point(src.XMin, src.YMax)),
				transform.Transform(new Point(src.XMax, src.YMax)),
				transform.Transform(new Point(src.XMax, src.YMin)),
			};
			var xMin = points.Select(point => point.x).Min();
			var xMax = points.Select(point => point.x).Max();
			var yMin = points.Select(point => point.y).Min();
			var yMax = points.Select(point => point.y).Max();
			return new BoundingBox(Floor(xMin), Floor(yMin), Ceiling(xMax), Ceiling(yMax));
		}

		public static TextGeometryModel ScaleModel(this TextGeometryModel model, double scaleFactor)
		{

			var targetModel = new TextGeometryModel(ScaleBoundingBox(model.PageBox, scaleFactor), GridUnit.ByResolution(Round(model.GridUnit.Divisor * scaleFactor)));
			ModelGeometryTransformer.TransformModelGeometry(model, targetModel,
				box => ScaleBoundingBox(box, scaleFactor));
			return targetModel;
		}

		internal static BoundingBox ScaleBoundingBox(BoundingBox src, double scaleFactor)
		{
			return new BoundingBox(Floor(src.XMin * scaleFactor), Floor(src.YMin * scaleFactor), Ceiling(src.XMax * scaleFactor), Ceiling(src.YMax * scaleFactor));
		}


		private static BoundingBox DeskewBoundingBoxBase(BoundingBox src, IsometricTransform transform)
		{
			var cosAngle = Math.Abs(transform.CosAngle);
			var sinAngle = Math.Abs(transform.SinAngle);
			if (cosAngle > sinAngle)
			{
				var tmp = sinAngle;
				sinAngle = cosAngle;
				cosAngle = tmp;
			}

			var cos2Angle = cosAngle * cosAngle - sinAngle * sinAngle;
			var sin2Angle = 2 * sinAngle * cosAngle;

			var points = new List<Point>
			{
				transform.Transform(new Point(src.XMin, src.YMin)),
				transform.Transform(new Point(src.XMin, src.YMax)),
				transform.Transform(new Point(src.XMax, src.YMax)),
				transform.Transform(new Point(src.XMax, src.YMin)),
			};
			var xMin = points.Select(point => point.x).Min();
			var xMax = points.Select(point => point.x).Max();
			var yMin = points.Select(point => point.y).Min();
			var yMax = points.Select(point => point.y).Max();
			var W = xMax - xMin;
			var H = yMax - yMin;
			var p = ((W * sin2Angle - H) * sin2Angle) / ((sin2Angle * sin2Angle - 1) * 2);
			var q = ((H * sin2Angle - W) * sin2Angle) / ((sin2Angle * sin2Angle - 1) * 2);
			//			var p = 0;
			//			var q = 0;
			var result = new BoundingBox(Floor(xMin + p), Floor(yMin + q), Ceiling(xMax - p), Ceiling(yMax - q));

			//			Console.Out.WriteLine(@"""{0}"": ({1}) => ({2})", word.Text, src, result);
			//			if (word.Text == "Óòâåðæäåíà")
			//			{
			//				Console.Out.WriteLine("Òðàíñôîðìàöèÿ:");
			//				Console.Out.WriteLine("xMin: {0:N4}", xMin);
			//				Console.Out.WriteLine("xMax: {0:N4}", xMax);
			//				Console.Out.WriteLine("yMin: {0:N4}", yMin);
			//				Console.Out.WriteLine("yMax: {0:N4}", yMax);
			//				Console.Out.WriteLine("W: {0:N4}", W);
			//				Console.Out.WriteLine("H: {0:N4}", H);
			//				Console.Out.WriteLine("p: {0:N4}", p);
			//				Console.Out.WriteLine("q: {0:N4}", q);
			//				Console.Out.WriteLine("result: {0}", result);
			//				Console.Out.WriteLine("cosAngle: {0:N4}", cosAngle);
			//				Console.Out.WriteLine("sinAngle: {0:N4}", sinAngle);
			//				Console.Out.WriteLine("cos2angle: {0:N4}", cos2Angle);
			//				Console.Out.WriteLine("sin2angle: {0:N4}", sin2Angle);
			//				foreach (var point in points)
			//				{
			//					Console.Out.WriteLine(point);
			//				}
			//				Console.Out.WriteLine(@"""{0}"": ({1}) => ({2})", word.Text, src, result);
			//			}

			return result;
		}

		private static int Floor(double val)
		{
			return (int)Math.Floor(val);
		}

		private static int Ceiling(double val)
		{
			return (int)Math.Ceiling(val);
		}

		private static int Round(double val)
		{
			return (int)Math.Round(val);
		}
	}
}