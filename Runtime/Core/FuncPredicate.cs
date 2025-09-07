using System;

namespace NekoFlow
{
    public class FuncPredicate : IPredicate
    {
        private readonly Func<bool> _func;

        public FuncPredicate(Func<bool> func) => _func = func ?? throw new ArgumentNullException(nameof(func));

        public bool Evaluate() => _func();
    }
}
