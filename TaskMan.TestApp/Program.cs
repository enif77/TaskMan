/* TaskMan - (C) 2017 - 2019 Premysl Fara 
 
TaskMan is available under the zlib license:

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
 
 */

namespace TaskMan.TestApp
{
    using System;
    using System.Threading;

    using TaskMan.Tasks;


    class Program
    {
        static void Main(string[] args)
        {
            var app = new Program();
            
            app._taskScheduler.TaskExecutionStarted += app._taskScheduler_TaskExecutionStarted;
            app._taskScheduler.TaskExecutionFinished += app._taskScheduler_TaskExecutionFinished;
            app._taskScheduler.TaskNotExecuted += app._taskScheduler_TaskNotExecuted;
            app._taskScheduler.TaskScheduled += app._taskScheduler_TaskScheduled;
            app._taskScheduler.TaskNotScheduled += app._taskScheduler_TaskNotScheduled;

            app._taskScheduler.Init();

            app.ScheduleFrequentTask(DateTime.Now.Add(TimeSpan.FromMinutes(1)), "First", 60 * 5, 10);
            app.ScheduleFrequentTask(DateTime.Now.Add(TimeSpan.FromMinutes(2)), "Second", 60 * 1, 10);
            app.ScheduleFrequentTask(DateTime.Now.Add(TimeSpan.FromMinutes(4)), "Third", 60 * 3, 10);

            app.StartTimer();

            Console.WriteLine("Press ENTER to stop...");
            Console.ReadLine();

            app.StopTimer();

            Console.WriteLine("DONE");
        }


        #region events

        private void _taskScheduler_TaskNotScheduled(object sender, string message, ITask task, TaskFinishedCode? taskFinishedCode = default(TaskFinishedCode?))
        {
            Console.Error.WriteLine(message);
        }


        private void _taskScheduler_TaskScheduled(object sender, string message, ITask task, TaskFinishedCode? taskFinishedCode = default(TaskFinishedCode?))
        {
            Console.WriteLine(message);
        }


        private void _taskScheduler_TaskNotExecuted(object sender, string message, ITask task, TaskFinishedCode? taskFinishedCode = default(TaskFinishedCode?))
        {
            Console.Error.WriteLine(message);
        }


        private void _taskScheduler_TaskExecutionFinished(object sender, string message, ITask task, TaskFinishedCode? taskFinishedCode = default(TaskFinishedCode?))
        {
            Console.WriteLine(message);
        }


        private void _taskScheduler_TaskExecutionStarted(object sender, string message, ITask task, TaskFinishedCode? taskFinishedCode = default(TaskFinishedCode?))
        {
            Console.WriteLine(message);
        }

        #endregion


        #region timer

        private Timer _appTimer = null;
        private readonly TaskScheduler _taskScheduler = new TaskScheduler();


        private void ScheduleFrequentTask(DateTime sinceDateTime, string name, int sleepSeconds, int minutesBetweenRuns)
        {
            _taskScheduler.ScheduleTask(new DoItFrequentlyTask(minutesBetweenRuns)
            {
                RunAt = sinceDateTime,
                State = this,
                Name = name,
                SleepSeconds = sleepSeconds
            });
        }


        private void StartTimer()
        {
            if (_appTimer == null)
            {
                _appTimer = new Timer(TimerCallback, this, 0, 5000); // 12 x per minute.
            }

            Console.WriteLine("Timer started.");
        }


        private void TimerCallback(object state)
        {
            try
            {
                Console.WriteLine("Tick: {0}", DateTime.Now);

                _taskScheduler.Update();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("The task scheduler thrown an exception: {0}", ex);
            }
        }


        private void StopTimer()
        {
            if (_appTimer == null) return;

            _appTimer.Change(Timeout.Infinite, 0);
            _appTimer.Dispose();
            _appTimer = null;

            Console.WriteLine("Timer stopped.");
        }

        #endregion
    }
}
