using System;
using System.Threading.Tasks;
using Grpc.Core;
using OpenTracing.Contrib.Grpc.Configuration;
using OpenTracing.Contrib.Grpc.Propagation;
using OpenTracing.Contrib.Grpc.Streaming;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.Grpc.Handler
{
    internal class InterceptedServerHandler<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly ServerTracingConfiguration _configuration;
        private readonly ServerCallContext _context;
        private readonly GrpcTraceLogger<TRequest, TResponse> _logger;
        private readonly StreamActions<TRequest> _inputStreamActions;
        private readonly StreamActions<TResponse> _outputStreamActions;

        public InterceptedServerHandler(ServerTracingConfiguration configuration, ServerCallContext context)
        {
            _configuration = configuration;
            _context = context;

            var span = GetSpanFromContext();
            _logger = new GrpcTraceLogger<TRequest, TResponse>(span, configuration);

            var inputScopeActions = new ScopeActions("new_request", _logger.BeginInputScope, _logger.EndInputScope);
            _inputStreamActions = new StreamActions<TRequest>(inputScopeActions, _logger.Request);

            var outputScopeActions = new ScopeActions("new_response", _logger.BeginOutputScope, _logger.EndOutputScope);
            _outputStreamActions = new StreamActions<TResponse>(outputScopeActions, _logger.Response);
        }

        private ISpan GetSpanFromContext()
        {
            var spanBuilder = GetSpanBuilderFromHeaders()
                .WithTag(Constants.TAGS_PEER_ADDRESS, _context.Peer)
                .WithTag(Tags.Component, Constants.TAGS_COMPONENT)
                .WithTag(Tags.SpanKind, Tags.SpanKindServer);

            foreach (var attribute in _configuration.TracedAttributes)
            {
                switch (attribute)
                {
                    case ServerTracingConfiguration.RequestAttribute.MethodName:
                        spanBuilder.WithTag(Constants.TAGS_GRPC_METHOD_NAME, _context.Method);
                        break;
                    case ServerTracingConfiguration.RequestAttribute.Headers:
                        // TODO: Check if this is always present immediately, especially in case of streaming!
                        spanBuilder.WithTag(Constants.TAGS_GRPC_HEADERS, _context.RequestHeaders?.ToReadableString());
                        break;
                }
            }
            return spanBuilder.StartActive(false).Span;
        }

        private ISpanBuilder GetSpanBuilderFromHeaders()
        {
            var operationName = _configuration.OperationNameConstructor.ConstructOperationName(_context.Method);
            var spanBuilder = _configuration.Tracer.BuildSpan(operationName);

            var parentSpanCtx = _configuration.Tracer.Extract(BuiltinFormats.HttpHeaders, new MetadataCarrier(_context.RequestHeaders));
            if (parentSpanCtx != null)
            {
                spanBuilder = spanBuilder.AsChildOf(parentSpanCtx);
            }
            return spanBuilder;
        }

        public async Task<TResponse> UnaryServerHandler(TRequest request, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                _logger.Request(request);
                var response = await continuation(request, _context).ConfigureAwait(false);
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

        public async Task<TResponse> ClientStreamingServerHandler(IAsyncStreamReader<TRequest> requestStream, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var tracingRequestStream = new TracingAsyncStreamReader<TRequest>(requestStream, _inputStreamActions);
                var response = await continuation(tracingRequestStream, _context).ConfigureAwait(false);
                _outputStreamActions.ScopeActions.BeginScope();
                _outputStreamActions.Message(response);
                _outputStreamActions.ScopeActions.EndScope();
                _logger.FinishSuccess();
                return response;
            }
            catch (Exception ex)
            {
                _logger.FinishException(ex);
                throw;
            }
        }

        public async Task ServerStreamingServerHandler(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var tracingResponseStream = new TracingServerStreamWriter<TResponse>(responseStream, _outputStreamActions);
                _inputStreamActions.ScopeActions.BeginScope();
                _inputStreamActions.Message(request);
                _inputStreamActions.ScopeActions.EndScope();
                await continuation(request, tracingResponseStream, _context).ConfigureAwait(false);
                _logger.FinishSuccess();
            }
            catch (Exception ex)
            {
                _logger.FinishException(ex);
                throw;
            }
        }

        public async Task DuplexStreamingServerHandler(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var tracingRequestStream = new TracingAsyncStreamReader<TRequest>(requestStream, _inputStreamActions);
                var tracingResponseStream = new TracingServerStreamWriter<TResponse>(responseStream, _outputStreamActions);
                await continuation(tracingRequestStream, tracingResponseStream, _context).ConfigureAwait(false);
                _logger.FinishSuccess();
            }
            catch (Exception ex)
            {
                _logger.FinishException(ex);
                throw;
            }
        }
    }
}