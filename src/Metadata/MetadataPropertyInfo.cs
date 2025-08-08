using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataPropertyInfo : MetadataInfo
    {
        private PropertyDefinition propertyDef;

        private Dictionary<string, MetadataAttributeInfo> attributes = [];

        public MetadataPropertyInfo(PropertyDefinition propertyDefinition, MetadataClassInfo classInfo, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            propertyDef = propertyDefinition;
            Name = Reader!.GetString(propertyDef.Name);

            XmlMemberName = $"P:{classInfo.FullName}.{Name}";
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > 0)
            {
                if (metadataInfo is MetadataClassInfo classInfo)
                {
                    try
                    {
                        var signature = propertyDef.DecodeSignature<string, MetadataInfo>(MetadataSignatureTypeProvider.Instance, this);
                        // TODO: For now we use this 'hack'
                        //       We should create a class that converts C# types to TS types.
                        Type = signature.ReturnType;

                        IsStandardType = BaseFormatter.csStandardTypes.Contains(Type!);

                        // We are going to look for Custom Attributes.
                        // For now we are only interested in JsonIgnoreAttribute.
                        // This code will get those for us. There are a number of 
                        // custom attributes we will skip, but that is OK for now.
                        var propAttributes = propertyDef.GetCustomAttributes();
                        foreach (var attributeHandle in propAttributes)
                        {
                            var attribute = Reader!.GetCustomAttribute(attributeHandle);
                            var customAttribute = new MetadataAttributeInfo(attribute, Reader);
                            if (!string.IsNullOrEmpty(customAttribute.Name))
                                attributes[customAttribute.Name!] = customAttribute;
                        }
                    }
                    catch (ArgumentNullException) { }
                }
                else
                {
                    throw new ArgumentException($"Parameter {nameof(metadataInfo)} must be of type {nameof(MetadataClassInfo)}");
                }
            }
        }

        public string? Type { get; private set; }

        public bool IsStandardType { get; private set; }

        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes => attributes.AsReadOnly();

        public bool DontSerialize => attributes.ContainsKey("JsonIgnoreAttribute");
    }
}