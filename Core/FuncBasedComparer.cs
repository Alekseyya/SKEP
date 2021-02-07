using System;
using System.Collections.Generic;

namespace Core
{
    public class FuncBasedComparer<T> : IComparer<T>
    {
        private Func<T, T, int> _compareFunc;

        public FuncBasedComparer(Func<T, T, int> compareFunc)
        {
            if (compareFunc == null)
                throw new ArgumentNullException(nameof(compareFunc));

            _compareFunc = compareFunc;
        }

        public int Compare(T x, T y)
        {
            return _compareFunc(x, y);
        }
    }
}
