using OpenTracing.Contrib.Grpc.OperationNameConstructor;

namespace OpenTracing.Contrib.Grpc.Configuration
{
    public abstract class TracingConfiguration
    {
        public ITracer Tracer { get; }
        public IOperationNameConstructor OperationNameConstructor { get; }
        public bool Streaming { get; }
        public bool StreamingInputSpans { get; }
        public bool StreamingOutputSpans { get; }
        public bool Verbose { get; }

        protected TracingConfiguration(ITracer tracer, IOperationNameConstructor operationNameConstructor = null, bool streaming = false, bool streamingInputSpans = false, bool streamingOutputSpans = false, bool verbose = false)
        {
            Tracer = tracer;
            OperationNameConstructor = operationNameConstructor ?? new DefaultOperationNameConstructor();
            Streaming = streaming;
            StreamingInputSpans = streamingInputSpans;
            StreamingOutputSpans = streamingOutputSpans;
            Verbose = verbose;
        }
    }
}