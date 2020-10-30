using System;
using System.Collections.Generic;
using Grpc.Core;
using OpenTracing.Contrib.Grpc.Configuration;

namespace OpenTracing.Contrib.Grpc
{
    internal class GrpcTraceLogger<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly ISpan _span;
        private readonly TracingConfiguration _configuration;

        private IScope _scope;
        private bool _isFinished;

        private ISpan ScopeSpan => _scope?.Span ?? _span;

        public GrpcTraceLogger(ISpan span, TracingConfiguration configuration)
        {
            _span = span;
            _configuration = configuration;

            if (_configuration.Verbose)
            {
                _span.Log("Started call");
            }
        }

        public void ResponseHeader(Metadata metadata)
        {
            if (!_configuration.Verbose) return;

            _span.Log(new Dictionary<string, object>
            {
                { LogFields.Event, "Response headers received" },
                { "data", metadata }
            });
        }

        public void BeginScope(string operationName)
        {
            if (!(_configuration.StreamingInputSpans || _configuration.Verbose)) return;

            _scope = _configuration.Tracer.BuildSpan(operationName).StartActive(false);
        }

        public void EndScope()
        {
            if (_scope == null || !(_configuration.StreamingInputSpans || _configuration.Verbose)) return;

            _scope.Span.Finish();
            _scope.Dispose();
            _scope = null;
        }

        public void Request(TRequest req)
        {
            if (!(_configuration.Streaming || _configuration.Verbose)) return;

            ScopeSpan.Log(new Dictionary<string, object>
            {
                { LogFields.Event, "gRPC request" },
                { "data", req }
            });
        }

        public void Response(TResponse rsp)
        {
            if (!(_configuration.Streaming || _configuration.Verbose)) return;

            ScopeSpan.Log(new Dictionary<string, object>
            {
                { LogFields.Event, "gRPC response" },
                { "data", rsp }
            });
        }

        public void FinishSuccess()
        {
            if (_configuration.Verbose)
            {
                _span.Log("Call completed");
            }
            Finish();
        }

        public void FinishException(Exception ex)
        {
            if (_configuration.Verbose)
            {
                _span.Log("Call failed");
            }
            _span.SetException(ex);
            Finish();
        }

        private void Finish()
        {
            if (_isFinished) return;

            EndScope();
            _span.Finish();
            _isFinished = true;
        }
    }
}