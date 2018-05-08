using System.Collections.Generic;
using OpenTracing.Contrib.Grpc.OperationNameConstructor;

namespace OpenTracing.Contrib.Grpc.Configuration
{
    public class ClientTracingConfiguration : TracingConfiguration
    {
        public enum RequestAttribute
        {
            MethodType,
            MethodName,
            Deadline,
            //Compressor, // TODO: Currently not supported by grpc-csharp
            Authority,
            AllCallOptions,
            Headers
        }

        public ISet<RequestAttribute> TracedAttributes { get; }

        internal ClientTracingConfiguration(ITracer tracer) : base(tracer)
        {
            TracedAttributes = new HashSet<RequestAttribute>();
        }

        internal ClientTracingConfiguration(ITracer tracer, IOperationNameConstructor operationNameConstructor, bool streaming, bool verbose, ISet<RequestAttribute> tracedAttributes) 
            : base(tracer, operationNameConstructor, streaming, verbose)
        {
            TracedAttributes = tracedAttributes ?? new HashSet<RequestAttribute>();
        }
    }
}