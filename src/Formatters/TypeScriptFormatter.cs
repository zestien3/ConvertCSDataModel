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
                                                                       "unknown", "Date", "Date", "string",
                                                                       "Enum" };

        private static readonly List<string> tsStandardTypeValues = new() { "null", "false", "0", "0", "0",
                                                                            "0", "0", "0", "0",
                                                                            "0", "0", "0", "0",
                                                                            "\"\"", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                            "undefined", "new Date()", "new Date()", "\"00000000-0000-0000-0000-000000000000\"",
                                                                            "0" };

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
            if (tsStandardTypes.Count != csStandardTypes.Count)
            {
                Logger.LogFatal($"The {nameof(TypeScriptFormatter)}.{nameof(tsStandardTypes)} array does not contain the correct number of entries.");
            }

            if (tsStandardTypeValues.Count != csStandardTypes.Count)
            {
                Logger.LogFatal($"The {nameof(TypeScriptFormatter)}.{nameof(tsStandardTypeValues)} array does not contain the correct number of entries.");
            }

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

        protected override void WriteFileReference(string className, string currentSubFolder)
        {
            if (AssemblyInfo.ClassesByName.TryGetValue(className, out var classInfo))
            {
                var subFolder = ".\\" + classInfo.SubFolder;

                subFolder = Path.GetRelativePath(currentSubFolder, subFolder).Replace("\\", "/");

                string shortTypeName = className.Substring(className.LastIndexOf('.') + 1);
                Output.WriteLine($"import {{ {shortTypeName} }} from \"{subFolder}/{ToKebabCase(shortTypeName)}\";");
            }
        }

        protected override void OpenNamespace(MetadataClassInfo classInfo)
        {
            // TypeScript does not support namespaces
        }

        protected override void OpenClass(MetadataClassInfo classInfo)
        {
            if (classInfo.IsEnum)
            {
                Output.Write($"export enum {classInfo.Name!} ");
            }
            else
            {
                Output.Write($"export class {classInfo.Name!} ");
            }

            if (null != classInfo.BaseType)
            {
                Output.Write($"extends {classInfo.BaseType.Name!} ");
            }
            Output.WriteLine("{");
        }

        protected override void WriteConstructor(MetadataClassInfo classInfo)
        {
            if (!classInfo.IsEnum)
            {
                WriteIndent(1);
                Output.WriteLine($"constructor(other?: {classInfo.Name!}) {{");

                if (null != classInfo.BaseType)
                {
                    WriteIndent(2);
                    Output.WriteLine("super(other);");
                }

                WriteIndent(2);
                Output.WriteLine("if (other) {");
                foreach (var property in classInfo.Properties.Values)
                {
                    WriteIndent(3);
                    Output.WriteLine($"this.{ToJSONCase(property.Name!)} = other.{ToJSONCase(property.Name!)};");
                }
                WriteIndent(2);
                Output.WriteLine("}");
                WriteIndent(1);
                Output.WriteLine("}");
            }
        }

        protected override void WriteProperty(MetadataPropertyInfo propertyInfo)
        {
            var type = FormatType(propertyInfo);
            WriteIndent(1);
            Output.Write($"{(propertyInfo.Visibility == Visibility.Public ? "public" : "protected")} {ToJSONCase(propertyInfo.Name!)}: {type}{(propertyInfo.IsArray ? "[]" : "")} = ");
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
                    if ((null != propertyInfo.ImplementedClass) && propertyInfo.ImplementedClass.IsEnum)
                    {
                        Output.WriteLine($"{propertyInfo.ImplementedClass.Name}.{propertyInfo.ImplementedClass.Fields.First().Value.Name};");
                    }
                    else
                    {
                        Output.WriteLine($"new {type}();");
                    }
                }
            }
        }

        protected override void WriteField(MetadataFieldInfo fieldInfo)
        {
            if (fieldInfo.DefiningClass.IsEnum)
            {
                WriteXmlDocumentation(fieldInfo.XmlComment, 1);
                WriteIndent(1);
                Output.Write(fieldInfo.Name);
                Output.WriteLine((fieldInfo == fieldInfo.DefiningClass.Fields.Last().Value) ? "" : ",");
            }
            else
            {
                WriteXmlDocumentation(fieldInfo.XmlComment, 1);

                var type = FormatType(fieldInfo);
                WriteIndent(1);
                Output.Write($"{(fieldInfo.Visibility == Visibility.Public ? "public" : "protected")} {ToJSONCase(fieldInfo.Name!)}: {type}{(fieldInfo.IsArray ? "[]" : "")} = ");
                if (fieldInfo.Type!.EndsWith("[]"))
                {
                    Output.WriteLine("[];");
                }
                else
                {
                    if (fieldInfo.IsStandardType)
                    {
                        Output.WriteLine($"{tsStandardTypeValues[BaseFormatter.csStandardTypes.IndexOf(fieldInfo.Type!)]};");
                    }
                    else
                    {
                        if ((null != fieldInfo.ImplementedClass) && fieldInfo.ImplementedClass.IsEnum)
                        {
                            Output.WriteLine($"{fieldInfo.ImplementedClass.Name}.{fieldInfo.ImplementedClass.Fields.First().Value.Name};");
                        }
                        else
                        {
                            Output.WriteLine($"new {type}();");
                        }
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

        protected override string ToStandardType(int index)
        {
            return TypeScriptFormatter.tsStandardTypes[index];
        }
    }
}