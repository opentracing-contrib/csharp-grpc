using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Utils;
using OpenTracing.Contrib.Grpc.Configuration;
using OpenTracing.Contrib.Grpc.Handler;
using System.Collections.Generic;
using System.Threading;
using OpenTracing.Contrib.Grpc.OperationNameConstructor;

namespace OpenTracing.Contrib.Grpc.Interceptors
{
    public class ClientTracingInterceptor : Interceptor
    {
        private readonly ClientTracingConfiguration _configuration;

        public ClientTracingInterceptor(ITracer tracer)
        {
            GrpcPreconditions.CheckNotNull(tracer, nameof(tracer));
            _configuration = new ClientTracingConfiguration(tracer);
        }

        private ClientTracingInterceptor(ClientTracingConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return new InterceptedClientHandler<TRequest, TResponse>(_configuration, context)
                .BlockingUnaryCall(request, continuation);
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return new InterceptedClientHandler<TRequest, TResponse>(_configuration, context)
                .AsyncUnaryCall(request, continuation);
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return new InterceptedClientHandler<TRequest, TResponse>(_configuration, context)
                .AsyncServerStreamingCall(request, continuation);
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return new InterceptedClientHandler<TRequest, TResponse>(_configuration, context)
                .AsyncClientStreamingCall(continuation);
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return new InterceptedClientHandler<TRequest, TResponse>(_configuration, context)
                .AsyncDuplexStreamingCall(continuation);
        }

        public class Builder
        {
            private readonly ITracer _tracer;
            private IOperationNameConstructor _operationNameConstructor;
            private bool _streaming;
            private bool _verbose;
            private ISet<ClientTracingConfiguration.RequestAttribute> _tracedAttributes;
            private bool _waitForReady;
            private CancellationToken _cancellationToken;

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

            /// <param name="tracedAttributes">to set as tags on client spans created by this interceptor</param>
            /// <returns>this Builder configured to trace attributes</returns>
            public Builder WithTracedAttributes(params ClientTracingConfiguration.RequestAttribute[] tracedAttributes)
            {
                _tracedAttributes = new HashSet<ClientTracingConfiguration.RequestAttribute>(tracedAttributes);
                return this;
            }

            /// <summary>
            /// Enables WaitForReady call option for all calls.
            /// </summary>
            /// <returns>this Builder configured to be verbose</returns>
            public Builder WithWaitForReady()
            {
                _waitForReady = true;
                return this;
            }

            /// <param name="cancellationToken">The cancellation token to set for all RPCs if none was set.</param>
            /// <returns>this Builder configured to be verbose</returns>
            public Builder WithFallbackCancellationToken(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                return this;
            }

            public ClientTracingInterceptor Build()
            {
                var configuration = new ClientTracingConfiguration(_tracer, _operationNameConstructor, _streaming, _verbose, _tracedAttributes, _waitForReady, _cancellationToken);
                return new ClientTracingInterceptor(configuration);
            }
        }
    }
}
