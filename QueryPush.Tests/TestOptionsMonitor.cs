using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace QueryPush.Tests;

public class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    public TestOptionsMonitor(T currentValue)
    {
        CurrentValue = currentValue;
    }

    public T CurrentValue { get; }

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string> listener) => new TestDisposable();

    private class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
