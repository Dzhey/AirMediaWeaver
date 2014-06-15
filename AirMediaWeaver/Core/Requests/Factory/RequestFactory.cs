using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;

namespace AirMedia.Core.Requests.Factory
{
    public class RequestFactory
    {
        private bool _isParallel;
        private bool _isDedicated;
        private bool _isDistinct;
        private string _actionTag;
        private readonly string _requestTypeName;
        private Type _requestType;
        private readonly IDictionary<string, ConstructorInfo> _constructors;

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

        public RequestFactory SetParallel(bool isParallel)
        {
            _isParallel = isParallel;

            return this;
        }

        public RequestFactory SetDedicated(bool isDedicated)
        {
            _isDedicated = isDedicated;

            return this;
        }

        public RequestFactory SetActionTag(string actionTag)
        {
            _actionTag = actionTag;

            return this;
        }

        public RequestFactory SetDistinct(bool isDistinct)
        {
            _isDistinct = true;

            return this;
        }

        public virtual AbsRequest Submit(params object[] args)
        {
            var constructor = ResolveConstructor(args);
            var rq = (AbsRequest) constructor.Invoke(args);

            rq.ActionTag = _actionTag;

            if (_isDistinct)
            {
                if (_actionTag == null) 
                    throw new ApplicationException("distinct request should have an appropriate action tag");

                if (RequestManager.Instance.HasPendingRequest(_actionTag))
                    return rq;
            }

            if (_isDedicated == false)
            {
                RequestManager.Instance.SubmitRequest(rq, _isParallel);
            }
            else
            {
                RequestManager.Instance.SubmitRequest(rq, _isParallel, _isDedicated);
            }

            return rq;
        }

        private ConstructorInfo ResolveConstructor(object[] args)
        {
            if (_requestType == null)
            {
                _requestType = Type.GetType(_requestTypeName, true);
            }
            var argTypes = RetrieveArgTypes(args);
            var argsHash = ComputeArgTypesHash(argTypes);

            if (_constructors.ContainsKey(argsHash))
                return _constructors[argsHash];

            var constructor = _requestType.GetConstructor(argTypes);
            if (constructor == null) 
                throw new ArgumentException("can't define constructor for specified arguments");

            _constructors.Add(argsHash, constructor);

            return constructor;
        }

        private Type[] RetrieveArgTypes(IEnumerable<object> args)
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
    }
}