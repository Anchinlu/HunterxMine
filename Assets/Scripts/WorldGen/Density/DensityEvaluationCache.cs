using System.Collections.Generic;

namespace MineCraftUnity.WorldGen.Density
{
    /// <summary>
    /// Per-column cache for MC density marker functions (Cache2d / CacheOnce / FlatCache).
    /// Scoped to one world column during chunk fill — not shared across columns or threads.
    /// </summary>
    public sealed class DensityEvaluationCache
    {
        private readonly int _columnX;
        private readonly int _columnZ;
        private readonly Dictionary<int, double> _cache2d = new();
        private readonly Dictionary<int, double> _cacheOnce = new();

        public DensityEvaluationCache(int columnX, int columnZ)
        {
            _columnX = columnX;
            _columnZ = columnZ;
        }

        public void BeginSample()
        {
            _cacheOnce.Clear();
        }

        public bool TryGetCache2d(int functionId, in DensityContext context, out double value)
        {
            if (context.BlockX != _columnX || context.BlockZ != _columnZ)
            {
                value = 0.0;
                return false;
            }

            return _cache2d.TryGetValue(functionId, out value);
        }

        public void SetCache2d(int functionId, double value)
        {
            _cache2d[functionId] = value;
        }

        public bool TryGetCacheOnce(int functionId, out double value) =>
            _cacheOnce.TryGetValue(functionId, out value);

        public void SetCacheOnce(int functionId, double value)
        {
            _cacheOnce[functionId] = value;
        }
    }
}
