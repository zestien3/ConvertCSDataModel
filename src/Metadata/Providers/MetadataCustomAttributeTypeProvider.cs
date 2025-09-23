// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TODO: This implementation of the ICustomAttributeTypeProvider interface
//       returns a string. I assume that it can also be used to return a 
//       MetadataAttributeInfo instance as well, which would make the code
//       using it much easier to read.

using System;
using System.ComponentModel.DataAnnotations;
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

        /// <summary>
        /// Prevent to create a new instance of this class,
        /// as we want to implement the singleton pattern.
        /// </summary>
        private MetadataCustomAttributeTypeProvider() { }

        /// <summary>
        /// Gets the TType representation for System.Type.
        /// </summary>
        /// <remarks>
        /// In our case TType is a string, as this class implements
        /// the ICustomAttributeTypeProvider&lt;string&gt; interface. 
        /// </remarks>
        public string GetSystemType()
        {
            return "System.Type";
        }

        /// <summary>
        /// Gets the type symbol for a single-dimensional array of
        /// the given element type with a lower bounds of zero.
        /// </summary>
        /// <remarks>
        /// In our case TType is a string, as this class implements
        /// the ICustomAttributeTypeProvider&lt;string&gt; interface. 
        /// </remarks>
        public string GetSZArrayType(string elementType) { return $"{elementType}[]"; }

        // TODO: To make this class return a MetadataAttributeInfo instance,
        //       we probably need to change the next 3 methods.
        //       Instead of a string, they can return something more useful. 
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => handle.ToTypeString(reader);


        /// <summary>
        /// Gets the type symbol for the given serialized type name.
        /// </summary>
        /// <param name="name">
        /// The serialized type name in so-called "reflection notation" format
        /// (as understood by the System.Type.GetType(System.String) method.)
        /// </param>
        /// <returns>
        /// In our case a string, as this class implements the
        /// ICustomAttributeTypeProvider&lt;string&gt; interface.
        /// But we maybe could return a TypeSCript representation
        /// of the class.
        /// </returns>
        public string GetTypeFromSerializedName(string name) { return name; }

        /// <summary>
        /// Gets the underlying type of the given enum type symbol.
        /// </summary>
        /// <param name="type">An enum type.</param>
        /// <returns>
        /// A type code that indicates the underlying type of the enumeration.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public PrimitiveTypeCode GetUnderlyingEnumType(string type)
        {
            // TODO: There are a handful of enums that we can't find.
            //       Just urn the code and check them. Might not be
            //       to hard to return the correct code for them.

            // Type.GetType can find types in the current executing assembly and
            // mscorlib.dll/System.Private.CoreLib.dll.
            var t = Type.GetType(type, false);

            // There are certain attributes we need. Since we now which one,
            // we can hard code their type here. This is a bit of a hack.
            // TODO: Check how we can get the underlying type of the Enum.
            if (null == t)
            {
                switch (type)
                {
                    case "System.ComponentModel.DataAnnotations.DataType":
                        return PrimitiveTypeCode.Int32;
                }
            }

            if ((null != t) && (null != t.BaseType))
                {
                    if (t.BaseType.FullName == typeof(SByte).FullName)
                        return PrimitiveTypeCode.SByte;

                    if (t.BaseType.FullName == typeof(Int16).FullName)
                        return PrimitiveTypeCode.Int16;

                    if (t.BaseType.FullName == typeof(Int32).FullName)
                        return PrimitiveTypeCode.Int32;

                    if (t.BaseType.FullName == typeof(Int64).FullName)
                        return PrimitiveTypeCode.Int64;

                    if (t.BaseType.FullName == typeof(Byte).FullName)
                        return PrimitiveTypeCode.Byte;

                    if (t.BaseType.FullName == typeof(UInt16).FullName)
                        return PrimitiveTypeCode.UInt16;

                    if (t.BaseType.FullName == typeof(UInt32).FullName)
                        return PrimitiveTypeCode.UInt32;

                    if (t.BaseType.FullName == typeof(UInt64).FullName)
                        return PrimitiveTypeCode.UInt64;
                }

            MetadataClassInfo? runtimeType = TypeResolver(type.Replace('/', '+'));

            // Default underlying value for Enum is int, so we try that if we  can't
            // find the type. Will work in most cases. The alternative is to throw an
            // exception, which will probably happen anyway if we get it wrong here.
            if ((null == runtimeType) || (null == runtimeType!.BaseType))
                return PrimitiveTypeCode.Int32;

            if (runtimeType.BaseType.FullName == typeof(SByte).FullName)
                return PrimitiveTypeCode.SByte;

            if (runtimeType.BaseType.FullName == typeof(Int16).FullName)
                return PrimitiveTypeCode.Int16;

            if (runtimeType.BaseType.FullName == typeof(Int32).FullName)
                return PrimitiveTypeCode.Int32;

            if (runtimeType.BaseType.FullName == typeof(Int64).FullName)
                return PrimitiveTypeCode.Int64;

            if (runtimeType.BaseType.FullName == typeof(Byte).FullName)
                return PrimitiveTypeCode.Byte;

            if (runtimeType.BaseType.FullName == typeof(UInt16).FullName)
                return PrimitiveTypeCode.UInt16;

            if (runtimeType.BaseType.FullName == typeof(UInt32).FullName)
                return PrimitiveTypeCode.UInt32;

            if (runtimeType.BaseType.FullName == typeof(UInt64).FullName)
                return PrimitiveTypeCode.UInt64;

            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Verifies if the given type represents System.Type.
        /// </summary>
        /// <remarks>
        /// The parameter is a string because we implement the
        /// ICustomAttributeTypeProvider&lt;string&gt; interface.
        /// </remarks>
        /// <param name="type">The type to verify.</param>
        /// <returns>true if the given type is a System.Type, false otherwise.</returns>
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

            var check = assemblyName.Name!.Contains('.') ? assemblyName.Name![..assemblyName.Name!.IndexOf('.')] : assemblyName.Name!;

            if ((null != foundName) && (foundName[..check.Length] == check))
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
                assemblyName = assemblyName[..assemblyName.LastIndexOf('.')];
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

                Logger.LogDebug($"Could not find {typeName} in assembly {assemblyName}");
                return null;
            }
            else
            {
                Logger.LogDebug($"Could not find assembly containing {typeName}");
                return null;
            }
        }
    }
}