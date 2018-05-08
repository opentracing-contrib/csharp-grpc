using Grpc.Core;

namespace OpenTracing.Contrib.Grpc
{
    public class DefaultOperationNameConstructor : IOperationNameConstructor
    {
        public string ConstructOperationName<TRequest, TResponse>(Method<TRequest, TResponse> method)
        {
            return method.FullName;
        }
    }
}
