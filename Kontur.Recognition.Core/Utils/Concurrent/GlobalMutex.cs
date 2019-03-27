using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Kontur.Recognition.Utils.Concurrent
{
	/// <summary>
	/// Implements access to global mutex which allows to synchronize different processes
	/// </summary>
	public class GlobalMutex : IDisposable
	{
		private readonly Mutex mutex;
		private volatile int hasHandle;

		public GlobalMutex(string name)
		{
			var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
			var securitySettings = new MutexSecurity();
			securitySettings.AddAccessRule(allowEveryoneRule);
			bool createdNew;
			mutex = new Mutex(false, string.Format(@"Global\{0}", name), out createdNew, securitySettings);
			hasHandle = 0;
		}

		~GlobalMutex()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Dispose(bool disposing)
		{
			ReleaseLock();
			if (disposing)
			{
				mutex.Dispose();
			}
		}

		/// <summary>
		/// Performs an attempt to lock mutex. If the mutex is free, it gets locked (true is returned), otherwise 
		/// an attepmts to lock the mutex are repeated until specified timeout elapses.
		/// </summary>
		/// <param name="timeout">The timeout to wait for mutex (in milliseconds)</param>
		/// <returns>True if the mutex was successfully locked; false if the mutex is still locked after timeout elapsed</returns>
		public bool TryObtainLock(int timeout)
		{
			try
			{
				ObtainLockImpl(timeout);
				return true;
			}
			catch (TimeoutException)
			{
				return false;
			}
		}

		/// <summary>
		/// Performs an attempt to lock mutex. If the mutex is free, it gets locked (true is returned), otherwise 
		/// an attepmts to lock the mutex are repeated until specified timeout elapses.
		/// </summary>
		/// <param name="timeout">The timeout (as a timespan) to wait for mutex</param>
		/// <returns>True if the mutex was successfully locked; false if the mutex is still locked after timeout elapsed</returns>
		public bool TryObtainLock(TimeSpan timeout)
		{
			try
			{
				var lTimeout = (long)timeout.TotalMilliseconds;
				if (-1L > lTimeout || int.MaxValue < lTimeout)
					throw new ArgumentOutOfRangeException("timeout", "Negative or too big timespan");
				ObtainLockImpl((int)lTimeout);
				return true;
			}
			catch (TimeoutException)
			{
				return false;
			}
		}

		/// <summary>
		/// Waits until the mutex is successfully locked. 
		/// </summary>
		/// <returns>True if the mutex was successfully locked; false if the mutex is still locked after timeout elapsed</returns>
		public void ObtainLock()
		{
			ObtainLockImpl(Timeout.Infinite);
		}

		private void ObtainLockImpl(int timeout)
		{
			try
			{
				var lockObtained = mutex.WaitOne(timeout, false);
				if (!lockObtained)
				{
					throw new TimeoutException("Timeout waiting for exclusive access");
				}
				hasHandle = 1;
			}
			catch (AbandonedMutexException)
			{
				hasHandle = 1;
			}
		}

		/// <summary>
		/// Unlocks (releases) this mutex if it was previously locked. This method is an idempotent one, i.e. it is safe to 
		/// invoke ReleaseLock multiple times.
		/// </summary>
		/// <returns></returns>
		public void ReleaseLock()
		{
			if (Interlocked.CompareExchange(ref hasHandle, 0, 1) == 1)
			{
				mutex.ReleaseMutex();
			}
		}

	}
}
