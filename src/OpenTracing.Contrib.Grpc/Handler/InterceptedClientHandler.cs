﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTracing.Contrib.Grpc.Configuration;
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
        private readonly StreamActions<TResponse> _inputStreamActions;
        private readonly StreamActions<TRequest> _outputStreamActions;

        public InterceptedClientHandler(ClientTracingConfiguration configuration, ClientInterceptorContext<TRequest, TResponse> context)
        {
            _configuration = configuration;
            _context = context;

            var callOptions = ApplyConfigToCallOptions(_context.Options);
            if (!Equals(callOptions, context.Options))
            {
                _context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, callOptions);
            }

            var span = InitializeSpanWithHeaders();
            _logger = new GrpcTraceLogger<TRequest, TResponse>(span, configuration);
            _configuration.Tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new MetadataCarrier(_context.Options.Headers));

            var inputScopeActions = new ScopeActions("new_response", _logger.BeginInputScope, _logger.EndInputScope);
            _inputStreamActions = new StreamActions<TResponse>(inputScopeActions, _logger.Response, _logger.FinishSuccess, _logger.FinishException);

            var outputScopeActions = new ScopeActions("new_request", _logger.BeginOutputScope, _logger.EndOutputScope);
            _outputStreamActions = new StreamActions<TRequest>(outputScopeActions, _logger.Request);
        }

        private CallOptions ApplyConfigToCallOptions(CallOptions callOptions)
        {
            var headers = callOptions.Headers;
            if (headers == null || headers.IsReadOnly)
            {
                // Use empty, writable metadata as base:
                var metadata = new Metadata();

                // Copy elements since it was read-only if it had elements:
                foreach (var header in headers ?? Enumerable.Empty<Metadata.Entry>())
                {
                    metadata.Add(header);
                }

                callOptions = callOptions.WithHeaders(metadata);
            }

            if (_configuration.WaitForReady && callOptions.IsWaitForReady != _configuration.WaitForReady)
            {
                callOptions = callOptions.WithWaitForReady();
            }

            if (_configuration.FallbackCancellationToken != default && callOptions.CancellationToken != _configuration.FallbackCancellationToken)
            {
                callOptions = callOptions.WithCancellationToken(_configuration.FallbackCancellationToken);
            }

            return callOptions;
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
                        spanBuilder.WithTag(Constants.TAGS_GRPC_AUTHORITY, _context.Options.Headers.GetAuthorizationHeaderValue());
                        break;
                    case ClientTracingConfiguration.RequestAttribute.AllCallOptions:
                        spanBuilder.WithTag(Constants.TAGS_GRPC_CALL_OPTIONS, _context.Options.ToReadableString());
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
            var tracingResponseStream = new TracingAsyncStreamReader<TResponse>(rspCnt.ResponseStream, _inputStreamActions);
            var rspHeaderAsync = rspCnt.ResponseHeadersAsync.ContinueWith(LogResponseHeader);
            return new AsyncServerStreamingCall<TResponse>(tracingResponseStream, rspHeaderAsync, rspCnt.GetStatus, rspCnt.GetTrailers, rspCnt.Dispose);
        }

        public AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall(Interceptor.AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var rspCnt = continuation(_context);
            var tracingRequestStream = new TracingClientStreamWriter<TRequest>(rspCnt.RequestStream, _outputStreamActions);
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
            var tracingRequestStream = new TracingClientStreamWriter<TRequest>(rspCnt.RequestStream, _outputStreamActions);
            var tracingResponseStream = new TracingAsyncStreamReader<TResponse>(rspCnt.ResponseStream, _inputStreamActions);
            var rspHeaderAsync = rspCnt.ResponseHeadersAsync.ContinueWith(LogResponseHeader);
            return new AsyncDuplexStreamingCall<TRequest, TResponse>(tracingRequestStream, tracingResponseStream, rspHeaderAsync, rspCnt.GetStatus, rspCnt.GetTrailers, rspCnt.Dispose);
        }
    }
}