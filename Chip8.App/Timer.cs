using System;
using System.Diagnostics;
using Raylib_cs;

namespace LandoWorks.Emulation.Chip8
{
    public class Timer
    {
        private Stopwatch Stopwatch;
        private long tickFrequency;

        private long nextTick_TicksPassed;
        private long nextTick_HasTicked;
            
        public Timer(long hz = 60)
        {
            Stopwatch = new Stopwatch();
            tickFrequency = Stopwatch.Frequency / hz;
            nextTick_HasTicked = tickFrequency;
        }

        public void Start()
        {    
            Stopwatch.Start();
        }

        /// <summary>
        /// Returns if one of the frequency ticks has occured and sets the next tick
        /// based on the current elapsed ticks. This adds potential drift but ensures
        /// every tick is accounted for.
        /// </summary>
        /// <returns></returns>
        public bool HasTicked()
        {
            var tickDifference = Stopwatch.ElapsedTicks - nextTick_HasTicked;

            if (tickDifference < 0)
                return false;

            nextTick_HasTicked = Stopwatch.ElapsedTicks + tickFrequency;
            return true;
        }

        /// <summary>
        /// Returns how many ticks have passed since the last call.
        /// </summary>
        /// <returns></returns>
        public int TicksPassed()
        {
            // Do not calculate the number of ticks passed directly here because
            // doing the division on these when the timer is early will result
            // in the wrong value, unless you do casting and floor the result.
            //
            // e.g. when using integer division: (100 - 150) / 100 + 1 = 1
            // because -50 / 100 = 0, not -0.5.
            var tickDifference = Stopwatch.ElapsedTicks - nextTick_TicksPassed;
            var ticksPassed = 0;

            if (tickDifference >= 0)
            {
                ticksPassed = (int)(tickDifference / tickFrequency) + 1;
                nextTick_TicksPassed += ticksPassed * tickFrequency;
            }

            return ticksPassed;
        }
    }
}

/*
 public int Update()
{
    // We need to cast the elapsed ticks to a double so that we end up with
    // -50.0 instead of -50 so that after the division we end up with -0.5
    // instead of 0 for when we add one and then floor it. Otherwise an early
    // timer would end up with one additional tick.
    var ticksPassed = (double)(Stopwatch.ElapsedTicks - nextTick) / tickFrequency + 1;
    ticksPassed = Math.Floor(ticksPassed);

    if (ticksPassed > 0)
    {
        nextTick += (int)ticksPassed * tickFrequency;    
        return (int)ticksPassed;
    }

    return 0;
}
*/