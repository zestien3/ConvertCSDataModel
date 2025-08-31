namespace Z3
{
    internal interface IMemberInfo
    {
        public MetadataClassInfo OwningClass { get; }

        public string? Type { get; }

        public string? MinimizedType { get; }

        public bool IsStandardType { get; }

        public bool IsArray { get; }

        public bool DontSerialize { get; }

    }
}