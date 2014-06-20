using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;

namespace AirMedia.Core.Requests.Factory
{
    public class RequestFactory : IRequestFactory
    {
        private bool _isDisposed;
        private bool _isParallel;
        private bool _isDedicated;
        private bool _isDistinct;
        private string _actionTag;
        private readonly string _requestTypeName;
        private Type _requestType;
        private readonly IDictionary<string, ConstructorInfo> _constructors;
        private RequestManager _requestManager;

        public event EventHandler<RequestSubmittedEventArgs> RequestSubmitted;

        public static RequestFactory Init(string requestTypeName)
        {
            return new RequestFactory(requestTypeName);
        }

        public static RequestFactory Init(Type requestType)
        {
            return new RequestFactory(requestType);
        }

        protected RequestFactory(string requestTypeName)
        {
            _requestTypeName = requestTypeName;
            _constructors = new Dictionary<string, ConstructorInfo>();
        }

        protected RequestFactory(Type requestType)
        {
            _requestType = requestType;
            _constructors = new Dictionary<string, ConstructorInfo>();
        }

        public IRequestFactory SetManager(RequestManager manager)
        {
            _requestManager = manager;

            return this;
        }

        public IRequestFactory SetParallel(bool isParallel)
        {
            _isParallel = isParallel;

            return this;
        }

        public IRequestFactory SetDedicated(bool isDedicated)
        {
            _isDedicated = isDedicated;

            return this;
        }

        public IRequestFactory SetActionTag(string actionTag)
        {
            _actionTag = actionTag;

            return this;
        }

        public IRequestFactory SetDistinct(bool isDistinct)
        {
            _isDistinct = true;

            return this;
        }

        public virtual AbsRequest Submit(params object[] args)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("factory is already disposed");

            var constructor = ResolveConstructor(args);
            var rq = (AbsRequest) constructor.Invoke(args);

            rq.ActionTag = _actionTag;

            if (_requestManager == null)
                _requestManager = RequestManager.Instance;

            if (_isDistinct)
            {
                if (_actionTag == null) 
                    throw new ApplicationException("distinct request should have an appropriate action tag");

                if (_requestManager.HasPendingRequest(_actionTag))
                    return rq;
            }

            if (_isDedicated == false)
            {
                _requestManager.SubmitRequest(rq, _isParallel);
            }
            else
            {
                _requestManager.SubmitRequest(rq, _isParallel, _isDedicated);
            }

            if (RequestSubmitted != null)
            {
                RequestSubmitted(this, new RequestSubmittedEventArgs(rq));
            }

            return rq;
        }

        protected ConstructorInfo ResolveConstructor(object[] args)
        {
            if (_requestType == null)
            {
                _requestType = Type.GetType(_requestTypeName, true);
            }

            return ResolveConstructorImpl(args, _requestType);
        }

        protected ConstructorInfo ResolveConstructorImpl(object[] args, Type requestType)
        {
            var argTypes = RetrieveArgTypes(args);
            var argsHash = ComputeArgTypesHash(argTypes);

            if (_constructors.ContainsKey(argsHash))
                return _constructors[argsHash];

            var constructor = requestType.GetConstructor(argTypes);
            if (constructor == null)
                throw new ArgumentException("can't define constructor for specified arguments");

            _constructors.Add(argsHash, constructor);

            return constructor;
        }

        protected Type[] RetrieveArgTypes(IEnumerable<object> args)
        {
            return args.Select(o => o.GetType()).ToArray();
        }

        private string ComputeArgTypesHash(IEnumerable<Type> argTypes)
        {
            var sb = new StringBuilder();
            foreach (var argType in argTypes)
            {
                sb.Append(argType.FullName.GetHashCode());
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                _requestManager = null;
            }

            _isDisposed = true;
        }
    }
}