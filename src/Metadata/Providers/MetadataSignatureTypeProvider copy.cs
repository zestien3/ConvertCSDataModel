// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
 
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
 
namespace Z3
{
    /// <summary>
    /// This Type Provider converts types to strings.
    /// The idea is to create a provider which will generate
    /// TypeScript code to convert .NET classes to TS types.
    /// </summary>
    /// <remarks>
    /// This code is based on some code found on the internet
    /// (hence the license comment at the top of the file).
    /// The original type was called EcmaSignatureTypeProviderForToString.
    /// </remarks>
    internal sealed class MetadataSignatureTypeProvider : ISignatureTypeProvider<string, MetadataInfo>
    {
        public static readonly MetadataSignatureTypeProvider Instance = new MetadataSignatureTypeProvider();

        private MetadataSignatureTypeProvider() { }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);
        public string GetTypeFromSpecification(MetadataReader reader, MetadataInfo genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => handle.ToTypeString(reader, genericContext);

        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[...]";  //Helpers.ComputeArraySuffix(shape.Rank, multiDim: true);
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetPointerType(string elementType) => elementType + "*";

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(genericType);
            sb.Append('[');
            for (int i = 0; i < typeArguments.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(',');
                }

                sb.Append(typeArguments[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        public string GetGenericTypeParameter(MetadataInfo genericContext, int index) => "Generic Type Parameters are not yet supported.";
        public string GetGenericMethodParameter(MetadataInfo genericContext, int index) => "Generic Method Parameters are not yet supported.";

        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(signature.ReturnType);
            sb.Append('(');
            for (int i = 0; i < signature.ParameterTypes.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(signature.ParameterTypes[i]);
            }
            sb.Append(')');
            return sb.ToString();
        }
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetPinnedType(string elementType) => elementType;

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Void:
                    return "void";
                case PrimitiveTypeCode.Boolean:
                    return "bool";
                case PrimitiveTypeCode.Char:
                    return "char";
                case PrimitiveTypeCode.SByte:
                    return "sbyte";
                case PrimitiveTypeCode.Byte:
                    return "byte";
                case PrimitiveTypeCode.Int16:
                    return "int16";
                case PrimitiveTypeCode.UInt16:
                    return "uint16";
                case PrimitiveTypeCode.Int32:
                    return "int32";
                case PrimitiveTypeCode.UInt32:
                    return "uint32";
                case PrimitiveTypeCode.Int64:
                    return "int64";
                case PrimitiveTypeCode.UInt64:
                    return "uint64";
                case PrimitiveTypeCode.Single:
                    return "single";
                case PrimitiveTypeCode.Double:
                    return "double";
                case PrimitiveTypeCode.String:
                    return "string";
                case PrimitiveTypeCode.TypedReference:
                    return "typesdReference";
                case PrimitiveTypeCode.IntPtr:
                    return "IntPtr";
                case PrimitiveTypeCode.UIntPtr:
                    return "UIntPtr";
                case PrimitiveTypeCode.Object:
                    return "object";
            }

            // Fallback value
            return typeCode.ToString();
        }
    }
}