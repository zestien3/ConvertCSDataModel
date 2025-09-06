using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Z3
{
    /// <summary>
    /// Class to convert C# types to TypeScript types.
    /// </summary>
    internal class TypeScriptTypeConverter : BaseTypeConverter
    {
        public static readonly List<string> tsStandardTypes = new() { "void", "boolean", "number", "number", "number",
                                                                      "number", "number", "number", "number",
                                                                      "number", "number", "number", "number",
                                                                      "string", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                      "unknown", "Date", "Date", "string",
                                                                      "Enum" };

        public static readonly List<string> tsStandardTypeValues = new() { "null", "false", "0", "0", "0",
                                                                           "0", "0", "0", "0",
                                                                           "0", "0", "0", "0",
                                                                           "\"\"", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                           "undefined", "new Date()", "new Date()", "\"00000000-0000-0000-0000-000000000000\"",
                                                                           "0" };

        private static readonly List<string> tsGenericArrayTypes = new() { BaseTypeConverter.GetGenericType(typeof(List<int>).FullName!),
                                                                           BaseTypeConverter.GetGenericType(typeof(IReadOnlyList<int>).FullName!),
                                                                           BaseTypeConverter.GetGenericType(typeof(IList<int>).FullName!) };

        public TypeScriptTypeConverter()
        {
            if (tsStandardTypes.Count != csStandardTypes.Count)
            {
                Logger.LogFatal($"The {nameof(TypeScriptFormatter)}.{nameof(tsStandardTypes)} array does not contain the correct number of entries.");
            }

            if (tsStandardTypeValues.Count != csStandardTypes.Count)
            {
                Logger.LogFatal($"The {nameof(TypeScriptFormatter)}.{nameof(tsStandardTypeValues)} array does not contain the correct number of entries.");
            }
        }

        /// <summary>
        /// Convert the given C# type to a TypeSCript type.
        /// </summary>
        /// <param name="classInfo">The C# type.</param>
        /// <returns>The given type converted to TypeScript.</returns>
        public override string ConvertType(MetadataClassInfo classInfo)
        {
            var result = classInfo.Name!;

            var isArray = classInfo.IsArray || classInfo.IsGeneric;

            if (classInfo.IsArray)
            {
                result = result[..^2];
            }

            if (classInfo.IsGeneric)
            {
                result = StripToBareType(result);
            }

            if (IsStandardType(result))
            {
                result = tsStandardTypes[BaseTypeConverter.csStandardTypes.IndexOf(result)];
            }
            else
            {
                result = result[(result.LastIndexOf('.') + 1)..];
            }

            if (isArray)
            {
                result += "[]";
            }

            return result;
        }

        /// <summary>
        /// Convert the given C# type to a filename for TypeScript.
        /// </summary>
        /// <param name="classInfo">The C# type.</param>
        /// <returns>The given type converted to a filename for TypeScript.</returns>
        public override string GetFileName(MetadataClassInfo classInfo)
        {
            var csType = StripToBareType(ConvertType(classInfo));
            return ToKebabCase(csType);
        }

        /// <summary>
        /// Returns true if the given C# type is a standard type in TypeSCript.
        /// </summary>
        /// <param name="csType">The C# type in string format.</param>
        /// <returns>True if the given C# type is a standard type in TypeScript, false otherwise.</returns>
        public override bool IsStandardType(string csType)
        {
            csType = StripToBareType(csType);
            var index = BaseTypeConverter.csStandardTypes.IndexOf(csType);
            if (-1 != index)
            {
                return !string.IsNullOrEmpty(tsStandardTypes[index]);
            }

            return false;
        }

        /// <summary>
        /// Demonstrate how .NET itself would convert property names when serializing.
        /// </summary>
        /// <remarks>
        /// This method will write some name conversion results to the console.
        /// Examples are ALLUPPERCASE, CamelCase, pascalCase, PARTIALUpperCase, PartialUPPERCASEIntheMiddle, PartialUpperCaseAtEND.
        /// </remarks>
        public static void DemoSerialization()
        {
            void ConvertTypeToJson(object o)
            {
                // These seem to be the serializer options used in a .net core web application.
                // From what we see here, the name of a variable is formatted with the following rules in that order:
                // If the name does not start with an uppercase character it is not changed.
                // If the name consists of all uppercase characters, they are all converted to lowercase.
                // If the name starts with an uppercase character, that character is converted to lowercase.
                // If the name starts with multiple uppercase characters, they are all but the last converted to lowercase.
                JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
                Console.Out.WriteLine(JsonSerializer.Serialize(o, options));
            }

            ConvertTypeToJson(new
            {
                CamelCase = 0,
                pascalCase = 0,
                snake_case = 0,
                ALLUPPERCASE = 0,
                MULTIPLEUpperCaseAtBegin = 0,
                MultipleUPPERCASEIntheMiddle = 0,
                MultipleUpperCaseAtEND = 0,
                sOMEUpperCaseAfterTheFirstCharacter = 0
            });
        }
    }
}