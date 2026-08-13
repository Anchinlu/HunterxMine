using System;
using MineCraftUnity.WorldGen.Synth;

namespace MineCraftUnity.WorldGen.Density
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.DensityFunctions
    /// </summary>
    public static class DensityFunctions
    {
        public const float GlobalOffset = -0.50375F;

        public static IDensityFunction Interpolated(IDensityFunction function) => new MarkerFunction(MarkerType.Interpolated, function);

        public static IDensityFunction FlatCache(IDensityFunction function) => new MarkerFunction(MarkerType.FlatCache, function);

        public static IDensityFunction Cache2d(IDensityFunction function) => new MarkerFunction(MarkerType.Cache2D, function);

        public static IDensityFunction CacheOnce(IDensityFunction function) => new MarkerFunction(MarkerType.CacheOnce, function);

        public static IDensityFunction BlendDensity(IDensityFunction function) => new MarkerFunction(MarkerType.BlendDensity, function);

        public static IDensityFunction MappedNoise(NoiseHolder noise, double xzScale, double yScale, double minTarget, double maxTarget) =>
            MapFromUnitTo(Noise(noise, xzScale, yScale), minTarget, maxTarget);

        public static IDensityFunction MappedNoise(NoiseHolder noise, double yScale, double minTarget, double maxTarget) =>
            MappedNoise(noise, 1.0, yScale, minTarget, maxTarget);

        public static IDensityFunction MappedNoise(NoiseHolder noise, double minTarget, double maxTarget) =>
            MappedNoise(noise, 1.0, minTarget, maxTarget);

        public static IDensityFunction ShiftedNoise2d(
            IDensityFunction shiftX,
            IDensityFunction shiftZ,
            double xzScale,
            NoiseHolder noise) =>
            new ShiftedNoiseFunction(shiftX, Zero(), shiftZ, xzScale, 0.0, noise);

        public static IDensityFunction Noise(NoiseHolder noise) => Noise(noise, 1.0, 1.0);

        public static IDensityFunction Noise(NoiseHolder noise, double xzScale, double yScale) =>
            new NoiseFunction(noise, xzScale, yScale);

        public static IDensityFunction Noise(NoiseHolder noise, double yScale) => Noise(noise, 1.0, yScale);

        public static IDensityFunction RangeChoice(
            IDensityFunction input,
            double minInclusive,
            double maxExclusive,
            IDensityFunction whenInRange,
            IDensityFunction whenOutOfRange) =>
            new RangeChoiceFunction(input, minInclusive, maxExclusive, whenInRange, whenOutOfRange);

        public static IDensityFunction IntervalSelect(
            IDensityFunction input,
            double[] thresholds,
            IDensityFunction[] functions) =>
            new IntervalSelectFunction(input, thresholds, functions);

        public static IDensityFunction ShiftA(NoiseHolder noise) => new ShiftAFunction(noise);

        public static IDensityFunction ShiftB(NoiseHolder noise) => new ShiftBFunction(noise);

        public static IDensityFunction Add(IDensityFunction f1, IDensityFunction f2) =>
            TwoArgumentFunction.Create(TwoArgumentType.Add, f1, f2);

        public static IDensityFunction Mul(IDensityFunction f1, IDensityFunction f2) =>
            TwoArgumentFunction.Create(TwoArgumentType.Mul, f1, f2);

        public static IDensityFunction Min(IDensityFunction f1, IDensityFunction f2) =>
            TwoArgumentFunction.Create(TwoArgumentType.Min, f1, f2);

        public static IDensityFunction Max(IDensityFunction f1, IDensityFunction f2) =>
            TwoArgumentFunction.Create(TwoArgumentType.Max, f1, f2);

        public static IDensityFunction Spline(ICubicSpline<DensityContext> spline) => new SplineFunction(spline);

        public static IDensityFunction Zero() => ConstantFunction.Zero;

        public static IDensityFunction Constant(double value) => new ConstantFunction(value);

        public static IDensityFunction YClampedGradient(int fromY, int toY, double fromValue, double toValue) =>
            new YClampedGradientFunction(fromY, toY, fromValue, toValue);

        public static IDensityFunction Clamp(IDensityFunction input, double minValue, double maxValue) =>
            new ClampFunction(input, minValue, maxValue);

        public static IDensityFunction Map(IDensityFunction function, DensityMapType type) =>
            MappedFunction.Create(type, function);

        public static IDensityFunction BlendAlpha() => BlendAlphaFunction.Instance;

        public static IDensityFunction BlendOffset() => BlendOffsetFunction.Instance;

        public static IDensityFunction Lerp(IDensityFunction alpha, IDensityFunction first, IDensityFunction second)
        {
            if (first is ConstantFunction constant)
            {
                return Lerp(alpha, constant.Value, second);
            }

            IDensityFunction alphaCached = CacheOnce(alpha);
            IDensityFunction oneMinusAlpha = Add(Mul(alphaCached, Constant(-1.0)), Constant(1.0));
            return Add(Mul(first, oneMinusAlpha), Mul(second, alphaCached));
        }

        public static IDensityFunction Lerp(IDensityFunction factor, double first, IDensityFunction second) =>
            Add(Mul(factor, Add(second, Constant(-first))), Constant(first));

        public static IDensityFunction FindTopSurface(
            IDensityFunction density,
            IDensityFunction upperBound,
            int lowerBound,
            int stepSize) =>
            new FindTopSurfaceFunction(density, upperBound, lowerBound, stepSize);

        private static IDensityFunction MapFromUnitTo(IDensityFunction function, double min, double max)
        {
            double middle = (min + max) * 0.5;
            double factor = (max - min) * 0.5;
            return Add(Constant(middle), Mul(Constant(factor), function));
        }

        public sealed class NoiseHolder
        {
            public NormalNoise Noise { get; }

            public NoiseHolder(NormalNoise noise) => Noise = noise;

            public double GetValue(double x, double y, double z) => Noise.GetValue(x, y, z);

            public double MaxValue => Noise.MaxValue();
        }

        public sealed class SplineCoordinate : IBoundedFloatFunction<DensityContext>
        {
            private readonly IDensityFunction _function;

            public SplineCoordinate(IDensityFunction function) => _function = function;

            public float Apply(DensityContext context) => (float)_function.Compute(in context);

            public float MinValue => (float)_function.MinValue;

            public float MaxValue => (float)_function.MaxValue;
        }

        private enum TwoArgumentType
        {
            Add,
            Mul,
            Min,
            Max
        }

        public enum DensityMapType
        {
            Abs,
            Square,
            Cube,
            HalfNegative,
            QuarterNegative,
            Invert,
            Squeeze
        }

        private enum MarkerType
        {
            Interpolated,
            FlatCache,
            Cache2D,
            CacheOnce,
            BlendDensity
        }

        private sealed class ConstantFunction : IDensityFunction
        {
            public static readonly ConstantFunction Zero = new(0.0);

            public double Value { get; }

            public ConstantFunction(double value) => Value = value;

            public double Compute(in DensityContext context) => Value;

            public double MinValue => Value;

            public double MaxValue => Value;
        }

        private sealed class BlendAlphaFunction : IDensityFunction
        {
            public static readonly BlendAlphaFunction Instance = new();

            public double Compute(in DensityContext context) => 1.0;

            public double MinValue => 1.0;

            public double MaxValue => 1.0;
        }

        private sealed class BlendOffsetFunction : IDensityFunction
        {
            public static readonly BlendOffsetFunction Instance = new();

            public double Compute(in DensityContext context) => 0.0;

            public double MinValue => 0.0;

            public double MaxValue => 0.0;
        }

        private sealed class MarkerFunction : IDensityFunction
        {
            private readonly MarkerType _type;
            private readonly IDensityFunction _wrapped;

            public MarkerFunction(MarkerType type, IDensityFunction wrapped)
            {
                _type = type;
                _wrapped = wrapped;
            }

            public double Compute(in DensityContext context) => _wrapped.Compute(in context);

            public double MinValue => _type == MarkerType.BlendDensity ? double.NegativeInfinity : _wrapped.MinValue;

            public double MaxValue => _type == MarkerType.BlendDensity ? double.PositiveInfinity : _wrapped.MaxValue;
        }

        private sealed class ClampFunction : IDensityFunction
        {
            private readonly IDensityFunction _input;
            private readonly double _minValue;
            private readonly double _maxValue;

            public ClampFunction(IDensityFunction input, double minValue, double maxValue)
            {
                _input = input;
                _minValue = minValue;
                _maxValue = maxValue;
            }

            public double Compute(in DensityContext context)
            {
                return Mth.Clamp(_input.Compute(in context), _minValue, _maxValue);
            }

            public double MinValue => Math.Max(_minValue, _input.MinValue);

            public double MaxValue => Math.Min(_maxValue, _input.MaxValue);
        }

        private sealed class YClampedGradientFunction : IDensityFunction
        {
            private readonly int _fromY;
            private readonly int _toY;
            private readonly double _fromValue;
            private readonly double _toValue;

            public YClampedGradientFunction(int fromY, int toY, double fromValue, double toValue)
            {
                _fromY = fromY;
                _toY = toY;
                _fromValue = fromValue;
                _toValue = toValue;
            }

            public double Compute(in DensityContext context) =>
                Mth.ClampedMap(context.BlockY, _fromY, _toY, _fromValue, _toValue);

            public double MinValue => Math.Min(_fromValue, _toValue);

            public double MaxValue => Math.Max(_fromValue, _toValue);
        }

        private sealed class MappedFunction : IDensityFunction
        {
            public static MappedFunction Create(DensityMapType type, IDensityFunction input)
            {
                double minValue = input.MinValue;
                double maxValue = input.MaxValue;
                double minImage = Transform(type, minValue);
                double maxImage = Transform(type, maxValue);
                if (type == DensityMapType.Invert)
                {
                    if (minValue < 0.0 && maxValue > 0.0)
                    {
                        return new MappedFunction(type, input, double.NegativeInfinity, double.PositiveInfinity);
                    }

                    return new MappedFunction(type, input, maxImage, minImage);
                }

                if (type == DensityMapType.Abs || type == DensityMapType.Square)
                {
                    return new MappedFunction(type, input, Math.Max(0.0, minValue), Math.Max(minImage, maxImage));
                }

                return new MappedFunction(type, input, minImage, maxImage);
            }

            private readonly DensityMapType _type;
            private readonly IDensityFunction _input;

            private MappedFunction(DensityMapType type, IDensityFunction input, double minValue, double maxValue)
            {
                _type = type;
                _input = input;
                MinValue = minValue;
                MaxValue = maxValue;
            }

            public double MinValue { get; }

            public double MaxValue { get; }

            public double Compute(in DensityContext context) => Transform(_type, _input.Compute(in context));

            private static double Transform(DensityMapType type, double input) =>
                type switch
                {
                    DensityMapType.Abs => Math.Abs(input),
                    DensityMapType.Square => input * input,
                    DensityMapType.Cube => input * input * input,
                    DensityMapType.HalfNegative => input > 0.0 ? input : input * 0.5,
                    DensityMapType.QuarterNegative => input > 0.0 ? input : input * 0.25,
                    DensityMapType.Invert => 1.0 / input,
                    DensityMapType.Squeeze => TransformSqueeze(input),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };

            private static double TransformSqueeze(double input)
            {
                double c = Mth.Clamp(input, -1.0, 1.0);
                return c / 2.0 - c * c * c / 24.0;
            }
        }

        private static class TwoArgumentFunction
        {
            public static IDensityFunction Create(TwoArgumentType type, IDensityFunction argument1, IDensityFunction argument2)
            {
                double min1 = argument1.MinValue;
                double min2 = argument2.MinValue;
                double max1 = argument1.MaxValue;
                double max2 = argument2.MaxValue;
                double minValue = type switch
                {
                    TwoArgumentType.Add => min1 + min2,
                    TwoArgumentType.Mul => min1 > 0.0 && min2 > 0.0
                        ? min1 * min2
                        : max1 < 0.0 && max2 < 0.0 ? max1 * max2 : Math.Min(min1 * max2, max1 * min2),
                    TwoArgumentType.Min => Math.Min(min1, min2),
                    TwoArgumentType.Max => Math.Max(min1, max2),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
                double maxValue = type switch
                {
                    TwoArgumentType.Add => max1 + max2,
                    TwoArgumentType.Mul => min1 > 0.0 && min2 > 0.0
                        ? max1 * max2
                        : max1 < 0.0 && max2 < 0.0 ? min1 * min2 : Math.Max(min1 * min2, max1 * max2),
                    TwoArgumentType.Min => Math.Min(max1, max2),
                    TwoArgumentType.Max => Math.Max(max1, max2),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };

                if (type is TwoArgumentType.Mul or TwoArgumentType.Add)
                {
                    if (argument1 is ConstantFunction constant1)
                    {
                        return new MulOrAddFunction(type, argument2, minValue, maxValue, constant1.Value);
                    }

                    if (argument2 is ConstantFunction constant2)
                    {
                        return new MulOrAddFunction(type, argument1, minValue, maxValue, constant2.Value);
                    }
                }

                return new Ap2Function(type, argument1, argument2, minValue, maxValue);
            }
        }

        private sealed class Ap2Function : IDensityFunction
        {
            private readonly TwoArgumentType _type;
            private readonly IDensityFunction _argument1;
            private readonly IDensityFunction _argument2;

            public Ap2Function(
                TwoArgumentType type,
                IDensityFunction argument1,
                IDensityFunction argument2,
                double minValue,
                double maxValue)
            {
                _type = type;
                _argument1 = argument1;
                _argument2 = argument2;
                MinValue = minValue;
                MaxValue = maxValue;
            }

            public double MinValue { get; }

            public double MaxValue { get; }

            public double Compute(in DensityContext context)
            {
                double v1 = _argument1.Compute(in context);
                return _type switch
                {
                    TwoArgumentType.Add => v1 + _argument2.Compute(in context),
                    TwoArgumentType.Mul => v1 == 0.0 ? 0.0 : v1 * _argument2.Compute(in context),
                    TwoArgumentType.Min => v1 < _argument2.MinValue ? v1 : Math.Min(v1, _argument2.Compute(in context)),
                    TwoArgumentType.Max => v1 > _argument2.MaxValue ? v1 : Math.Max(v1, _argument2.Compute(in context)),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        private sealed class MulOrAddFunction : IDensityFunction
        {
            private readonly TwoArgumentType _type;
            private readonly IDensityFunction _input;
            private readonly double _argument;

            public MulOrAddFunction(
                TwoArgumentType type,
                IDensityFunction input,
                double minValue,
                double maxValue,
                double argument)
            {
                _type = type;
                _input = input;
                _argument = argument;
                MinValue = minValue;
                MaxValue = maxValue;
            }

            public double MinValue { get; }

            public double MaxValue { get; }

            public double Compute(in DensityContext context)
            {
                double input = _input.Compute(in context);
                return _type == TwoArgumentType.Add ? input + _argument : input * _argument;
            }
        }

        private sealed class NoiseFunction : IDensityFunction
        {
            private readonly NoiseHolder _noise;
            private readonly double _xzScale;
            private readonly double _yScale;

            public NoiseFunction(NoiseHolder noise, double xzScale, double yScale)
            {
                _noise = noise;
                _xzScale = xzScale;
                _yScale = yScale;
            }

            public double Compute(in DensityContext context) =>
                _noise.GetValue(context.BlockX * _xzScale, context.BlockY * _yScale, context.BlockZ * _xzScale);

            public double MinValue => -MaxValue;

            public double MaxValue => _noise.MaxValue;
        }

        private abstract class ShiftNoiseFunction : IDensityFunction
        {
            protected readonly NoiseHolder OffsetNoise;

            protected ShiftNoiseFunction(NoiseHolder offsetNoise) => OffsetNoise = offsetNoise;

            public abstract double Compute(in DensityContext context);

            public double MinValue => -MaxValue;

            public double MaxValue => OffsetNoise.MaxValue * 4.0;

            protected double ComputeShift(double localX, double localY, double localZ) =>
                OffsetNoise.GetValue(localX * 0.25, localY * 0.25, localZ * 0.25) * 4.0;
        }

        private sealed class ShiftAFunction : ShiftNoiseFunction
        {
            public ShiftAFunction(NoiseHolder offsetNoise) : base(offsetNoise)
            {
            }

            public override double Compute(in DensityContext context) =>
                ComputeShift(context.BlockX, 0.0, context.BlockZ);
        }

        private sealed class ShiftBFunction : ShiftNoiseFunction
        {
            public ShiftBFunction(NoiseHolder offsetNoise) : base(offsetNoise)
            {
            }

            public override double Compute(in DensityContext context) =>
                ComputeShift(context.BlockZ, context.BlockX, 0.0);
        }

        private sealed class ShiftedNoiseFunction : IDensityFunction
        {
            private readonly IDensityFunction _shiftX;
            private readonly IDensityFunction _shiftY;
            private readonly IDensityFunction _shiftZ;
            private readonly double _xzScale;
            private readonly double _yScale;
            private readonly NoiseHolder _noise;

            public ShiftedNoiseFunction(
                IDensityFunction shiftX,
                IDensityFunction shiftY,
                IDensityFunction shiftZ,
                double xzScale,
                double yScale,
                NoiseHolder noise)
            {
                _shiftX = shiftX;
                _shiftY = shiftY;
                _shiftZ = shiftZ;
                _xzScale = xzScale;
                _yScale = yScale;
                _noise = noise;
            }

            public double Compute(in DensityContext context)
            {
                double x = context.BlockX * _xzScale + _shiftX.Compute(in context);
                double y = context.BlockY * _yScale + _shiftY.Compute(in context);
                double z = context.BlockZ * _xzScale + _shiftZ.Compute(in context);
                return _noise.GetValue(x, y, z);
            }

            public double MinValue => -MaxValue;

            public double MaxValue => _noise.MaxValue;
        }

        private sealed class RangeChoiceFunction : IDensityFunction
        {
            private readonly IDensityFunction _input;
            private readonly double _minInclusive;
            private readonly double _maxExclusive;
            private readonly IDensityFunction _whenInRange;
            private readonly IDensityFunction _whenOutOfRange;

            public RangeChoiceFunction(
                IDensityFunction input,
                double minInclusive,
                double maxExclusive,
                IDensityFunction whenInRange,
                IDensityFunction whenOutOfRange)
            {
                _input = input;
                _minInclusive = minInclusive;
                _maxExclusive = maxExclusive;
                _whenInRange = whenInRange;
                _whenOutOfRange = whenOutOfRange;
            }

            public double Compute(in DensityContext context)
            {
                double inputValue = _input.Compute(in context);
                return inputValue >= _minInclusive && inputValue < _maxExclusive
                    ? _whenInRange.Compute(in context)
                    : _whenOutOfRange.Compute(in context);
            }

            public double MinValue => Math.Min(_whenInRange.MinValue, _whenOutOfRange.MinValue);

            public double MaxValue => Math.Max(_whenInRange.MaxValue, _whenOutOfRange.MaxValue);
        }

        private sealed class IntervalSelectFunction : IDensityFunction
        {
            private readonly IDensityFunction _input;
            private readonly double[] _thresholds;
            private readonly IDensityFunction[] _functions;

            public IntervalSelectFunction(IDensityFunction input, double[] thresholds, IDensityFunction[] functions)
            {
                if (thresholds.Length != functions.Length - 1)
                {
                    throw new ArgumentException("Expected thresholds.Length == functions.Length - 1");
                }

                _input = input;
                _thresholds = thresholds;
                _functions = functions;
            }

            public double Compute(in DensityContext context)
            {
                double inputValue = _input.Compute(in context);
                for (var i = 0; i < _thresholds.Length; i++)
                {
                    if (inputValue < _thresholds[i])
                    {
                        return _functions[i].Compute(in context);
                    }
                }

                return _functions[^1].Compute(in context);
            }

            public double MinValue
            {
                get
                {
                    double minValue = double.MaxValue;
                    foreach (IDensityFunction function in _functions)
                    {
                        minValue = Math.Min(function.MinValue, minValue);
                    }

                    return minValue;
                }
            }

            public double MaxValue
            {
                get
                {
                    double maxValue = double.MinValue;
                    foreach (IDensityFunction function in _functions)
                    {
                        maxValue = Math.Max(function.MaxValue, maxValue);
                    }

                    return maxValue;
                }
            }
        }

        private sealed class SplineFunction : IDensityFunction
        {
            private readonly IBoundedFloatFunction<DensityContext> _sampler;

            public SplineFunction(ICubicSpline<DensityContext> spline) => _sampler = CubicSpline.AsSampler(spline);

            public double Compute(in DensityContext context) => _sampler.Apply(context);

            public double MinValue => _sampler.MinValue;

            public double MaxValue => _sampler.MaxValue;
        }

        private sealed class FindTopSurfaceFunction : IDensityFunction
        {
            private readonly IDensityFunction _density;
            private readonly IDensityFunction _upperBound;
            private readonly int _lowerBound;
            private readonly int _cellHeight;

            public FindTopSurfaceFunction(
                IDensityFunction density,
                IDensityFunction upperBound,
                int lowerBound,
                int cellHeight)
            {
                _density = density;
                _upperBound = upperBound;
                _lowerBound = lowerBound;
                _cellHeight = cellHeight;
            }

            public double Compute(in DensityContext context)
            {
                int topY = Mth.Floor(_upperBound.Compute(in context) / _cellHeight) * _cellHeight;
                if (topY <= _lowerBound)
                {
                    return _lowerBound;
                }

                for (int blockY = topY; blockY >= _lowerBound; blockY -= _cellHeight)
                {
                    var pointContext = new DensityContext
                    {
                        BlockX = context.BlockX,
                        BlockY = blockY,
                        BlockZ = context.BlockZ
                    };
                    if (_density.Compute(in pointContext) > 0.0)
                    {
                        return blockY;
                    }
                }

                return _lowerBound;
            }

            public double MinValue => _lowerBound;

            public double MaxValue => Math.Max(_lowerBound, _upperBound.MaxValue);
        }
    }

    public static class DensityFunctionExtensions
    {
        public static IDensityFunction Clamp(this IDensityFunction function, double min, double max) =>
            DensityFunctions.Clamp(function, min, max);

        public static IDensityFunction Abs(this IDensityFunction function) =>
            DensityFunctions.Map(function, DensityFunctions.DensityMapType.Abs);

        public static IDensityFunction Square(this IDensityFunction function) =>
            DensityFunctions.Map(function, DensityFunctions.DensityMapType.Square);

        public static IDensityFunction Cube(this IDensityFunction function) =>
            DensityFunctions.Map(function, DensityFunctions.DensityMapType.Cube);

        public static IDensityFunction HalfNegative(this IDensityFunction function) =>
            DensityFunctions.Map(function, DensityFunctions.DensityMapType.HalfNegative);

        public static IDensityFunction QuarterNegative(this IDensityFunction function) =>
            DensityFunctions.Map(function, DensityFunctions.DensityMapType.QuarterNegative);

        public static IDensityFunction Invert(this IDensityFunction function) =>
            DensityFunctions.Map(function, DensityFunctions.DensityMapType.Invert);

        public static IDensityFunction Squeeze(this IDensityFunction function) =>
            DensityFunctions.Map(function, DensityFunctions.DensityMapType.Squeeze);
    }
}
