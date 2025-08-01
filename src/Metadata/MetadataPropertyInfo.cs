using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataPropertyInfo : MetadataInfo
    {
        private PropertyDefinition propertyDef;
        private MetadataReader reader;
        private List<string> attributes = new();

        public MetadataPropertyInfo(PropertyDefinition propertyDefinition, MetadataReader metadataReader)
        {
            propertyDef = propertyDefinition;
            reader = metadataReader;
            Name = reader.GetString(propertyDef.Name);
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > 0)
            {
                if (metadataInfo is MetadataClassInfo classInfo)
                {
                    var signature = propertyDef.DecodeSignature<string, MetadataInfo>(MetadataSignatureTypeProvider.Instance, this);
                    // TODO: For now we use this 'hack'
                    //       We should create a class that converts C# types to TS types.
                    Type = signature.ReturnType;

                    // We are going to look for Custom Attributes.
                    // For now we are only interested in JsonIgnoreAttribute.
                    // This code will get those for us. There are a number of 
                    // custom attributes we will skip, but that is OK for now.
                    var propAttributes = propertyDef.GetCustomAttributes();
                    foreach (var attributeHandle in propAttributes)
                    {
                        var attribute = reader.GetCustomAttribute(attributeHandle);
                        switch (attribute.Constructor.Kind)
                        {
                            case HandleKind.MemberReference:
                                var ctor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                                switch (ctor.Parent.Kind)
                                {
                                    case HandleKind.TypeReference:
                                        var a = reader.GetTypeReference((TypeReferenceHandle)ctor.Parent);
                                        attributes.Add(reader.GetString(a.Name));
                                        break;
                                }
                                break;
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Parameter {nameof(metadataInfo)} must be of type {nameof(MetadataClassInfo)}");
                }
            }
        }

        public string? Type { get; private set; }

        public bool IsStandardType { get; private set; }

        public IReadOnlyList<string> Attributes { get { return attributes.AsReadOnly(); } }
    }
}