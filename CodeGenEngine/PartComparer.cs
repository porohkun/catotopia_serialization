using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace CodeGenEngine
{
    internal class PartComparer<T> : IEqualityComparer<T>
    {
        private Func<T, object> _getComparePart;

        internal PartComparer(Func<T, object> getComparePart)
        {
            _getComparePart = getComparePart;
            if (_getComparePart == null)
                throw new ArgumentNullException();
        }

        public bool Equals(T x, T y)
        {
            return _getComparePart(x).Equals(_getComparePart(y));
        }

        public int GetHashCode(T obj)
        {
            return _getComparePart(obj).GetHashCode();
        }
    }
}