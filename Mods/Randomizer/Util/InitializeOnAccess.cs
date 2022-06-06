using System;

namespace GRandomizer.Util
{
    public class InitializeOnAccess<T>
    {
        readonly Func<T> _initializeFunc;
        T _value;

        public InitializeOnAccess(Func<T> initializeFunc)
        {
            _initializeFunc = initializeFunc;
            IsInitialized = false;
        }

        public bool IsInitialized { get; private set; }

        public T Get
        {
            get
            {
                if (!IsInitialized)
                {
                    _value = _initializeFunc();
                    IsInitialized = true;
                }

                return _value;
            }
        }

        public void Reset()
        {
            IsInitialized = false;
            _value = default;
        }

        public void SetValue(T value)
        {
            _value = value;
            IsInitialized = true;
        }

        public static implicit operator T(InitializeOnAccess<T> initOnAccess)
        {
            return initOnAccess.Get;
        }
    }
}
