using System;
using System.Collections.Generic;
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