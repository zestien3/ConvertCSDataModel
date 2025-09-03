using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Z3
{
    /// <summary>
    /// Base class to convert C# types to another programming language
    /// </summary>
    internal abstract partial class BaseTypeConverter
    {

        [GeneratedRegex(@".*`\d+\[.+\]")]
        private static partial Regex IsGenericTypeRegex();

        [GeneratedRegex(@"(.*)`\d+\[.+\]")]
        private static partial Regex GetGenericTypeRegex();

        [GeneratedRegex(@".*`\d+\[(.+)\]")]
        private static partial Regex StripGenericTypeRegex();


        // A neater way of defining the values for this array would be to use typeof().Name.
        // But that gives us
        /// <summary>
        /// A list containing the standard type names.
        /// </summary>
        /// <remarks>
        /// First of all a standard type is coupled to a programming language.
        /// Here we see that int64 is a standard type. In TypeScript it has to be represented by a number.
        /// So we are looking for C# types that can be represented by a standard type in another programming
        /// language. System.DataOnly for example is not a standfard type in C#, but Date is in TypeScript.
        /// So when implementing more languages to convert to, this array might need to be expanded.
        /// A neater way to get the names would be to use typeof(void).Name, but that gives us the string "Void".
        /// The reflection metadata framework however returns "void", so it is not compatible anyway.
        /// </remarks>
        public static readonly List<string> csStandardTypes = new() { "void", "bool", "sbyte", "byte", "char",
                                                                      "int16", "uint16", "int32", "uint32",
                                                                      "int64", "uint64", "single", "double",
                                                                      "string", "typedReference", "IntPtr", "UIntPtr", "object",
                                                                      "System.DateTime", "System.DateOnly", "System.Guid", "System.Enum" };

        /// <summary>
        /// Convert the given C# type to a type for the language for which this converter is designed.
        /// </summary>
        /// <param name="csType">The C# type in string format.</param>
        /// <returns>The given type converted to the language this converter is designed for.</returns>
        public abstract string ConvertType(string csType);

        /// <summary>
        /// Convert the given C# type to a filename for the language for which this converter is designed.
        /// </summary>
        /// <param name="csType">The C# type in string format.</param>
        /// <returns>The given type converted to a filename for the language this converter is designed for.</returns>
        public abstract string GetFileName(string csType);

        /// <summary>
        /// Returns true if the given C# type is a standard type for the language to convert to.
        /// </summary>
        /// <param name="csType">The C# type in string format.</param>
        /// <returns>True if the given C# type is a standard type for the language to convert to, false otherwise.</returns>
        public abstract bool IsStandardType(string csType);

        /// <summary>
        /// Returns true if the given C# type is a generic type.
        /// </summary>
        /// <param name="csType">The C# type in string format.</param>
        /// <returns>True if the given C# type is a generic type, false otherwise.</returns>
        public static bool IsGeneric(string csType)
        {
            Regex regex = IsGenericTypeRegex();
            var result = regex.IsMatch(csType);
            return result;
        }

        /// <summary>
        /// Returns the generic C# type of this generic type.
        /// </summary>
        /// <param name="csType">The C# type in string format.</param>
        /// <returns>The generic C# type is of this generic type.</returns>
        public static string GetGenericType(string csType)
        {
            var regex = GetGenericTypeRegex();
            if (regex.IsMatch(csType))
            {
                var result = regex.Matches(csType)[0];
                return result.Groups[1].Value;
            }

            return csType;
        }

        /// <summary>
        /// Returns the C# parameter type of this generic type.
        /// </summary>
        /// <param name="csType">The complete C# type in string format.</param>
        /// <returns>The C# parameter type of this generic type.</returns>
        public static string StripToBareType(string csType)
        {
            var result = csType;
            Regex regex = StripGenericTypeRegex();
            if (regex.IsMatch(csType))
            {
                result = regex.Matches(csType)[0].Groups[1].Value;
            }

            // The type can be int?[] and int[]?
            if (result.EndsWith('?'))
            {
                result = result[..^1];
            }

            if (result.EndsWith("[]"))
            {
                result = result[..^2];
            }

            if (result.EndsWith('?'))
            {
                result = result[..^1];
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given C# type is an array.
        /// </summary>
        /// <param name="csType">The C# type in string format.</param>
        /// <returns>True if the given C# type is an array, false otherwise.</returns>
        public static bool IsArray(string csType)
        {
            return csType.EndsWith("[]");
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
            bool startOfStringPart = true;
            for (int i = 1; i < s.Length; i++)
            {
                startOfStringPart = startOfStringPart && char.IsAsciiLetterUpper(s[i]);
                if (char.IsAsciiLetterUpper(s[i]) && !startOfStringPart)
                {
                    startOfStringPart = true;
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

        /// <summary>
        /// Convert the given string to match the JsonSerializerDefaults.Web behavior.
        /// See <see cref="TypeScriptTypeConverter.DemoSerialization"/> for an explanation about this format.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The CamelCase representation of the given string.</returns>
        public static string ToJSONCase(string str)
        {
            // If the name does not start with an uppercase character it is not changed.
            if (char.IsLower(str[0]))
            {
                return str;
            }

            // If the name consists of all uppercase characters, they are all converted to lowercase.
            if (str.Equals(str.ToUpper()))
            {
                return str.ToLower();
            }

            // If the name starts with an uppercase character, that character is converted to lowercase.
            string result = str[0..1].ToLower();
            str = str[1..];

            // If the name starts with multiple uppercase characters, they are all but the last converted to lowercase.
            while (char.IsUpper(str[0]) && char.IsUpper(str[1]))
            {
                result += char.ToLower(str[0]);
                str = str[1..];
            }

            return result + str;
        }

        /// <summary>
        /// Convert the given string to CamelCasing (WhichLooksLikeThis).
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The CamelCase representation of the given string.</returns>
        public static string ToCamelCase(string str)
        {
            return JoinWithCharacter(OneUpperRestLowerCase(SplitCamelCasing(str)), '\0');
        }

        /// <summary>
        /// Convert the given string to SnakeCasing (which_looks_like_this).
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The SnakeCase representation of the given string.</returns>
        public static string ToSnakeCase(string str)
        {
            return JoinWithCharacter(ToLowerCase(SplitCamelCasing(str)), '_');
        }

        /// <summary>
        /// Convert the given string to KebabCasing (which-looks-like-this).
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The KebabCase representation of the given string.</returns>
        public static string ToKebabCase(string str)
        {
            return JoinWithCharacter(ToLowerCase(SplitCamelCasing(str)), '-');
        }

        /// <summary>
        /// Convert the given string to PascalCasing (whichLooksLikeThis).
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The PascalCase representation of the given string.</returns>
        public static string ToPascalCase(string str)
        {
            var result = ToCamelCase(str);
            return char.ToLower(result[0]) + result[1..];
        }

        /// <summary>
        /// Convert each string in the list to lowercase.
        /// </summary>
        /// <param name="strings">The list of strings to convert.</param>
        /// <returns>A list of the same strings as in the given list where every string is in lowercase.</returns>
        private static List<string> ToLowerCase(List<string> strings)
        {
            var result = new List<string>();
            foreach (var s in strings)
            {
                result.Add(s.ToLower());
            }
            return result;
        }

        /// <summary>
        /// Convert each string in the list to uppercase.
        /// </summary>
        /// <param name="strings">The list of strings to convert.</param>
        /// <returns>A list of the same strings as in the given list where every string is in uppercase.</returns>
        private static List<string> ToUpperCase(List<string> strings)
        {
            var result = new List<string>();
            foreach (var s in strings)
            {
                result.Add(s.ToUpper());
            }
            return result;
        }

        /// <summary>
        /// Convert each string in the list to lowercase except for the first letter which is in uppercase.
        /// </summary>
        /// <param name="strings">The list of strings to convert.</param>
        /// <returns>A list of the same strings as in the given list where every string is in lowercase except for its first letter which is in uppercase.</returns>
        private static List<string> OneUpperRestLowerCase(List<string> strings)
        {
            var result = new List<string>();
            foreach (var s in strings)
            {
                result.Add(char.ToUpper(s[0]) + s[1..].ToLower());
            }
            return result;
        }

        /// <summary>
        /// Join all the strings in the given list and add the <paramref name="filler"/> character in between the string.
        /// </summary>
        /// <remarks>
        /// If the filler character is '\0', no filler is used. 
        /// </remarks>
        /// <param name="parts">The list of strings to join.</param>
        /// <param name="filler">The filler character to add in between the string parts.</param>
        /// <returns>A joined string of all the string parts with filler characters in between the parts.</returns>
        private static string JoinWithCharacter(List<string> parts, char filler)
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