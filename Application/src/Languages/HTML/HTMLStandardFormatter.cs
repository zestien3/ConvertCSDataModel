using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Zestien3;
using Zestien3.ConvertCSDataModel;

namespace Z3
{
    internal class HTMLStandardFormatter : HTMLFormatter
    {
        /// <summary>
        /// Create an instance of the <see cref="HTMLFormatter"/> class.
        /// </summary>
        /// <param name="assemblyInfo">The assembly for which we create the output.</param>
        /// <param name="output">The output to which the type script code must be written.</param>
        public HTMLStandardFormatter(MetadataAssemblyInfo assemblyInfo, TextWriter output) : base(assemblyInfo, output)
        {
            buttonSectionOffset = 4;
            buttonSectionWidth = 8;
            dialogType = DialogType.Standard;
        }

        protected override void WriteMemberInfo(MetadataMemberInfo info)
        {
            var indent = 3;

            var addHideOptionForMember = ClassInfo!.Any(c =>
                {
                    var uif = c.UseInFrontend.FirstOrDefault(u => (u.Language == Language.HTML) && u.DialogType == dialogType);
                    return uif?.HiddenProperties.Contains(info.Name!) ?? false;
                });

            var category = string.Empty;
            if (info.Attributes.ContainsKey(nameof(CategoryAttribute)))
            {
                category = (string)info.Attributes[nameof(CategoryAttribute)].FixedArguments[0].Value!;
            }

            if (info.Attributes.ContainsKey(nameof(PropertyTabAttribute)))
            {
                if (!string.IsNullOrEmpty(category) && (category == previousCategory))
                {
                    throw new ArgumentException("Cannot set a PropertyTab in the middle of a Category");                    
                }

                var tab = (string) info.Attributes[nameof(PropertyTabAttribute)].FixedArguments[0].Value!;
                WriteIndent(indent);
                Output.WriteLine("<div class=\"row\">");
                WriteIndent(indent + 1);
                Output.WriteLine($"<label class=\"col-4 section\" i18n=\"edit|{tab}\">{tab}</label>");
                WriteIndent(indent + 1);
                Output.WriteLine("<div class=\"col-8\"><hr></div>");
                WriteIndent(indent);
                Output.WriteLine("</div>");
            }

            if (addHideOptionForMember)
            {
                WriteIndent(indent++);
                Output.WriteLine($"@if (!hide{info.Name}) {{");
            }

            var type = Converter.ConvertType(info);
            var value = "[(ngModel)]";
            var change = "";

            if (type == "file")
            {
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

            if (string.IsNullOrEmpty(previousCategory))
            {
                WriteIndent(indent);
                Output.WriteLine("<div class=\"row\">");
                WriteIndent(indent + 1);
                Output.WriteLine($"<label class=\"col-4\" i18n=\"edit|{label}\">{label}</label>");
                WriteIndent(indent + 1);
                Output.WriteLine($"<div class=\"col-8{(string.IsNullOrEmpty(category) ? "" : $" category-{category.ToLower().Replace(" ", "-")}")}\">");
            }

            WriteIndent(indent + 2);
            if (editor == "input")
            {
                var readOnly = info.Attributes.ContainsKey(nameof(ReadOnlyAttribute)) && (bool)info.Attributes[nameof(ReadOnlyAttribute)].FixedArguments[0].Value!;
                var classes = "form-control";
                if (type == "checkbox")
                {
                    classes += " form-check-input";
                }
                Output.Write($"<input class=\"{classes}\"");
                if (!noNgModel)
                    Output.Write($" {value}=\"{fullName}\"{change}");
                if (!string.IsNullOrEmpty(type))
                {
                    Output.Write($" type=\"{type}\"");
                }
                if (readOnly)
                {
                    Output.Write(" readonly");
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

            if (string.IsNullOrEmpty(category))
            {
                WriteIndent(indent + 1);
                Output.WriteLine("</div>");
                WriteIndent(indent);
                Output.WriteLine("</div>");
            }

            if (addHideOptionForMember)
            {
                WriteIndent(3);
                Output.WriteLine("}");
            }

            previousCategory = category;
        }
    }
}