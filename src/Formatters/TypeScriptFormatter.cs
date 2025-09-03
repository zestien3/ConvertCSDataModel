using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            return Path.Combine(classInfo.SubFolder, $"{BaseTypeConverter.ToKebabCase(classInfo.Name!)}.ts");
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
                var subFolder = ".\\" + classInfo.SubFolder;

                subFolder = Path.GetRelativePath(currentSubFolder, subFolder).Replace("\\", "/");

                string shortTypeName = className.Substring(className.LastIndexOf('.') + 1);
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
                    Output.WriteLine($"this.{BaseTypeConverter.ToJSONCase(property.Name!)} = other.{BaseTypeConverter.ToJSONCase(property.Name!)};");
                }
                WriteIndent(2);
                Output.WriteLine("}");
                WriteIndent(1);
                Output.WriteLine("}");
            }
        }

        protected override void WriteProperty(MetadataPropertyInfo propertyInfo)
        {
            var type = Converter.ConvertType(propertyInfo.Type!);
            WriteIndent(1);
            Output.Write($"{(propertyInfo.Visibility == Visibility.Public ? "public" : "protected")} {BaseTypeConverter.ToJSONCase(propertyInfo.Name!)}: {type} = ");
            if (type.EndsWith("[]"))
            {
                Output.WriteLine("[];");
            }
            else
            {
                if (Converter.IsStandardType(type))
                {
                    Output.WriteLine($"{TypeScriptTypeConverter.tsStandardTypeValues[BaseTypeConverter.csStandardTypes.IndexOf(propertyInfo.Type!)]};");
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

                var type = Converter.ConvertType(fieldInfo.Type!);
                WriteIndent(1);
                Output.Write($"{(fieldInfo.Visibility == Visibility.Public ? "public" : "protected")} {BaseTypeConverter.ToJSONCase(fieldInfo.Name!)}: {type}{(fieldInfo.IsArray ? "[]" : "")} = ");
                if (fieldInfo.Type!.EndsWith("[]"))
                {
                    Output.WriteLine("[];");
                }
                else
                {
                    if (Converter.IsStandardType(type))
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