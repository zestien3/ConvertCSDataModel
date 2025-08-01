using System;
using System.Collections.Generic;
using System.IO;

namespace Z3
{
    internal class TypeScriptFormatter : BaseFormatter
    {
        private static readonly string INDENT = "  ";
        private static readonly List<string> tsStandardTypes = new() { "void", "boolean", "number", "number", "number",
                                                                       "number", "number", "number", "number",
                                                                       "number", "number", "number", "number",
                                                                       "string", "<TYPEDREFERENCE>", "<INTPTR>", "<UINTPTR>",
                                                                       "<OBJECT>", "Date" };

        public TextWriter Output { get; }

        public static string GetFileNameFromClass(MetadataClassInfo classInfo)
        {
            return $"{BaseFormatter.ToKebabCase(classInfo.Name!)}.model.ts";
        }

        public TypeScriptFormatter(TextWriter output)
        {
            Output = output;
        }

        protected override void WriteFileHeader(MetadataClassInfo classInfo)
        {
            // TypeScript does not have a file header.
        }

        protected override void WriteUsings(MetadataClassInfo classInfo)
        {
            var usingsFound = false;
            foreach (var property in classInfo.Properties.Values)
            {
                if (!property.DontSerialize())
                {
                    string type = property.Type!;
                    if (!IsStandardType(type))
                    {
                        usingsFound = true;
                        var formattedType = FormatType(type).Replace("[]", "");
                        Output.WriteLine($"import {{ {formattedType} }} from \"./{ToKebabCase(formattedType)}.model.ts\";");
                    }
                }
            }

            if (usingsFound)
            {
                Output.WriteLine();
            }
        }

        protected override void OpenNamespace(MetadataClassInfo classInfo)
        {
            // TypeScript does not support namespaces
        }

        protected override void OpenClass(MetadataClassInfo classInfo)
        {
            Output.WriteLine($"export class {ToCamelCase(classInfo.Name!)} {{");
        }

        protected override void WriteProperties(MetadataClassInfo classInfo)
        {
            foreach (var property in classInfo.Properties.Values)
            {
                if (!property.DontSerialize())
                {
                    Output.WriteLine($"{INDENT}public {ToPascalCase(property.Name!)}: {FormatType(property.Type!)};");
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

        protected override string FormatType(string type)
        {
            if (IsStandardType(type))
            {
                return TypeScriptFormatter.tsStandardTypes[BaseFormatter.csStandardTypes.IndexOf(type)];
            }

            if (type.StartsWith("System.Collections.Generic.List`1["))
            {
                type = type.Substring("System.Collections.Generic.List`1[".Length);
                type = type.Substring(0, type.Length - 1);
                if (IsStandardType(type))
                {
                    type = TypeScriptFormatter.tsStandardTypes[BaseFormatter.csStandardTypes.IndexOf(type)];
                }

                type += "[]";
            }

            // Remove the namspace
            if (type.Contains('.'))
            {
                type = type.Substring(type.LastIndexOf('.') + 1);
            }

            return type;
        }
    }
}