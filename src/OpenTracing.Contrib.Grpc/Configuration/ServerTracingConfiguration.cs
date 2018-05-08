using System.Collections.Generic;
using OpenTracing.Contrib.Grpc.OperationNameConstructor;

namespace OpenTracing.Contrib.Grpc.Configuration
{
    public sealed class ServerTracingConfiguration : TracingConfiguration
    {
        public enum RequestAttribute
        {
            Headers,
            //MethodType, // TODO: Currently not supported by grpc-csharp
            MethodName,
            //CallAttributes, // TODO: Currently not supported by grpc-csharp
        }

        public ISet<RequestAttribute> TracedAttributes { get; }

        internal ServerTracingConfiguration(ITracer tracer) : base(tracer)
        {
            TracedAttributes = new HashSet<RequestAttribute>();
        }

        internal ServerTracingConfiguration(ITracer tracer, IOperationNameConstructor operationNameConstructor, bool streaming, bool verbose, ISet<RequestAttribute> tracedAttributes) 
            : base(tracer, operationNameConstructor, streaming, verbose)
        {
            TracedAttributes = tracedAttributes ?? new HashSet<RequestAttribute>();
        }
    }
}