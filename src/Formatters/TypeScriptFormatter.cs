using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Zestien3;

namespace Z3
{
    internal class TypeScriptFormatter : BaseFormatter
    {
        private static readonly List<string> tsStandardTypes = new() { "void", "boolean", "number", "number", "number",
                                                                       "number", "number", "number", "number",
                                                                       "number", "number", "number", "number",
                                                                       "string", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                       "<OBJECT>", "Date", "Date" };

        private static readonly List<string> tsStandardTypeValues = new() { "null", "false", "0", "0", "0",
                                                                            "0", "0", "0", "0",
                                                                            "0", "0", "0", "0",
                                                                            "\"\"", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                            "<OBJECT>", "new Date()", "new Date()" };

        /// <summary>
        /// Return the file name to store the TypeScript representation of the given MetadataClassInfo.
        /// </summary>
        /// <remarks>
        /// Used by the program to create the output filename.
        /// </remarks>
        /// <param name="classInfo">The MetadataClassInfo instance for which the file name name is required.</param>
        /// <returns>The file name to store the TypeScript representation of the given MetadataClassInfo.</returns>
        public static string GetFileNameFromClass(MetadataClassInfo classInfo)
        {
            return Path.Combine(classInfo.SubFolder, $"{ToKebabCase(classInfo.Name!)}.ts");
        }

        /// <summary>
        /// Create an instance of the <see cref="TypeScriptFormatter"/> class.
        /// </summary>
        /// <param name="assemblyInfo">The assembly for which we create the output.</param>
        /// <param name="output">The output to which the type script code must be written.</param>
        public TypeScriptFormatter(MetadataAssemblyInfo assemblyInfo, TextWriter output) : base(assemblyInfo, output)
        {
            IndentLength = 2;
        }

        protected override void WriteComment(string str, int indentLevel)
        {
            Output.WriteLine($"// {str}");
        }

        protected override void WriteMultilineComment(IEnumerable<string> str, int indentLevel)
        {
            Output.WriteLine("/*");
            foreach (var s in str)
            {
                Output.WriteLine($" * {s}");
            }
            Output.WriteLine(" */");
        }

        protected override void WriteXmlDocumentation(XmlDocumentation? documentation, int indentLevel)
        {
            if (null != documentation && documentation.HasContent)
            {
                WriteIndent(indentLevel);
                Output.WriteLine("/**");

                foreach (var str in documentation.Summary)
                {
                    WriteIndent(indentLevel);
                    Output.WriteLine($" *  {str}");
                }

                if (documentation.Summary.Any() && documentation.Remarks.Any())
                    Output.WriteLine(" *");

                foreach (var str in documentation.Remarks)
                {
                    WriteIndent(indentLevel);
                    Output.WriteLine($" *  {str}");
                }

                WriteIndent(indentLevel);
                Output.WriteLine(" */");
            }
        }

        protected override void WriteFileHeader(MetadataClassInfo classInfo)
        {
            // TypeScript does not have a file header.
        }

        protected override void WriteUsing(IMemberInfo memberInfo)
        {
            if (!memberInfo.DontSerialize)
            {
                string type = memberInfo.MinimizedType!;
                if (!memberInfo.IsStandardType)
                {
                    string subFolder = "./";
                    if (AssemblyInfo.ClassesByName.TryGetValue(type, out MetadataClassInfo? importedClass))
                    {
                        subFolder = "./" + importedClass.SubFolder;
                    }

                    subFolder = Path.GetRelativePath(memberInfo.OwningClass.SubFolder, subFolder).Replace("\\", "/");

                    var formattedType = FormatType(memberInfo);
                    Output.WriteLine($"import {{ {formattedType} }} from \"{subFolder}/{ToKebabCase(formattedType)}\";");
                }
            }
        }

        protected override void OpenNamespace(MetadataClassInfo classInfo)
        {
            // TypeScript does not support namespaces
        }

        protected override void OpenClass(MetadataClassInfo classInfo)
        {
            Output.WriteLine($"export class {ToCamelCase(classInfo.Name!)} {{");
        }

        protected override void WriteConstructor(MetadataClassInfo classInfo)
        {
            WriteIndent(1);
            Output.WriteLine($"constructor(other?: {ToCamelCase(classInfo.Name!)}) {{");
            WriteIndent(2);
            Output.WriteLine("if (other) {");
            foreach (var property in classInfo.Properties.Values)
            {
                if (!property.DontSerialize)
                {
                    WriteIndent(3);
                    Output.WriteLine($"this.{ToJSONCase(property.Name!)} = other.{ToJSONCase(property.Name!)};");
                }
            }
            WriteIndent(2);
            Output.WriteLine("}");
            WriteIndent(1);
            Output.WriteLine("}");
            Output.WriteLine();
        }

        protected override void WriteProperty(MetadataPropertyInfo propertyInfo)
        {
            WriteIndent(1);
            Output.Write($"public {ToJSONCase(propertyInfo.Name!)}: {FormatType(propertyInfo)}{(propertyInfo.IsArray ? "[]" : "")} = ");
            if (propertyInfo.IsArray)
            {
                Output.WriteLine("[];");
            }
            else
            {
                if (propertyInfo.IsStandardType)
                {
                    Output.WriteLine($"{tsStandardTypeValues[BaseFormatter.csStandardTypes.IndexOf(propertyInfo.Type!)]};");
                }
                else
                {
                    Output.WriteLine($"new {FormatType(propertyInfo)}();");
                }
            }
        }

        protected override void WriteField(MetadataFieldInfo fieldInfo)
        {
            WriteIndent(1);
            Output.Write($"public {ToJSONCase(fieldInfo.Name!)}: {FormatType(fieldInfo)}{(fieldInfo.IsArray ? "[]" : "")} = ");
            if (fieldInfo.Type!.EndsWith("[]"))
            {
                Output.WriteLine("[];");
            }
            {
                if (fieldInfo.IsStandardType)
                {
                    Output.WriteLine($"{tsStandardTypeValues[BaseFormatter.csStandardTypes.IndexOf(fieldInfo.Type!)]};");
                }
                else
                {
                    Output.WriteLine($"new {FormatType(fieldInfo)}();");
                }
            }
        }

        protected override void CloseClass(MetadataClassInfo classInfo)
        {
            Output.WriteLine("}");
        }

        protected override void CloseNamespace(MetadataClassInfo classInfo)
        {
            // TypeScript does not support namespaces
        }

        protected override string FormatType(IMemberInfo memberInfo)
        {
            if (string.IsNullOrEmpty(memberInfo.MinimizedType))
            {
                return string.Empty;
            }

            if (memberInfo.IsStandardType)
            {
                return TypeScriptFormatter.tsStandardTypes[BaseFormatter.csStandardTypes.IndexOf(memberInfo.MinimizedType!)];
            }

            // Remove the namespace
            if (memberInfo.MinimizedType!.Contains('.'))
            {
                return memberInfo.MinimizedType.Substring(memberInfo.MinimizedType.LastIndexOf('.') + 1);
            }

            return memberInfo.MinimizedType;
        }
    }
}