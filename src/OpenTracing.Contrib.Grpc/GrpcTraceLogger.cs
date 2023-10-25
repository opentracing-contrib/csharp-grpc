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

        private ISpan ScopeSpan
        {
            get
            {
                lock (this)
                {
                    return _scope?.Span ?? _span;
                }
            }
        }

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
                { "data", metadata?.ToReadableString() }
            });
        }

        public void BeginInputScope(string operationName)
        {
            if (!(_configuration.StreamingInputSpans || _configuration.Verbose)) return;

            BeginScope(operationName);
        }

        public void BeginOutputScope(string operationName)
        {
            if (!(_configuration.StreamingOutputSpans || _configuration.Verbose)) return;

            BeginScope(operationName);
        }

        private void BeginScope(string operationName)
        {
            lock (this)
            {
                if (_scope != null) EndScope();

                _scope = _configuration.Tracer.BuildSpan(operationName)
                    .AsChildOf(_span.Context)
                    .StartActive(false);
            }
        }

        public void EndInputScope()
        {
            if (!(_configuration.StreamingInputSpans || _configuration.Verbose)) return;

            EndScope();
        }

        public void EndOutputScope()
        {
            if (!(_configuration.StreamingOutputSpans || _configuration.Verbose)) return;

            EndScope();
        }

        private void EndScope()
        {
            lock (this)
            {
                if (_scope == null) return;

                _scope.Span.Finish();
                _scope.Dispose();
                _scope = null;
            }
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