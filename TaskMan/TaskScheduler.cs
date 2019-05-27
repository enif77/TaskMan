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

namespace TaskMan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using TaskMan.Tasks;


    /// <summary>
    /// A class for scheduling task to be executed at certain time in the future or repetately.
    /// </summary>
    public class TaskScheduler
    {
        /// <summary>
        /// The maximal number of running tasks.
        /// 100 by default.
        /// </summary>
        public int MaxRunningTasksAllowed { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        public TaskScheduler()
        {
            MaxRunningTasksAllowed = 100;
        }

        /// <summary>
        /// Returns the next scheduled task.
        /// </summary>
        public ITask NextScheduledTask
        {
            get
            {
                return _scheduledTasks.Count == 0
                    ? null
                    : _scheduledTasks.First().Value;
            }
        }

        /// <summary>
        /// Retuns the number of actually running tasks.
        /// </summary>
        public int RunningTasksCount
        {
            get
            {
                lock (_lock)
                {
                    return _runningTasks.Count;
                }
            }
        }

        #region events

        /// <summary>
        /// A delegate type for task related events notifications.
        /// </summary>
        /// <param name="sender">A sender.</param>
        /// <param name="message">A message.</param>
        /// <param name="task">A task.</param>
        /// <param name="taskFinishedCode">A TaskFinishedCode constant, when TaskExecutionFinished event is fired.</param>
        public delegate void TaskEventHandler(object sender, string message, ITask task, TaskFinishedCode? taskFinishedCode = null);

        /// <summary>
        /// A task scheduled.
        /// </summary>
        public event TaskEventHandler TaskScheduled;

        /// <summary>
        /// A task was not successfully scheduled.
        /// </summary>
        public event TaskEventHandler TaskNotScheduled;

        /// <summary>
        /// A task can not be executed.
        /// </summary>
        public event TaskEventHandler TaskNotExecuted;

        /// <summary>
        /// A task execution started.
        /// </summary>
        public event TaskEventHandler TaskExecutionStarted;

        /// <summary>
        /// A task execution finished.
        /// </summary>
        public event TaskEventHandler TaskExecutionFinished;

        /// <summary>
        /// An operation failed.
        /// </summary>
        public event TaskEventHandler OperationFailed;

        #endregion


        #region public methods

        /// <summary>
        /// Removes all scheduled tasks.
        /// </summary>
        public void Clear()
        {
            if (_updating)
            {
                NotifyOperationFailed("Updating. Can not clear scheduled tasks.");

                return;
            }

            lock (_lock)
            {
                _scheduledTasks.Clear();
            }
        }

        /// <summary>
        /// Schedules a task for a future execution.
        /// </summary>
        /// <param name="task">A task to be scheduled.</param>
        /// <returns>True on success.</returns>
        public bool ScheduleTask(ITask task)
        {
            if (_updating)
            {
                NotifyTaskNotScheduled("Can not schedule a new task at " + task.NextRunAt + ". This scheduler is updating its state now.", task);

                return false;
            }

            lock (_lock)
            {
                return ScheduleTaskInternal(task, true);
            }
        }
               
        /// <summary>
        /// Initializes this instance for running new tasks.
        /// Call at the begining and after the Stop() method call, if needed.
        /// </summary>
        public void Init()
        {
            if (_updating)
            {
                NotifyOperationFailed("Updating. Can not initialize.");

                return;
            }

            lock (_lock)
            {
                // Do not create a new cancelation token, if the current one is still OK and not canceled.
                if (_cancellationToken != null && _cancellationToken.IsCancellationRequested == false)
                {
                    return;
                }

                _cancellationToken = new CancellationTokenSource();
            }
        }

        /// <summary>
        /// Updates the internal scheduler state.
        /// Call often. (Every minute or so.)
        /// </summary>
        public void Update()
        {
            if (_updating)
            {
                NotifyOperationFailed("Updating. Can not update.");

                return;
            }

            _updating = true;

            try
            {
                // Find all completed or canceled tasks.
                List<Task<TaskFinishedCode>> completedTasks = null;
                foreach (var task in _runningTasks)
                {
                    if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                    {
                        if (completedTasks == null)
                        {
                            completedTasks = new List<Task<TaskFinishedCode>>();
                        }

                        completedTasks.Add(task);
                    }
                }

                // Remove all completed or canceled tasks.
                if (completedTasks != null && completedTasks.Count > 0)
                {
                    foreach (var completedTask in completedTasks)
                    {
                        _runningTasks.Remove(completedTask);
                    }
                }

                // Do not schedule a new task, if there are too many tasks already running.
                if (_runningTasks.Count >= MaxRunningTasksAllowed)
                {
                    NotifyTaskNotExecuted("Can not execute a new task. Too many tasks are running.", null);

                    return;
                }

                // Do not schedule a new task, if the Init() method was not called yet (at the beggining or after the Stop() method call).
                if (_cancellationToken == null)
                {
                    NotifyTaskNotExecuted("Can not execute a new task. The Init() method was not called yet.", null);

                    return;
                }

                // Find tasks to run.
                var dueTasks = new List<KeyValuePair<DateTime, ITask>>();
                var now = DateTime.Now;
                foreach (var scheduledTaskItem in _scheduledTasks)
                {
                    if (scheduledTaskItem.Key > now)
                    {
                        // This task is in the future.
                        continue;
                    }

                    dueTasks.Add(scheduledTaskItem);
                }

                // Reschedule runned tasks.
                foreach (var taskItem in dueTasks)
                {
                    // Remove the runned task from the list of scheduled tasks.
                    _scheduledTasks.Remove(taskItem.Key);

                    // Run the task assync.
                    if (taskItem.Value.IsActive)
                    {
                        // Run active tasks only.
                        _runningTasks.Add(ExecuteTask(_cancellationToken.Token, taskItem.Value));
                    }

                    // Reschedule it for the next run, if possible.
                    ScheduleTaskInternal(taskItem.Value, false);
                }
            }
            finally
            {
                _updating = false;
            }
        }

        /// <summary>
        /// Stops all running tasks.
        /// </summary>
        public void Stop()
        {
            if (_updating)
            {
                NotifyOperationFailed("Updating. Can not stop.");

                return;
            }

            lock (_lock)
            {
                if (_cancellationToken != null)
                {
                    // false = cancel all.
                    _cancellationToken.Cancel(false);
                    _cancellationToken = null;
                }
            }
        }


        public static System.Threading.Tasks.TaskScheduler GetSynchronizationContext()
        {
            // https://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
            if (SynchronizationContext.Current != null)
            {
                return System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
            }

            // If there is no SyncContext for this thread (e.g. we are in a unit test
            // or console scenario instead of running in an app), then just use the
            // default scheduler because there is no UI thread to sync with.
            return System.Threading.Tasks.TaskScheduler.Current;
        }

        #endregion


        #region private

        private volatile bool _updating = false;
        private readonly object _lock = new object();
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private readonly List<Task<TaskFinishedCode>> _runningTasks = new List<Task<TaskFinishedCode>>();
        private readonly SortedList<DateTime, ITask> _scheduledTasks = new SortedList<DateTime, ITask>();


        private bool ScheduleTaskInternal(ITask task, bool isFirstRun)
        {
            // Reschedule it, if possible.
            if (task.UpdateNextRunAt(isFirstRun) == false)
            {
                // No next run requested.
                return false;
            }

            // A new run requested.
            if (_scheduledTasks.ContainsKey(task.NextRunAt))
            {
                NotifyTaskNotScheduled("Can not schedule a new task at " + task.NextRunAt + ". A different task is scheduled for this time.", task);

                return false;
            }

            // We have a free time slot.
            _scheduledTasks.Add(task.NextRunAt, task);

            NotifyTaskScheduled("A new task scheduled to run at " + task.NextRunAt, task);

            return true;
        }


        private async Task<TaskFinishedCode> ExecuteTask(CancellationToken cancellationToken, ITask task)
        {
            NotifyTaskExecutionStarted("Executing task for " + task.NextRunAt, task);

            TaskFinishedCode result;
            try
            {
                // We are executing this task now...
                task.LatstExecutionTime = DateTime.Now;

                // Hey task, do something!
                result = await Task.Factory.StartNew(
                    task.Action,
                    cancellationToken,
                    TaskCreationOptions.PreferFairness,
                    GetSynchronizationContext());
            }
            catch (OperationCanceledException)
            {
                // If cancellation is requested, an OperationCanceledException results.  
                result = TaskFinishedCode.Canceled;

                NotifyTaskExecutionFinished("The finished task was canceled by the OperationCanceledException exception.", task, result);
            }
            catch (Exception ex)
            {
                result = TaskFinishedCode.Failed;

                NotifyTaskExecutionFinished("The finished task failed with an exception: " + ex.Message, task, result);

                return result;
            }

            switch (result)
            {
                case TaskFinishedCode.Canceled:
                    NotifyTaskExecutionFinished("The finished task was canceled.", task, result);
                    break;

                case TaskFinishedCode.Ok:
                    NotifyTaskExecutionFinished("The finished task state is OK.", task, result);
                    break;

                case TaskFinishedCode.Failed:
                    NotifyTaskExecutionFinished("The finished task failed.", task, result);
                    break;
            }

            return result;
        }


        private void NotifyTaskScheduled(string message, ITask task)
        {
            if (TaskScheduled != null)
            {
                TaskScheduled(this, message, task);
            }
        }


        private void NotifyTaskNotScheduled(string message, ITask task)
        {
            if (TaskNotScheduled != null)
            {
                TaskNotScheduled(this, message, task);
            }
        }


        private void NotifyTaskNotExecuted(string message, ITask task)
        {
            if (TaskNotExecuted != null)
            {
                TaskNotExecuted(this, message, task);
            }
        }


        private void NotifyTaskExecutionStarted(string message, ITask task)
        {
            if (TaskExecutionStarted != null)
            {
                TaskExecutionStarted(this, message, task);
            }
        }


        private void NotifyTaskExecutionFinished(string message, ITask task, TaskFinishedCode taskFinishedCode)
        {
            if (TaskExecutionFinished != null)
            {
                TaskExecutionFinished(this, message, task, taskFinishedCode);
            }
        }


        private void NotifyOperationFailed(string message)
        {
            if (OperationFailed != null)
            {
                OperationFailed(this, message, null, null);
            }
        }

        #endregion
    }
}
