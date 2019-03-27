using System;
using System.IO;
using System.Reflection;
using System.Text;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Logging;

namespace Kontur.Recognition.Utils.Files
{
	public static class FileUtils
	{
		/// <summary>
		/// Puts given content into given file in specified encoding (or UTF8, if encoding is not specified)
		/// </summary>
		/// <param name="targetFile">The file to which the content is to be placed</param>
		/// <param name="content">The content to put into file</param>
		/// <param name="targetEncoding">The encoding to use (if not present, UTF8 will be used)</param>
		public static void StoreToFile(string targetFile, string content, Encoding targetEncoding = null)
		{
			using (var outStream = File.Open(targetFile, FileMode.Create, FileAccess.Write, FileShare.Read))
			using (var writer = new StreamWriter(outStream, targetEncoding ?? Encoding.UTF8))
			{
				writer.Write(content);
				writer.Flush();
			}
		}

		/// <summary>
		/// Provides a logic to reserve file with specified prefix and suffix (extension) in specified directory.
		/// File is created by adding random number after specified prefix, this way the code tries to avoid conflicting names when files are created in the same directory
		/// </summary>
		/// <param name="pathToDir">Where the files should be located</param>
		/// <param name="prefix">The prefix of the name of the file being created (if set to null, starting assembly's name is used as a prefix)</param>
		/// <param name="extension">The suffix (extension) of the name of the file being created</param>
		/// <returns></returns>
		/// <exception cref="IOException">An exception of type IOException is thrown if file can not be created</exception>
		static public string ReserveFileName([NotNull] this string pathToDir, [CanBeNull] string prefix, [NotNull] string extension)
		{
			var builder = new StringBuilder();
			builder.Append(!string.IsNullOrEmpty(prefix) ? prefix : Assembly.GetEntryAssembly().GetName().Name);
			builder.Append("_{0:D6}");
			builder.Append(extension ?? "");
			var fileNamePattern = builder.ToString();
			var random = new Random();
			IOException lastException = null;
			for (var counter = 0; counter < 100; counter++)
			{
				var randSuffix = random.Next(1000000);
				var fileName = Path.Combine(pathToDir, string.Format(fileNamePattern, randSuffix));
				if (!File.Exists(fileName))
				{
					try
					{
						using (File.Open(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
						{
							return fileName;
						}
					}
					catch (IOException ex)
					{
						// Ignore the problem and just continue
						lastException = ex;
					}
				}
			}
			throw new IOException(string.Format("Can't obtain new file at {0}", pathToDir), lastException);
		}

		public static void Delete(string file)
		{
			try
			{
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			}
			catch (Exception ex)
			{
				Log.ErrorFormat(typeof(FileUtils), "Cannot delete file: {0}. Reason: {1}", file, ex.Message);
			}
		}
	}
}