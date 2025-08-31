namespace Z3
{
    internal interface IMemberInfo
    {
        public string? MinimizedType { get; }

        public bool IsStandardType { get; }
    }
}