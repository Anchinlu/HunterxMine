using System;
using System.Collections.Generic;
using MineCraftUnity.WorldGen.Synth;

namespace MineCraftUnity.WorldGen.Density
{
    /// <summary>
    /// MC ref: net.minecraft.util.BoundedFloatFunction
    /// </summary>
    public interface IBoundedFloatFunction<in TContext>
    {
        float Apply(TContext context);
        float MinValue { get; }
        float MaxValue { get; }
    }

    /// <summary>
    /// MC ref: net.minecraft.util.CubicSpline — minimal runtime sampler for TerrainProvider.
    /// </summary>
    public interface ICubicSpline<in TContext>
    {
        float MinValue { get; }
        float MaxValue { get; }
    }

    public static class CubicSpline
    {
        public static ICubicSpline<TContext> Constant<TContext>(float value) => new ConstantSpline<TContext>(value);

        public static Builder<TContext> CreateBuilder<TContext>(IBoundedFloatFunction<TContext> coordinate) =>
            new Builder<TContext>(coordinate, v => v);

        public static Builder<TContext> CreateBuilder<TContext>(
            IBoundedFloatFunction<TContext> coordinate,
            Func<float, float> valueTransformer) =>
            new Builder<TContext>(coordinate, valueTransformer);

        public static float Sample<TContext>(ICubicSpline<TContext> spline, TContext coordinate) =>
            spline switch
            {
                ConstantSpline<TContext> constant => constant.Value,
                MultipointSpline<TContext> multipoint => MultipointSpline<TContext>.Sample(multipoint, coordinate),
                _ => throw new ArgumentException("Unknown spline type", nameof(spline))
            };

        public static IBoundedFloatFunction<TContext> AsSampler<TContext>(ICubicSpline<TContext> spline) =>
            spline switch
            {
                ConstantSpline<TContext> constant => BoundedFloatFunction.Constant<TContext>(constant.Value),
                MultipointSpline<TContext> multipoint => new Sampler<TContext>(multipoint),
                _ => throw new ArgumentException("Unknown spline type", nameof(spline))
            };

        public sealed class Builder<TContext>
        {
            private readonly IBoundedFloatFunction<TContext> _coordinate;
            private readonly Func<float, float> _valueTransformer;
            private readonly List<float> _locations = new();
            private readonly List<ICubicSpline<TContext>> _values = new();
            private readonly List<float> _derivatives = new();

            internal Builder(IBoundedFloatFunction<TContext> coordinate, Func<float, float> valueTransformer)
            {
                _coordinate = coordinate;
                _valueTransformer = valueTransformer;
            }

            public Builder<TContext> AddPoint(float location, float value) =>
                AddPoint(location, Constant<TContext>(_valueTransformer(value)), 0.0F);

            public Builder<TContext> AddPoint(float location, float value, float derivative) =>
                AddPoint(location, Constant<TContext>(_valueTransformer(value)), derivative);

            public Builder<TContext> AddPoint(float location, ICubicSpline<TContext> sampler) =>
                AddPoint(location, sampler, 0.0F);

            private Builder<TContext> AddPoint(float location, ICubicSpline<TContext> sampler, float derivative)
            {
                if (_locations.Count > 0 && location <= _locations[^1])
                {
                    throw new ArgumentException("Please register points in ascending order");
                }

                _locations.Add(location);
                _values.Add(sampler);
                _derivatives.Add(derivative);
                return this;
            }

            public ICubicSpline<TContext> Build()
            {
                if (_locations.Count == 0)
                {
                    throw new InvalidOperationException("No elements added");
                }

                return new MultipointSpline<TContext>(
                    _coordinate,
                    _locations.ToArray(),
                    _values.ToArray(),
                    _derivatives.ToArray());
            }
        }

        private sealed class ConstantSpline<TContext> : ICubicSpline<TContext>
        {
            public float Value { get; }

            public ConstantSpline(float value) => Value = value;

            public float MinValue => Value;
            public float MaxValue => Value;
        }

        private sealed class MultipointSpline<TContext> : ICubicSpline<TContext>
        {
            private readonly IBoundedFloatFunction<TContext> _coordinate;
            private readonly float[] _locations;
            private readonly ICubicSpline<TContext>[] _values;
            private readonly float[] _derivatives;

