using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Grpc.Core;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.Grpc
{
    public static class Extensions
    {
        public static ISpan SetException(this ISpan span, Exception ex)
        {
            return span?.SetTag(Tags.Error, true)
                .Log(new Dictionary<string, object>(5)
                {
                    {LogFields.Event, Tags.Error.Key},
                    {LogFields.ErrorObject, ex},

                    // Those fields will be removed once Configration.WithExpandExceptionLogs is implemented
                    {LogFields.ErrorKind, ex.GetType().Name}, 
                    {LogFields.Message, ex.Message},
                    {LogFields.Stack, ex.StackTrace}
                });
        }

        public static TimeSpan TimeRemaining(this DateTime deadline)
        {
            return deadline.Subtract(DateTime.UtcNow);
        }

        public static string ToReadableString(this Metadata metadata)
        {
            if (metadata.Count == 0)
                return null;

            return string.Join(";", metadata.Select(e => $"{e.Key} = {e.Value}"));
        }

        public static string ToReadableString(this CallOptions options)
        {
            // Should this be converted to milis?
            var deadline = options.Deadline.HasValue ? options.Deadline.Value.ToUniversalTime().ToString(CultureInfo.InvariantCulture) : "Infinite";
            var headers = options.Headers.ToReadableString() ?? "Empty";
            var writeOptions = options.WriteOptions != null ? options.WriteOptions.Flags.ToString() : "None";
            var isContextPropagated = options.PropagationToken != null;

            return $"Headers: {headers}; " +
                   $"Deadline: {deadline}; " +
                   $"IsWaitForReady: {options.IsWaitForReady}; " +
                   $"WriteOptions: {writeOptions} " +
                   $"IsContextPropagated: {isContextPropagated}";
        }

        public static string GetAuthorizationHeaderValue(this Metadata headers)
        {
            var authorization = headers?.FirstOrDefault(x => x.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase))?.Value;

            return string.IsNullOrWhiteSpace(authorization) ? "NoAuth" : authorization;
        }
    }
}