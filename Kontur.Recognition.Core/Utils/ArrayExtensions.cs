using System;
using System.Collections.Generic;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using System.Linq;

namespace Kontur.Recognition.Utils
{
	/// <summary>
	/// This class contains helper methods to work with arrays
	/// </summary>
	public static class ArrayExtensions
	{
		/// <summary>
		/// Checks whether given byte array starts with given prefix (another array of data)
		/// </summary>
		/// <param name="data">The data where to look for a prefix</param>
		/// <param name="prefix">The prefix to check</param>
		/// <returns>true if the data start with given prefix</returns>
		public static bool StartsWith(this byte[] data, [NotNull] byte[] prefix)
		{
			return prefix.IsPrefixOf(data, 0, data.Length);
		}

		/// <summary>
		/// Checks whether given prefix is at beginning of the portion of given buffer starting at given offset and having specified number of bytes
		/// </summary>
		/// <param name="prefix">The prefix to look for</param>
		/// <param name="buffer">The buffer where to look for the prefix</param>
		/// <param name="offset">The offset of portion of buffer to scan</param>
		/// <param name="count">The number of bytes to scan</param>
		/// <returns>true if there is a given prefix starting from specified offset</returns>
		public static bool IsPrefixOf(this byte [] prefix, [NotNull] byte [] buffer, int offset, int count)
		{
			if (offset > buffer.Length)
				offset = buffer.Length;

			var maxLength = buffer.Length - offset;
			if (count > maxLength)
				count = maxLength;

			if (count < prefix.Length)
				return false;

			var lengthToScan = prefix.Length;
			for (var idx = 0; idx < lengthToScan; idx++)
			{
				if (buffer[offset + idx] != prefix[idx])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Skips given bytes in the given array segment
		/// </summary>
		/// <param name="buffer">Byte buffer to scan through</param>
		/// <param name="offset">Starting index of the segment to scan through</param>
		/// <param name="count">Length of the segment to scan through</param>
		/// <param name="filter">Bytes to skip</param>
		/// <returns>Number of skipped bytes</returns>
		/// <exception cref="T:System.ArgumentNullException">Свойство <paramref name="buffer" /> имеет значение null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset" /> находится вне диапазона допустимых индексов для <paramref name="buffer" />.-или-Значение параметра <paramref name="count" /> меньше нуля.-или-<paramref name="offset" /> и <paramref name="count" /> не указывают допустимый раздел в <paramref name="buffer" />.</exception>
		public static int Skip([NotNull] this byte[] buffer, int offset, int count, ByteFilter filter)
		{
			return buffer.Skip(offset).Take(count).TakeWhile(filter.Contains).Count();
		}

		/// <summary>
		/// Skips bytes in the given array segment until terminal byte is found
		/// </summary>
		/// <param name="buffer">Byte buffer to scan through</param>
		/// <param name="offset">Starting index of the segment to scan through</param>
		/// <param name="count">Length of the segment to scan through</param>
		/// <param name="terminalByte">The byte to scan up to</param>
		/// <returns>Number of skipped bytes</returns>
		/// <exception cref="T:System.ArgumentNullException">Свойство <paramref name="buffer" /> имеет значение null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset" /> находится вне диапазона допустимых индексов для <paramref name="buffer" />.-или-Значение параметра <paramref name="count" /> меньше нуля.-или-<paramref name="offset" /> и <paramref name="count" /> не указывают допустимый раздел в <paramref name="buffer" />.</exception>
		public static int SkipUntil([NotNull] this byte[] buffer, int offset, int count, byte terminalByte)
		{
			return buffer.Skip(offset).Take(count).TakeWhile(b => b != terminalByte).Count();
		}

		/// <summary>
		/// Locates first entry of given byte sequence
		/// </summary>
		/// <param name="buffer">Look-in buffer</param>
		/// <param name="dataToFind">Look-for sequence</param>
		/// <param name="offset">The offset of a portion of the buffer where to look for byte sequence</param>
		/// <param name="count">The length of a portion of the buffer where to look for byte sequence</param>
		/// <returns>Returns first found entry of given byte sequence otherwise -1</returns>
		/// <exception cref="T:System.ArgumentNullException">Свойство <paramref name="buffer" /> имеет значение null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset" /> находится вне диапазона допустимых индексов для <paramref name="buffer" />.-или-Значение параметра <paramref name="count" /> меньше нуля.-или-<paramref name="offset" /> и <paramref name="count" /> не указывают допустимый раздел в <paramref name="buffer" />.</exception>
		public static int Find([NotNull] this byte[] buffer, [NotNull] byte[] dataToFind, int offset, int count)
		{
			return buffer.FindAll(dataToFind, offset, count).DefaultIfEmpty(-1).First();
		}

		/// <summary>
		/// Locates all entries of given byte sequence
		/// </summary>
		/// <param name="buffer">The buffer where to look for byte sequence</param>
		/// <param name="dataToFind">The byte sequence to look for</param>
		/// <returns>Returns enumeration of positions where there is an entry of given byte sequence</returns>
		public static IEnumerable<int> FindAll(this byte[] buffer, [NotNull] byte[] dataToFind)
		{
			return FindAll(buffer, dataToFind, 0, buffer.Length);
		}

		/// <summary>
		/// Locates all entries of given byte sequence located in given buffer in specified range (defined by offset and byte count)
		/// </summary>
		/// <param name="buffer">The buffer where to look for byte sequence</param>
		/// <param name="dataToFind">The byte sequence to look for</param>
		/// <param name="offset">The offset of a portion of the buffer where to look for byte sequence</param>
		/// <param name="count">The length of a portion of the buffer where to look for byte sequence</param>
		/// <returns>Returns enumeration of positions where there is an entry of given byte sequence 
		/// (positions are returnet relative to the whole buffer, not relative to offset)</returns>
		public static IEnumerable<int> FindAll(this byte[] buffer, [NotNull] byte[] dataToFind, int offset, int count)
		{
			if (dataToFind.Length == 0)
				yield break;
			
			var startIndex = offset;
			var searchSegmentLength = count;
			var firstByte = dataToFind[0];

			int idx;
			while ((idx = Array.IndexOf(buffer, firstByte, startIndex, searchSegmentLength)) >= 0)
			{
				var skipped = idx - startIndex;
				if (dataToFind.IsPrefixOf(buffer, idx, searchSegmentLength - skipped))
				{
					yield return idx;
				}
				skipped++;
				startIndex += skipped;
				searchSegmentLength -= skipped;
			}
		}

		/// <summary>
		/// Utility routine to correctly convert signed bytes array into integer value, 
		/// high order bytes should precede low order bytes in array (BigEndian notation) 
		/// </summary>
		/// <param name="buffer">The array to read data from</param>
		/// <param name="offset">The offset where to start read</param>
		/// <returns></returns>
		public static int ReadInt32BigEndian(this byte[] buffer, int offset)
		{
			int result = 0;
			result |= (buffer[offset++] << 24);
			result |= (buffer[offset++] << 16) & 0x00FF0000;
			result |= (buffer[offset++] << 8) & 0x0000FF00;
			result |= (buffer[offset]) & 0x000000FF;
			return result;
		}

		/// <summary>
		/// Utility routine to correctly convert integer value into signed bytes array, 
		/// high order bytes will precede low order bytes in array (BigEndian notation)
		/// </summary>
		/// <param name="buffer">The array to read data to</param>
		/// <param name="offset">The offset where to start write to</param>
		/// <param name="value">Value to convert</param>
		/// <returns></returns>
		public static void WriteInt32BigEndian(this byte[] buffer, int offset, int value)
		{
			buffer[offset++] = (byte)((value >> 24) & 0xFF);
			buffer[offset++] = (byte)((value >> 16) & 0xFF);
			buffer[offset++] = (byte)((value >> 8) & 0xFF);
			buffer[offset] = (byte)(value & 0xFF);
		}

		/// <summary>
		/// Utility routine to correctly convert signed bytes array into integer value, 
		/// low order bytes should precede high order bytes in array (LittleEndian notation) 
		/// </summary>
		/// <param name="buffer">The array to read data from</param>
		/// <param name="offset">The offset where to start read</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		public static int ReadInt32LittleEndian(this byte[] buffer, int offset, int count = 4)
		{
			int result = 0;
			if (count-- > 0)
				result |= (buffer[offset++] & 0x000000FF);
			if (count-- > 0)
				result |= (buffer[offset++] << 8) & 0x0000FF00;
			if (count-- > 0)
				result |= (buffer[offset++] << 16) & 0x00FF0000;
			if (count > 0)
				result |= (buffer[offset]) << 24;
			return result;
		}

		/// <summary>
		/// Utility routine to correctly convert signed bytes array into 64-bit integer value, 
		/// low order bytes should precede high order bytes in array (LittleEndian notation) 
		/// </summary>
		/// <param name="buffer">The array to read data from</param>
		/// <param name="offset">The offset where to start read</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		public static long ReadLongLittleEndian(this byte[] buffer, int offset, int count = 8)
		{
			long result = 0;
			if (count-- > 0)
				result |= (buffer[offset++] & 0x00000000000000FFL);
			if (count-- > 0)
				result |= ((long)buffer[offset++] << 8) & 0x000000000000FF00L;
			if (count-- > 0)
				result |= ((long)buffer[offset++] << 16) & 0x0000000000FF0000L;
			if (count-- > 0)
				result |= ((long)buffer[offset++] << 24) & 0x00000000FF000000L;
			if (count-- > 0)
				result |= ((long)buffer[offset++] << 32) & 0x000000FF00000000L;
			if (count-- > 0)
				result |= ((long)buffer[offset++] << 40) & 0x0000FF0000000000L;
			if (count-- > 0)
				result |= ((long)buffer[offset++] << 48) & 0x00FF000000000000L;
			if (count > 0)
				result |= ((long)buffer[offset] << 56);
			return result;
		}

		/// <summary>
		/// Fills the given array with a specified value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="value"></param>
		public static void FillWith<T>(this T[] array, T value)
		{
			if (array.Length == 0)
				return;         // nothing to do

			// Prepare initial single value
			array[0] = value;

			// Fill at least the half of an array by doubling the number of elements being copied
			var halfLength = array.Length / 2;
			var blockLength = 1;
			while (blockLength <= halfLength)
			{
				Array.Copy(array, 0, array, blockLength, blockLength);
				blockLength *= 2;
			}
			Array.Copy(array, 0, array, blockLength, array.Length - blockLength);
		}


	}
}