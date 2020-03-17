[![Build status][ci-img]][ci] [![NuGet][nuget-img]][nuget]

**WARNING: This project is a work in progress and not yet ready for production**

# OpenTracing gRPC Instrumentation

OpenTracing instrumentation for gRPC.

## Installation

Install the [NuGet package](https://www.nuget.org/packages/OpenTracing.Contrib.Grpc/):

    Install-Package OpenTracing.Contrib.Grpc

## Usage

### Server

- Instantiate tracer
- Create a `ServerTracingInterceptor`
- Intercept a service

```csharp
using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTracing.Contrib.Grpc;

    public class YourServer {

        private readonly string host;
        private readonly int port;
        private readonly Server server;
        private readonly Tracer tracer;

        private void Start() {
            ServerTracingInterceptor tracingInterceptor = new ServerTracingInterceptor(this.tracer);

            Server server = new Server
            {
                Ports = { new ServerPort(this.host, this.port, ServerCredentials.Insecure) },
                Services = { SomeService.BindService(new SomeServiceImpl()).Intercept(tracingInterceptor) }
            };
            server.Start();
        }
    }
```

### Client

- Instantiate a tracer
- Create a `ClientTracingInterceptor`
- Intercept the client channel

```csharp
using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTracing.Contrib.Grpc;

    public class YourClient {

        private readonly Channel channel;
        private readonly Tracer tracer;
        private readonly SomeServiceClient client;

        public YourClient(string host, int port) {
            this.channel = new Channel(host, port, ChannelCredentials.Insecure);

            ClientTracingInterceptor tracingInterceptor = new ClientTracingInterceptor(this.tracer);
            this.client = new SomeService.SomeServiceClient(this.channel.Intercept(tracingInterceptor));
        }
    }
```

## Server Tracing

A `ServerTracingInterceptor` uses default settings, which you can override by creating it using a `ServerTracingInterceptor.Builder`.

- `WithOperationName(IOperationNameConstructor operationName)`: Define how the operation name is constructed for all spans created for this intercepted server. Default is the name of the RPC method. More details in the `Operation Name` section.
- `WithStreaming()`: Logs to the server span whenever a message is received. *Note:* This package supports streaming but has not been rigorously tested. If you come across any issues, please let us know.
- `WithVerbosity()`: Logs to the server span additional events, such as message received, headers received and call complete. Default only logs if a call is cancelled.
- `WithTracedAttributes(params ServerRequestAttribute[] attrs)`: Sets tags on the server span in case you want to track information about the RPC call.

### Example

```csharp
ServerTracingInterceptor tracingInterceptor = new ServerTracingInterceptor
    .Builder(tracer)
    .WithStreaming()
    .WithVerbosity()
    .WithOperationName(new PrefixOperationNameConstructor("Server"))
    .WithTracedAttributes(ServerTracingConfiguration.RequestAttribute.Headers,
        ServerTracingConfiguration.RequestAttribute.MethodType)
    .Build();
```

## Client Tracing

A `ClientTracingInterceptor` also has default settings, which you can override by creating it using a `ClientTracingInterceptor.Builder`.

- `WithOperationName(IOperationNameConstructor operationName)`: Define how the operation name is constructed for all spans created for this intercepted client. Default is the name of the RPC method. More details in the `Operation Name` section.
- `WithStreaming()`: Logs to the client span whenever a message is sent or a response is received. *Note:* This package supports streaming but has not been rigorously tested. If you come across any issues, please let us know.
- `WithVerbosity()`: Logs to the client span additional events, such as call started, message sent, headers received, response received, and call complete. Default only logs if a call is cancelled.
- `WithTracedAttributes(params ClientRequestAttribute[] attrs)`: Sets tags on the client span in case you want to track information about the RPC call.
- `WithWaitForReady()`: Enables WaitForReady on all RPC calls.
- `WithFallbackCancellationToken(CancellationToken cancellationToken)`: Sets the cancellation token if the RPC call hasn't defined one.

### Example
```csharp
public class CustomOperationNameConstructor : IOperationNameConstructor
{
    public string ConstructOperationName<TRequest, TResponse>(Method<TRequest, TResponse> method)
    {
        // construct some operation name from the method descriptor
    }
}

ClientTracingInterceptor tracingInterceptor = new ClientTracingInterceptor
    .Builder(tracer)
    .WithStreaming()
    .WithVerbosity()
    .WithOperationName(new CustomOperationNameConstructor())
    .WithTracingAttributes(ClientTracingConfiguration.RequestAttribute.AllCallOptions,
        ClientTracingConfiguration.ClientRequestAttribute.Headers)
    .WithWaitForReady()
    .WithFallbackCancellationToken(cancellationToken)
    .Build();
```

## Current Span Context

In your server request handler, you can access the current active span for that request by calling

```csharp
Span span = tracer.ActiveSpan;
```

This is useful if you want to manually set tags on the span, log important events, or create a new child span for internal units of work. You can also use this key to wrap these internal units of work with a new context that has a user-defined active span.

## Operation Names

The default operation name for any span is the RPC method name (`Grpc.Core.Method<TRequest, TResponse>.FullName`). However, you may want to add your own prefixes, alter the name, or define a new name. For examples of good operation names, check out the OpenTracing `semantics`.

To alter the operation name, you need to add an implementation of the interface `IOperationNameConstructor` to the `ClientTracingInterceptor.Builder` or `ServerTracingInterceptor.Builder`. For example, if you want to add a prefix to the default operation name of your ClientInterceptor, your code would look like this:

```csharp
public class CustomPrefixOperationNameConstructor : IOperationNameConstructor
{
    public string ConstructOperationName<TRequest, TResponse>(Method<TRequest, TResponse> method)
    {
        return "your-prefix" + method.FullName;
    }
    public string ConstructOperationName(string method)
    {
        return "your-prefix" + method;
    }
}

ClientTracingInterceptor interceptor = ClientTracingInterceptor.Builder ...
    .WithOperationName(new CustomPrefixOperationNameConstructor())
    .With....
    .Build()
```

You can also use the default implementation using `PrefixOperationNameConstructor`:


```csharp
ClientTracingInterceptor interceptor = ClientTracingInterceptor.Builder ...
    .WithOperationName(new PrefixOperationNameConstructor("your-prefix"))
    .With....
    .Build()
```

Due to how the C# version of GRPC interceptors are written, it's currently not possible get more information on the method than the method name in an `ServerTracingInterceptor`.

## Integrating with Other Interceptors
GRPC provides `Intercept(Interceptor)` methods that allow you chaining multiple interceptors. Preferably put the tracing interceptor at the top of the interceptor stack so that it traces the entire request lifecycle, including other interceptors:

### Server
```csharp
Server server = new Server
{
    Services = { SomeService.BindService(new SomeServiceImpl()).Intercept(someInterceptor).Intercept(someOtherInterceptor).Intercept(serverTracingInterceptor) }
    Ports = ...
};
```

### Client
```csharp
client = new SomeService.SomeServiceClient(this.channel.Intercept(someInterceptor).Intercept(someOtherInterceptor).Intercept(clientTracingInterceptor));
```

[ci-img]: https://ci.appveyor.com/api/projects/status/github/opentracing-contrib/csharp-grpc?svg=true
[ci]: https://ci.appveyor.com/project/opentracing/csharp-grpc
[nuget-img]: https://img.shields.io/nuget/v/OpenTracing.Contrib.Grpc.svg
[nuget]: https://www.nuget.org/packages/OpenTracing.Contrib.Grpc/
