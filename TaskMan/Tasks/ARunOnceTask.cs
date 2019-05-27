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

namespace TaskMan.Tasks
{
    /// <summary>
    /// A base for tasks, that run just once.
    /// </summary>
    public abstract class ARunOnceTask : ABaseTask
    {
        /// <summary>
        /// Calculates the NextRunAt based on the DateTime.Now property.
        /// </summary>
        /// <param name="isFirstRun">If true, this task will run for the first time. (Is being scheduled.)</param>
        /// <returns>True, if a new run time was calculated.</returns> 
        public override bool UpdateNextRunAt(bool isFirstRun)
        {
            if (isFirstRun)
            {
                NextRunAt = RunAt;

                return true;
            }

            return false;
        }
    }
}
