﻿//=====================================================
// - FileName:      TimerMgr.cs
// - Created:       codingriver
//======================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


	public class TimerMgr:Singleton<TimerMgr>
	{
        private struct Timer
        {
            public long Id { get; set; }
            public long Time { get; set; }
            public TaskCompletionSource<bool> tcs;
        }

        private readonly Dictionary<long, Timer> timers = new Dictionary<long, Timer>();

		/// <summary>
		/// key: time, value: timer id
		/// </summary>
		private readonly MultiMap<long, long> timeId = new MultiMap<long, long>();

		private readonly Queue<long> timeOutTime = new Queue<long>();
		
		private readonly Queue<long> timeOutTimerIds = new Queue<long>();

		// 记录最小时间，不用每次都去MultiMap取第一个值
		private long minTime;

		public void Update()
		{
			if (this.timeId.Count == 0)
			{
				return;
			}

			long timeNow = TimeHelper.Now();

			if (timeNow < this.minTime)
			{
				return;
			}
			
			foreach (KeyValuePair<long, List<long>> kv in this.timeId.GetDictionary())
			{
				long k = kv.Key;
				if (k > timeNow)
				{
					minTime = k;
					break;
				}
				this.timeOutTime.Enqueue(k);
			}

			while(this.timeOutTime.Count > 0)
			{
				long time = this.timeOutTime.Dequeue();
				foreach(long timerId in this.timeId[time])
				{
					this.timeOutTimerIds.Enqueue(timerId);	
				}
				this.timeId.Remove(time);
			}

			while(this.timeOutTimerIds.Count > 0)
			{
				long timerId = this.timeOutTimerIds.Dequeue();
				Timer timer;
				if (!this.timers.TryGetValue(timerId, out timer))
				{
					continue;
				}
				this.timers.Remove(timerId);
				timer.tcs.SetResult(true);
			}
		}

		private void Remove(long id)
		{
			Timer timer;
			if (!this.timers.TryGetValue(id, out timer))
			{
				return;
			}
			this.timers.Remove(id);
		}

		public Task WaitTillAsync(long tillTime,  out Action cancelAction)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = tillTime, tcs = tcs };
			this.timers[timer.Id] = timer;
			this.timeId.Add(timer.Time, timer.Id);
			if (timer.Time < this.minTime)
			{
				this.minTime = timer.Time;
			}
			cancelAction =() => { this.Remove(timer.Id); };
			return tcs.Task;
		}

		public Task WaitTillAsync(long tillTime)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = tillTime, tcs = tcs };
			this.timers[timer.Id] = timer;
			this.timeId.Add(timer.Time, timer.Id);
			if (timer.Time < this.minTime)
			{
				this.minTime = timer.Time;
			}
			return tcs.Task;
		}

		public Task WaitAsync(long time, out Action cancelAction)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = TimeHelper.Now() + time, tcs = tcs };
			this.timers[timer.Id] = timer;
			this.timeId.Add(timer.Time, timer.Id);
			if (timer.Time < this.minTime)
			{
				this.minTime = timer.Time;
			}
			cancelAction =() => { this.Remove(timer.Id); };
			return tcs.Task;
		}

		public Task WaitAsync(long time)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = TimeHelper.Now() + time, tcs = tcs };
			this.timers[timer.Id] = timer;
			this.timeId.Add(timer.Time, timer.Id);
			if (timer.Time < this.minTime)
			{
				this.minTime = timer.Time;
			}
			return tcs.Task;
		}
	}