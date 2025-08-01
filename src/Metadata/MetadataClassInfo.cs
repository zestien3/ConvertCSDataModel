using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataClassInfo : MetadataInfo
    {
        private TypeDefinition typeDef;
        private MetadataReader reader;
        private Dictionary<string, MetadataPropertyInfo> properties = new();
        private Dictionary<string, MetadataFieldInfo> fields = new();
        private List<string> attributes = new();

        public MetadataClassInfo(TypeDefinition typeDefinition, MetadataReader metadataReader)
        {
            typeDef = typeDefinition;
            reader = metadataReader;
            Name = reader.GetString(typeDef.Name);
            Namespace = reader.GetString(typeDef.Namespace);

            // We are going to look for Custom Attributes.
            // For now we are only interested in UseInFrontendAttribute.
            // This code will get those for us. There are a number of 
            // custom attributes we will skip, but that is OK for now.
            var propAttributes = typeDef.GetCustomAttributes();
            foreach (var attributeHandle in propAttributes)
            {
                var attribute = reader.GetCustomAttribute(attributeHandle);
                switch (attribute.Constructor.Kind)
                {
                    case HandleKind.MemberReference:
                    {
                        var ctor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                        switch (ctor.Parent.Kind)
                        {
                            case HandleKind.TypeReference:
                                var a = reader.GetTypeReference((TypeReferenceHandle)ctor.Parent);
                                var attr = reader.GetString(a.Name);
                                attributes.Add(attr);
                                break;
                            default:
                                Console.Error.WriteLine($"{nameof(MetadataClassInfo)}: Attribute.Parent kind is of type {ctor.Parent.Kind}");
                                break;
                        }
                        break;
                    }
                    case HandleKind.MethodDefinition:
                    {
                        var ctor = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                        var attr = ctor.GetDeclaringType().ToTypeString(reader);
                        attr = attr.Substring(attr.LastIndexOf('.') + 1);
                        attributes.Add(attr);
                        break;
                    }
                    default:
                        Console.Error.WriteLine($"{nameof(MetadataClassInfo)}: Attribute.Constructor kind is of type {attribute.Constructor.Kind}");
                        break;
                }
            }
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > loadedDepth)
            {
                if (metadataInfo is MetadataAssemblyInfo assemblyInfo)
                {
                    switch (typeDef.BaseType.Kind)
                    {
                        case HandleKind.TypeDefinition:
                            {
                                var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)typeDef.BaseType);
                                BaseTypeFullName = reader.GetString(baseType.Namespace) + "." + reader.GetString(baseType.Name);
                                BaseType = assemblyInfo.ClassesByName[BaseTypeFullName];
                                break;
                            }
                        case HandleKind.TypeReference:
                            {
                                var baseType = reader.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
                                BaseTypeFullName = reader.GetString(baseType.Namespace) + "." + reader.GetString(baseType.Name);
                                break;
                            }
                        case HandleKind.TypeSpecification:
                            {
                                // I guess this is very likely a anonymouse class.
                                BaseTypeFullName = "Anonymouse class";
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

                if (depthToLoad >= loadedDepth + 1)
                {
                    foreach (var propertyHandle in typeDef.GetProperties())
                    {
                        var propertyInfo = new MetadataPropertyInfo(reader.GetPropertyDefinition(propertyHandle), reader);
                        properties[propertyInfo.Name!] = propertyInfo;
                    }

                    foreach (var fieldHandle in typeDef.GetFields())
                    {
                        var fieldInfo = new MetadataFieldInfo(reader.GetFieldDefinition(fieldHandle), reader);
                        fields[fieldInfo.Name!] = fieldInfo;
                    }
                }

                if (depthToLoad >= loadedDepth + 2)
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

                loadedDepth = depthToLoad;
            }
        }

        public ReadOnlyDictionary<string, MetadataPropertyInfo> Properties { get { return properties.AsReadOnly(); } }

        public ReadOnlyDictionary<string, MetadataFieldInfo> Fields { get { return fields.AsReadOnly(); } }

        public MetadataClassInfo? BaseType { get; private set; }

        public string? BaseTypeFullName { get; private set; }

        public string Namespace { get; }

        public string FullName { get { return Namespace + "." + Name; } }

        public IReadOnlyList<string> Attributes { get { return attributes.AsReadOnly(); } }
    }
}