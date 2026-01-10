using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace Z3
{
    internal static class ExtensionMethods
    {
        public static string ToTypeString(this TypeDefinitionHandle handle, MetadataReader reader)
        {
            var typeDef = reader.GetTypeDefinition(handle);
            return $"{reader.GetString(typeDef.Namespace)}.{reader.GetString(typeDef.Name)}";
        }

        public static string ToTypeString(this TypeReferenceHandle handle, MetadataReader reader)
        {
            var typeRef = reader.GetTypeReference(handle);
            return $"{reader.GetString(typeRef.Namespace)}.{reader.GetString(typeRef.Name)}";
        }

        public static string ToTypeString(this TypeSpecificationHandle handle, MetadataReader reader, MetadataInfo genericContext)
        {
            return reader.GetTypeSpecification(handle).DecodeSignature(MetadataSignatureTypeProvider.Instance, genericContext);
        }

        public static string? GetPropertyValue(this System.Reflection.Metadata.CustomAttributeValue<string> customAttributeValue, string propertyName)
        {
            var na = customAttributeValue.NamedArguments.FirstOrDefault(na => na.Name == propertyName);
            return (string?)(na.Value);
        }
    }
}