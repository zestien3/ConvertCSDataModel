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
                var c = ClassInfo;
                var classInheritance = new List<MetadataClassInfo>();
                var membersToSkip = new List<string>();

                while (null != c)
                {
                    // Construct a list of all base classes.
                    classInheritance.Add(c);
                    c = c.BaseType;
                }

                // We start with the parameters of the topmost base class.
                classInheritance.Reverse();

                CreateMethodSignature();

                CreateMethodContent(classInheritance);
            }
        }

        private void CreateMethodSignature()
        {
            Output.WriteLine();
            Formatter.WriteIndent(1);
            Output.WriteLine("public CopyFromWeakType(other: any): void {");
        }

        private void CreateMethodContent(List<MetadataClassInfo> classInheritance)
        {
            CreateCodeToCopyFromOther(classInheritance);

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
                        var parameterName = BaseTypeConverter.ToJSONCase(member.Name!);
                        Formatter.WriteIndent(2);
                        Output.WriteLine($"this.{parameterName} = other.{parameterName};");
                    }
                }
            }
        }
    }
}