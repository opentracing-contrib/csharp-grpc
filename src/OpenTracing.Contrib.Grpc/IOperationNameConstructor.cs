using Grpc.Core;

namespace OpenTracing.Contrib.Grpc
{
    public interface IOperationNameConstructor
    {
        string ConstructOperationName<TRequest, TResponse>(Method<TRequest, TResponse> method);
    }
}
