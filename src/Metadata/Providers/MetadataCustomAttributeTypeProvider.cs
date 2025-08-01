// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
 
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
    internal sealed class MetadataCustomAttributeTypeProvider : ICustomAttributeTypeProvider<string>
    {
        private class StringDiff : IComparable
        {
            private int value = 0;

            public StringDiff(string found, string search)
            {
                Value = found;
                for (int i = 0; i < search.Length && i < found.Length; i++)
                {
                    if (search[i] != found[i])
                    {
                        value = i - 1;
                        return;
                    }
                }
            }

            public int CompareTo(object? obj)
            {
                if (obj is StringDiff other)
                {
                    return value.CompareTo(other.value);
                }

                throw new ArgumentException($"Parameter should be of type {nameof(StringDiff)}");
            }

            public string Value { get; private set; }
        }

        public static readonly MetadataCustomAttributeTypeProvider Instance = new MetadataCustomAttributeTypeProvider();

        private MetadataCustomAttributeTypeProvider() { }

        public string GetSystemType()
        {
            return "System.Type";
        }

        public string GetSZArrayType(string elementType)
        {
            return $"{elementType}[]";
        }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);

        public string GetTypeFromSerializedName(string name)
        {
            throw new NotImplementedException();
        }

        public PrimitiveTypeCode GetUnderlyingEnumType(string type)
        {
            MetadataClassInfo? runtimeType = TypeResolver(type.Replace('/', '+'));

            if (runtimeType?.BaseType?.FullName == typeof(SByte).FullName)
                return PrimitiveTypeCode.SByte;

            if (runtimeType?.BaseType?.FullName == typeof(Int16).FullName)
                return PrimitiveTypeCode.Int16;

            if (runtimeType?.BaseType?.FullName == typeof(Int32).FullName)
                return PrimitiveTypeCode.Int32;

            if (runtimeType?.BaseType?.FullName == typeof(Int64).FullName)
                return PrimitiveTypeCode.Int64;

            if (runtimeType?.BaseType?.FullName == typeof(Byte).FullName)
                return PrimitiveTypeCode.Byte;

            if (runtimeType?.BaseType?.FullName == typeof(UInt16).FullName)
                return PrimitiveTypeCode.UInt16;

            if (runtimeType?.BaseType?.FullName == typeof(UInt32).FullName)
                return PrimitiveTypeCode.UInt32;

            if (runtimeType?.BaseType?.FullName == typeof(UInt64).FullName)
                return PrimitiveTypeCode.UInt64;

            throw new ArgumentOutOfRangeException();
        }

        public bool IsSystemType(string type)
        {
            return type == "System.Type";
        }

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
                    return "typedReference";
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

        private static MetadataAssemblyInfo? AssemblyResolver(AssemblyName assemblyName)
        {
            var folder = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());
            var foundName = folder.GetFiles("*.dll").Max(f => new StringDiff(f.Name, assemblyName.Name!))?.Value;

            if (null != foundName)
            {
                var fullName = Path.Combine(folder.FullName, foundName!);

                if (File.Exists(fullName))
                {
                    return MetadataAssemblyInfo.Factory(fullName);
                }
            }

            return null;
        }

        private static MetadataClassInfo? TypeResolver(string typeName)
        {
            var assemblyName = typeName;
            MetadataAssemblyInfo? assembly = null;

            while ((null == assembly) && assemblyName.Contains('.'))
            {
                assemblyName = assemblyName.Substring(0, typeName.LastIndexOf('.') - 1);
                assembly = AssemblyResolver(new AssemblyName(assemblyName));
            }

            if (null != assembly)
            {
                foreach (var type in assembly.ClassesByName.Values)
                {
                    if (type.FullName == typeName)
                    {
                        // For this class we load a little bit more info
                        // than for the rest of the classes in the assembly.
                        type.AllClassesLoaded(assembly, 1);
                        return type;
                    }
                }

                Console.Error.WriteLine($"Could not find {typeName} in assembly {assemblyName}");
                return null;
            }
            else
            {
                Console.Error.WriteLine($"Could not find assembly containing {typeName}");
                return null;
            }
        }
    }
}