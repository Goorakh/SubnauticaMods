using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    public class InitializeAsync<T>
    {
        readonly Func<IOut<T>, IEnumerator> _coroutineFunc;
        TaskResult<T> _taskResult;
        IEnumerator _coroutine;
        int _updateCallbackIndex = -1;
        T _result;

        public InitializeAsync(Func<IOut<T>, IEnumerator> coroutineFunc)
        {
            _coroutineFunc = coroutineFunc;
            State = AsyncOperationState.NotStarted;
        }

        public void StartAsyncOperation()
        {
            _taskResult = new TaskResult<T>();
            _coroutine = _coroutineFunc(_taskResult);
            _updateCallbackIndex = GlobalObject.RegisterUpdateCallback(update);

            State = AsyncOperationState.InProgress;
        }

        public AsyncOperationState State { get; private set; }

        public T Get
        {
            get
            {
                if (State != AsyncOperationState.Complete)
                {
                    if (State == AsyncOperationState.NotStarted)
                        StartAsyncOperation();

                    step(true);
                }

                return _result;
            }
        }

        void update()
        {
            if (State == AsyncOperationState.InProgress)
            {
                step(false);
            }
        }

        void step(bool forceCompleteThisFrame)
        {
            if (forceCompleteThisFrame)
            {
                while (_coroutine.MoveNext())
                {
                }
            }

            if (forceCompleteThisFrame || !_coroutine.MoveNext())
            {
                State = AsyncOperationState.Complete;

                if (_updateCallbackIndex != -1)
                {
                    GlobalObject.UnregisterUpdateCallback(_updateCallbackIndex);
                }

                _result = _taskResult.Get();
            }
        }
    }
}
