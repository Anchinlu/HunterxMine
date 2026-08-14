namespace MineCraftUnity.World
{
    /// <summary>MC ref: net.minecraft.world.level.MoonPhase — 8 lunar phases over 8 days.</summary>
    public enum MoonPhase
    {
        FullMoon = 0,
        WaningGibbous = 1,
        ThirdQuarter = 2,
        WaningCrescent = 3,
        NewMoon = 4,
        WaxingCrescent = 5,
        FirstQuarter = 6,
        WaxingGibbous = 7
    }

    public static class MoonPhaseExtensions
    {
        public static string TextureFileName(this MoonPhase phase)
        {
            return phase switch
            {
                MoonPhase.FullMoon => "full_moon",
                MoonPhase.WaningGibbous => "waning_gibbous",
                MoonPhase.ThirdQuarter => "third_quarter",
                MoonPhase.WaningCrescent => "waning_crescent",
                MoonPhase.NewMoon => "new_moon",
                MoonPhase.WaxingCrescent => "waxing_crescent",
                MoonPhase.FirstQuarter => "first_quarter",
                MoonPhase.WaxingGibbous => "waxing_gibbous",
                _ => "full_moon"
            };
        }

        /// <summary>MC ref: phase index from total world day count.</summary>
        public static MoonPhase FromDayTime(long dayTime)
        {
            var index = (int)((dayTime / WorldTime.TicksPerDay % 8 + 8) % 8);
            return (MoonPhase)index;
        }
    }
}
