using System;
using System.IO;

using Zestien3;

namespace Z3
{
    internal class TypeScriptConstructors
    {
        private MetadataClassInfo ClassInfo { get; set; }
        private BaseTypeConverter Converter { get; set; }
        private BaseFormatter Formatter { get; set; }
        private TextWriter Output { get; set; }

        public TypeScriptConstructors(MetadataClassInfo classInfo, BaseFormatter formatter, BaseTypeConverter converter, TextWriter output)
        {
            ClassInfo = classInfo;
            Formatter = formatter;
            Converter = converter;
            Output = output;
            switch (ClassInfo.UseInFrontend.Constructor)
            {
                case TSConstructorType.None:
                    break;
                case TSConstructorType.Copy:
                    CreateCopyConstructor();
                    break;
                case TSConstructorType.AllMembers:
                    CreateConstructorWithAllProperties();
                    break;
            }
        }

        private void CreateCopyConstructor()
        {
            if (!ClassInfo.IsEnum)
            {
                Formatter.WriteIndent(1);
                Output.WriteLine($"constructor(other?: {ClassInfo.Name!}) {{");

                if (null != ClassInfo.BaseType)
                {
                    Formatter.WriteIndent(2);
                    Output.WriteLine("super(other);");
                }

                Formatter.WriteIndent(2);
                Output.WriteLine("if (other) {");
                foreach (var property in ClassInfo.Properties.Values)
                {
                    Formatter.WriteIndent(3);
                    Output.WriteLine($"this.{BaseTypeConverter.ToJSONCase(property.Name!)} = other.{BaseTypeConverter.ToJSONCase(property.Name!)};");
                }
                Formatter.WriteIndent(2);
                Output.WriteLine("}");
                Formatter.WriteIndent(1);
                Output.WriteLine("}");
            }
        }

        private void CreateConstructorWithAllProperties()
        {
            if (!ClassInfo.IsEnum)
            {
                Formatter.WriteIndent(1);
                Output.Write($"constructor( ");

                var ci = ClassInfo;
                var first = true;
                var topLevel = true;
                while (null != ci)
                {
                    foreach (var property in ci.Properties.Values)
                    {
                        if (!first)
                        {
                            Output.Write(", ");
                        }
                        var type = Converter.ConvertType(property.ImplementedClass!);
                        if (topLevel) Output.Write($"{property.Visibility.ToString().ToLower()} ");
                        Output.Write($"{BaseTypeConverter.ToJSONCase(property.Name!)}: {type}{(property.IsNullable ? " | null" : "")}");
                        first = false;
                    }
                    foreach (var field in ci.Fields.Values)
                    {
                        if (!first)
                        {
                            Output.Write(", ");
                        }
                        var type = Converter.ConvertType(field.ImplementedClass!);
                        if (topLevel) Output.Write($"{field.Visibility.ToString().ToLower()} ");
                        Output.Write($"{BaseTypeConverter.ToJSONCase(field.Name!)}: {type}{(field.IsNullable ? " | null" : "")}");
                        first = false;
                    }
                    topLevel = false;
                    ci = ci.BaseType;
                }

                Output.Write(" ) {");

                if (null != ClassInfo.BaseType)
                {
                    Output.WriteLine("");
                    Formatter.WriteIndent(2);
                    Output.Write("super( ");

                    ci = ClassInfo.BaseType;
                    first = true;
                    while (null != ci)
                    {
                        foreach (var property in ci.Properties.Values)
                        {
                            if (!first)
                            {
                                Output.Write(", ");
                            }
                            Output.Write($"{BaseTypeConverter.ToJSONCase(property.Name!)}");
                            first = false;
                        }
                        foreach (var field in ci.Fields.Values)
                        {
                            if (!first)
                            {
                                Output.Write(", ");
                            }
                            Output.Write($"{BaseTypeConverter.ToJSONCase(field.Name!)}");
                            first = false;
                        }
    
                        ci = ci.BaseType;
                    }
                    Output.WriteLine(" );");
                    Formatter.WriteIndent(1);
                    Output.WriteLine("}");
                }
                else
                {
                    Output.WriteLine(" }");
                }
            }
        }
    }
}