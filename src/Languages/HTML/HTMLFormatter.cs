using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Z3
{
    internal class HTMLFormatter : BaseFormatter
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
            var result = Path.Combine(subFolder, $"app-{BaseTypeConverter.ToKebabCase(bareTypeName)}.html");
            Logger.LogDebug($"Compiled filename vor {classInfo.Name!}: {result}");
            return result;
        }

        /// <summary>
        /// Create an instance of the <see cref="HTMLFormatter"/> class.
        /// </summary>
        /// <param name="assemblyInfo">The assembly for which we create the output.</param>
        /// <param name="output">The output to which the type script code must be written.</param>
        public HTMLFormatter(MetadataAssemblyInfo assemblyInfo, TextWriter output)
            : base(assemblyInfo, output, new TypeScriptTypeConverter())
        {
            IndentSize = 2;
        }

        protected override void WriteComment(string str, int indentLevel)
        {
            WriteIndent(indentLevel);
            Output.WriteLine($"<!-- {str} -->");
        }

        protected override void WriteMultilineComment(IEnumerable<string> str, int indentLevel)
        {
            WriteIndent(indentLevel);
            Output.WriteLine("<!--");
            foreach (var s in str)
            {
                WriteIndent(indentLevel + 1);
                Output.WriteLine($"{s}");
            }
            WriteIndent(indentLevel);
            Output.WriteLine(" -->");
        }

        protected override void WriteXmlDocumentation(XmlDocumentation? documentation, int indentLevel)
        {
            if (null != documentation && documentation.HasContent)
            {
                WriteIndent(indentLevel);
                Output.WriteLine("<!--");

                foreach (var str in documentation.Summary)
                {
                    WriteIndent(indentLevel + 1);
                    Output.WriteLine($" *  {str}");
                }

                if (documentation.Summary.Any() && documentation.Remarks.Any())
                    Output.WriteLine();

                foreach (var str in documentation.Remarks)
                {
                    WriteIndent(indentLevel + 1);
                    Output.WriteLine($"{str}");
                }

                WriteIndent(indentLevel);
                Output.WriteLine(" -->");
            }
        }

        protected override void WriteFileHeader()
        {
            // HTML does not have a file header.
            // At least not in this case, where we write a snippet of HTML to be used as a angular template
        }

        protected override void WriteFileReference(string className, string fileName, string currentSubFolder)
        {
            // HTML does not use references.
        }

        protected override void OpenNamespace()
        {
            // HTML does not use namespaces
        }

        protected override void OpenClass()
        {
            Output.WriteLine($"@if({BaseTypeConverter.ToJSONCase(ClassInfo!.Name!)}) {{");
            WriteIndent(1);
            Output.WriteLine("<div class=\"row\">");
            WriteIndent(2);
            Output.WriteLine("<div class=\"col-12\">");
        }

        protected override void WriteConstructor()
        {
            // HTML does not use constructors.
        }

        protected override void WriteProperties()
        {
            foreach (var propertyInfo in ClassInfo!.Properties.Values)
            {
                WriteIndent(3);
                Output.WriteLine("<div class=\"row\">");
                WriteIndent(4);
                Output.WriteLine($"<label class=\"col-3\">{BaseTypeConverter.ToLabelCase(propertyInfo.Name!)}</label>");
                WriteIndent(4);
                Output.WriteLine("<div class=\"col-9\">");
                WriteIndent(5);
                Output.Write("<input class=\"form-control\" [(value)]=\"");
                Output.WriteLine($"{BaseTypeConverter.ToJSONCase(ClassInfo!.Name!)}.{BaseTypeConverter.ToJSONCase(propertyInfo.Name!)}\" />");
                WriteIndent(4);
                Output.WriteLine("</div>");
                WriteIndent(3);
                Output.WriteLine("</div>");
            }
        }

        protected override void WriteFields()
        {
            foreach (var fieldInfo in ClassInfo!.Fields.Values)
            {
                WriteIndent(3);
                Output.WriteLine("<div class=\"form-group\">");
                WriteIndent(4);
                Output.Write("<input class=\"form-control\" [(value)]=\"");
                Output.WriteLine($"{BaseTypeConverter.ToJSONCase(ClassInfo!.Name!)}.{BaseTypeConverter.ToJSONCase(fieldInfo.Name!)}\">");
                WriteIndent(3);
                Output.WriteLine("</div>");
            }
        }

        protected override void CloseClass()
        {
            WriteIndent(2);
            Output.WriteLine("</div>");
            WriteIndent(1);
            Output.WriteLine("</div>");
            Output.WriteLine("}");
        }

        protected override void CloseNamespace()
        {
            // HTML does not use namespaces
        }
    }
}