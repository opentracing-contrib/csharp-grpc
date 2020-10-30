using System;

namespace OpenTracing.Contrib.Grpc.Streaming
{
    internal partial class TracingAsyncStreamReader<T>
    {
        internal readonly struct StreamActions
        {
            public ScopeActions ScopeActions { get; }
            public Action<T> OnMessage { get; }
            public Action OnStreamEnd { get; }
            public Action<Exception> OnException { get; }

            public StreamActions(ScopeActions scopeActions, Action<T> onMessage, Action onStreamEnd = null, Action<Exception> onException = null)
            {
                ScopeActions = scopeActions;
                OnMessage = onMessage;
                OnStreamEnd = onStreamEnd;
                OnException = onException;
            }

            public void Message(T msg)
            {
                OnMessage?.Invoke(msg);
            }

            public void Exception(Exception ex)
            {
                OnException?.Invoke(ex);
            }

            public void StreamEnd()
            {
                OnStreamEnd?.Invoke();
            }
        }
    }
}