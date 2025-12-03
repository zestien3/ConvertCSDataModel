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
                WriteIndent(indentLevel);
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
            if (new TypeScriptConstructors(ClassInfo!, this, Converter, Output).CreateConstructor())
            {
                WriteMemberInfo(ClassInfo!.Members[0]);
            }
        }

        protected override void WriteMembers()
        {
            // In TypeScript we only have max 1 member to declare, the rest is declared in the constructor.
            // So since we only know if we need to create that single member while creating the constructor,
            // we will generate the member there if required.
        }

        protected override void WriteMethods()
        {
            // We create a method to clone a weak typed object.
            // We receive those from the server when making a HTTP request.
            new TypeScriptWeakTypeCloneMethod(ClassInfo!, this, Converter, Output).CreateMethod();
        }

        private void WriteMemberInfo(MetadataMemberInfo info)
        {
            Output.WriteLine();
            WriteXmlDocumentation(info.XmlComment, 1);

            if (info.DefiningClass!.IsEnum)
            {
                WriteIndent(1);
                Output.Write(info.Name);
                Output.WriteLine((info == info.DefiningClass.Members.Last()) ? "" : ",");
            }
            else
            {
                var type = null == info.ImplementedClass ? info.Type! : Converter.ConvertType(info);

                WriteIndent(1);
                Output.Write($"{(info.Visibility == Visibility.Public ? "public" : "protected")} ");
                Output.Write($"{BaseTypeConverter.ToJSONCase(info.Name!)}: {type}{(info.IsArray ? "[]" : "")}");
                Output.Write($"{(info.IsNullable ? " | null" : "")}");
                Output.Write(Converter.GetDefaultMemberValue(info));
                Output.WriteLine(";");
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