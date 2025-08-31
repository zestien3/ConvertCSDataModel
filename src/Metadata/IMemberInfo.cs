namespace Z3
{
    internal interface IMemberInfo
    {
        public string? ReferencedType { get; }

        public bool IsStandardType { get; }
    }
}