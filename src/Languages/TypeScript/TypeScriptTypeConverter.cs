using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                                                                      "Enum", "File", "string" };

        public static readonly List<string> tsStandardTypeValues = new() { "null", "false", "0", "0", "0",
                                                                           "0", "0", "0", "0",
                                                                           "0", "0", "0", "0",
                                                                           "\"\"", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                           "undefined", "new Date()", "new Date()", "\"00000000-0000-0000-0000-000000000000\"",
                                                                           "0", "new File()", "\"black\"" };

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
        /// Convert the given C# type to a TypeScript type.
        /// </summary>
        /// <param name="memberInfo">The C# type.</param>
        /// <returns>The given type converted to TypeScript.</returns>
        public override string ConvertType(MetadataMemberInfo memberInfo)
        {
            var isArray = memberInfo.IsArray || memberInfo.IsGeneric;
            var result = memberInfo.ImplementedClass!.Name!;

            result = StripToBareType(result);
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
        public override string GetFileNameForReference(MetadataClassInfo classInfo)
        {
            var csType = StripToMinimalType(classInfo.Name!);
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
        /// Returns a string containing the default value of the given member.
        /// </summary>
        /// <remarks>
        /// Something like " = 0", " = []" or " = new MyClass()".
        /// It can be used in definitions of members or in defining constructors.
        /// </remarks>
        /// <param name="member">The member for which the default value is requested.</param>
        /// <returns>A string containing the default value of the member, preceded by " = ".</returns>
        public override string GetDefaultMemberValue(MetadataMemberInfo member)
        {
            var result = new StringBuilder();
            var type = null == member.ImplementedClass ? member.Type! : ConvertType(member);

            result.Append(" = ");
            if (member.IsNullable)
            {
                result.Append("null");
            }
            else
            {
                if (type.EndsWith("[]"))
                {
                    result.Append("[]");
                }
                else
                {
                    if (IsStandardType(member.Type!))
                    {
                        result.Append($"{TypeScriptTypeConverter.tsStandardTypeValues[BaseTypeConverter.csStandardTypes.IndexOf(member.Type!)]}");
                    }
                    else
                    {
                        if ((null != member.ImplementedClass) && member.ImplementedClass.IsEnum)
                        {
                            result.Append($"{member.ImplementedClass.Name}.{member.ImplementedClass.Members.First().Name}");
                        }
                        else
                        {
                            result.Append($"new {type}()");
                        }
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Get a code snippet representing the check for a specific type.
        /// </summary>
        /// <param name="member">The member for which the check must be constructed.</param>
        /// <returns>A code snippet to check the real type of the given member.</returns>
        public string GetUnionTypeCheck(MetadataMemberInfo member)
        {
            var result = string.Empty;
            var tsType = this.ConvertType(member);

            if (IsArray(tsType))
            {
                result = $"{BaseTypeConverter.ToJSONCase(member.Name!)} instanceof Array && ";
                tsType = tsType[..^2];

                if (tsStandardTypes.Contains(tsType) && (tsType != "Date") && (tsType != "File"))
                {
                    return result + $"typeof {BaseTypeConverter.ToJSONCase(member.Name!)}[0] === \"{tsType}\"";
                }

                return result + $"{BaseTypeConverter.ToJSONCase(member.Name!)}[0] instanceof {tsType}";
            }

            if (tsStandardTypes.Contains(tsType) && (tsType != "Date") && (tsType != "File"))
            {
                return $"typeof {BaseTypeConverter.ToJSONCase(member.Name!)} === \"{tsType}\"";
            }

            return $"{BaseTypeConverter.ToJSONCase(member.Name!)} instanceof {tsType}";
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

        /// <summary>
        /// Return the file name to store the TypeScript representation of the given MetadataClassInfo.
        /// </summary>
        /// <remarks>
        /// Used by the program to create the output filename.
        /// </remarks>
        /// <param name="classInfo">The MetadataClassInfo instance for which the file name name is required.</param>
        /// <param name="subFolder">The subfolder in which the file should be created.</param>
        /// <returns>The file name to store the TypeScript representation of the given MetadataClassInfo.</returns>
        public static string GetFileNameFromClass(MetadataClassInfo classInfo, string subFolder)
        {
            var bareTypeName = BaseTypeConverter.StripToBareType(classInfo.Name!);
            var result = Path.Combine(subFolder, $"{BaseTypeConverter.ToKebabCase(bareTypeName)}.ts");
            Logger.LogDebug($"Compiled filename vor {classInfo.Name!}: {result}");
            return result;
        }
    }
}