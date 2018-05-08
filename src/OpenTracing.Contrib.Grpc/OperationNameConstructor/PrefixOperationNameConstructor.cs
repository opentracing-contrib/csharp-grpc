using Grpc.Core;

namespace OpenTracing.Contrib.Grpc.OperationNameConstructor
{
    public class PrefixOperationNameConstructor : IOperationNameConstructor
    {
        private readonly string _prefix;

        public PrefixOperationNameConstructor(string prefix)
        {
            _prefix = prefix;
        }

        public string ConstructOperationName<TRequest, TResponse>(Method<TRequest, TResponse> method)
        {
            return $"{_prefix} {method.FullName}";
        }

        public string ConstructOperationName(string method)
        {
            return $"{_prefix} {method}";
        }
    }
}
