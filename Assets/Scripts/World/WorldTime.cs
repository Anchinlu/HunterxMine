namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: SharedConstants.TICKS_PER_GAME_DAY (24000), 20 TPS world clock.
    /// </summary>
    public sealed class WorldTime
    {
        public const int TicksPerDay = 24000;
        public const float TicksPerSecond = 20f;

        public long DayTime { get; private set; }

        /// <summary>Fractional progress toward the next whole tick (for debug display).</summary>
        public float TickRemainder { get; private set; }

        public float DayFraction
        {
            get
            {
                var tick = DayTime % TicksPerDay;
                if (tick < 0)
                {
                    tick += TicksPerDay;
                }

                return tick / (float)TicksPerDay;
            }
        }

        /// <summary>MC ref: lunar phase cycles every 8 overworld days.</summary>
        public MoonPhase MoonPhase => MoonPhaseExtensions.FromDayTime(DayTime);

        public WorldTime(long startDayTime = 1000)
        {
            DayTime = startDayTime;
        }

        public void SetDayTime(long dayTime)
        {
            DayTime = dayTime;
            TickRemainder = 0f;
        }

        public void Advance(float deltaSeconds, float timeScale = 1f)
        {
            if (deltaSeconds <= 0f || timeScale <= 0f)
            {
                return;
            }

            TickRemainder += deltaSeconds * TicksPerSecond * timeScale;
            var wholeTicks = (long)TickRemainder;
            if (wholeTicks == 0)
            {
                return;
            }

            DayTime += wholeTicks;
            TickRemainder -= wholeTicks;
        }

        /// <summary>Tick within the current MC day (0–23999).</summary>
        public static long NormalizeDayTick(long dayTime)
        {
            var tick = dayTime % TicksPerDay;
            if (tick < 0)
            {
                tick += TicksPerDay;
            }

            return tick;
        }

        /// <summary>MC clock: tick 0 = 06:00, 6000 = 12:00, 12000 = 18:00, 18000 = 00:00.</summary>
        public static string FormatClock(long dayTime)
        {
            var tick = NormalizeDayTick(dayTime);
            var totalMinutes = (tick * 24 * 60) / TicksPerDay;
            var hours = (totalMinutes / 60 + 6) % 24;
            var minutes = totalMinutes % 60;
            return $"{hours:00}:{minutes:00}";
        }

        /// <summary>F3 overlay: wall clock + live day tick (20 ticks ≈ 1 real second at 1×).</summary>
        public string FormatDebugTime()
        {
            var tick = NormalizeDayTick(DayTime) + TickRemainder;
            return $"{FormatClock(DayTime)}  tick {tick:00000.0} / {TicksPerDay}";
        }

        /// <summary>F3 overlay from raw dayTime (no sub-tick fraction).</summary>
        public static string FormatDebugTime(long dayTime)
        {
            var tick = NormalizeDayTick(dayTime);
            return $"{FormatClock(dayTime)}  tick {tick,5} / {TicksPerDay}";
        }
    }
}