            public MultipointSpline(
                IBoundedFloatFunction<TContext> coordinate,
                float[] locations,
                ICubicSpline<TContext>[] values,
                float[] derivatives)
            {
                if (locations.Length != values.Length || locations.Length != derivatives.Length)
                {
                    throw new ArgumentException("All lengths must be equal");
                }

                if (locations.Length == 0)
                {
                    throw new ArgumentException("Cannot create a multipoint spline with no points");
                }

                _coordinate = coordinate;
                _locations = locations;
                _values = values;
                _derivatives = derivatives;

                float minValue = float.PositiveInfinity;
                float maxValue = float.NegativeInfinity;
                int lastIndex = locations.Length - 1;
                float minInput = _coordinate.MinValue;
                float maxInput = _coordinate.MaxValue;

                if (minInput < locations[0])
                {
                    float edge1 = LinearExtend(minInput, locations, values[0].MinValue, derivatives, 0);
                    float edge2 = LinearExtend(minInput, locations, values[0].MaxValue, derivatives, 0);
                    minValue = Math.Min(minValue, Math.Min(edge1, edge2));
                    maxValue = Math.Max(maxValue, Math.Max(edge1, edge2));
                }

                if (maxInput > locations[lastIndex])
                {
                    float edge1 = LinearExtend(maxInput, locations, values[lastIndex].MinValue, derivatives, lastIndex);
                    float edge2 = LinearExtend(maxInput, locations, values[lastIndex].MaxValue, derivatives, lastIndex);
                    minValue = Math.Min(minValue, Math.Min(edge1, edge2));
                    maxValue = Math.Max(maxValue, Math.Max(edge1, edge2));
                }

                foreach (ICubicSpline<TContext> value in values)
                {
                    minValue = Math.Min(minValue, value.MinValue);
                    maxValue = Math.Max(maxValue, value.MaxValue);
                }

                for (var i = 0; i < lastIndex; i++)
                {
                    float x1 = locations[i];
                    float x2 = locations[i + 1];
                    float xDiff = x2 - x1;
                    float min1 = values[i].MinValue;
                    float max1 = values[i].MaxValue;
                    float min2 = values[i + 1].MinValue;
                    float max2 = values[i + 1].MaxValue;
                    float d1 = derivatives[i];
                    float d2 = derivatives[i + 1];
                    if (d1 != 0.0F || d2 != 0.0F)
                    {
                        float p1 = d1 * xDiff;
                        float p2 = d2 * xDiff;
                        float minLerp1 = Math.Min(min1, min2);
                        float maxLerp1 = Math.Max(max1, max2);
                        float minA = p1 - max2 + min1;
                        float maxA = p1 - min2 + max1;
                        float minB = -p2 + min2 - max1;
                        float maxB = -p2 + max2 - min1;
                        float minLerp2 = Math.Min(minA, minB);
                        float maxLerp2 = Math.Max(maxA, maxB);
                        minValue = Math.Min(minValue, minLerp1 + 0.25F * minLerp2);
                        maxValue = Math.Max(maxValue, maxLerp1 + 0.25F * maxLerp2);
                    }
                }

                MinValue = minValue;
                MaxValue = maxValue;
            }

            public float MinValue { get; }
            public float MaxValue { get; }

            public static float Sample(MultipointSpline<TContext> sampler, TContext context)
            {
                float input = sampler._coordinate.Apply(context);
                int start = FindIntervalStart(sampler._locations, input);
                int lastIndex = sampler._locations.Length - 1;
                if (start < 0)
                {
                    return LinearExtend(
                        input,
                        sampler._locations,
                        Sample(sampler._values[0], context),
                        sampler._derivatives,
                        0);
                }

                if (start == lastIndex)
                {
                    return LinearExtend(
                        input,
                        sampler._locations,
                        Sample(sampler._values[lastIndex], context),
                        sampler._derivatives,
                        lastIndex);
                }

                float x1 = sampler._locations[start];
                float x2 = sampler._locations[start + 1];
                float t = (input - x1) / (x2 - x1);
                float d1 = sampler._derivatives[start];
                float d2 = sampler._derivatives[start + 1];
                float y1 = Sample(sampler._values[start], context);
                float y2 = Sample(sampler._values[start + 1], context);
                float a = d1 * (x2 - x1) - (y2 - y1);
                float b = -d2 * (x2 - x1) + (y2 - y1);
                return (float)Mth.Lerp(t, y1, y2) + t * (1.0F - t) * (float)Mth.Lerp(t, a, b);
            }

            private static float Sample(ICubicSpline<TContext> spline, TContext context) =>
                CubicSpline.Sample(spline, context);

            private static float LinearExtend(float input, float[] locations, float value, float[] derivatives, int index)
            {
                float derivative = derivatives[index];
                return derivative == 0.0F ? value : value + derivative * (input - locations[index]);
            }

            private static int FindIntervalStart(float[] locations, float input) =>
                Mth.BinarySearch(0, locations.Length, i => input < locations[i]) - 1;
        }

        private sealed class Sampler<TContext> : IBoundedFloatFunction<TContext>
        {
            private readonly MultipointSpline<TContext> _multipoint;

            public Sampler(MultipointSpline<TContext> multipoint) => _multipoint = multipoint;

            public float Apply(TContext context) => MultipointSpline<TContext>.Sample(_multipoint, context);

            public float MinValue => _multipoint.MinValue;
            public float MaxValue => _multipoint.MaxValue;
        }

        private static class BoundedFloatFunction
        {
            public static IBoundedFloatFunction<TContext> Constant<TContext>(float value) =>
                new ConstantFunction<TContext>(value);

            private sealed class ConstantFunction<TContext> : IBoundedFloatFunction<TContext>
            {
                private readonly float _value;

                public ConstantFunction(float value) => _value = value;

                public float Apply(TContext context) => _value;
                public float MinValue => _value;
                public float MaxValue => _value;
            }
        }
    }
}
