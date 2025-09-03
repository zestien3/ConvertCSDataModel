using System;
using System.Collections.Generic;
using System.Reflection;
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
            if (!propertyDef.GetAccessors().Getter.IsNil)
            {
                var getter = Reader!.GetMethodDefinition(propertyDef.GetAccessors().Getter);
                switch (getter.Attributes & MethodAttributes.MemberAccessMask)
                {
                    case MethodAttributes.PrivateScope:
                    case MethodAttributes.Private:
                        Visibility = Visibility.Private;
                        break;
                    case MethodAttributes.FamANDAssem:
                    case MethodAttributes.Family:
                    case MethodAttributes.FamORAssem:
                        Visibility = Visibility.Protected;
                        break;
                    case MethodAttributes.Assembly:
                    case MethodAttributes.Public:
                        Visibility = Visibility.Public;
                        break;
                }
            }

            Name = Reader!.GetString(propertyDef.Name);
            DefiningClass = classInfo;

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
                        //       We should extend the TypeScriptFormatter class to convert C# types to TS types.
                        Type = signature.ReturnType;
                        IsGeneric = BaseTypeConverter.IsGeneric(Type);
                        IsArray = BaseTypeConverter.IsArray(Type);

                        // If we have a nested class we need to add the namespace of the defining class.
                        // This will be the class in which the property is defined, which is classInfo.
                        // Note that Type should also be changed, but we see how far we get without doing that.
                        if (BaseTypeConverter.StripToBareType(Type)[0] == '.')
                        {
                            if (IsGeneric)
                            {
                                Type = BaseTypeConverter.GetGenericType(Type) + "`1[" + classInfo.FullName + BaseTypeConverter.StripToBareType(Type) + "]";
                            }
                            else
                            {
                                if (IsArray)
                                {
                                    Type = classInfo.FullName + Type + "[]";
                                }
                                else
                                {
                                    Type = classInfo.FullName + Type;
                                }
                            }
                        }

                        if (DefiningClass.ContainingAssembly.ClassesByName.TryGetValue(Type, out var implementedClass))
                        {
                            ImplementedClass = implementedClass;
                        }

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

        /// <summary>
        /// The class where this property is defined.
        /// </summary>
        public MetadataClassInfo DefiningClass { get; private set; }

        /// <summary>
        /// The class which this property is implementing.
        /// </summary>
        public MetadataClassInfo? ImplementedClass { get; private set; }

        /// <summary>
        /// The full name of the type, like System.Generic.List`1[SomeType].
        /// </summary>
        public string? Type { get; private set; }

        /// <summary>
        /// Indicates if this is an array or a list.
        /// </summary>
        public bool IsArray { get; private set; }

        /// <summary>
        /// Indicates if this is a generic type.
        /// </summary>
        public bool IsGeneric { get; private set; }

        /// <summary>
        /// Indicates if this property should be serialized or not.
        /// </summary>
        public bool DontSerialize => attributes.ContainsKey("JsonIgnoreAttribute");

        /// <summary>
        /// A list of attributes that are set on this property.
        /// </summary>
        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes => attributes.AsReadOnly();
    }
}