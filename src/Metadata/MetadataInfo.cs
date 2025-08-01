namespace Z3
{
    internal abstract class MetadataInfo
    {
        protected int loadedDepth = 0;
        public abstract void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad);

        public string? Name { get; protected set; }
    }
}