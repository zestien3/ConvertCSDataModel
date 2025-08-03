using System.Reflection.Metadata;
using System.Xml;

namespace Z3
{
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
        public XmlNode? XmlComment
        {
            get
            {
                return XmlDoc?.GetXmlComments(XmlMemberName);
            }
        }

        protected int LoadedDepth { get; set; }
        public string XmlMemberName { get; protected set; }
        protected MetadataReader? Reader { get; set; }
        protected XmlDocumentationFile? XmlDoc { get; set; }
    }
}