using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace Z3
{
    internal static class ExtensionMethods
    {
        public static IEnumerable<string> XmlCleanup(this string str)
        {
            var result = new List<string>();
            foreach (string s in str.Split('\r', '\n'))
            {
                var trimmedStr = s.Trim();
                if (!string.IsNullOrEmpty(trimmedStr))
                {
                    result.Add(trimmedStr);
                }
            }

            return result;
        }

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
    }
}