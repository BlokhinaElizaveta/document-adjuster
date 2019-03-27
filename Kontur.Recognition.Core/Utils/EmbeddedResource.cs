using System;

namespace Kontur.Recognition.Utils
{
	public static class EmbeddedResource
	{
		public static byte[] ReadBytes(Type rootType, string relativePath)
		{
			using (var stream = rootType.Assembly.GetManifestResourceStream(rootType, relativePath))
			{
				var length = stream.Length;
				var bytes = new byte[length];
				stream.Read(bytes, 0, (int)length);
				return bytes;
			}
		}
	}
}