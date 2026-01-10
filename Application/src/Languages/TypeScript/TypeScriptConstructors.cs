using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Z3
{
    internal class TypeScriptConstructors
    {
        private MetadataClassInfo ClassInfo { get; set; }
        private TypeScriptTypeConverter Converter { get; set; }
        private TypeScriptFormatter Formatter { get; set; }
        private TextWriter Output { get; set; }
        private bool SingleParameterMustBeCreated { get; set; }
        public string? FirstParameterName { get; private set; }

        public TypeScriptConstructors(MetadataClassInfo classInfo, BaseFormatter formatter, BaseTypeConverter converter, TextWriter output)
        {
            ClassInfo = classInfo;
            Formatter = (TypeScriptFormatter)formatter;
            Converter = (TypeScriptTypeConverter)converter;
            Output = output;
            SingleParameterMustBeCreated = null == ClassInfo.BaseType;
        }

        public bool CreateConstructor()
        {
            if (!ClassInfo.IsEnum)
            {
                var c = ClassInfo;
                var classInheritance = new List<MetadataClassInfo>();
                var membersToSkip = new List<string>();

                while (null != c)
                {
                    // Find the members to skip and construct a list of all base classes.
                    FindMembersToSkip(c, membersToSkip);
                    classInheritance.Add(c);
                    c = c.BaseType;
                }

                // We start with the parameters of the topmost base class.
                classInheritance.Reverse();

                CreateConstructorSignature(classInheritance, membersToSkip);

                CreateConstructorContent(classInheritance, membersToSkip);

                return SingleParameterMustBeCreated;
            }

            return false;
        }

        private void FindMembersToSkip(MetadataClassInfo classInfo, List<string> membersToSkip)
        {
            foreach (var member in classInfo.Members)
            {
                if (member.DontSerialize)
                {
                    membersToSkip.Add(member.Name!);
                }
            }

            var doubleNames = classInfo.FixedConstructionParameters.Keys.Where(name => membersToSkip.Contains(name));
            foreach (var doubleName in doubleNames)
                Console.Error.WriteLine($"Can not set the fixed construction parameter for '{doubleName}' as it is already set in {classInfo.Name}");

            if (doubleNames.Count() > 0)
                throw new ApplicationException("Fixed construction parameter set on multiple classes for the same parameter.");

            if (classInfo != ClassInfo)
                membersToSkip.AddRange(classInfo.FixedConstructionParameters.Keys);
        }

        private void GenerateConstructorParameter(MetadataMemberInfo member, bool isFirstParameter, bool isThisClass)
        {
            if (isFirstParameter)
                FirstParameterName = BaseTypeConverter.ToJSONCase(member.Name!);
            else
                Output.Write(",");

            Output.Write(" ");

            if (isThisClass && !isFirstParameter)
                Output.Write($"{member.Visibility.ToString().ToLower()} ");

            if (isThisClass && isFirstParameter)
                SingleParameterMustBeCreated = true;

            var type = Converter.ConvertType(member);
            Output.Write($"{BaseTypeConverter.ToJSONCase(member.Name!)}: ");
            Output.Write($"{type}{(isFirstParameter ? $" | {ClassInfo.Name}" : "")}{(member.IsNullable || isFirstParameter ? " | null" : "")}");

            if (isFirstParameter)
                Output.Write(" = null");
            else
                Output.Write(Converter.GetDefaultMemberValue(member));
        }

        private void CreateConstructorSignature(List<MetadataClassInfo> classInheritance, List<string> membersToSkip)
        {
            Formatter.WriteIndent(1);
            Output.Write($"constructor(");

            var firstParameter = true;

            foreach (var ci in classInheritance)
            {
                var lastClass = ci == classInheritance.Last();
                foreach (var member in ci.Members)
                    if (!membersToSkip.Contains(member.Name!) && !ClassInfo.FixedConstructionParameters.ContainsKey(member.Name!))
                    {
                        GenerateConstructorParameter(member, firstParameter, lastClass);
                        firstParameter = false;
                    }
            }

            if (!firstParameter)
                Output.Write(" ");

            Output.Write(")");
        }

        private void CreateConstructorContent(List<MetadataClassInfo> classInheritance, List<string> membersToSkip)
        {
            Output.WriteLine(" {");

            if (null != ClassInfo.BaseType)
                CreateCallToSuper(classInheritance, membersToSkip);

            CreateCodeToCopyFromOther();

            Formatter.WriteIndent(1);
            Output.WriteLine("}");
        }

        private void CreateCallToSuper(List<MetadataClassInfo> classInheritance, List<string> membersToSkip)
        {
            Formatter.WriteIndent(2);
            Output.Write("super(");

            var firstParameter = true;

            // The last class in the list is the class for which we
            // are creating the constructor, so those properties are
            // not wanted in the call to the constructor of the
            // base-class. Hence we remove it from the list.
            foreach (var ci in new List<MetadataClassInfo>(classInheritance[..^1]))
                foreach (var member in ci.Members)
                    // Check if we need to skip this parameter, because it had a fixed value
                    // in one of the constructors of all the base-classes.
                    if (!membersToSkip.Contains(member.Name!))
                    {
                        if (!firstParameter)
                            Output.Write(",");

                        Output.Write(" ");

                        if (ClassInfo.FixedConstructionParameters.ContainsKey(member.Name!))
                            if ((member.Type == "string") || (member.Type == nameof(String)))
                                Output.Write($"\"{ClassInfo.FixedConstructionParameters[member.Name!]}\"");
                            else
                                Output.Write($"{ClassInfo.FixedConstructionParameters[member.Name!]}");
                        else
                            Output.Write($"{BaseTypeConverter.ToJSONCase(member.Name!)}");

                        firstParameter = false;
                    }

            if (!firstParameter)
                Output.Write(" ");

            Output.WriteLine(");");
        }

        private void CreateCodeToCopyFromOther()
        {
            if (ClassInfo.Members.Any(m => !m.DontSerialize))
            {
                Formatter.WriteIndent(2);
                Output.WriteLine($"if (null != {FirstParameterName}) {{");

                // Create a check for the type of the first parameter.

                Formatter.WriteIndent(3);
                Output.WriteLine($"if ({FirstParameterName} instanceof {ClassInfo.Name!}) {{");

                foreach (var member in ClassInfo.Members)
                {
                    if (BaseTypeConverter.csStandardTypes.Contains(member.ImplementedClass?.Name ?? ""))
                    {
                        var parameterName = BaseTypeConverter.ToJSONCase(member.Name!);
                        Formatter.WriteIndent(4);
                        Output.WriteLine($"this.{parameterName} = {FirstParameterName}.{parameterName};");
                    }
                    else
                    {
                        var parameterName = BaseTypeConverter.ToJSONCase(member.Name!);
                        Formatter.WriteIndent(4);
                        Output.WriteLine($"this.{parameterName}{Converter.GetDefaultMemberValue(member)};");
                        if (member.IsArray || member.IsGeneric)
                        {
                            Formatter.WriteIndent(4);
                            Output.WriteLine($"for (let x of {FirstParameterName}.{parameterName}) {{");
                            Formatter.WriteIndent(5);
                            Output.WriteLine($"let newX{Converter.GetDefaultMemberValue(member, true)};");
                            Formatter.WriteIndent(5);
                            Output.WriteLine($"newX.CopyFromWeakType(x);");
                            Formatter.WriteIndent(5);
                            Output.WriteLine($"this.{parameterName}.push(newX);");
                            Formatter.WriteIndent(4);
                            Output.WriteLine("}");
                        }
                        else
                        {
                            Formatter.WriteIndent(4);
                            Output.WriteLine($"this.{parameterName}.CopyFromWeakType({FirstParameterName}.{parameterName});");
                        }
                    }
                }

                Formatter.WriteIndent(3);
                Output.WriteLine("} else {");

                Formatter.WriteIndent(4);
                Output.WriteLine($"this.{FirstParameterName} = {FirstParameterName};");

                Formatter.WriteIndent(3);
                Output.WriteLine("}");

                Formatter.WriteIndent(2);
                Output.WriteLine("}");
            }
        }
    }
}