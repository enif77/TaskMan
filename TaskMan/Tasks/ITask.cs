/* TaskMan 1.0 - (C) 2017 Premysl Fara 
 
TaskMan 1.0 and newer are available under the zlib license:
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

namespace TaskMan.Tasks
{
    using System;


    /// <summary>
    /// How a task execution finished.
    /// </summary>
    public enum TaskFinishedCode
    {
        Ok,
        Failed,
        Canceled
    }


    /// <summary>
    /// Defines a scheduled task interface.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Determines, if this task is active, so its body is executed.
        /// If false, this task is scheduled for runs, but its body is not executed.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// At which time this task should run.
        /// </summary>
        DateTime RunAt { get; set; }

        /// <summary>
        /// The last time, when this task was executed.
        /// </summary>
        DateTime LatstExecutionTime { get; set; }

        /// <summary>
        /// The next time, when this task should be executed.
        /// </summary> 
        DateTime NextRunAt { get; }

        /// <summary>
        /// An optional shared task state.
        /// </summary>
        object State { get; set; }

        /// <summary>
        /// The action of this task.
        /// </summary>
        /// <returns>A TaskFinishedCode constant.</returns>
        TaskFinishedCode Action();

        /// <summary>
        /// Calculates the NextRunAt based on the DateTime.Now property.
        /// </summary>
        /// <param name="isFirstRun">If true, this task will run for the first time. (Is being scheduled.)</param>
        /// <returns>True, if a new run time was calculated.</returns>
        bool UpdateNextRunAt(bool isFirstRun);
    }
}
