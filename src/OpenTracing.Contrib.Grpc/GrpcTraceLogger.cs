using System;
using System.Collections.Generic;
using Grpc.Core;
using OpenTracing.Contrib.Grpc.Configuration;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.Grpc
{
    internal class GrpcTraceLogger<TRequest, TResponse> 
        where TRequest : class 
        where TResponse : class
    {
        private readonly ISpan _span;
        private readonly TracingConfiguration _configuration;

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

        public void Request(TRequest req)
        {
            if (!(_configuration.Streaming || _configuration.Verbose)) return;

            _span.Log(new Dictionary<string, object>
            {
                { LogFields.Event, "gRPC request" },
                { "data", req }
            });
        }

        public void Response(TResponse rsp)
        {
            if (!(_configuration.Streaming || _configuration.Verbose)) return;

            _span.Log(new Dictionary<string, object>
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
            _span.Finish();
        }

        public void FinishException(Exception ex)
        {
            if (_configuration.Verbose)
            {
                _span.Log("Call failed");
            }
            _span.SetException(ex)
                .Finish();
        }
    }
}