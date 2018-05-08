using Grpc.Core;

namespace OpenTracing.Contrib.Grpc.OperationNameConstructor
{
    public interface IOperationNameConstructor
    {
        string ConstructOperationName<TRequest, TResponse>(Method<TRequest, TResponse> method);
        string ConstructOperationName(string method);
    }
}
