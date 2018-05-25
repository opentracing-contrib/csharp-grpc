using System;
using System.Collections.Generic;
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
                .Log(new Dictionary<string, object>(4)
                {
                    {LogFields.Event, Tags.Error.Key},
                    {LogFields.ErrorKind, ex.GetType().Name},
                    {LogFields.ErrorObject, ex},
                    {LogFields.Message, ex.Message}
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
    }
}
