using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTracing.Contrib.Grpc.Configuration;
using OpenTracing.Contrib.Grpc.Interceptors;
using OpenTracing.Mock;
using Tutorial;

namespace OpenTracing.Contrib.Grpc.Test
{
    internal class Program
    {
        private class ConsoleMockTracer : MockTracer
        {
            protected override void OnSpanFinished(MockSpan span)
            {
                Console.WriteLine(span);
                Console.WriteLine("Tags:");
                Console.WriteLine(string.Join("; ", span.Tags.Select(e => $"{e.Key} = {e.Value}")));
                Console.WriteLine("Logs:");
                span.LogEntries.ForEach(entry =>
                    Console.WriteLine($"Timestamp: {entry.Timestamp}, Fields: "
                                      + string.Join("; ", entry.Fields.Select(e => $"{e.Key} = {e.Value}"))));
                Console.WriteLine();
            }
        }

        private static readonly ServerTracingInterceptor ServerTracingInterceptor = new ServerTracingInterceptor(new ConsoleMockTracer());

        private static Task Main()
        {
            return MainAsync();
        }

        private static async Task MainAsync()
        {
            Server server = new Server
            {
                Ports = { new ServerPort("localhost", 8011, ServerCredentials.Insecure) },
                Services = { Phone.BindService(new PhoneImpl()).Intercept(ServerTracingInterceptor) }
            };
            server.Start();

            var tracingInterceptor = new ClientTracingInterceptor
                .Builder(new ConsoleMockTracer())
                .WithStreaming()
                .WithVerbosity()
                .WithTracedAttributes(ClientTracingConfiguration.RequestAttribute.AllCallOptions, ClientTracingConfiguration.RequestAttribute.Headers)
                .WithWaitForReady()
                .Build();

            var client = new Phone.PhoneClient(new Channel("localhost:8011", ChannelCredentials.Insecure).Intercept(tracingInterceptor));
            var request = new Person { Name = "Karl Heinz" };
            var _ = await client.GetNameAsync(request);

            var response2 = client.GetNameRequestStream();
            await response2.RequestStream.WriteAsync(request);
            await response2.RequestStream.WriteAsync(request);
            await response2.RequestStream.WriteAsync(request);
            await response2.RequestStream.CompleteAsync();
            await response2.ResponseAsync;

            var response3 = client.GetNameResponseStream(request);
            while (await response3.ResponseStream.MoveNext())
            {
                // Ignore
            }

            var response4 = client.GetNameBiDiStream(new Metadata());
            await response4.RequestStream.WriteAsync(request);
            await response4.RequestStream.WriteAsync(request);
            await response4.RequestStream.WriteAsync(request);
            await response4.RequestStream.CompleteAsync();
            while (await response4.ResponseStream.MoveNext())
            {
                // Ignore
            }

            try
            {
                var options =
                    new CallOptions()
                        .WithHeaders(new Metadata { { "CorrelationId", Guid.NewGuid().ToString() } })
                        .WithDeadline(DateTime.UtcNow.AddHours(1))
                        .WithWaitForReady(true)
                        .WithWriteOptions(new WriteOptions(WriteFlags.NoCompress));

                var response5 = await client.GetNameAsync(
                    new Person { Name = "Test" },
                    options);
            }
            catch
            {
                // Ignore
            }

            await server.ShutdownAsync();
            Console.ReadLine();
        }

        public class PhoneImpl : Phone.PhoneBase
        {
            public override Task<Person> GetName(Person request, ServerCallContext context)
            {
                if (string.IsNullOrEmpty(request.Name))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "name must not be empty"));

                return Task.FromResult(request);
            }

            public override async Task<Person> GetNameRequestStream(IAsyncStreamReader<Person> requestStream, ServerCallContext context)
            {
                Person request = null;
                while (await requestStream.MoveNext())
                {
                    request = requestStream.Current;
                    if (string.IsNullOrEmpty(request.Name))
                        throw new RpcException(new Status(StatusCode.InvalidArgument, "name must not be empty"));
                }

                return request ?? new Person();
            }

            public override async Task GetNameResponseStream(Person request, IServerStreamWriter<Person> responseStream, ServerCallContext context)
            {
                if (string.IsNullOrEmpty(request.Name))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "name must not be empty"));

                for (int i = 0; i < 3; i++)
                {
                    await responseStream.WriteAsync(request);
                    await Task.Delay(100);
                }
            }

            public override async Task GetNameBiDiStream(IAsyncStreamReader<Person> requestStream, IServerStreamWriter<Person> responseStream, ServerCallContext context)
            {
                while (await requestStream.MoveNext())
                {
                    var request = requestStream.Current;
                    if (string.IsNullOrEmpty(request.Name))
                        throw new RpcException(new Status(StatusCode.InvalidArgument, "name must not be empty"));

                    await responseStream.WriteAsync(request);
                }
            }
        }
    }
}
