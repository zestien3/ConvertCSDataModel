using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Z3
{
    internal class XmlDocumentation
    {
        public XmlDocumentation(XmlNode? xmlNode)
        {
            if (null == xmlNode)
            {
                Summary = [];
                Remarks = [];
                Parameters = [];
                Returns = string.Empty;
            }
            else
            {
                Summary = XmlCleanup(xmlNode!.SelectSingleNode("./summary")?.InnerText);
                Remarks = XmlCleanup(xmlNode.SelectSingleNode("./remarks")?.InnerText);

                Parameters = new();
                var paramNodes = xmlNode.SelectNodes("./param");
                if (paramNodes != null)
                {
                    foreach (XmlNode node in paramNodes!)
                    {
                        if (node.Attributes?["name"] != null)
                            Parameters[node.Attributes!["name"]!.Value] = node.InnerText.Trim();
                    }
                }

                Returns = xmlNode.SelectSingleNode("./returns")?.InnerText.Trim();
            }
        }

        private static IEnumerable<string> XmlCleanup(string? str)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(str))
            {
                return result;
            }

            foreach (string s in str.Split('\r', '\n'))
            {
                var trimmedStr = s.Trim();
                if (!string.IsNullOrEmpty(trimmedStr))
                {
                    result.Add(trimmedStr);
                }
            }

            return result;
        }


        public IEnumerable<string> Summary { get; private set; }

        public IEnumerable<string> Remarks { get; private set; }

        public Dictionary<string, string> Parameters { get; private set; }

        public string? Returns { get; private set; }
        public bool HasContent
        {
            get
            {
                return Summary.Any() ||
                       Remarks.Any() ||
                       Parameters.Count > 0 ||
                       !string.IsNullOrEmpty(Returns);
            }
        }
    }
}