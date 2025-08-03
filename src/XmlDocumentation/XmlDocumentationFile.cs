using System.IO;
using System.Xml;

namespace Z3
{
    internal class XmlDocumentationFile
    {
        private XmlDocument? xmlDoc;
        private XmlNamespaceManager? nsMgr;

        public XmlDocumentationFile(string fileName)
        {
            var xmlCommentsFileName = fileName;
            if (File.Exists(xmlCommentsFileName))
            {
                xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlCommentsFileName);
                nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsMgr.AddNamespace("ns", xmlDoc.DocumentElement!.NamespaceURI);
            }
        }

        public XmlNode? GetXmlComments(string? xmlMemberName)
        {
            if (null == xmlDoc || null == nsMgr || (string.IsNullOrEmpty(xmlMemberName)))
            {
                return null;
            }

            return xmlDoc?.SelectSingleNode($"/doc/members/member[@name='{xmlMemberName}']", nsMgr!);
        }
    }
}