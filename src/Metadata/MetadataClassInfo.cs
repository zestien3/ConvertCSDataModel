using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Zestien3;

namespace Z3
{
    internal class MetadataClassInfo : MetadataInfo
    {
        private TypeDefinition typeDef;
        protected Dictionary<string, MetadataAttributeInfo> attributes = [];

        private List<MetadataMemberInfo> members = [];

        protected MetadataClassInfo(string type) : base(null, null)
        {
            Logger.LogDebug($"Undefined or anonymous class found: {type}");

            UseInFrontend = new();

            ContainingAssembly = null;
            Name = type;
            Namespace = string.Empty;
        }

        public MetadataClassInfo(MetadataAssemblyInfo assembly, TypeDefinition typeDefinition, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            ContainingAssembly = assembly;
            typeDef = typeDefinition;
            Name = reader.GetString(typeDef.Name);
            Namespace = reader.GetString(typeDef.Namespace);

            if (string.IsNullOrEmpty(Namespace) && assembly.ClassesByHandle.ContainsKey(typeDef.GetDeclaringType()))
            {
                Namespace = assembly.ClassesByHandle[typeDef.GetDeclaringType()].FullName;
            }

            Logger.LogDebug($"Creating class info for {FullName}");

            XmlMemberName = $"T:{FullName}";

            // We are going to look for Custom Attributes.
            var classAttributes = typeDef.GetCustomAttributes();
            foreach (var attributeHandle in classAttributes)
            {
                var attribute = new MetadataAttributeInfo(reader.GetCustomAttribute(attributeHandle), Reader!);

                if (null != attribute.Name)
                    attributes[attribute.Name!] = attribute;

                if (!string.IsNullOrEmpty(attribute.Name))
                {
                    ProcessFoundAttribute(attribute);
                    Logger.LogDebug($"Found attribute {attribute.Name} on class {Name}");
                }
            }
        }

        private void ProcessFoundAttribute(MetadataAttributeInfo attribute)
        {
            if (attribute.Name == "NullableContextAttribute")
            {
                NullableContext = (byte)attribute.FixedArguments[0].Value!;
            }

            // Get the UseInFrontendAttribute and store it.
            if (attribute.Name == nameof(UseInFrontendAttribute))
            {
                if (!attribute.NamedArguments.TryGetValue(nameof(UseInFrontendAttribute.Language), out var language))
                {
                    throw new ArgumentException($"${nameof(UseInFrontendAttribute)} must have it's ${nameof(UseInFrontendAttribute.Language)} property set.");
                }
                if (!attribute.NamedArguments.TryGetValue(nameof(UseInFrontendAttribute.SubFolder), out var subFolder))
                {
                    subFolder = new(nameof(UseInFrontendAttribute.SubFolder), CustomAttributeNamedArgumentKind.Property, "string", ".");
                }

                if (!attribute.NamedArguments.TryGetValue(nameof(UseInFrontendAttribute.HiddenProperties), out var hidden))
                {
                    string[] value = [];
                    hidden = new(nameof(UseInFrontendAttribute.HiddenProperties), CustomAttributeNamedArgumentKind.Property, "string[]", value);
                }

                UseInFrontend[(Language)language.Value!] =
                    new()
                    {
                        SubFolder = (string)subFolder.Value!,
                        Language = (Language)language.Value!,
                        HiddenProperties = []
                    };

                foreach (var hiddenProperty in (IEnumerable)hidden.Value!)
                {
                    UseInFrontend[(Language)language.Value!].HiddenProperties.Add((string)((CustomAttributeTypedArgument<string>)hiddenProperty).Value!);
                }
            }

            if (attribute.Name == nameof(FixedParameterValueAttribute))
            {
                if (attribute.NamedArguments.TryGetValue(nameof(FixedParameterValueAttribute.Name), out var name))
                {
                    if (attribute.NamedArguments.TryGetValue(nameof(FixedParameterValueAttribute.Value), out var value))
                    {
                        FixedConstructionParameters[(string)name.Value!] = (string)value.Value!;
                    }
                }
            }
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if ((depthToLoad > LoadedDepth) && (LoadedDepth == 0))
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
                                BaseType.DerivedTypes.Add(this);
                                break;
                            }
                        case HandleKind.TypeReference:
                            {
                                var baseType = Reader!.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
                                BaseTypeFullName = Reader.GetString(baseType.Namespace) + "." + Reader.GetString(baseType.Name);
                                IsEnum = BaseTypeFullName == "System.Enum";
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

                LoadedDepth++;
            }

