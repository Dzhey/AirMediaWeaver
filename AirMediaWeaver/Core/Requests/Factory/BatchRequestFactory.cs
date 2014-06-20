using System;
using System.Collections.Generic;
using System.Timers;
using AirMedia.Core.Requests.Impl;


namespace AirMedia.Core.Requests.Factory
{
    public class BatchRequestFactory : RequestFactory
    {
        public const int FlushTimeoutMillisDefalt = 80;
        public const int FlishQueueSizeThresholdDefault = 20;

        public int FlushTimeoutMillis { get; set; }
        public int FlushQueueSizeThreshold { get; set; }

        private Timer _timer;
        private bool _isDisposed;
        private List<BatchRequest.BatchRequestArgs> _args;
        private Type _requestType;

        public static BatchRequestFactory Init(string requestTypeName, Type batchRequestType = null)
        {
            return new BatchRequestFactory(requestTypeName);
        }

        public static BatchRequestFactory Init(Type requestType, Type batchRequestType = null)
        {
            return new BatchRequestFactory(requestType);
        }

        protected BatchRequestFactory(string requestTypeName, Type batchRequestType = null) : 
            base(batchRequestType ?? typeof(BatchRequest))
        {
            batchRequestType = batchRequestType ?? typeof (BatchRequest);
            Init(requestTypeName, FlushTimeoutMillisDefalt, FlishQueueSizeThresholdDefault);
            EnsureBatchRequestType(batchRequestType);
        }

        protected BatchRequestFactory(Type requestType, Type batchRequestType = null)
            : base(batchRequestType ?? typeof(BatchRequest))
        {
            batchRequestType = batchRequestType ?? typeof(BatchRequest);
            Init(requestType, FlushTimeoutMillisDefalt, FlishQueueSizeThresholdDefault);
            EnsureBatchRequestType(batchRequestType);
        }

        private void Init(string requestTypeName, int flushTimeoutMillis, int flushQueueSizeThreshold)
        {
            if (string.IsNullOrEmpty(requestTypeName))
                throw new ArgumentException("request type name should not be empty");

            var requestType = Type.GetType(requestTypeName);
            if (requestType == null)
                throw new ApplicationException(string.Format(
                    "can't obtain request type for type name \"{0}\"", requestTypeName));
            Init(requestType, flushTimeoutMillis, flushQueueSizeThreshold);
        }

        private void Init(Type requestType, int flushTimeoutMillis, int flushQueueSizeThreshold)
        {
            _args = new List<BatchRequest.BatchRequestArgs>();
            _requestType = requestType;
            FlushTimeoutMillis = flushTimeoutMillis;
            FlushQueueSizeThreshold = flushQueueSizeThreshold;
        }

        private void EnsureBatchRequestType(Type batchRequestType)
        {
            if (Consts.Debug && typeof(BatchRequest).IsAssignableFrom(batchRequestType) == false)
                throw new ArgumentException(string.Format(
                    "specified type \"{0}\" is not batch request type", batchRequestType));
        }

        public override Abs.AbsRequest Submit(params object[] args)
        {
            if (FlushQueueSizeThreshold < 1) 
                throw new ArgumentException("flush queue size thresold should be >= 0");

            if (_timer == null && FlushTimeoutMillis > 0 && FlushQueueSizeThreshold > 1)
            {
                _timer = new Timer(FlushTimeoutMillis);
                _timer.AutoReset = true;
                _timer.Elapsed += OnTimerElapsed;
            }

            if (_timer != null)
                _timer.Start();

            EnqueueArgs(args);

            if (args.Length >= FlushQueueSizeThreshold)
            {
                return PerformBatchSubmit();
            }

            return null;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs args)
        {
            if (PerformBatchSubmit() == null)
            {
                if (_timer != null) 
                    _timer.Stop();
            }
        }

        private BatchRequest PerformBatchSubmit()
        {
            BatchRequest.BatchRequestArgs[] args;
            lock (_args)
            {
                if (_args.Count < 1) return null;

                args = _args.ToArray();
                _args.Clear();
            }

            return (BatchRequest)base.Submit(new object[] { args });
        }

        private void EnqueueArgs(object[] args)
        {
            var constructorInfo = ResolveConstructorImpl(args, _requestType);

            lock (args)
            {
                _args.Add(new BatchRequest.BatchRequestArgs
                {
                    RequestConstructorArgs = args,
                    RequestConstructorInfo = constructorInfo
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                if (_timer != null)
                {
                    _timer.Elapsed -= OnTimerElapsed;
                    _timer.Dispose();
                }
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}