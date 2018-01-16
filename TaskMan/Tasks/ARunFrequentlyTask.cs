/* TaskMan 1.0 - (C) 2017 - 2018 Premysl Fara 
 
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
    /// A base for tasks, that run every N minutes.
    /// </summary>
    public abstract class ARunFrequentlyTask : ABaseTask
    {
        /// <summary>
        /// A minutes between runs.
        /// </summary>
        public int MinutesBetweenRuns { get; }


        protected ARunFrequentlyTask(int minutesBetweenRuns)
            : base()
        {
            if (minutesBetweenRuns <= 0) throw new ArgumentException("The minutesBetweenRuns > 0 expected.");

            MinutesBetweenRuns = minutesBetweenRuns;
        }

        /// <summary>
        /// Calculates the NextRunAt based on the DateTime.Now property.
        /// </summary>
        /// <param name="isFirstRun">If true, this task will run for the first time. (Is being scheduled.)</param>
        /// <returns>True, if a new run time was calculated.</returns>
        public override bool UpdateNextRunAt(bool isFirstRun)
        {
            var now = DateTime.Now;

            // If this task was never executed, or if it was executed long time ago.
            if (LatstExecutionTime < now.AddMinutes(-(MinutesBetweenRuns * 2)))
            {
                // Long time ago, so schedule it for now.
                LatstExecutionTime = now.AddMinutes(-MinutesBetweenRuns);

                // If the RunAt is set to something in the future, use it.
                if (LatstExecutionTime < RunAt)
                {
                    LatstExecutionTime = RunAt.AddMinutes(-MinutesBetweenRuns);
                }
            }

            var atTime = LatstExecutionTime.AddMinutes(MinutesBetweenRuns);

            // We missed the time, lets do it later.
            while (now > atTime)
            {
                atTime = atTime.AddMinutes(MinutesBetweenRuns);
            }

            NextRunAt = atTime;

            return true;
        }
    }
}
