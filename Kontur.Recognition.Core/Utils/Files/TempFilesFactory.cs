using System;
using System.IO;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Files
{
	/// <summary>
	///  This files factory works with specified directory and creates temporary files there regardless of which type of file (local or temporary) is requested
	/// </summary>
	public sealed class TempFilesFactory : FilesFactory
	{
		private static readonly Lazy<TempFilesFactory> localTempFilesFactory = new Lazy<TempFilesFactory>(CreateLocalTempFilesFactory);
		private static readonly Lazy<TempFilesFactory> systemTempFilesFactory = new Lazy<TempFilesFactory>(CreateSystemTempFilesFactory);

		private static TempFilesFactory CreateLocalTempFilesFactory()
		{
			return new TempFilesFactory(GetLocalTempPath());
		}

		private static TempFilesFactory CreateSystemTempFilesFactory()
		{
			return new TempFilesFactory(Path.GetTempPath());
		}

		public static IFilesFactory GetLocalTempFileFactory()
		{
			return localTempFilesFactory.Value;
		}

		public static IFilesFactory GetSystemTempFileFactory()
		{
			return systemTempFilesFactory.Value;
		}

		[UsedImplicitly]
		public static string GetLocalTempPath()
		{
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
		}

		[UsedImplicitly]
		public TempFilesFactory(string pathToTempDir = null) : base(pathToTempDir, pathToTempDir)
		{
		}

		public override LocalFile NewFile(string prefix, string extension)
		{
			var result = base.NewFile(prefix, extension);
			result.DeleteOnDispose();
			return result;
		}

		public override LocalFile AsFile(string filePath)
		{
			return AsTempFile(filePath);
		}
	}
}
