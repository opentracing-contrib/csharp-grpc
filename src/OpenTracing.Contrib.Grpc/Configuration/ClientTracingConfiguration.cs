using System.Collections.Generic;
using System.Threading;
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
        public bool WaitForReady { get; }
        public CancellationToken FallbackCancellationToken { get; }

        internal ClientTracingConfiguration(ITracer tracer) : base(tracer)
        {
            TracedAttributes = new HashSet<RequestAttribute>();
        }

        internal ClientTracingConfiguration(ITracer tracer, IOperationNameConstructor operationNameConstructor, bool streaming, bool streamingInputSpans, bool streamingOutputSpans, bool verbose, ISet<RequestAttribute> tracedAttributes, bool waitForReady, CancellationToken fallbackCancellationToken)
            : base(tracer, operationNameConstructor, streaming, streamingInputSpans, streamingOutputSpans, verbose)
        {
            TracedAttributes = tracedAttributes ?? new HashSet<RequestAttribute>();
            WaitForReady = waitForReady;
            FallbackCancellationToken = fallbackCancellationToken;
        }
    }
}