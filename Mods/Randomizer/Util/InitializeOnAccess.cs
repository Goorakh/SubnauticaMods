using System;

namespace GRandomizer.Util
{
    public class InitializeOnAccess<T>
    {
        readonly Func<T> _initializeFunc;

        bool _isInitialized;
        T _value;

        public InitializeOnAccess(Func<T> initializeFunc)
        {
            _initializeFunc = initializeFunc;
        }

        public T Get
        {
            get
            {
                if (!_isInitialized)
                {
                    _value = _initializeFunc();
                    _isInitialized = true;
                }

                return _value;
            }
        }

        public void Reset()
        {
            _isInitialized = false;
            _value = default;
        }

        public static implicit operator T(InitializeOnAccess<T> initOnAccess)
        {
            return initOnAccess.Get;
        }
    }
}
