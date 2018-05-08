namespace OpenTracing.Contrib.Grpc.Configuration
{
    public abstract class TracingConfiguration
    {
        public ITracer Tracer { get; }
        public IOperationNameConstructor OperationNameConstructor { get; }
        public bool Streaming { get; }
        public bool Verbose { get; }

        protected TracingConfiguration(ITracer tracer, IOperationNameConstructor operationNameConstructor = null, bool streaming = false, bool verbose = false)
        {
            Tracer = tracer;
            OperationNameConstructor = operationNameConstructor ?? new DefaultOperationNameConstructor();
            Streaming = streaming;
            Verbose = verbose;
        }
    }
}