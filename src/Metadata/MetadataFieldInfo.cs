using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataFieldInfo : MetadataInfo, IMemberInfo
    {
        private FieldDefinition fieldDef;
        private Dictionary<string, MetadataAttributeInfo> attributes = [];

        public MetadataFieldInfo(FieldDefinition fieldDefinition, MetadataClassInfo classInfo, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            fieldDef = fieldDefinition;
            Name = Reader!.GetString(fieldDef.Name);
            OwningClass = classInfo;

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
                        // TODO: For now we use this 'hack'
                        //       We should create a class that converts C# types to TS types.
                        Type = MinimizedType = signature;
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
                        foreach (var attributeHandle in fieldDef.GetCustomAttributes())
                        {
                            var attribute = Reader!.GetCustomAttribute(attributeHandle);
                            var customAttribute = new MetadataAttributeInfo(attribute, Reader);
                            if (!string.IsNullOrEmpty(customAttribute.Name))
                                attributes[customAttribute.Name!] = customAttribute;
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

        public MetadataClassInfo OwningClass { get; private set; }

        public string? Type { get; private set; }

        public string? MinimizedType { get; private set; }

        public bool IsStandardType { get; private set; }

        public bool IsArray { get; private set; }

        public bool DontSerialize => attributes.ContainsKey("JsonIgnoreAttribute");

        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes => attributes.AsReadOnly();
    }
}