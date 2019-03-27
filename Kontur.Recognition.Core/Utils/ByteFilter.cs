using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils
{
	/// <summary>
	/// Byte filter realized as array where byte used as index and bool value indicates of whether given byte belongs to filter or not
	/// </summary>
	public class ByteFilter
	{
		private readonly bool[] filter = new bool[256];

		/// <summary>
		/// Constructor taking in bytes to filter
		/// </summary>
		/// <param name="bytes">Bytes to filter</param>
		public ByteFilter([CanBeNull] params byte[] bytes)
		{
			Add(bytes);
		}

		/// <summary>
		/// Adds given bytes to filter
		/// </summary>
		/// <param name="bytes">Bytes to add to filter</param>
		public void Add([CanBeNull] params byte[] bytes)
		{
			if (bytes == null)
				return;
			foreach (var b in bytes)
			{
				filter[b] = true;
			}
		}

		/// <summary>
		/// Checks whether filter contains given byte
		/// </summary>
		/// <param name="b">Byte to check</param>
		/// <returns>True if fileter contains given byte false otherwise</returns>
		public bool Contains(byte b)
		{
			return filter[b];
		}
	}
}