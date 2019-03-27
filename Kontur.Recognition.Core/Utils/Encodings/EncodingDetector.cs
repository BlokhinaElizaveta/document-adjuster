using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Encodings
{
	/// <summary>
	/// Class provide method to detect encoding of text document. 
	/// It is assumed that document contains Russian text (known properties of language are used to detect the encoding)
	/// </summary>
	public class EncodingDetector
	{
		/// <summary>
		/// Tries to detect character encoding of given byte sequence.
		/// The following encodings are detected: UTF-8, UTF-16 (both big endian and little endian), windows-1251, Cp866, ISO-8859-5, KOI8-R
		/// If the bytes do not look like UTF-8 or UTF-16 code, then an attempt to detect single-byte Russian encoding is performed. 
		/// If the bytes look like binary content (i.e. has no statistical characteristics suitable for text) then null is returned.
		/// To detect single-byte encoding an assumption is made that byte sequence represents text in Russian,
		/// otherwise result can not be predicted. 
		/// </summary>
		/// <param name="data">The byte sequence to analyze</param>
		/// <param name="lengthToAnalyze">The number of bytes to analyze (0 means to analyze the whole sequence)</param>
		/// <returns></returns>
		public static Encoding TryDetectTextEncoding(byte[] data, int lengthToAnalyze = 0)
		{
			if ((lengthToAnalyze == 0) || (lengthToAnalyze > data.Length))
			{
				lengthToAnalyze = data.Length;
			}
			bool hasAsciiControlCharacters;
			if (IsUTF8EncodingImpl(data, lengthToAnalyze, out hasAsciiControlCharacters))
			{
				return hasAsciiControlCharacters ? null : Encoding.UTF8;
			}
			var byteFrequencies = new ByteFrequencies(data, lengthToAnalyze);
			var utf16Encoding = DetectUtf16Encoding(data, lengthToAnalyze, byteFrequencies);
			if (utf16Encoding != null)
			{
				return utf16Encoding;
			}
			if (HasControlBytes(byteFrequencies.Frequencies))
			{
				return null;
			}
			var singleByteRussianEncoding = DetectSingleByteRussianEncoding(byteFrequencies.Frequencies, false);
			if (singleByteRussianEncoding != null)
			{
				return singleByteRussianEncoding;
			}
			return Encoding.ASCII;
		}

		private static Encoding DetectUtf16Encoding(byte[] data, int lengthToAnalyze, [CanBeNull] ByteFrequencies byteFrequencies)
		{
			byteFrequencies = byteFrequencies ?? new ByteFrequencies(data, lengthToAnalyze);
			var utf16BeHighBytesCount = byteFrequencies.EvenPosFrequencies.Take(5).Sum();
			var utf16LeHighBytesCount = byteFrequencies.OddPosFrequencies.Take(5).Sum();
			var utf16BeDetected = (utf16BeHighBytesCount >= byteFrequencies.EvenPosCount * 0.9);
			var utf16LeDetected = (utf16LeHighBytesCount >= byteFrequencies.OddPosCount * 0.9);
			var utf16BeBomDetected = false;
			var utf16LeBomDetected = false;

			if (byteFrequencies.Count > 2)
			{
				// Unicode Little Endian BOM character
				utf16LeBomDetected = data[0] == 0xFF && data[1] == 0xFE;
				// Unicode Big Endian BOM character
				utf16BeBomDetected = data[1] == 0xFF && data[0] == 0xFE;
			}
			// The presence of BOM character overrides results of frequency analisys
			utf16BeDetected &= !utf16LeBomDetected;
			utf16LeDetected &= !utf16BeBomDetected;

			if (utf16BeDetected && utf16LeDetected)
			{
				// Frequency analisys allows both encodings. 
				// Prefer the encoding which has more correct high bytes
				if (utf16BeHighBytesCount > utf16LeHighBytesCount)
				{
					utf16LeDetected = false;
				}
				else
				{
					utf16BeDetected = false;
				}
			}

			if (utf16LeDetected)
			{
				// Unicode is detected
				// Encoding utf-16, codepage 1200
				return Encoding.GetEncoding("utf-16");
			}

			if (utf16BeDetected)
			{
				// Unicode is detected
				// Encoding utf-16BE, codepage 1201
				return Encoding.GetEncoding("utf-16BE");
			}

			return null;
		}

		/// <summary>
		/// Returns type of byte in accordance with UTF8 code. 
		/// Type codes are chosen in such a way that for leading bytes
		/// they correspond to code sequence length
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		private static int DetectByteType(byte b)
		{
			if ((b & 0x80) == 0x00)
			{
				// single byte code
				return 1;
			}
			if ((b & 0xC0) == 0x80)
			{
				// continuation byte
				return 0;
			}
			if ((b & 0xE0) == 0xC0)
			{
				// leading byte of two-bytes code
				return 2;
			}
			if ((b & 0xF0) == 0xE0)
			{
				// leading byte of three-bytes code
				return 3;
			}
			if ((b & 0xF8) == 0xF0)
			{
				// leading byte of four-bytes code
				return 4;
			}
			// bad byte
			return -2;
		}

		/// <summary>
		/// Tries to decode byte sequence as a UTF8 code. Returns true if no code violation is found.
		/// </summary>
		/// <param name="data">The data to analyze</param>
		/// <param name="lengthToAnalyze">Number of bytes to analyze</param>
		/// <returns></returns>
		public static bool IsUTF8Encoding(byte[] data, int lengthToAnalyze = 0)
		{
			if ((lengthToAnalyze == 0) || (lengthToAnalyze > data.Length))
			{
				lengthToAnalyze = data.Length;
			}
			bool hasAsciiControlCharacters;
			return IsUTF8EncodingImpl(data, lengthToAnalyze, out hasAsciiControlCharacters);
		}

		/// <summary>
		/// Tries to decode byte sequence as a UTF8 code. Returns true if no code violation is found and no non-printable ASCII control character is found.
		/// </summary>
		/// <param name="data">The data to analyze</param>
		/// <param name="lengthToAnalyze">Number of bytes to analyze</param>
		/// <returns></returns>
		public static bool IsUTF8EncodingText(byte[] data, int lengthToAnalyze = 0)
		{
			if ((lengthToAnalyze == 0) || (lengthToAnalyze > data.Length))
			{
				lengthToAnalyze = data.Length;
			}
			bool hasAsciiControlCharacters;
			return IsUTF8EncodingImpl(data, lengthToAnalyze, out hasAsciiControlCharacters) && !hasAsciiControlCharacters;
		}

		/// <summary>
		/// Tries to decode byte sequence as a UTF8 code. Returns true if no code violation is found.
		/// </summary>
		/// <param name="data">The data to analyze</param>
		/// <param name="lengthToAnalyze">Number of bytes to analyze</param>
		/// <param name="hasAsciiControlChars">Whether decoded UTF8 value contains ASCII control characters which can not be represented in text</param>
		/// <returns></returns>
		private static bool IsUTF8EncodingImpl(byte[] data, int lengthToAnalyze, out bool hasAsciiControlChars)
		{
			hasAsciiControlChars = false;
			var errorsCount = 0;
			var continuationCount = 0;
			var pos = 0;
			// skip leading continuation bytes (to start decoding in synchronized state)
			// there can be at most 3 continuation bytes in row
			while (pos < lengthToAnalyze && pos < 3 && DetectByteType(data[pos]) == 0)
			{
				pos++;
			}
			// now we must be in synchronized state (looking at leading byte)
			// so decoding may start
			while (pos < lengthToAnalyze)
			{
				var byteType = DetectByteType(data[pos]);
				switch (byteType)
				{
					case -2:
					{
						errorsCount++;
						continuationCount = 0;
						break;
					}
					case 0:
					{
						if (continuationCount > 0)
						{
							continuationCount--;
						}
						else
						{
							errorsCount++;
						}
						break;
					}
					case 1:
					{
						if (continuationCount > 0)
						{
							errorsCount++;
						}
						// expecting this number of continuation characters
						continuationCount = byteType - 1;
						hasAsciiControlChars |= asciiControlBytesHashSet.Contains(data[pos]);
						break;
					}
					case 2:	
					case 3: // intentionally pass through
					case 4: // intentionally pass through
					{
						if (continuationCount > 0)
						{
							errorsCount++;
						}
						// expecting this number of continuation characters
						continuationCount = byteType - 1;
						break;
					}
				}
				if (errorsCount > 10)
				{
					break;
				}
				pos++;
			}
			return (errorsCount == 0);
		}

		/// <summary>
		/// Calculates number of occurences of each byte in given sequence
		/// </summary>
		/// <param name="data"></param>
		/// <param name="lengthToAnalyze"></param>
		/// <returns></returns>
		private static int[] BuildByteFrequencies(byte[] data, int lengthToAnalyze)
		{
			if ((lengthToAnalyze == 0) || (lengthToAnalyze > data.Length))
			{
				lengthToAnalyze = data.Length;
			}

			var byteCounters = new int[256];
			foreach (var b in data.Take(lengthToAnalyze))
			{
				byteCounters[b]++;
			}
			return byteCounters;
		}

		class ByteFrequencies
		{
			public readonly int[] OddPosFrequencies = new int[256];
			public readonly int[] EvenPosFrequencies = new int[256];
			public readonly int[] Frequencies = new int[256];
			// Number of odd positions in data (number of entries counted in OddPosFrequencies; 
			// position is indexed from zero)
			public readonly int OddPosCount;
			// Number of even positions in data (number of entries counted in EvenPosFrequencies;
			// position is indexed from zero)
			public readonly int EvenPosCount;
			// The number of processed bytes
			public readonly int Count;

			public ByteFrequencies(byte[] data, int lengthToAnalyze)
			{
				if ((lengthToAnalyze == 0) || (lengthToAnalyze > data.Length))
				{
					lengthToAnalyze = data.Length;
				}

				Count = lengthToAnalyze;
				EvenPosCount = (lengthToAnalyze + 1) / 2;
				OddPosCount = lengthToAnalyze - EvenPosCount;

				for (var idx = 0; idx < lengthToAnalyze; idx++)
				{
					var b = data[idx];
					Frequencies[b]++;
					var posFreqs = ((idx % 2) == 0) ? EvenPosFrequencies : OddPosFrequencies;
					posFreqs[b]++;
				}
			}
		}

		class EncodingDetectionInfo
		{
			private static readonly char[] rusAlphabetUpCase = "ÀÁÂÃÄÅ¨ÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞß".ToCharArray();
			private static readonly char[] rusAlphabetLowCase = "àáâãäå¸æçèéêëìíîïðñòóôõö÷øùúûüýþÿ".ToCharArray();

			private readonly int[] bytesToCharMap;

			public int[] BytesToCharMap
			{
				get { return bytesToCharMap; }
			}

			public Encoding Encoding
			{
				get { return encoding; }
			}

			private readonly Encoding encoding;

			public EncodingDetectionInfo(Encoding encoding)
			{
				this.encoding = encoding;
				bytesToCharMap = BuildEncodingBytesToCharMap(encoding);
			}

			/// <summary>
			/// Builds map that translates byte (code of character) into alphabet letter position (number in range from 0 to 33; 0 means no mapping)
			/// </summary>
			/// <param name="encoding"></param>
			/// <returns></returns>
			private static int[] BuildEncodingBytesToCharMap(Encoding encoding)
			{
				if (!encoding.IsSingleByte)
				{
					throw new ArgumentException("Only single-byte encodings are accepted");
				}
				var result = new int[256];
				for (var charNumber = 0; charNumber < 33; charNumber++)
				{
					var charCode = encoding.GetBytes(rusAlphabetLowCase, charNumber, 1)[0];
					result[charCode] = charNumber + 1;
					charCode = encoding.GetBytes(rusAlphabetUpCase, charNumber, 1)[0];
					result[charCode] = charNumber + 1;
				}
				return result;
			}
		}

		private static readonly EncodingDetectionInfo[] knownRussianEncodings =
		{
			new EncodingDetectionInfo(Encoding.GetEncoding("windows-1251")),
			new EncodingDetectionInfo(Encoding.GetEncoding("cp866")),
			new EncodingDetectionInfo(Encoding.GetEncoding("iso-8859-5")),
			new EncodingDetectionInfo(Encoding.GetEncoding("koi8-r"))
		};

		/// <summary>
		/// Predefined frequencies of Russian alphabet characters
		/// </summary>
		private static readonly int[] rusAlphabetCharFrequencies =
		{ 40487008,		8051767,   22930719,	8564640,	15052118,
		  42691213,		 184928,    4746916,	8329904,	37153142,
		   6106262,	   17653469,   22230174,   16203060,    33838881,
		  55414481,    14201572,   23916825,   27627040,    31620970,
		  13245712,		1335747,	4904176,	2438807,	 7300193,
		   3678738,		1822476,	 185452,	9595941,	 8784613,
		   1610107,		3220715,   10139085
		};

		/// <summary>
		/// Predefined distribution of Russian alphabet characters
		/// </summary>
		private static readonly double[] rusAlphabetCharDistribution =
			BuildDistributionFromFrequencies(rusAlphabetCharFrequencies);

		/// <summary>
		/// Normalizes array of character frequencies to obtain distribution
		/// </summary>
		/// <param name="frequencies"></param>
		/// <returns></returns>
		private static double[] BuildDistributionFromFrequencies(int[] frequencies)
		{
			var result = new double[frequencies.Length];
			var totalCount = frequencies.Sum();
			for (var idx = 0; idx < result.Length; idx++)
			{
				result[idx] = (double) frequencies[idx]/totalCount;
			}
			return result;
		}

		/// <summary>
		/// Calculates the difference between two distributions
		/// </summary>
		/// <param name="distribution"></param>
		/// <returns></returns>
		private static double DistanceToRussianDistribution(double[] distribution)
		{
			if (rusAlphabetCharDistribution.Length != distribution.Length)
			{
				throw new ArgumentException("Both distributions must have same length");
			}
			double distance = 0;
			for (var idx = 0; idx < rusAlphabetCharDistribution.Length; idx++)
			{
				double diff = (rusAlphabetCharDistribution[idx] - distribution[idx]);
				distance += diff*diff;
			}
			return distance;
		}

		/// <summary>
		/// Given array of byte frequencies and mapping from bytes to characters calculates 
		/// array of character frequencies
		/// </summary>
		/// <param name="frequencies"></param>
		/// <param name="bytesToCharMap"></param>
		/// <returns></returns>
		private static int[] MapBytesFrequenciesToCharsFrequencies(int[] frequencies, int[] bytesToCharMap)
		{
			var result = new int[33];
			for (var idx = 0; idx < frequencies.Length; idx++)
			{
				var mapping = bytesToCharMap[idx];
				if (mapping > 0)
				{
					result[mapping-1] += frequencies[idx];
				}
			}
			return result;
		}

		class EncodingStatistics
		{
			public double DistributionDistance { get; set; }
			public int TotalCharCount { get; set; }
			public EncodingDetectionInfo EncodingInfo { get; set; }
		}

		private static readonly byte[] asciiControlBytes = 
			{0, 1, 2, 3, 4, 5, 6, 11, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
		private static HashSet<byte> asciiControlBytesHashSet = new HashSet<byte>(asciiControlBytes);


		/// <summary>
		/// Performs detection of whether ASCII control bytes are present in given array of bytes frequencies
		/// </summary>
		/// <param name="frequencies"></param>
		/// <returns></returns>
		private static bool HasControlBytes(int[] frequencies)
		{
			return asciiControlBytes.Sum(b => frequencies[b]) > 0;
		}

		/// <summary>
		/// Performs detection of single-byte Russian character encoding by given table of byte frequencies
		/// </summary>
		/// <param name="frequencies"></param>
		/// <param name="checkControlBytesPresence">If set to false, disables check for control characters 
		/// (attempt to detect encoding will be done even in case when they are present)</param>
		/// <returns></returns>
		private static Encoding DetectSingleByteRussianEncoding(int[] frequencies, bool checkControlBytesPresence)
		{
			if (checkControlBytesPresence && HasControlBytes(frequencies))
			{
				return null;
			}

			EncodingStatistics bestByDistance = null;
			var bestDistance = double.MaxValue;

			EncodingStatistics bestByCharCount = null;
			var bestCharCount = int.MinValue;

			foreach (var encodingInfo in knownRussianEncodings)
			{
				var charFrequencies = MapBytesFrequenciesToCharsFrequencies(frequencies, encodingInfo.BytesToCharMap);
				var totalCharCount = charFrequencies.Sum();
				var distributionInEncoding = BuildDistributionFromFrequencies(charFrequencies);
				var distance = DistanceToRussianDistribution(distributionInEncoding);

				var statistics = new EncodingStatistics
				                 {
					                 DistributionDistance = distance,
					                 TotalCharCount = totalCharCount,
					                 EncodingInfo = encodingInfo
				                 };
				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestByDistance = statistics;
				}
				if (totalCharCount > bestCharCount)
				{
					bestCharCount = totalCharCount;
					bestByCharCount = statistics;
				}
			}
			if ((bestByDistance == null) || (bestByCharCount == null))
			{
				// it should never happen as we have at least one known encoding
				return null;		
			}
			if ((bestByCharCount == bestByDistance) 
				|| ((bestByCharCount.DistributionDistance - bestByDistance.DistributionDistance) > 0.03)
				|| (bestByDistance.DistributionDistance < 0.008))
			{
				return bestByDistance.EncodingInfo.Encoding;
			}
			// When encoding allows to decode more character while it has almost the same distance
			// from predefined Russian distribution then we will use that encoding instead of
			// the best one selected by distance.
			if ((bestByCharCount.DistributionDistance - bestByDistance.DistributionDistance) < 0.01)
			{
				return bestByCharCount.EncodingInfo.Encoding;
			}
			return null;
		}
	}
}