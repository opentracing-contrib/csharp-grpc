namespace OpenTracing.Contrib.Grpc
{
    public static class Constants
    {
        public const string TAGS_COMPONENT = "grpc";

        public const string TAGS_GRPC_AUTHORITY = "grpc.authority";
        public const string TAGS_GRPC_CALL_OPTIONS = "grpc.call_options";
        public const string TAGS_GRPC_DEADLINE_MILLIS = "grpc.deadline_millis";
        public const string TAGS_GRPC_HEADERS = "grpc.headers";
        public const string TAGS_GRPC_METHOD_NAME = "grpc.method_name";
        public const string TAGS_GRPC_METHOD_TYPE = "grpc.method_type";

        public const string TAGS_PEER_ADDRESS = "peer.address";
    }
}
