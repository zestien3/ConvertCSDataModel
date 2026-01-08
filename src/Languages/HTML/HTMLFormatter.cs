using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Zestien3;

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
            var baseClassSelector = null == ClassInfo!.BaseType ? "" : BaseTypeConverter.StripToMinimalName(ClassInfo.BaseType!.Name!);

            if (!ClassInfo.IsAbstract)
                Output.WriteLine($"@if (showTitle) {{ <h4 i18n=\"generic|{title}\"><b>{title}</b></h4> }}");
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
                    if (c.UseInFrontend.ContainsKey(Language.HTML))
                    {
                        return c.UseInFrontend[Language.HTML].HiddenProperties;
                    }

                    return [];
                });
                WriteIndent(1);
                Output.Write($"<app-edit-{baseClassSelector} [{baseClassName}]=\"{className}\" [showTitle]=\"false\"");
                foreach(var hiddenProperty in hiddenProperties)
                {
                    Output.Write($" [hide{hiddenProperty}]=\"true\"");
                }
                Output.WriteLine($"></app-edit-{baseClassSelector}>");
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
            var addHideOptionForMember = ClassInfo!.Any(c =>
                {
                    if (c.UseInFrontend.ContainsKey(Language.HTML))
                    {
                        return c.UseInFrontend[Language.HTML]!.HiddenProperties.Contains(info.Name!);
                    }

                    return false;
                });

            var indent = 3;
            if (addHideOptionForMember)
            {
                indent++;
                WriteIndent(3);
                Output.WriteLine($"@if (!hide{info.Name}) {{");
            }

            var type = Converter.ConvertType(info);
            var value = "[(ngModel)]";
            var change = "";

            if (type == "File")
            {
                value = "(value)";
                change = " (change)=\"fileChanged($event)\"";
            }

            var label = BaseTypeConverter.ToLabelCase(info.Name!);

            if (info.Attributes.ContainsKey(nameof(DisplayNameAttribute)))
            {
                label = info.Attributes[nameof(DisplayNameAttribute)].FixedArguments[0].Value!.ToString();
            }

            var editor = "input";
            if (info.Attributes.ContainsKey(nameof(EditorAttribute)))
            {
                editor = info.Attributes[nameof(EditorAttribute)].FixedArguments[0].Value!.ToString();
            }

            var fullName = $"{BaseTypeConverter.ToJSONCase(info.DefiningClass!.Name!)}.{BaseTypeConverter.ToJSONCase(info.Name!)}";
            var noNgModel = info.IsArray || info.IsGeneric;
            WriteIndent(indent);
            Output.WriteLine("<div class=\"row\">");
            WriteIndent(indent + 1);
            Output.WriteLine($"<label class=\"col-4\" i18n=\"edit|{label}\">{label}</label>");
            WriteIndent(indent + 1);
            Output.WriteLine("<div class=\"col-8\">");
            WriteIndent(indent + 2);

            if (editor == "input")
            {
                var classes = "form-control";
                if (type == "checkbox")
                {
                    classes += " form-check-input";
                }
                Output.Write($"<input class=\"{classes}\")");
                if (!noNgModel)
                    Output.Write($" {value}=\"{fullName}\"{change}");
                if (!string.IsNullOrEmpty(type))
                {
                    Output.Write($" type=\"{type}\"");
                }
                Output.WriteLine($" autocomplete=\"off\" #{BaseTypeConverter.ToJSONCase(info.Name!)} id=\"{BaseTypeConverter.ToJSONCase(info.Name!)}\" />");
            }
            else
            {
                Output.Write($"<{editor} ");
                if (!noNgModel)
                    Output.Write($"[(ngModel)]=\"{fullName}\" ");
                Output.WriteLine($"#{BaseTypeConverter.ToJSONCase(info.Name!)} id=\"{BaseTypeConverter.ToJSONCase(info.Name!)}\"></{editor}>");
            }

            WriteIndent(indent + 1);
            Output.WriteLine("</div>");
            WriteIndent(indent);
            Output.WriteLine("</div>");

            if (addHideOptionForMember)
            {
                WriteIndent(3);
                Output.WriteLine("}");
            }
        }

        protected override void CloseClass()
        {
            WriteIndent(2);
            Output.WriteLine("</div>");
            WriteIndent(1);
            Output.WriteLine("</div>");
            Output.WriteLine("}");

            if (!ClassInfo!.IsAbstract)
            {
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
       }

        protected override void CloseNamespace()
        {
            // HTML does not use namespaces
        }
    }
}