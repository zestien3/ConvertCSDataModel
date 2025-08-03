using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataFieldInfo : MetadataInfo
    {
        private FieldDefinition fieldDef;
        private List<string> attributes = new();

        public MetadataFieldInfo(FieldDefinition fieldDefinition, MetadataClassInfo classInfo, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            fieldDef = fieldDefinition;
            Name = Reader!.GetString(fieldDef.Name);

            XmlMemberName = $"T:{classInfo.FullName}.{Name}";
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > 0)
            {
                if (metadataInfo is MetadataClassInfo classInfo)
                {
                    try
                    {
                        var signature = fieldDef.DecodeSignature<string, MetadataInfo>(MetadataSignatureTypeProvider.Instance, this);
                        Type = signature;

                        foreach (var attributeHandle in fieldDef.GetCustomAttributes())
                        {
                            var attribute = Reader!.GetCustomAttribute(attributeHandle);
                            var customAttribute = attribute.DecodeValue(MetadataCustomAttributeTypeProvider.Instance);
                        }
                    }
                    catch (ArgumentOutOfRangeException) { }
                }
                else
                {
                    throw new ArgumentException($"Parameter {nameof(metadataInfo)} must be of type {nameof(MetadataClassInfo)}");
                }
            }
        }

        public string? Type { get; private set; }

        public IReadOnlyList<string> Attributes => attributes.AsReadOnly();

        public bool DontSerialize => attributes.Contains("JsonIgnoreAttribute");
    }
}