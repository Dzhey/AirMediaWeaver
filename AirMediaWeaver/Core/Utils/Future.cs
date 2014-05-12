using System;
using System.Threading;

namespace AirMedia.Core.Utils
{
    public class Future<T>
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        private T _value;
        private bool _hasValue;
        private Exception _error;

        public void SetValue(T value)
        {
            _value = value;
            _hasValue = true;
            _resetEvent.Set();
        }

        public void SetError(Exception error)
        {
            _error = error;
            _resetEvent.Set();
        }

        public bool HasError
        {
            get { return _error != null; }
        }

        public T Value
        {
            get
            {
                _resetEvent.WaitOne();
                _resetEvent.Close();
                if (_error != null)
                {
                    throw new AggregateException(_error);
                }
                return _value;
            }
        }

        public bool HasValue
        {
            get { return _hasValue; }
        }

        public Exception Error
        {
            get
            {
                _resetEvent.WaitOne();
                return _error;
            }
        }
    }

}
