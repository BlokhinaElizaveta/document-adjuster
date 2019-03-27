using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kontur.Recognition.Utils.Files
{
	public static class DirectoryLocator
	{
		/// <summary>
		/// Looks for a directory for which the given predicate is true. The directory tree is traversed
		/// starting from given start directory up to the root. Then an attempt is made to add each of 
		/// subdirectories to the current directory and check the predicate for it. The first directory 
		/// for which the predicate is true is returned
		/// </summary>
		/// <param name="startDirectory"></param>
		/// <param name="predicate"></param>
		/// <param name="subdirectories"></param>
		/// <returns></returns>
		public static string TryFindDirectory(string startDirectory, Func<string, bool> predicate, IEnumerable<string> subdirectories)
		{
			var subDirs = subdirectories as ICollection<string> ?? subdirectories.ToList();
			
			var currentDirectory = startDirectory;
			
			while (currentDirectory != null)
			{
				var directory = currentDirectory;
				foreach (var path in subDirs.Select(x => Path.Combine(directory, x)))
				{
					if (predicate(path))
					{
						return path;
					}
				}
				
				currentDirectory = Path.GetDirectoryName(currentDirectory);
			}
			
			return null;
		}
	}
}