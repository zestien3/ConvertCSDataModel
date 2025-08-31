using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataPropertyInfo : MetadataInfo, IMemberInfo
    {
        private PropertyDefinition propertyDef;

        private Dictionary<string, MetadataAttributeInfo> attributes = [];

        public MetadataPropertyInfo(PropertyDefinition propertyDefinition, MetadataClassInfo classInfo, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            propertyDef = propertyDefinition;
            Name = Reader!.GetString(propertyDef.Name);
            OwningClass = classInfo;

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
                        Type = MinimizedType = signature.ReturnType;
                        IsArray = MinimizedType.Contains("`1[");
                        if (IsArray)
                        {
                            MinimizedType = MinimizedType.Substring(MinimizedType.IndexOf("`1[") + 3);
                            MinimizedType = MinimizedType.Substring(0, MinimizedType.Length - 1);
                        }

                        IsStandardType = BaseFormatter.csStandardTypes.Contains(MinimizedType!);

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

        public MetadataClassInfo OwningClass { get; private set; }

        public string? Type { get; private set; }

        public string? MinimizedType { get; private set; }

        public bool IsStandardType { get; private set; }

        public bool IsArray { get; private set; }

        public bool DontSerialize => attributes.ContainsKey("JsonIgnoreAttribute");

        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes => attributes.AsReadOnly();
    }
}