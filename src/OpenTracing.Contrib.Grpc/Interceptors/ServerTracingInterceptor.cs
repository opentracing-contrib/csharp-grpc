using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Utils;
using OpenTracing.Contrib.Grpc.Configuration;
using OpenTracing.Contrib.Grpc.Handler;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTracing.Contrib.Grpc.OperationNameConstructor;

namespace OpenTracing.Contrib.Grpc.Interceptors
{
    public class ServerTracingInterceptor : Interceptor
    {
        private readonly ServerTracingConfiguration _configuration;

        public ServerTracingInterceptor(ITracer tracer)
        {
            GrpcPreconditions.CheckNotNull(tracer, nameof(tracer));
            _configuration = new ServerTracingConfiguration(tracer);
        }

        private ServerTracingInterceptor(ServerTracingConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            return new InterceptedServerHandler<TRequest, TResponse>(_configuration, context)
                .UnaryServerHandler(request, continuation);
        }

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            return new InterceptedServerHandler<TRequest, TResponse>(_configuration, context)
                .ClientStreamingServerHandler(requestStream, continuation);
        }

        public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            return new InterceptedServerHandler<TRequest, TResponse>(_configuration, context)
                .ServerStreamingServerHandler(request, responseStream, continuation);
        }

        public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            return new InterceptedServerHandler<TRequest, TResponse>(_configuration, context)
                .DuplexStreamingServerHandler(requestStream, responseStream, continuation);
        }

        public class Builder
        {
            private readonly ITracer _tracer;
            private IOperationNameConstructor _operationNameConstructor;
            private bool _streaming;
            private bool _verbose;
            private ISet<ServerTracingConfiguration.RequestAttribute> _tracedAttributes;

            public Builder(ITracer tracer)
            {
                _tracer = tracer;
            }

            /// <param name="operationNameConstructor">to name all spans created by this intercepter</param>
            /// <returns>this Builder with configured operation name</returns>
            public Builder WithOperationName(IOperationNameConstructor operationNameConstructor)
            {
                _operationNameConstructor = operationNameConstructor;
                return this;
            }

            /// <summary>
            /// Logs streaming events to client spans.
            /// </summary>
            /// <returns>this Builder configured to log streaming events</returns>
            public Builder WithStreaming()
            {
                _streaming = true;
                return this;
            }

            /// <summary>
            /// Logs all request life-cycle events to client spans.
            /// </summary>
            /// <returns>this Builder configured to be verbose</returns>
            public Builder WithVerbosity()
            {
                _verbose = true;
                return this;
            }

            /// <param name="tracedAttributes">to set as tags on client spans created by this intercepter</param>
            /// <returns>this Builder configured to trace attributes</returns>
            public Builder WithTracedAttributes(params ServerTracingConfiguration.RequestAttribute[] tracedAttributes)
            {
                _tracedAttributes = new HashSet<ServerTracingConfiguration.RequestAttribute>(tracedAttributes);
                return this;
            }

            public ServerTracingInterceptor Build()
            {
                var configuration = new ServerTracingConfiguration(_tracer, _operationNameConstructor, _streaming, _verbose, _tracedAttributes);
                return new ServerTracingInterceptor(configuration);
            }
        }
    }
}
