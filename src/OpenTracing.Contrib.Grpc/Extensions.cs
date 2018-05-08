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

        public static ISpanBuilder WithTag(this ISpanBuilder spanBuilder, AbstractTag<bool> tagSetter, bool value)
        {
            return spanBuilder.WithTag(tagSetter.Key, value);
        }

        public static ISpanBuilder WithTag(this ISpanBuilder spanBuilder, AbstractTag<int> tagSetter, int value)
        {
            return spanBuilder.WithTag(tagSetter.Key, value);
        }

        public static ISpanBuilder WithTag(this ISpanBuilder spanBuilder, AbstractTag<double> tagSetter, double value)
        {
            return spanBuilder.WithTag(tagSetter.Key, value);
        }

        public static ISpanBuilder WithTag(this ISpanBuilder spanBuilder, AbstractTag<string> tagSetter, string value)
        {
            return spanBuilder.WithTag(tagSetter.Key, value);
        }

        public static ISpan SetTag(this ISpan span, AbstractTag<string> tagSetter, string value)
        {
            return span.SetTag(tagSetter.Key, value);
        }

        public static ISpan SetTag(this ISpan span, AbstractTag<bool> tagSetter, bool value)
        {
            return span.SetTag(tagSetter.Key, value);
        }

        public static ISpan SetTag(this ISpan span, AbstractTag<int> tagSetter, int value)
        {
            return span.SetTag(tagSetter.Key, value);
        }

        public static ISpan SetTag(this ISpan span, AbstractTag<double> tagSetter, double value)
        {
            return span.SetTag(tagSetter.Key, value);
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
