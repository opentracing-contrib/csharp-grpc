using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace OpenTracing.Contrib.Grpc.Streaming
{
    internal partial class TracingAsyncStreamReader<T> : IAsyncStreamReader<T>
    {
        private readonly IAsyncStreamReader<T> _reader;
        private readonly StreamActions _streamActions;

        public T Current => _reader.Current;

        public TracingAsyncStreamReader(IAsyncStreamReader<T> reader, StreamActions streamActions)
        {
            _reader = reader;
            _streamActions = streamActions;
        }

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            try
            {
                _streamActions.ScopeActions.EndScope();

                var hasNext = await _reader.MoveNext(cancellationToken).ConfigureAwait(false);
                if (hasNext)
                {
                    _streamActions.ScopeActions.BeginScope();
                    _streamActions.Message(Current);
                }
                else
                {
                    _streamActions.StreamEnd();
                }

                return hasNext;
            }
            catch (Exception ex)
            {
                _streamActions.Exception(ex);
                throw;
            }
        }
    }
}