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
            DefiningClass = classInfo;

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
                        Type = ReferencedType = signature;
                        IsArray = ReferencedType.Contains("`1[");
                        if (IsArray)
                        {
                            ReferencedType = ReferencedType.Substring(ReferencedType.IndexOf("`1[") + 3);
                            ReferencedType = ReferencedType.Substring(0, ReferencedType.Length - 1);
                        }

                        IsStandardType = BaseFormatter.csStandardTypes.Contains(ReferencedType!);

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

        /// <summary>
        /// The class where this field is defined.
        /// </summary>
        public MetadataClassInfo DefiningClass { get; private set; }

        /// <summary>
        /// The full name of the type, like System.Generic.List`1[SomeType].
        /// </summary>
        public string? Type { get; private set; }

        /// <summary>
        /// The base type which is referenced.
        /// So if the full type is System.Generic.List`1[SomeType], this would be SomeType.
        /// </summary>
        public string? ReferencedType { get; private set; }

        /// <summary>
        /// Indicates if this ia a C# standard type like int32, string or object.
        /// </summary>
        public bool IsStandardType { get; private set; }

        /// <summary>
        /// Indicates if this is an array or a list.
        /// </summary>
        public bool IsArray { get; private set; }

        /// <summary>
        /// Indicates if this field should be serialized or not.
        /// </summary>
        public bool DontSerialize => attributes.ContainsKey("JsonIgnoreAttribute");

        /// <summary>
        /// A list of attributes that are set on this field.
        /// </summary>
        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes => attributes.AsReadOnly();
    }
}