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

        public WorldTime(long startDayTime = 1000)
        {
            DayTime = startDayTime;
        }

        public void SetDayTime(long dayTime) => DayTime = dayTime;

        public void Advance(float deltaSeconds, float timeScale = 1f)
        {
            if (deltaSeconds <= 0f || timeScale <= 0f)
            {
                return;
            }

            DayTime += (long)(deltaSeconds * TicksPerSecond * timeScale);
        }

        /// <summary>MC clock: tick 0 = 06:00, 6000 = 12:00, 12000 = 18:00, 18000 = 00:00.</summary>
        public static string FormatClock(long dayTime)
        {
            var tick = dayTime % TicksPerDay;
            if (tick < 0)
            {
                tick += TicksPerDay;
            }

            var totalMinutes = (tick * 24 * 60) / TicksPerDay;
            var hours = (totalMinutes / 60 + 6) % 24;
            var minutes = totalMinutes % 60;
            return $"{hours:00}:{minutes:00}";
        }
    }
}
