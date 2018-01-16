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

namespace TaskMan.TestApp
{
    using System;
    using System.Threading;

    using TaskMan.Tasks;


    /// <summary>
    /// A frequently running task.
    /// </summary>
    public class DoItFrequentlyTask : ARunFrequentlyTask
    {
        /// <summary>
        /// A name of this task.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// For how long this task should be "working" in seconds.
        /// </summary>
        public int SleepSeconds { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="minutesBetweenRuns">How many minutes should be between two runs of this task.</param>
        public DoItFrequentlyTask(int minutesBetweenRuns)
            : base(minutesBetweenRuns)
        {
            Name = string.Empty;
            SleepSeconds = 5;
        }


        public override TaskFinishedCode Action()
        {
            Console.WriteLine("{0}: I am working for {1} seconds...", Name, SleepSeconds);

            Thread.Sleep(SleepSeconds * 1000);

            Console.WriteLine("{0}: I am finished.", Name);

            return TaskFinishedCode.Ok;
        }
    }
}
