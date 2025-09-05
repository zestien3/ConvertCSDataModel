using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zestien3;

namespace Z3
{
    internal class TypeScriptFormatter : BaseFormatter
    {
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
            var result = Path.Combine(classInfo.UseInFrontend.SubFolder!, $"{BaseTypeConverter.ToKebabCase(classInfo.Name!)}.ts");
            Logger.LogDebug($"Compiled filename vor {classInfo.Name!}: {result}");
            return result;
        }

        /// <summary>
        /// Create an instance of the <see cref="TypeScriptFormatter"/> class.
        /// </summary>
        /// <param name="assemblyInfo">The assembly for which we create the output.</param>
        /// <param name="output">The output to which the type script code must be written.</param>
        public TypeScriptFormatter(MetadataAssemblyInfo assemblyInfo, TextWriter output) : base(assemblyInfo, output, new TypeScriptTypeConverter())
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

        protected override void WriteFileReference(string className, string fileName, string currentSubFolder)
        {
            if (AssemblyInfo.ClassesByName.TryGetValue(className, out var classInfo))
            {
                var subFolder = ".\\" + classInfo.UseInFrontend.SubFolder!;

                subFolder = Path.GetRelativePath(currentSubFolder, subFolder).Replace("\\", "/");

                string shortTypeName = className[(className.LastIndexOf('.') + 1)..];
                Output.WriteLine($"import {{ {shortTypeName} }} from \"{subFolder}/{fileName}\";");
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
            _ = new TypeScriptConstructors(classInfo, this, Converter, Output);
        }

        protected override void WriteProperties(MetadataClassInfo classInfo)
        {
            if (classInfo.UseInFrontend.Constructor != TSConstructorType.AllMembers)
            {
                foreach (var propertyInfo in classInfo.Properties.Values)
                {
                    Output.WriteLine();
                    WriteXmlDocumentation(propertyInfo.XmlComment, 1);

                    var type = Converter.ConvertType(propertyInfo.ImplementedClass!);
                    WriteIndent(1);
                    Output.Write($"{(propertyInfo.Visibility == Visibility.Public ? "public" : "protected")} ");
                    Output.Write($"{BaseTypeConverter.ToJSONCase(propertyInfo.Name!)}: {type}");
                    Output.Write($"{(propertyInfo.IsNullable ? " | null" : "")} = ");
                    if (type.EndsWith("[]"))
                    {
                        Output.WriteLine("[];");
                    }
                    else
                    {
                        if (Converter.IsStandardType(propertyInfo.Type!))
                        {
                            Output.WriteLine($"{TypeScriptTypeConverter.tsStandardTypeValues[BaseTypeConverter.csStandardTypes.IndexOf(propertyInfo.ImplementedClass!.Name!)]};");
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
            }
        }

        protected override void WriteFields(MetadataClassInfo classInfo)
        {
            if (classInfo.UseInFrontend.Constructor != TSConstructorType.AllMembers)
            {
                foreach (var fieldInfo in classInfo.Fields.Values)
                {
                    Output.WriteLine();
                    WriteXmlDocumentation(fieldInfo.XmlComment, 1);

                    if (fieldInfo.DefiningClass.IsEnum)
                    {
                        WriteIndent(1);
                        Output.Write(fieldInfo.Name);
                        Output.WriteLine((fieldInfo == fieldInfo.DefiningClass.Fields.Last().Value) ? "" : ",");
                    }
                    else
                    {
                        var type = null == fieldInfo.ImplementedClass ? fieldInfo.Type : Converter.ConvertType(fieldInfo.ImplementedClass!);
                        WriteIndent(1);
                        Output.Write($"{(fieldInfo.Visibility == Visibility.Public ? "public" : "protected")} ");
                        Output.Write($"{BaseTypeConverter.ToJSONCase(fieldInfo.Name!)}: {type}{(fieldInfo.IsArray ? "[]" : "")}");
                        Output.Write($"{(fieldInfo.IsNullable ? " | null" : "")} = ");
                        if (fieldInfo.Type!.EndsWith("[]"))
                        {
                            Output.WriteLine("[];");
                        }
                        else
                        {
                            if (Converter.IsStandardType(fieldInfo.Type!))
                            {
                                Output.WriteLine($"{TypeScriptTypeConverter.tsStandardTypeValues[BaseTypeConverter.csStandardTypes.IndexOf(fieldInfo.Type!)]};");
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
    }
}