using System;
using System.Diagnostics;

namespace utilities
{
	public class HiResTimer
	{
		Stopwatch stopwatch;

		public HiResTimer()
		{
			stopwatch = new Stopwatch();
			stopwatch.Reset();
		}

		public long ElapsedMilliseconds
		{
			get { return stopwatch.ElapsedMilliseconds; }
		}

		public void Start()
		{
			if (!stopwatch.IsRunning)
			{
				stopwatch.Reset();
				stopwatch.Start();
			}
		}

		public void Stop()
		{
			stopwatch.Stop();
		}
	}
}
