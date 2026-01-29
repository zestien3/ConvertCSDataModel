using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Z3
{
    internal class TypeScriptWeakTypeCloneMethod
    {
        private MetadataClassInfo ClassInfo { get; set; }
        private TypeScriptTypeConverter Converter { get; set; }
        private TypeScriptFormatter Formatter { get; set; }
        private TextWriter Output { get; set; }

        public TypeScriptWeakTypeCloneMethod(MetadataClassInfo classInfo, BaseFormatter formatter, BaseTypeConverter converter, TextWriter output)
        {
            ClassInfo = classInfo;
            Formatter = (TypeScriptFormatter)formatter;
            Converter = (TypeScriptTypeConverter)converter;
            Output = output;
        }

        public void CreateMethod()
        {
            if (!ClassInfo.IsEnum)
            {
                CreateMethodSignature();

                CreateMethodContent();
            }
        }

        private void CreateMethodSignature()
        {
            Output.WriteLine();
            Formatter.WriteIndent(1);
            Output.WriteLine("public CopyFromWeakType(other: any): void {");

            if (null != ClassInfo.BaseType)
            {
                Formatter.WriteIndent(2);
                Output.WriteLine("super.CopyFromWeakType(other);");
            }
        }

        private void CreateMethodContent()
        {
            CreateCodeToCopyFromOther([ClassInfo] );

            Formatter.WriteIndent(1);
            Output.WriteLine("}");
        }

        private void CreateCodeToCopyFromOther(List<MetadataClassInfo> classInheritance)
        {
            foreach (var classInfo in classInheritance)
            {
                if (classInfo.Members.Count > 0)
                {
                    foreach (var member in classInfo.Members)
                    {
                        if (!member.DontSerialize)
                        {
                            if (BaseTypeConverter.csStandardTypes.Contains(member.ImplementedClass?.Name ?? ""))
                            {
                                var parameterName = BaseTypeConverter.ToJSONCase(member.Name!);
                                Formatter.WriteIndent(2);
                                Output.WriteLine($"this.{parameterName} = other.{parameterName};");
                            }
                            else
                            {
                                var parameterName = BaseTypeConverter.ToJSONCase(member.Name!);
                                if (member.IsArray || member.IsGeneric)
                                {
                                    Formatter.WriteIndent(2);
                                    Output.WriteLine($"for (let x of other.{parameterName}) {{");
                                    Formatter.WriteIndent(3);
                                    Output.WriteLine($"let newX{Converter.GetDefaultMemberValue(member, true)};");
                                    Formatter.WriteIndent(3);
                                    Output.WriteLine($"newX.CopyFromWeakType(x);");
                                    Formatter.WriteIndent(3);
                                    Output.WriteLine($"this.{parameterName}.push(newX);");
                                    Formatter.WriteIndent(2);
                                    Output.WriteLine("}");
                                }
                                else
                                {
                                    Formatter.WriteIndent(2);
                                    Output.WriteLine($"this.{parameterName}.CopyFromWeakType(other.{parameterName});");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}