using System;
using System.IO;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Files
{
	/// <summary>
	/// Special subclass of LocalFile optimized to case when temporary file (with automatic deletion) is used
	/// </summary>
	public class TemporaryFile : LocalFile
	{
		public TemporaryFile([NotNull] string path)
			: base(path, true)
		{
			File.SetAttributes(FilePath, FileAttributes.Temporary);
		}

		public TemporaryFile([NotNull] TemporaryFile file)
			: base(file)
		{
		}

		[NotNull]
		public override object Clone()
		{
			return new TemporaryFile(this);
		}

		[NotNull]
		public new TemporaryFile DuplicateReference()
		{
			var result = Clone() as TemporaryFile;
			if (result == null)
				throw new NullReferenceException();
			return result;
		}
	}
}