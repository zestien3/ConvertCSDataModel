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
        /// <param name="subFolder">The subfolder in which the file should be created.</param>
        /// <returns>The file name to store the TypeScript representation of the given MetadataClassInfo.</returns>
        public static string GetFileNameFromClass(MetadataClassInfo classInfo, string subFolder)
        {
            var bareTypeName = BaseTypeConverter.StripToBareType(classInfo.Name!);
            var result = Path.Combine(subFolder, $"{BaseTypeConverter.ToKebabCase(bareTypeName)}.ts");
            Logger.LogDebug($"Compiled filename vor {classInfo.Name!}: {result}");
            return result;
        }

        /// <summary>
        /// Create an instance of the <see cref="TypeScriptFormatter"/> class.
        /// </summary>
        /// <param name="assemblyInfo">The assembly for which we create the output.</param>
        /// <param name="output">The output to which the type script code must be written.</param>
        public TypeScriptFormatter(MetadataAssemblyInfo assemblyInfo, TextWriter output)
            : base(assemblyInfo, output, new TypeScriptTypeConverter())
        {
            IndentSize = 4;
        }

        protected override void WriteComment(string str, int indentLevel)
        {
            WriteIndent(indentLevel);
            Output.WriteLine($"// {str}");
        }

        protected override void WriteMultilineComment(IEnumerable<string> str, int indentLevel)
        {
            WriteIndent(indentLevel);
            Output.WriteLine("/*");
            foreach (var s in str)
            {
                WriteIndent(indentLevel + 1);
                Output.WriteLine($" * {s}");
            }
            WriteIndent(indentLevel);
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

        protected override void WriteFileHeader()
        {
            // TypeScript does not have a file header.
        }

        protected override void WriteFileReference(string className, string fileName, string currentSubFolder)
        {
            if (AssemblyInfo.ClassesByName.TryGetValue(className, out var classInfo))
            {
                if (classInfo.UseInFrontend.ContainsKey(UseInFrontend!.Language))
                {
                    var subFolder = ".\\" + classInfo.UseInFrontend[UseInFrontend.Language].SubFolder!;

                    subFolder = Path.GetRelativePath(currentSubFolder, subFolder).Replace("\\", "/");

                    string shortTypeName = className[(className.LastIndexOf('.') + 1)..];
                    Output.WriteLine($"import {{ {shortTypeName} }} from \"{subFolder}/{fileName}\";");
                }
            }
        }

        protected override void OpenNamespace()
        {
            // TypeScript does not support namespaces
        }

        protected override void OpenClass()
        {
            if (ClassInfo!.IsEnum)
            {
                Output.Write($"export enum {ClassInfo.Name!} ");
            }
            else
            {
                Output.Write($"export class {ClassInfo.Name!} ");
            }

            if (null != ClassInfo.BaseType)
            {
                Output.Write($"extends {ClassInfo.BaseType.Name!} ");
            }
            Output.WriteLine("{");
        }

        protected override void WriteConstructor()
        {
            _ = new TypeScriptConstructors(ClassInfo!, this, Converter, Output);
        }

        protected override void WriteProperties()
        {
            if (UseInFrontend!.Constructor != TSConstructorType.AllMembers)
            {
                foreach (var propertyInfo in ClassInfo!.Properties.Values)
                {
                    Output.WriteLine();
                    WriteXmlDocumentation(propertyInfo.XmlComment, 1);

                    var type = Converter.ConvertType(propertyInfo);
                    WriteIndent(1);
                    Output.Write($"{(propertyInfo.Visibility == Visibility.Public ? "public" : "protected")} ");
                    Output.Write($"{BaseTypeConverter.ToJSONCase(propertyInfo.Name!)}: {type}");
                    Output.Write($"{(propertyInfo.IsNullable ? " | null" : "")} = ");
                    if (propertyInfo.IsNullable)
                    {
                        Output.WriteLine("null;");
                    }
                    else
                    {
                        if (type.EndsWith("[]"))
                        {
                            Output.WriteLine("[];");
                        }
                        else
                        {
                            if (Converter.IsStandardType(propertyInfo.Type!))
                            {
                                Output.WriteLine(
                                    $"{TypeScriptTypeConverter.tsStandardTypeValues[BaseTypeConverter.csStandardTypes.IndexOf(propertyInfo.ImplementedClass!.Name!)]};");
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
        }

        protected override void WriteFields()
        {
            if (UseInFrontend!.Constructor != TSConstructorType.AllMembers)
            {
                foreach (var fieldInfo in ClassInfo!.Fields.Values)
                {
                    Output.WriteLine();
                    WriteXmlDocumentation(fieldInfo.XmlComment, 1);

                    if (fieldInfo.DefiningClass!.IsEnum)
                    {
                        WriteIndent(1);
                        Output.Write(fieldInfo.Name);
                        Output.WriteLine((fieldInfo == fieldInfo.DefiningClass.Fields.Last().Value) ? "" : ",");
                    }
                    else
                    {
                        var type = null == fieldInfo.ImplementedClass ? fieldInfo.Type : Converter.ConvertType(fieldInfo);
                        WriteIndent(1);
                        Output.Write($"{(fieldInfo.Visibility == Visibility.Public ? "public" : "protected")} ");
                        Output.Write($"{BaseTypeConverter.ToJSONCase(fieldInfo.Name!)}: {type}{(fieldInfo.IsArray ? "[]" : "")}");
                        Output.Write($"{(fieldInfo.IsNullable ? " | null" : "")} = ");
                        if (fieldInfo.IsNullable)
                        {
                            Output.WriteLine("null;");
                        }
                        else
                        {
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
        }

        protected override void CloseClass()
        {
            Output.WriteLine("}");
        }

        protected override void CloseNamespace()
        {
            // TypeScript does not support namespaces
        }
    }
}