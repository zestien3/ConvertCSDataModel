using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataFieldInfo : MetadataMemberInfo
    {
        private FieldDefinition fieldDef;

        public MetadataFieldInfo(FieldDefinition fieldDefinition, MetadataClassInfo classInfo, MetadataReader reader, XmlDocumentationFile? xmlDoc) : base(reader, xmlDoc)
        {
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

        protected override string DecodeMemberSignature()
        {
            return fieldDef.DecodeSignature<string, MetadataInfo>(MetadataSignatureTypeProvider.Instance, this);
        }

        protected override CustomAttributeHandleCollection GetMemberAttributes()
        {
            return fieldDef.GetCustomAttributes();
        }
    }
}