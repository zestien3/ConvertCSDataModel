using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Z3
{
    /// <summary>
    /// Class to convert C# types to TypeScript types.
    /// </summary>
    internal class HTMLTypeConverter : BaseTypeConverter
    {
        public static readonly List<string> htmlInputTypes = new() { "", "checkbox", "number", "number", "",
                                                                     "number", "number", "number", "number",
                                                                     "number", "number", "number", "number",
                                                                     "text", "", "", "", "",
                                                                     "date", "date", "text", "Enum", "file" };
        public HTMLTypeConverter()
        {
            if (htmlInputTypes.Count != csStandardTypes.Count)
            {
                Logger.LogFatal($"The {nameof(TypeScriptFormatter)}.{nameof(htmlInputTypes)} array does not contain the correct number of entries.");
            }
        }

        /// <summary>
        /// Convert the given C# type to a TypeSCript type.
        /// </summary>
        /// <param name="memberInfo">The C# type.</param>
        /// <returns>The given type converted to TypeScript.</returns>
        public override string ConvertType(MetadataMemberInfo memberInfo)
        {
            // For the HTML code generation, this is only used to
            // set the type attribute of the generated input element.
            if (memberInfo.Attributes.ContainsKey(nameof(EmailAddressAttribute)))
            {
                return "email";
            }
            var csType = StripToBareType(memberInfo.Type!);
            var index = BaseTypeConverter.csStandardTypes.IndexOf(csType);
            if (-1 != index)
            {
                return htmlInputTypes[index];
            }

            return "";
        }

        /// <summary>
        /// Convert the given C# type to a filename for TypeScript.
        /// </summary>
        /// <param name="classInfo">The C# type.</param>
        /// <returns>The given type converted to a filename for TypeScript.</returns>
        public override string GetFileNameForReference(MetadataClassInfo classInfo)
        {
            return string.Empty;
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
                return !string.IsNullOrEmpty(htmlInputTypes[index]);
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
            // At this moment, this method is not used by the HTML language generator. 
            return string.Empty;
        }

        /// <summary>
        /// Return the file name to store the HTML representation of the given MetadataClassInfo.
        /// </summary>
        /// <remarks>
        /// Used by the program to create the output filename.
        /// </remarks>
        /// <param name="classInfo">The MetadataClassInfo instance for which the file name name is required.</param>
        /// <param name="subFolder">The subfolder in which the file should be created.</param>
        /// <returns>The file name to store the TypeScript representation of the given MetadataClassInfo.</returns>
        public static string GetFileNameFromClass(MetadataClassInfo classInfo, string subFolder)
        {
            var nameParts = ToLowerCase(SplitCamelCasing(classInfo.Name!));
            nameParts.Remove("model");
            nameParts.Remove("data");
            nameParts.Remove("component");
            if (nameParts.Remove("view"))
            {
                nameParts.Insert(0, "view");
            }
            if (nameParts.Remove("edit"))
            {
                nameParts.Insert(0, "edit");
            }

            var fileName = $"{JoinWithCharacter(nameParts, '-')}.component.html";
            var result = Path.Combine(subFolder, fileName);
            Logger.LogDebug($"Compiled filename vor {classInfo.Name!}: {result}");
            return result;
        }
    }
}