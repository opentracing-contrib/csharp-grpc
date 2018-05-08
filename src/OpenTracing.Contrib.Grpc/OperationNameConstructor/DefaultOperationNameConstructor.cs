using Grpc.Core;

namespace OpenTracing.Contrib.Grpc.OperationNameConstructor
{
    public class DefaultOperationNameConstructor : IOperationNameConstructor
    {
        public string ConstructOperationName<TRequest, TResponse>(Method<TRequest, TResponse> method)
        {
            return method.FullName;
        }

        public string ConstructOperationName(string method)
        {
            return method;
        }
    }
}
