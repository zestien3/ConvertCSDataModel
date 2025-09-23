using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Zestien3;

namespace Z3
{
    internal class TypeScriptFormatter : BaseFormatter
    {
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
                    WriteMemberInfo(propertyInfo);
                }
            }
        }

        protected override void WriteFields()
        {
            if (UseInFrontend!.Constructor != TSConstructorType.AllMembers)
            {
                foreach (var fieldInfo in ClassInfo!.Fields.Values)
                {
                    WriteMemberInfo(fieldInfo);
                }
            }
        }

        private void WriteMemberInfo(MetadataMemberInfo info)
        {
            Output.WriteLine();
            WriteXmlDocumentation(info.XmlComment, 1);

            if (info.DefiningClass!.IsEnum)
            {
                WriteIndent(1);
                Output.Write(info.Name);
                Output.WriteLine((info == info.DefiningClass.Fields.Last().Value) ? "" : ",");
            }
            else
            {
                var type = null == info.ImplementedClass ? info.Type! : Converter.ConvertType(info);

                WriteIndent(1);
                Output.Write($"{(info.Visibility == Visibility.Public ? "public" : "protected")} ");
                Output.Write($"{BaseTypeConverter.ToJSONCase(info.Name!)}: {type}{(info.IsArray ? "[]" : "")}");
                Output.Write($"{(info.IsNullable ? " | null" : "")} = ");
                if (info.IsNullable)
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
                        if (Converter.IsStandardType(info.Type!))
                        {
                            Output.WriteLine($"{TypeScriptTypeConverter.tsStandardTypeValues[BaseTypeConverter.csStandardTypes.IndexOf(info.Type!)]};");
                        }
                        else
                        {
                            if ((null != info.ImplementedClass) && info.ImplementedClass.IsEnum)
                            {
                                Output.WriteLine($"{info.ImplementedClass.Name}.{info.ImplementedClass.Fields.First().Value.Name};");
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