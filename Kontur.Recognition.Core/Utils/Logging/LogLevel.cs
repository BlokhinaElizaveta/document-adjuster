namespace Kontur.Recognition.Utils.Logging
{
	public enum LogLevel
	{
		/// <summary>
		/// special value (to filter events) which blocks all events (as it is below the lowest level)
		/// </summary>
		None = -1,

		/// <summary>
		/// system is unusable (level 0)
		/// </summary>
		Emergency = 0,

		/// <summary>
		/// action must be taken immediately (level 1)
		/// </summary>
		Alert = 1,

		/// <summary>
		/// critical conditions (level 2)
		/// </summary>
		Critical = 2,

		/// <summary>
		/// error conditions (level 3)
		/// </summary>
		Error = 3,

		/// <summary>
		/// warning conditions (level 4)
		/// </summary>
		Warning = 4,

		/// <summary>
		/// normal but significant condition (level 5)
		/// </summary>
		Notice = 5,

		/// <summary>
		/// informational (level 6)
		/// </summary>
		Info = 6,

		/// <summary>
		/// debug-level messages (level 7)
		/// </summary>
		Debug = 7
	}
}