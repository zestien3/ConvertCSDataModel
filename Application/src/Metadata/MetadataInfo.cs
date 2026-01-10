using System.Reflection.Metadata;

namespace Z3
{
    internal enum Visibility
    {
        Private,
        Protected,
        Public
    }

    internal abstract class MetadataInfo
    {
        public MetadataInfo(MetadataReader? reader, XmlDocumentationFile? xmlDoc)
        {
            LoadedDepth = 0;
            Reader = reader;
            XmlDoc = xmlDoc;
            XmlMemberName = "";
        }

        public abstract void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad);

        public string? Name { get; protected set; }
        public XmlDocumentation? XmlComment
        {
            get
            {
                return new XmlDocumentation(XmlDoc?.GetXmlComments(XmlMemberName));
            }
        }

        protected int LoadedDepth { get; set; }
        public string XmlMemberName { get; protected set; }
        public Visibility Visibility { get; protected set; }
        protected MetadataReader? Reader { get; set; }
        protected XmlDocumentationFile? XmlDoc { get; set; }
    }
}