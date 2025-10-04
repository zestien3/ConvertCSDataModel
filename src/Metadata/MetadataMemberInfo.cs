using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataMemberInfo : MetadataInfo
    {
        protected Dictionary<string, MetadataAttributeInfo> attributes = [];

        private readonly PropertyDefinition propertyDef;

        private readonly FieldDefinition fieldDef;

        private readonly bool isProperty;

        public MetadataMemberInfo(PropertyDefinition propertyDefinition, MetadataClassInfo classInfo, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            isProperty = true;
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

        public MetadataMemberInfo(FieldDefinition fieldDefinition, MetadataClassInfo classInfo, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
            isProperty = false;
            fieldDef = fieldDefinition;
            switch (fieldDef.Attributes & FieldAttributes.FieldAccessMask)
            {
                case FieldAttributes.PrivateScope:
                case FieldAttributes.Private:
                    Visibility = Visibility.Private;
                    break;
                case FieldAttributes.FamANDAssem:
                case FieldAttributes.Family:
                case FieldAttributes.FamORAssem:
                    Visibility = Visibility.Protected;
                    break;
                case FieldAttributes.Assembly:
                case FieldAttributes.Public:
                    Visibility = Visibility.Public;
                    break;
            }

            Name = Reader!.GetString(fieldDef.Name);
            DefiningClass = classInfo;

            XmlMemberName = $"F:{classInfo.FullName}.{Name}";
        }

        protected string DecodeMemberSignature()
        {
            if (isProperty)
            {
                return propertyDef.DecodeSignature<string, MetadataInfo>(MetadataSignatureTypeProvider.Instance, this).ReturnType;
            }

            return fieldDef.DecodeSignature<string, MetadataInfo>(MetadataSignatureTypeProvider.Instance, this);
        }

        protected CustomAttributeHandleCollection GetMemberAttributes()
        {
            if (isProperty)
            {
                return propertyDef.GetCustomAttributes();
            }

            return fieldDef.GetCustomAttributes();
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > 0)
            {
                if (metadataInfo is MetadataClassInfo classInfo)
                {
                    try
                    {
                        Type = DecodeMemberSignature();
                        var bareType = BaseTypeConverter.StripToBareType(Type);

                        // If we have a nested class we need to add the namespace of the defining class.
                        // This will be the class in which the member is defined, which is classInfo.
                        // Note that Type should also be changed, but we see how far we get without doing that.
                        if (bareType[0] == '.')
                        {
                            if (IsGeneric)
                            {
                                Type = BaseTypeConverter.GetGenericType(Type) + $"`1[{classInfo.FullName}{bareType}]";
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

                        if (DefiningClass!.ContainingAssembly!.ClassesByName.TryGetValue(bareType, out var implementedClass))
                        {
                            ImplementedClass = implementedClass;
                        }
                        else
                        {
                            ImplementedClass = new MetadataClassInfoNotFound(Type);
                            classInfo.ContainingAssembly!.AddClassToAssembly(ImplementedClass);
                        }

                        // We are going to look for Custom Attributes.
                        // For now we are only interested in JsonIgnoreAttribute
                        // and NullableAttribute.
                        var memberAttributes = GetMemberAttributes();
                        foreach (var attributeHandle in memberAttributes)
                        {
                            var attribute = Reader!.GetCustomAttribute(attributeHandle);
                            var customAttribute = new MetadataAttributeInfo(attribute, Reader);
                            if (!string.IsNullOrEmpty(customAttribute.Name))
                            {
                                attributes[customAttribute.Name!] = customAttribute;
                                Logger.LogDebug($"Found attribute {customAttribute.Name} on {classInfo.Name}.{Name}");
                            }
                        }

                        // The NullableAttribute and NullableContextAttribute
                        // are used to find out if a member is nullable.
                        // See https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md
                        // for more information.
                        // It comes down to these attributes having 3 possible values: 0, 1 and 2.
                        // 0: The member is unaware of the nullable concept.
                        //    I assume that this is the #nullable disabled state, the way nullable worked in the good old days.
                        // 1: The member is annotated to be nullable.
                        //    I assume that this is the #nullable enabled state, where there is no ? trailing the type definition.
                        // 2: The member is annotated to be nullable.
                        //    I assume that this is the #nullable enabled state, where there is a ? trailing the type definition.
                        // If the attribute is not set on the member, we check the
                        // NullableCOntextAttribute set on the defining class.
                        // The values for this attribute are the same.
                        // Thr information is made up of the information in the link above
                        // (thanks to StackOverflow) and some old fashioned trial and error.
                        if (attributes.TryGetValue("NullableAttribute", out var na))
                        {
                            IsNullable = (byte)(na.FixedArguments[0].Value!) != 1;
                        }
                        else
                        {
                            var nullableContext = 1;
                            var definingClass = DefiningClass;
                            while (null != definingClass && null == definingClass.NullableContext)
                            {
                                if (definingClass.ContainingAssembly!.ClassesByName.ContainsKey(definingClass.Namespace))
                                {
                                    definingClass = definingClass.ContainingAssembly.ClassesByName[definingClass.Namespace];
                                }
                                else
                                {
                                    definingClass = null;
                                }
                            }

                            if (null != definingClass?.NullableContext)
                            {
                                nullableContext = definingClass.NullableContext.Value;
                            }

                            if ((nullableContext == 0) && (Type != "string"))
                            {
                                IsNullable = !string.IsNullOrEmpty(ImplementedClass.BaseTypeFullName);
                            }
                            else
                            {
                                IsNullable = nullableContext != 1;
                            }
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
        /// A list of attributes that are set on this property.
        /// </summary>
        public IReadOnlyDictionary<string, MetadataAttributeInfo> Attributes => attributes.AsReadOnly();

        /// <summary>
        /// The class where this property is defined.
        /// </summary>
        public MetadataClassInfo? DefiningClass { get; protected set; }

        /// <summary>
        /// The class which this property is implementing.
        /// </summary>
        public MetadataClassInfo? ImplementedClass { get; private set; } = null;

        /// <summary>
        /// The full name of the type, like System.Generic.List`1[SomeType].
        /// </summary>
        public string? Type { get; private set; }

        /// <summary>
        /// Indicates if this is an array or a list.
        /// </summary>
        public bool IsArray => BaseTypeConverter.IsArray(Type);

        /// <summary>
        /// Indicates if this is a generic type.
        /// </summary>
        public bool IsGeneric => BaseTypeConverter.IsGeneric(Type);

        /// <summary>
        /// Indicates if this is a generic property.
        /// </summary>
        public bool IsNullable { get; private set; } = false;

        /// <summary>
        /// Indicates if this property should be serialized or not.
        /// </summary>
        public bool DontSerialize => attributes.ContainsKey("JsonIgnoreAttribute");
    }
}