            if ((depthToLoad > LoadedDepth) && (LoadedDepth == 1))
            {
                foreach (var propertyHandle in typeDef.GetProperties())
                {
                    var propertyInfo = new MetadataMemberInfo(Reader!.GetPropertyDefinition(propertyHandle), this, Reader, XmlDoc);
                    propertyInfo.AllClassesLoaded(this, 1);
                    if (!propertyInfo.DontSerialize && (propertyInfo.Visibility != Visibility.Private))
                    {
                        members.Add(propertyInfo);
                    }
                }

                foreach (var fieldHandle in typeDef.GetFields())
                {
                    var fieldInfo = new MetadataMemberInfo(Reader!.GetFieldDefinition(fieldHandle), this, Reader, XmlDoc);
                    fieldInfo.AllClassesLoaded(this, 1);
                    if (!fieldInfo.DontSerialize &&
                        (fieldInfo.Visibility != Visibility.Private) &&
                        !fieldInfo.Attributes.ContainsKey("CompilerGeneratedAttribute") &&
                        !fieldInfo.Attributes.ContainsKey("SpecialNameAttribute"))
                    {
                        members.Add(fieldInfo);
                    }
                }

                LoadedDepth++;
            }
        }

        public bool Any(Func<MetadataClassInfo, bool> callBack)
        {
            if (callBack(this))
            {
                return true;
            }
            foreach (var derivedType in DerivedTypes)
            {
                if (derivedType.Any(callBack))
                {
                    return true;
                }
            }

            return false;
        }

        public bool All(Func<MetadataClassInfo, bool> callBack)
        {
            if (!callBack(this))
            {
                return false;
            }
            foreach (var derivedType in DerivedTypes)
            {
                if (!derivedType.Any(callBack))
                {
                    return false;
                }
            }

            return true;
        }

        public List<T> Select<T>(Func<MetadataClassInfo, List<T>> callBack)
        {
            List<T> result = callBack(this);
            foreach (var derivedType in DerivedTypes)
            {
                var o = callBack(derivedType);
                if (null != o)
                {
                    result.AddRange(o);
                }

                result.AddRange(derivedType.Select<T>(callBack));
            }

            return result;
        }

        /// <summary>
        /// A list of attributes that are set on this class.
        /// </summary>
        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes => attributes.AsReadOnly();

        public MetadataAssemblyInfo? ContainingAssembly { get; private set; }

        public IReadOnlyList<MetadataMemberInfo> Members { get { return members.AsReadOnly(); } }

        public MetadataClassInfo? BaseType { get; private set; }

        public string? BaseTypeFullName { get; private set; }

        public List<MetadataClassInfo> DerivedTypes { get; } = [];

        public string Namespace { get; }

        public string FullName { get { return Namespace + "." + Name; } }

        public bool IsEnum { get; private set; } = false;

        public bool IsArray => BaseTypeConverter.IsArray(Name);

        public bool IsGeneric => BaseTypeConverter.IsGeneric(Name);

        public byte? NullableContext { get; private set; }

        public Dictionary<Language, UseInFrontendAttribute> UseInFrontend { get; private set; } = [];
        
        public Dictionary<string, string> FixedConstructionParameters { get; private set; } = [];
    }

    internal class MetadataClassInfoNotFound : MetadataClassInfo
    {
        public MetadataClassInfoNotFound(string type) : base(type)
        {
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            // This class is not read using the metadata reflection framework,
            // so there is no need to do anything here.
            LoadedDepth = depthToLoad;
        }
    }
}