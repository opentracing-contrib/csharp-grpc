using System.Collections;
using System.Collections.Generic;
using Grpc.Core;
using OpenTracing.Propagation;

namespace OpenTracing.Contrib.Grpc.Propagation
{
    internal class MetadataCarrier : ITextMap
    {
        private readonly Metadata _metadata;

        public MetadataCarrier(Metadata metadata)
        {
            _metadata = metadata;
        }

        public void Set(string key, string value)
        {
            // We remove all the existing values for that key before adding the new one to have a "set" behavior:
            for (var i = _metadata.Count - 1; i >= 0; i--)
            {
                if (_metadata[i].Key == key)
                {
                    _metadata.RemoveAt(i);
                }
            }

            _metadata.Add(key, value);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var entry in _metadata)
            {
                if (entry.IsBinary)
                    continue;

                yield return new KeyValuePair<string, string>(entry.Key, entry.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}