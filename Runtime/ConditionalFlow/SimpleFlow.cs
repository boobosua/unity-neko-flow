using System;

namespace NekoFlow.Conditional
{
    /// <summary>
    /// A simple conditional flow that executes one of two actions based on a predicate.
    /// </summary>
    public sealed class SimpleFlow
    {
        private readonly Func<bool> _predicate;
        private readonly Action _onSuccess;
        private readonly Action _onFailure;

        public SimpleFlow(Func<bool> predicate, Action onSuccess, Action onFailure = null)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _onSuccess = onSuccess ?? throw new ArgumentNullException(nameof(onSuccess));
            _onFailure = onFailure;
        }

        /// <summary>
        /// Execute the simple flow, invoking the success action if the predicate is true, otherwise invoking the failure action if provided.
        /// </summary>
        public bool Execute()
        {
            if (_predicate())
            {
                _onSuccess();
                return true;
            }

            _onFailure?.Invoke();
            return false;
        }
    }
}
