using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            switch (ClassInfo!.UseInFrontend[Language.TypeScript].Constructor)
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
                var c = ClassInfo;
                var classInheritance = new List<MetadataClassInfo>();
                var propertiesToSkip = new List<string>();
                var thisClass = true;
                while (null != c)
                {
                    var doubleNames = c.FixedConstructionParameters.Keys.Where(name => propertiesToSkip.Contains(name));
                    foreach (var doubleName in doubleNames)
                    {
                        Console.Error.WriteLine($"Can not set the fixed construction parameter for '{doubleName}' as it is already set in {c.Name}");
                    }
                    if (doubleNames.Count() > 0)
                    {
                        throw new ApplicationException("Fixed construction parameter set on multiple classes for the same parameter.");
                    }

                    if (!thisClass)
                    {
                        propertiesToSkip.AddRange(c.FixedConstructionParameters.Keys);
                    }

                    classInheritance.Add(c);
                    c = c.BaseType;
                    thisClass = false;
                }

                classInheritance.Reverse();

                Formatter.WriteIndent(1);
                Output.Write($"constructor(");

                var first = true;

                foreach (var ci in classInheritance)
                {
                    var topLevel = ci == classInheritance.Last();
                    foreach (var property in ci.Properties.Values)
                    {
                        if (!propertiesToSkip.Contains(property.Name!) && !ClassInfo.FixedConstructionParameters.ContainsKey(property.Name!))
                        {
                            if (!first)
                            {
                                Output.Write(",");
                            }
                            Output.Write(" ");

                            var type = Converter.ConvertType(property);
                            if (topLevel) Output.Write($"{property.Visibility.ToString().ToLower()} ");
                            Output.Write($"{BaseTypeConverter.ToJSONCase(property.Name!)}: {type}{(property.IsNullable ? " | null" : "")}");
                            first = false;
                        }
                    }
                    foreach (var field in ci.Fields.Values)
                    {
                        if (!propertiesToSkip.Contains(field.Name!) && !ClassInfo.FixedConstructionParameters.ContainsKey(field.Name!))
                        {
                            if (!first)
                            {
                                Output.Write(",");
                            }
                            Output.Write(" ");

                            var type = Converter.ConvertType(field);
                            if (topLevel) Output.Write($"{field.Visibility.ToString().ToLower()} ");
                            Output.Write($"{BaseTypeConverter.ToJSONCase(field.Name!)}: {type}{(field.IsNullable ? " | null" : "")}");
                            first = false;
                        }
                    }
                }

                if (!first)
                {
                    Output.Write(" ");
                }
                Output.Write(") {");

                if (null != ClassInfo.BaseType)
                {
                    Output.WriteLine("");
                    Formatter.WriteIndent(2);
                    Output.Write("super(");

                    first = true;

                    // The last class in the list is the class for which we
                    // are creating the constructor, so those properties are
                    // not wanted in the call to the constructor of the
                    // base-class. Hence we remove it from the list.
                    classInheritance.Remove(classInheritance.Last());

                    foreach (var ci in classInheritance)
                    {
                        // Last is the direct baseclass of class for which we create the constructor.
                        var last = ci == classInheritance.Last();
                        foreach (var property in ci.Properties.Values)
                        {
                            if (!propertiesToSkip.Contains(property.Name!))
                            {
                                if (!first)
                                {
                                    Output.Write(",");
                                }
                                Output.Write(" ");

                                if (ClassInfo.FixedConstructionParameters.ContainsKey(property.Name!))
                                {
                                    if ((property.Type == "string") || (property.Type == nameof(String)))
                                    {
                                        Output.Write($"\"{ClassInfo.FixedConstructionParameters[property.Name!]}\"");
                                    }
                                    else
                                    {
                                        Output.Write($"{ClassInfo.FixedConstructionParameters[property.Name!]}");
                                    }
                                }
                                else
                                {
                                    Output.Write($"{BaseTypeConverter.ToJSONCase(property.Name!)}");
                                }

                                first = false;
                            }
                        }
                        foreach (var field in ci.Fields.Values)
                        {
                            if (!propertiesToSkip.Contains(field.Name!))
                            {
                                if (!first)
                                {
                                    Output.Write(",");
                                }
                                Output.Write(" ");

                                if (ClassInfo.FixedConstructionParameters.ContainsKey(field.Name!))
                                {
                                    if ((field.Type == "string") || (field.Type == nameof(String)))
                                    {
                                        Output.Write($"\"{ClassInfo.FixedConstructionParameters[field.Name!]}\"");
                                    }
                                    else
                                    {
                                        Output.Write($"{ClassInfo.FixedConstructionParameters[field.Name!]}");
                                    }
                                }
                                else
                                {
                                    Output.Write($"{BaseTypeConverter.ToJSONCase(field.Name!)}");
                                }
                            }
                        }
                    }

                    if (!first)
                    {
                        Output.Write(" ");
                    }
                    Output.WriteLine(");");
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