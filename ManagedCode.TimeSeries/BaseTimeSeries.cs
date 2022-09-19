using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries;

public abstract class BaseTimeSeries<T, TSample>
{
    protected const int _defaultSampleCount = 100;
    protected readonly int _samplesCount;

    protected BaseTimeSeries(TimeSpan sampleInterval, int samplesCount)
    {
        _samplesCount = samplesCount;
        SampleInterval = sampleInterval;
        Start = DateTimeOffset.UtcNow.Round(SampleInterval);
        End = Start;
    }

    protected BaseTimeSeries(TimeSpan sampleInterval, int samplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset lastDate)
    {
        _samplesCount = samplesCount;
        SampleInterval = sampleInterval;
        Start = start.Round(SampleInterval);
        End = end.Round(SampleInterval);
        LastDate = lastDate;
    }

    public Dictionary<DateTimeOffset, TSample> Samples { get; protected set; } = new();
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; protected set; }
    public TimeSpan SampleInterval { get; }

    public DateTimeOffset LastDate { get; protected set; }

    public int SamplesCount => Samples.Count;
    public ulong DataCount { get; protected set; }

    public bool IsFull => Samples.Count >= _samplesCount;
    public bool IsOverflow => Samples.Count > _samplesCount;

    public void AddNewData(T data)
    {
        DataCount += 1;
        var rounded = DateTimeOffset.UtcNow.Round(SampleInterval);
        AddData(rounded, data);
        End = rounded;
        LastDate = DateTimeOffset.UtcNow;
        CheckSamplesSize();
    }

    public void AddNewData(DateTimeOffset dateTimeOffset, T data)
    {
        DataCount += 1;
        AddData(dateTimeOffset.Round(SampleInterval), data);
        CheckSamplesSize();
    }

    protected bool CheckSamplesSize()
    {
        if (_samplesCount <= 0)
        {
            return false;
        }

        while (IsOverflow)
        {
            Samples.Remove(Samples.Keys.MinBy(o => o)); //check performance here
        }

        return true;
    }

    public void MarkupAllSamples(MarkupDirection direction = MarkupDirection.Past)
    {
        var samples = _samplesCount > 0 ? _samplesCount : _defaultSampleCount;

        if (direction is MarkupDirection.Past or MarkupDirection.Feature)
        {
            var now = Start;
            for (var i = 0; i < samples; i++)
            {
                now = now.Round(SampleInterval);
                if (!Samples.ContainsKey(now))
                {
                    if (typeof(TSample).IsClass)
                    {
                        Samples.Add(now, (TSample)Activator.CreateInstance(typeof(TSample)));
                    }
                    else
                    {
                        Samples.Add(now, default);
                    }
                }

                now = direction is MarkupDirection.Feature ? now.Add(SampleInterval) : now.Subtract(SampleInterval);
            }
        }
        else
        {
            var nowForFeature = Start;
            var nowForPast = Start;

            for (var i = 0; i < samples / 2 + 1; i++)
            {
                nowForFeature = nowForFeature.Round(SampleInterval);
                nowForPast = nowForPast.Round(SampleInterval);

                if (!Samples.ContainsKey(nowForFeature))
                {
                    if (typeof(TSample).IsClass)
                    {
                        Samples.Add(nowForFeature, (TSample)Activator.CreateInstance(typeof(TSample)));
                    }
                    else
                    {
                        Samples.Add(nowForFeature, default);
                    }
                }

                if (!Samples.ContainsKey(nowForPast))
                {
                    if (typeof(TSample).IsClass)
                    {
                        Samples.Add(nowForPast, (TSample)Activator.CreateInstance(typeof(TSample)));
                    }
                    else
                    {
                        Samples.Add(nowForPast, default);
                    }
                }

                nowForFeature = nowForFeature.Add(SampleInterval);
                nowForPast = nowForPast.Subtract(SampleInterval);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void AddData(DateTimeOffset now, T data);
}