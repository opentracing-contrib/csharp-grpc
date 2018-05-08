using System;
using System.Globalization;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTracing.Contrib.Grpc.Configuration;
using OpenTracing.Contrib.Grpc.Interceptors;
using OpenTracing.Contrib.Grpc.Propagation;
using OpenTracing.Contrib.Grpc.Streaming;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.Grpc.Handler
{
    internal class InterceptedClientHandler<TRequest, TResponse> 
        where TRequest : class 
        where TResponse : class
    {
        private readonly ClientTracingConfiguration _configuration;
        private readonly ClientInterceptorContext<TRequest, TResponse> _context;
        private readonly GrpcTraceLogger<TRequest, TResponse> _logger;

        public InterceptedClientHandler(ClientTracingConfiguration configuration, ClientInterceptorContext<TRequest, TResponse> context)
        {
            _configuration = configuration;
            _context = context;
            if (context.Options.Headers == null)
            {
                _context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, 
                    context.Options.WithHeaders(new Metadata())); // Add empty metadata to options
            }

            var span = InitializeSpanWithHeaders();
            _logger = new GrpcTraceLogger<TRequest, TResponse>(span, configuration);
            _configuration.Tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new MetadataCarrier(_context.Options.Headers));
        }

        private ISpan InitializeSpanWithHeaders()
        {
            var operationName = _configuration.OperationNameConstructor.ConstructOperationName(_context.Method);
            var spanBuilder = _configuration.Tracer.BuildSpan(operationName)
                .WithTag(Constants.TAGS_PEER_ADDRESS, _context.Host)
                .WithTag(Tags.Component, Constants.TAGS_COMPONENT)
                .WithTag(Tags.SpanKind, Tags.SpanKindClient);

            foreach (var attribute in _configuration.TracedAttributes)
            {
                switch (attribute)
                {
                    case ClientTracingConfiguration.RequestAttribute.MethodType:
                        spanBuilder.WithTag(Constants.TAGS_GRPC_METHOD_TYPE, _context.Method?.Type.ToString());
                        break;
                    case ClientTracingConfiguration.RequestAttribute.MethodName:
                        spanBuilder.WithTag(Constants.TAGS_GRPC_METHOD_NAME, _context.Method?.FullName);
                        break;
                    case ClientTracingConfiguration.RequestAttribute.Deadline:
                        spanBuilder.WithTag(Constants.TAGS_GRPC_DEADLINE_MILLIS, _context.Options.Deadline?.TimeRemaining().TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
                        break;
                    case ClientTracingConfiguration.RequestAttribute.Authority:
                        // TODO: Serialization is wrong
                        spanBuilder.WithTag(Constants.TAGS_GRPC_AUTHORITY, _context.Options.Credentials?.ToString());
                        break;
                    case ClientTracingConfiguration.RequestAttribute.AllCallOptions:
                        // TODO: Serialization is wrong
                        spanBuilder.WithTag(Constants.TAGS_GRPC_CALL_OPTIONS, _context.Options.ToString());
                        break;
                    case ClientTracingConfiguration.RequestAttribute.Headers:
                        // TODO: Check if this is always present immediately, expecially in case of streaming!
                        spanBuilder.WithTag(Constants.TAGS_GRPC_HEADERS, _context.Options.Headers?.ToReadableString());
                        break;
                }
            }
            return spanBuilder.Start();
        }

        private Metadata LogResponseHeader(Task<Metadata> metadata)
        {
            var responseHeader = metadata.Result;
            _logger.ResponseHeader(responseHeader);
            return responseHeader;
        }

        public TResponse BlockingUnaryCall(TRequest request, Interceptor.BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                _logger.Request(request);
                var response = continuation(request, _context);
                _logger.Response(response);
                _logger.FinishSuccess();
                return response;
            }
            catch (Exception ex)
            {
                _logger.FinishException(ex);
                throw;
            }
        }

        public AsyncUnaryCall<TResponse> AsyncUnaryCall(TRequest request, Interceptor.AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            _logger.Request(request);
            var rspCnt = continuation(request, _context);
            var rspAsync = rspCnt.ResponseAsync.ContinueWith(rspTask =>
            {
                try
                {
                    var response = rspTask.Result;
                    _logger.Response(response);
                    _logger.FinishSuccess();
                    return response;
                }
                catch (AggregateException ex)
                {
                    _logger.FinishException(ex.InnerException);
                    throw ex.InnerException;
                }
            });
            var rspHeaderAsync = rspCnt.ResponseHeadersAsync.ContinueWith(LogResponseHeader);
            return new AsyncUnaryCall<TResponse>(rspAsync, rspHeaderAsync, rspCnt.GetStatus, rspCnt.GetTrailers, rspCnt.Dispose);
        }

        public AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall(TRequest request, Interceptor.AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            _logger.Request(request);
            var rspCnt = continuation(request, _context);
            var tracingResponseStream = new TracingAsyncStreamReader<TResponse>(rspCnt.ResponseStream, _logger.Response, _logger.FinishSuccess, _logger.FinishException);
            var rspHeaderAsync = rspCnt.ResponseHeadersAsync.ContinueWith(LogResponseHeader);
            return new AsyncServerStreamingCall<TResponse>(tracingResponseStream, rspHeaderAsync, rspCnt.GetStatus, rspCnt.GetTrailers, rspCnt.Dispose);
        }

        public AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall(Interceptor.AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var rspCnt = continuation(_context);
            var tracingRequestStream = new TracingClientStreamWriter<TRequest>(rspCnt.RequestStream, _logger.Request);
            var rspAsync = rspCnt.ResponseAsync.ContinueWith(rspTask =>
            {
                try
                {
                    var response = rspTask.Result;
                    _logger.Response(response);
                    _logger.FinishSuccess();
                    return response;
                }
                catch (AggregateException ex)
                {
                    _logger.FinishException(ex.InnerException);
                    throw ex.InnerException;
                }
            });
            var rspHeaderAsync = rspCnt.ResponseHeadersAsync.ContinueWith(LogResponseHeader);
            return new AsyncClientStreamingCall<TRequest, TResponse>(tracingRequestStream, rspAsync, rspHeaderAsync, rspCnt.GetStatus, rspCnt.GetTrailers, rspCnt.Dispose);
        }

        public AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall(Interceptor.AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var rspCnt = continuation(_context);
            var tracingRequestStream = new TracingClientStreamWriter<TRequest>(rspCnt.RequestStream, _logger.Request);
            var tracingResponseStream = new TracingAsyncStreamReader<TResponse>(rspCnt.ResponseStream, _logger.Response, _logger.FinishSuccess, _logger.FinishException);
            var rspHeaderAsync = rspCnt.ResponseHeadersAsync.ContinueWith(LogResponseHeader);
            return new AsyncDuplexStreamingCall<TRequest, TResponse>(tracingRequestStream, tracingResponseStream, rspHeaderAsync, rspCnt.GetStatus, rspCnt.GetTrailers, rspCnt.Dispose);
        }
    }
}