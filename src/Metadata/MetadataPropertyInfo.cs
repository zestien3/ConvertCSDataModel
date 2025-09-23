using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataPropertyInfo : MetadataMemberInfo
    {
        private PropertyDefinition propertyDef;

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

        protected override string DecodeMemberSignature()
        {
            return propertyDef.DecodeSignature<string, MetadataInfo>(MetadataSignatureTypeProvider.Instance, this).ReturnType;
        }

        protected override CustomAttributeHandleCollection GetMemberAttributes()
        {
            return propertyDef.GetCustomAttributes();
        }
    }
}