using System;
using System.Collections.Generic;

namespace Core
{
    public class FuncBasedEqualityComparer<T> : IEqualityComparer<T>
    {
        private Func<T, T, bool> _compareFunc;

        private Func<T, int> _hashFunc;

        public FuncBasedEqualityComparer(Func<T, T, bool> compareFunc) : this(compareFunc, x => 0)
        {
        }

        public FuncBasedEqualityComparer(Func<T, T, bool> compareFunc, Func<T, int> hashFunc)
        {
            if (compareFunc == null)
                throw new ArgumentNullException(nameof(compareFunc));
            if (hashFunc == null)
                throw new ArgumentNullException(nameof(hashFunc));

            _compareFunc = compareFunc;
            _hashFunc = hashFunc;
        }

        public bool Equals(T x, T y)
        {
            return _compareFunc(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _hashFunc(obj);
        }
    }
}
