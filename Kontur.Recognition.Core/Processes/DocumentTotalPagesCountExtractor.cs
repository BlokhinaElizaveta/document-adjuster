using System.Text.RegularExpressions;

namespace Kontur.Recognition.Processes
{
	/// <summary>
	/// This is used to extract total page count from execution result output.
	/// </summary>
	public static class DocumentTotalPageCountExtractor
	{
		private const string totalPageMessagePattern = @"Total page count: {0}";
		private static readonly Regex regex = new Regex(string.Format(totalPageMessagePattern, @"(\d*)"));

		public static int? TryGetTotalPageCount<T>(ProcessExecutionResult<T> executionResult)
		{
			var result = regex.Match(executionResult.Output);

			if (result.Groups.Count > 1)
			{
				int totalPageCount;
				if (int.TryParse(result.Groups[1].Value, out totalPageCount))
				{
					return totalPageCount;
				}
			}

			return null;
		}

		/// <summary>
		/// Generate message which must be used in cmd output for total page count, otherwise we are not able to parse it.
		/// </summary>
		public static string TryGetTotalPageCountMessage(int? totalPageCount)
		{
			return totalPageCount.HasValue ? string.Format(totalPageMessagePattern, totalPageCount.Value) : null;
		}
	}
}