using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Z3
{
    internal class HTMLFormatter : BaseFormatter
    {
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
            var title = $"Edit {BaseTypeConverter.StripToMinimalName(ClassInfo!.Name!)}";
            var className = BaseTypeConverter.ToJSONCase(ClassInfo!.Name!);
            var baseClassName = null == ClassInfo!.BaseType ? "" : BaseTypeConverter.ToJSONCase(ClassInfo!.BaseType!.Name!);
            var baseClassSelector = null == ClassInfo!.BaseType ? "" : BaseTypeConverter.StripToMinimalName(ClassInfo.BaseType!.Name!);

            Output.WriteLine($"@if (showTitle) {{ <h4 i18n=\"generic|{title}\">{title}</h4> }}");
            Output.WriteLine($"@if({className}) {{");
            WriteIndent(1);
            Output.WriteLine($"@if (showTitle) {{ <hr /> }}");

            if (!string.IsNullOrEmpty(baseClassSelector))
            {
                WriteIndent(1);
                Output.WriteLine($"<app-edit-{baseClassSelector} [{baseClassName}]=\"{className}\" [showTitle]=\"false\"></app-edit-{baseClassSelector}>");
            }

            WriteIndent(1);
            Output.WriteLine("<div class=\"row\">");
            WriteIndent(2);
            Output.WriteLine("<div class=\"col-12\">");
        }

        protected override void WriteConstructor()
        {
            // HTML does not use constructors.
        }

        protected override void WriteMembers()
        {
            foreach (var memberInfo in ClassInfo!.Members)
            {
                WriteMemberInfo(memberInfo);
            }
        }

        protected override void WriteMethods()
        {
            // Not used for generating HTML code.
        }

        private void WriteMemberInfo(MetadataMemberInfo info)
        {
            var type = Converter.ConvertType(info);
            var value = "[(value)]";
            var change = "";

            if (type == "File")
            {
                value = "(value)";
                change = " (change)=\"fileChanged($event)\"";
            }

            var label = BaseTypeConverter.ToLabelCase(info.Name!);

            var fullName = $"{BaseTypeConverter.ToJSONCase(info.DefiningClass!.Name!)}.{BaseTypeConverter.ToJSONCase(info.Name!)}";
            WriteIndent(3);
            Output.WriteLine("<div class=\"row\">");
            WriteIndent(4);
            Output.WriteLine($"<label class=\"col-4\" i18n=\"edit|{label}\">{label}</label>");
            WriteIndent(4);
            Output.WriteLine("<div class=\"col-8\">");
            WriteIndent(5);
            Output.Write($"<input class=\"form-control\" {value}=\"{fullName}\"{change}");
            if (!string.IsNullOrEmpty(type))
            {
                Output.Write($" type=\"{type}\"");
            }
            Output.WriteLine(" />");
            WriteIndent(4);
            Output.WriteLine("</div>");
            WriteIndent(3);
            Output.WriteLine("</div>");
        }

        protected override void CloseClass()
        {
            WriteIndent(2);
            Output.WriteLine("</div>");
            WriteIndent(1);
            Output.WriteLine("</div>");
            Output.WriteLine("}");

            Output.WriteLine("@if (showTitle) {");
            WriteIndent(1);
            Output.WriteLine("<div class=\"row\">");
            WriteIndent(2);
            Output.WriteLine("<div class=\"col-12\">");
            WriteIndent(3);
            Output.WriteLine("<br/>");
            WriteIndent(3);
            Output.WriteLine("<br/>");
            WriteIndent(3);
            Output.WriteLine("<button type=\"button\" class=\"btn btn-danger\" (click)=\"clickedCancel()\" i18n=\"generic|Cancel\">Cancel</button>");
            WriteIndent(3);
            Output.WriteLine("<button type=\"button\" class=\"btn btn-success float-right\" (click)=\"clickedOK()\" i18n=\"generic|OK\">OK</button>");
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