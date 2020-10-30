using System;

namespace OpenTracing.Contrib.Grpc.Streaming
{
    internal readonly struct ScopeActions
    {
        public string ScopeOperationName { get; }
        public Action<string> OnBeginScope { get; }
        public Action OnEndScope { get; }

        public ScopeActions(string scopeOperationName, Action<string> onBeginScope, Action onEndScope)
        {
            ScopeOperationName = scopeOperationName;
            OnBeginScope = onBeginScope;
            OnEndScope = onEndScope;
        }

        public void BeginScope()
        {
            OnBeginScope?.Invoke(ScopeOperationName);
        }

        public void EndScope()
        {
            OnEndScope?.Invoke();
        }
    }
}