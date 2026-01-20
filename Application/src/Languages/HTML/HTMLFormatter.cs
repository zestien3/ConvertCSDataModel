using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Zestien3;
using Zestien3.ConvertCSDataModel;

namespace Z3
{
    internal abstract class HTMLFormatter : BaseFormatter
    {
        protected string previousCategory = string.Empty;
        protected int buttonSectionOffset = 0;
        protected int buttonSectionWidth = 12;
        protected DialogType dialogType;

        /// <summary>
        /// Create an instance of the <see cref="HTMLFormatter"/> class.
        /// </summary>
        /// <param name="assemblyInfo">The assembly for which we create the output.</param>
        /// <param name="output">The output to which the type script code must be written.</param>
        public HTMLFormatter(MetadataAssemblyInfo assemblyInfo, TextWriter output)
            : base(assemblyInfo, output, new HTMLTypeConverter())
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
            var title = $"{BaseTypeConverter.StripToMinimalName(ClassInfo!.Name!)}";
            if (ClassInfo!.Attributes.TryGetValue("DisplayNameAttribute", out var displayName))
            {
                title = $"{displayName.FixedArguments[0].Value!}";
            }

            var className = BaseTypeConverter.ToJSONCase(ClassInfo!.Name!);
            var baseClassName = null == ClassInfo!.BaseType ? "" : BaseTypeConverter.ToJSONCase(ClassInfo!.BaseType!.Name!);
            var baseClassSelector = null == ClassInfo!.BaseType ? "" : BaseTypeConverter.StripToMinimalName(ClassInfo.BaseType!.Name!).Replace(" ", "-");

            if (!ClassInfo.IsAbstract)
                Output.WriteLine($"@if (showTitle) {{ <h4 i18n=\"edit|{title}\"><b>{title}</b></h4> }}");
            Output.WriteLine($"@if({className}) {{");
            if (!ClassInfo.IsAbstract)
            {
                WriteIndent(1);
                Output.WriteLine($"@if (showTitle) {{ <hr /> }}");
            }

            if (!string.IsNullOrEmpty(baseClassSelector))
            {
                var hiddenProperties = ClassInfo.Select<string>(c =>
                {
                    var uif = c.UseInFrontend.FirstOrDefault(u => (u.Language == Language.HTML) && u.DialogType == dialogType);
                    return uif?.HiddenProperties ?? [];
                });
                WriteIndent(1);
                Output.Write($"<app-edit-{baseClassSelector} [{baseClassName}]=\"{className}\" [showTitle]=\"false\"");
                foreach(var hiddenProperty in hiddenProperties)
                {
                    Output.Write($" [hide{hiddenProperty}]=\"true\"");
                }
                Output.WriteLine($"></app-edit-{baseClassSelector}>");
            }
        }

        protected override void WriteConstructor()
        {
            // HTML does not use constructors.
        }

        protected override void WriteMembers()
        {
            if (ClassInfo!.Members.Count > 0)
            {
                WriteIndent(1);
                Output.WriteLine("<div class=\"row top-of-dialog\">");
                WriteIndent(2);
                Output.WriteLine("<div class=\"col-12\">");

                foreach (var memberInfo in ClassInfo!.Members)
                {
                    WriteMemberInfo(memberInfo);
                }

                WriteIndent(2);
                Output.WriteLine("</div>");
                WriteIndent(1);
                Output.WriteLine("</div>");
            }
        }

        protected override void WriteMethods()
        {
            // Not used for generating HTML code.
        }

        protected abstract void WriteMemberInfo(MetadataMemberInfo info);

        protected override void CloseClass()
        {
            Output.WriteLine("}");

            if (!ClassInfo!.IsAbstract)
            {
                Output.WriteLine("@if (showTitle || showButtons) {");
                WriteIndent(1);
                Output.WriteLine("<div class=\"row\">");
                WriteIndent(2);
                Output.WriteLine($"<div class=\"offset-{buttonSectionOffset} col-{buttonSectionWidth}\">");
                WriteIndent(3);
                Output.WriteLine("<br/>");
                WriteIndent(3);
                Output.WriteLine("<br/>");
                WriteIndent(3);
                Output.WriteLine("<button type=\"button\" class=\"btn btn-danger\" (click)=\"clickedCancel()\" i18n=\"generic|Cancel\">Cancel</button>");
                WriteIndent(3);
                ClassInfo.Buttons.ForEach(button =>
                {
                    var btnCallback = BaseTypeConverter.ToCamelCase(button.Text);
                    var btnName = BaseTypeConverter.ToPascalCase(button.Text);
                    Output.Write($"<button type=\"button\" class=\"btn btn-{button.Color} float-right\" (click)=\"clicked{btnCallback}()\" "); 
                    Output.WriteLine($"#{btnName}Btn id=\"{btnName}Btn\" i18n=\"generic|{button.Text}\">{button.Text}</button>");
                    WriteIndent(3);
                });
                Output.WriteLine("<button type=\"button\" class=\"btn btn-success float-right\" (click)=\"clickedOK()\" i18n=\"generic|OK\">OK</button>");
                WriteIndent(2);
                Output.WriteLine("</div>");
                WriteIndent(1);
                Output.WriteLine("</div>");
                Output.WriteLine("}");
            }
        }

        protected override void CloseNamespace()
        {
            // HTML does not use namespaces
        }
    }
}