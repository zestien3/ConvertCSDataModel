using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Z3
{
    internal class TypeScriptFormatter : BaseFormatter
    {
        private static readonly List<string> tsStandardTypes = new() { "void", "boolean", "number", "number", "number",
                                                                       "number", "number", "number", "number",
                                                                       "number", "number", "number", "number",
                                                                       "string", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                       "<OBJECT>", "Date" };

        private static readonly List<string> tsStandardTypeValues = new() { "null", "false", "0", "0", "0",
                                                                            "0", "0", "0", "0",
                                                                            "0", "0", "0", "0",
                                                                            "\"\"", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                            "<OBJECT>", "new Date()" };

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
            if (classInfo.Attributes.ContainsKey(nameof(Zestien3.UseInFrontendAttribute)) &&
                classInfo.Attributes[nameof(Zestien3.UseInFrontendAttribute)].ContainsKey(nameof(Zestien3.UseInFrontendAttribute.SubFolder)))
            {
                var subFolder = classInfo.Attributes[nameof(Zestien3.UseInFrontendAttribute)][nameof(Zestien3.UseInFrontendAttribute.SubFolder)];
                return Path.Combine(subFolder, $"{BaseFormatter.ToKebabCase(classInfo.Name!)}.ts");
            }
            return $"{BaseFormatter.ToKebabCase(classInfo.Name!)}.ts";
        }

        /// <summary>
        /// Create an instance of the <see cref="TypeScriptFormatter"/> class.
        /// </summary>
        /// <param name="output">The output to which the type script code must be written.</param>
        public TypeScriptFormatter(TextWriter output) : base(output)
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

        protected override void WriteUsing(MetadataPropertyInfo propertyInfo)
        {
            if (!propertyInfo.DontSerialize)
            {
                string type = propertyInfo.Type!;
                if (!IsStandardType(type))
                {
                    var formattedType = FormatType(type).Replace("[]", "");
                    Output.WriteLine($"import {{ {formattedType} }} from \"./{ToKebabCase(formattedType)}.model.ts\";");
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

        protected override void WriteProperty(MetadataPropertyInfo propertyInfo)
        {
            if (!propertyInfo.DontSerialize)
            {
                WriteIndent(1);
                Output.Write($"public {ToPascalCase(propertyInfo.Name!)}: {FormatType(propertyInfo.Type!)} = ");
                if (propertyInfo.Type!.EndsWith("]"))
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
                        Output.WriteLine($"new {FormatType(propertyInfo.Type!)}();");
                    }
                }
            }
        }

        protected override void WriteField(MetadataFieldInfo fieldInfo)
        {
            if (!fieldInfo.DontSerialize)
            {
                WriteIndent(1);
                Output.Write($"public {ToPascalCase(fieldInfo.Name!)}: {FormatType(fieldInfo.Type!)} ");
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
                        Output.WriteLine($"new {FormatType(fieldInfo.Type!)}();");
                    }
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

        protected override string FormatType(string type)
        {
            if (IsStandardType(type))
            {
                return TypeScriptFormatter.tsStandardTypes[BaseFormatter.csStandardTypes.IndexOf(type)];
            }

            if (type.StartsWith("System.Collections.Generic.List`1["))
            {
                type = type.Substring("System.Collections.Generic.List`1[".Length);
                type = type.Substring(0, type.Length - 1);
                if (IsStandardType(type))
                {
                    type = TypeScriptFormatter.tsStandardTypes[BaseFormatter.csStandardTypes.IndexOf(type)];
                }

                type += "[]";
            }

            // Remove the namespace
            if (type.Contains('.'))
            {
                type = type.Substring(type.LastIndexOf('.') + 1);
            }

            return type;
        }
    }
}