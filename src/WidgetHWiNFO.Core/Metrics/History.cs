namespace WidgetHWiNFO.Core.Metrics;

/// <summary>Fixed-size ring buffer of samples used for sparklines. Missing samples are stored as NaN.</summary>
public sealed class History
{
    private readonly double[] _items;
    private int _start;

    public History(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _items = new double[capacity];
    }

    public int Capacity => _items.Length;
    public int Count { get; private set; }

    /// <summary>Incremented on every change, so renderers can skip redundant work.</summary>
    public int Version { get; private set; }

    public void Add(double? value)
    {
        double v = value ?? double.NaN;
        if (Count < _items.Length)
        {
            _items[(_start + Count) % _items.Length] = v;
            Count++;
        }
        else
        {
            _items[_start] = v;
            _start = (_start + 1) % _items.Length;
        }
        Version++;
    }

    public void Clear()
    {
        _start = 0;
        Count = 0;
        Version++;
    }

    /// <summary>Sample by age order: index 0 is the oldest.</summary>
    public double this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _items[(_start + index) % _items.Length];
        }
    }

    public double Max()
    {
        double max = double.NaN;
        for (int i = 0; i < Count; i++)
        {
            double v = this[i];
            if (double.IsFinite(v) && (double.IsNaN(max) || v > max)) max = v;
        }
        return max;
    }
}
