using System.Threading.Tasks;
using Grpc.Core;

namespace OpenTracing.Contrib.Grpc.Streaming
{
    internal class TracingClientStreamWriter<T> : IClientStreamWriter<T>
    {
        private readonly IClientStreamWriter<T> _writer;
        private readonly StreamActions<T> _streamActions;

        public TracingClientStreamWriter(IClientStreamWriter<T> writer, StreamActions<T> streamActions)
        {
            _writer = writer;
            _streamActions = streamActions;
        }

        public WriteOptions WriteOptions
        {
            get => _writer.WriteOptions;
            set => _writer.WriteOptions = value;
        }

        public Task WriteAsync(T message)
        {
            _streamActions.ScopeActions.EndScope();
            _streamActions.ScopeActions.BeginScope();
            _streamActions.Message(message);

            return _writer.WriteAsync(message);
        }

        public Task CompleteAsync()
        {
            _streamActions.ScopeActions.EndScope();
            _streamActions.StreamEnd();

            return _writer.CompleteAsync();
        }
    }
}