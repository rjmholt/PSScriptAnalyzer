using System;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    internal interface IValueBox<out T>
    {
        bool IsValueCreated { get; }

        T Value { get; }
    }

    internal class LazyValueBox<T> : IValueBox<T>
    {
        private readonly Lazy<T> _lazyValue;

        public LazyValueBox(Func<T> factory)
            : this(new Lazy<T>(factory))
        {
        }

        public LazyValueBox(Lazy<T> lazyValue)
        {
            _lazyValue = lazyValue;
        }

        public bool IsValueCreated => _lazyValue.IsValueCreated;

        public T Value => _lazyValue.Value;
    }

    internal class StrictValueBox<T> : IValueBox<T>
    {
        public StrictValueBox(T value)
        {
            Value = value;
        }

        public bool IsValueCreated => true;

        public T Value { get; }
    }
}
