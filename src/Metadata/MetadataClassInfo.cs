using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataClassInfo : MetadataInfo
    {
        private TypeDefinition typeDef;
        private Dictionary<string, MetadataPropertyInfo> properties = new();
        private Dictionary<string, MetadataFieldInfo> fields = new();
        private Dictionary<string, Dictionary<string, string>> attributes = new();

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

                                // This would get the parameters, but for now we are not interested in them. 
                                // var parameters = attribute.DecodeValue(MetadataCustomAttributeTypeProvider.Instance);

                                attributes[attr] = [];
                                break;
                            default:
                                Console.Error.WriteLine($"{nameof(MetadataClassInfo)}: Attribute.Parent kind is of type {ctor.Parent.Kind}");
                                break;
                        }
                        break;
                    }
                    case HandleKind.MethodDefinition:
                    {
                        // The attribute is defined using it's constructor. 
                        var ctor = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);

                        // The return value of the constructor is void, so we get the declaring type,
                        // which is the attribute class. We remove the namespace.
                        var attr = ctor.GetDeclaringType().ToTypeString(reader);
                        attr = attr.Substring(attr.LastIndexOf('.') + 1);

                        // TODO: We can move this code to the outer scope, as the attribute is defined
                        //       in that scope.
                        // We get all the parameters passed to the method, which are FixedArguments
                        // or NamedArguments. For now we are only interested in the named arguments.
                        var parameters = attribute.DecodeValue(MetadataCustomAttributeTypeProvider.Instance);
                        attributes[attr] = [];

                        // We add all named arguments of type string and their values to the dictionary.
                        foreach (var na in parameters.NamedArguments)
                        {
                            if (na.Type == "string")
                            {
                                attributes[attr][na.Name!] = (string) na.Value!;
                            }   
                        }
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

        public ReadOnlyDictionary<string, MetadataPropertyInfo> Properties { get { return properties.AsReadOnly(); } }

        public ReadOnlyDictionary<string, MetadataFieldInfo> Fields { get { return fields.AsReadOnly(); } }

        public MetadataClassInfo? BaseType { get; private set; }

        public string? BaseTypeFullName { get; private set; }

        public string Namespace { get; }

        public string FullName { get { return Namespace + "." + Name; } }

        public IReadOnlyDictionary<string, Dictionary<string, string>> Attributes { get { return attributes.AsReadOnly(); } }
    }
}