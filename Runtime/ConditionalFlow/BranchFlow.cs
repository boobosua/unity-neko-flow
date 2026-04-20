using System;
using System.Collections.Generic;

namespace NekoFlow.Conditional
{
    /// <summary>
    /// A conditional flow that evaluates multiple branches in order and executes the first matching action.
    /// </summary>
    public sealed class BranchFlow
    {
        private const int DefaultCapacity = 8;

        private readonly struct Branch
        {
            public readonly Func<bool> Predicate;
            public readonly Action Action;

            public Branch(Func<bool> predicate, Action action)
            {
                Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
                Action = action ?? throw new ArgumentNullException(nameof(action));
            }
        }

        private readonly List<Branch> _branches;
        private Action _fallback;

        public BranchFlow()
        {
            _branches = new List<Branch>(DefaultCapacity);
            _fallback = null;
        }

        public BranchFlow When(Func<bool> predicate, Action action)
        {
            _branches.Add(new Branch(predicate, action));
            return this;
        }

        public BranchFlow Otherwise(Action fallback)
        {
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            return this;
        }

        /// <summary>
        /// Execute the branch flow, evaluating each branch in order and executing the first matching action.
        /// </summary>
        public FlowResult Execute()
        {
            for (int i = 0; i < _branches.Count; i++)
            {
                var b = _branches[i];
                if (b.Predicate())
                {
                    b.Action();
                    return FlowResult.Matched;
                }
            }

            if (_fallback != null)
            {
                _fallback();
                return FlowResult.Fallback;
            }

            return FlowResult.None;
        }

        /// <summary>
        /// Clear all branches and the fallback action.
        /// </summary>
        public BranchFlow Clear()
        {
            _branches.Clear();
            _fallback = null;
            return this;
        }
    }
}
