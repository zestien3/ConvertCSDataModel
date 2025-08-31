using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Zestien3;

namespace Z3
{
    internal class MetadataClassInfo : MetadataInfo
    {
        private TypeDefinition typeDef;
        private Dictionary<string, MetadataPropertyInfo> properties = [];
        private Dictionary<string, MetadataFieldInfo> fields = [];
        private Dictionary<string, MetadataAttributeInfo> attributes = [];

        public MetadataClassInfo(TypeDefinition typeDefinition, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            typeDef = typeDefinition;
            Name = reader.GetString(typeDef.Name);
            Namespace = reader.GetString(typeDef.Namespace);

            XmlMemberName = $"T:{FullName}";

            // We are going to look for Custom Attributes.
            // For now we are only interested in UseInFrontendAttribute.
            // This code will get those for us. There are a number of 
            // custom attributes we will skip, but that is OK for now.
            var classAttributes = typeDef.GetCustomAttributes();
            foreach (var attributeHandle in classAttributes)
            {
                var attribute = new MetadataAttributeInfo(reader.GetCustomAttribute(attributeHandle), Reader!);
                if (!string.IsNullOrEmpty(attribute.Name))
                    attributes[attribute.Name!] = attribute;
            }

            // Get the SubFolder where the file for this classinfo needs to be stored.
            if (attributes.TryGetValue(nameof(UseInFrontendAttribute), out var attr) &&
                attr.NamedArguments.TryGetValue(nameof(UseInFrontendAttribute.SubFolder), out var subFolder))
            {
                SubFolder = (string)subFolder.Value!;
            }
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > LoadedDepth)
            {
                if (metadataInfo is MetadataAssemblyInfo assemblyInfo)
                {
                    switch (typeDef.BaseType.Kind)
                    {
                        case HandleKind.TypeDefinition:
                        {
                            var baseType = Reader!.GetTypeDefinition((TypeDefinitionHandle)typeDef.BaseType);
                            BaseTypeFullName = Reader.GetString(baseType.Namespace) + "." + Reader.GetString(baseType.Name);
                            BaseType = assemblyInfo.ClassesByName[BaseTypeFullName];
                            break;
                        }
                        case HandleKind.TypeReference:
                        {
                            var baseType = Reader!.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
                            BaseTypeFullName = Reader.GetString(baseType.Namespace) + "." + Reader.GetString(baseType.Name);
                            break;
                        }
                        case HandleKind.TypeSpecification:
                        {
                            // I guess this is very likely a anonymous class.
                            BaseTypeFullName = "Anonymous class";
                            break;
                        }
                        default:
                            BaseTypeFullName = typeDef.BaseType.Kind.ToString();
                            break;
                    }
                }
                else
                {
                    throw new ArgumentException($"Parameter {nameof(metadataInfo)} must be of type {nameof(MetadataAssemblyInfo)}");
                }

                if (depthToLoad >= LoadedDepth + 1)
                {
                    foreach (var propertyHandle in typeDef.GetProperties())
                    {
                        var propertyInfo = new MetadataPropertyInfo(Reader!.GetPropertyDefinition(propertyHandle), this, Reader, XmlDoc);
                        properties[propertyInfo.Name!] = propertyInfo;
                    }

                    foreach (var fieldHandle in typeDef.GetFields())
                    {
                        var fieldInfo = new MetadataFieldInfo(Reader!.GetFieldDefinition(fieldHandle), this, Reader, XmlDoc);
                        fields[fieldInfo.Name!] = fieldInfo;
                    }
                }

                if (depthToLoad >= LoadedDepth + 2)
                {
                    foreach (var propInfo in Properties.Values)
                    {
                        propInfo.AllClassesLoaded(this, depthToLoad - 1);
                    }

                    foreach (var fieldInfo in Fields.Values)
                    {
                        fieldInfo.AllClassesLoaded(this, depthToLoad - 1);
                    }
                }

                LoadedDepth = depthToLoad;
            }
        }

        public IReadOnlyDictionary<string, MetadataPropertyInfo> Properties { get { return properties.AsReadOnly(); } }

        public IReadOnlyDictionary<string, MetadataFieldInfo> Fields { get { return fields.AsReadOnly(); } }

        public MetadataClassInfo? BaseType { get; private set; }

        public string? BaseTypeFullName { get; private set; }

        public string Namespace { get; }

        public string FullName { get { return Namespace + "." + Name; } }

        public string SubFolder { get; private set; } = string.Empty;

        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes { get { return attributes.AsReadOnly(); } }
    }
}