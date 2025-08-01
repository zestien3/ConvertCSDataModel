using System.Collections.Generic;
using System.Text;

namespace Z3
{
    internal abstract class BaseFormatter
    {
        protected static readonly List<string> csStandardTypes = new() { "void", "bool", "sbyte", "byte", "char",
                                                                         "int16", "uint16", "int32", "uint32",
                                                                         "int64", "uint64", "single", "double",
                                                                         "string", "typedReference", "IntPtr", "UIntPtr",
                                                                         "object", "System.DateTime" };

        /// <summary>
        /// Write the class formatted to the required programming language.
        /// </summary>
        /// <param name="classInfo"></param>
        public void FormatClass(MetadataClassInfo classInfo)
        {
            WriteFileHeader(classInfo);
            WriteUsings(classInfo);
            OpenNamespace(classInfo);
            OpenClass(classInfo);
            WriteProperties(classInfo);
            CloseClass(classInfo);
            CloseNamespace(classInfo);
        }

        protected abstract void WriteFileHeader(MetadataClassInfo classInfo);
        protected abstract void WriteUsings(MetadataClassInfo classInfo);
        protected abstract void OpenNamespace(MetadataClassInfo classInfo);
        protected abstract void OpenClass(MetadataClassInfo classInfo);
        protected abstract void WriteProperties(MetadataClassInfo classInfo);
        protected abstract void CloseClass(MetadataClassInfo classInfo);
        protected abstract void CloseNamespace(MetadataClassInfo classInfo);
        protected abstract string FormatType(string type);

        /// <summary>
        /// Checks is a type is a standard type. These are defined in the <see cref="csStandardTypes"/> list.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns>True if it is a standard type, false otherwise.</returns>
        protected static bool IsStandardType(string type)
        {
            return csStandardTypes.Contains(type);
        }


        /// <summary>
        /// Convert the CamelCased string into a list of small strings,
        /// each one starting from an uppercase letter.
        /// The casing itself is not changed.
        /// </summary>
        /// <remarks>
        /// Since the strings are pretty short, we will not use a StringBuilder.
        /// </remarks>
        /// <param name="s">The CamelCased string to split.</param>
        /// <returns>A list of string making up the CamelCased string</returns>
        protected static List<string> SplitCamelCasing(string s)
        {
            var result = new List<string>();
            var stringPart = new string(s[0], 1);
            bool startOfString = true;
            for (int i = 1; i < s.Length; i++)
            {
                startOfString = startOfString && char.IsAsciiLetterUpper(s[i]);
                if (char.IsAsciiLetterUpper(s[i]) && !startOfString)
                {
                    startOfString = true;
                    result.Add(stringPart);
                    stringPart = new string(s[i], 1);
                }
                else
                {
                    stringPart += s[i];
                }
            }

            result.Add(stringPart);
            return result;
        }

        protected static string ToCamelCase(string str)
        {
            return CombineWithCharacter(OneUpperRestLowerCase(SplitCamelCasing(str)), '\0');
        }

        protected static string ToSnakeCase(string str)
        {
            return CombineWithCharacter(ToLowerCase(SplitCamelCasing(str)), '_');
        }

        protected static string ToKebabCase(string str)
        {
            return CombineWithCharacter(ToLowerCase(SplitCamelCasing(str)), '-');
        }

        protected static string ToPascalCase(string str)
        {
            var result = ToCamelCase(str);
            return char.ToLower(result[0]) + result.Substring(1);
        }

        private static List<string> ToLowerCase(List<string> strings)
        {
            var result = new List<string>();
            foreach (var s in strings)
            {
                result.Add(s.ToLower());
            }
            return result;
        }

        private static List<string> ToUpperCase(List<string> strings)
        {
            var result = new List<string>();
            foreach (var s in strings)
            {
                result.Add(s.ToUpper());
            }
            return result;
        }

        private static List<string> OneUpperRestLowerCase(List<string> strings)
        {
            var result = new List<string>();
            foreach (var s in strings)
            {
                result.Add(char.ToUpper(s[0]) + s.Substring(1).ToLower());
            }
            return result;
        }

        private static string CombineWithCharacter(List<string> parts, char filler)
        {
            var result = new StringBuilder();
            var start = true;
            foreach (var s in parts)
            {
                if (!start && filler != '\0')
                {
                    result.Append(filler);
                }

                result.Append(s);
                start = false;
            }

            return result.ToString();
        }
    }
}