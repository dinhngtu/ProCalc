using System.Numerics;

namespace ProCalcCore;

public interface IStackEntry {
    public object Object { get; }
    string? Comment { get; set; }
    string? AltComment { get; set; }
}

internal struct StackEntry<T> : IStackEntry
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T> {
    public T Value { get; set; }
    public readonly object Object => Value;
    public string? Comment { get; set; }
    public string? AltComment { get; set; }
}
