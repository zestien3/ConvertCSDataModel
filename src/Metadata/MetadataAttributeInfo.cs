using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace Z3
{
    internal class MetadataAttributeInfo : MetadataInfo
    {
        private readonly List<CustomAttributeTypedArgument<string>> fixedArguments = [];
        private readonly Dictionary<string, CustomAttributeNamedArgument<string>> namedArguments = [];

        public MetadataAttributeInfo(string name) : base(null, null)
        {
            XmlMemberName = name;
        }

        public MetadataAttributeInfo(CustomAttribute attribute, MetadataReader reader) : base(reader, null)
        {
            try
            {
                CustomAttributeValue<string>? arguments = null;
                switch (attribute.Constructor.Kind)
                {
                    case HandleKind.MemberReference:
                        var ctorAsMember = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                        switch (ctorAsMember.Parent.Kind)
                        {
                            case HandleKind.TypeReference:
                                var a = reader.GetTypeReference((TypeReferenceHandle)ctorAsMember.Parent);
                                Name = reader.GetString(a.Name);

                                // This would get the parameters, but for now we are not interested in them. 
                                arguments = attribute.DecodeValue(MetadataCustomAttributeTypeProvider.Instance);

                                break;
                            default:
                                Console.Error.WriteLine($"{nameof(MetadataClassInfo)}: Attribute.Parent kind is of type {ctorAsMember.Parent.Kind}");
                                break;
                        }
                        break;
                    case HandleKind.MethodDefinition:
                        // The attribute is defined using it's constructor. 
                        var ctorAsMethod = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);

                        // The return value of the constructor is void, so we get the declaring type,
                        // which is the attribute class. We remove the namespace.
                        var type = ctorAsMethod.GetDeclaringType().ToTypeString(reader);
                        Name = type.Substring(type.LastIndexOf('.') + 1);

                        // TODO: We can move this code to the outer scope, as the attribute is defined
                        //       in that scope.
                        // We get all the parameters passed to the method, which are FixedArguments
                        // or NamedArguments. For now we are only interested in the named arguments.
                        arguments = attribute.DecodeValue(MetadataCustomAttributeTypeProvider.Instance);

                        break;
                    default:
                        Console.Error.WriteLine($"{nameof(MetadataClassInfo)}: Attribute.Constructor kind is of type {attribute.Constructor.Kind}");
                        break;
                }

                // We add all named arguments of type string and their values to the dictionary.
                if (null != arguments)
                {
                    foreach (var na in arguments!.Value.NamedArguments)
                    {
                        namedArguments[na.Name!] = na;
                    }

                    foreach (var na in arguments!.Value.FixedArguments)
                    {
                        fixedArguments.Add(na);
                    }
                }
            }
            catch (Exception)
            {
                Name = null;
            }
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad) { }

        public IReadOnlyList<CustomAttributeTypedArgument<string>> FixedArguments { get { return fixedArguments.AsReadOnly(); } }

        public IReadOnlyDictionary<string, CustomAttributeNamedArgument<string>> NamedArguments { get { return namedArguments.AsReadOnly(); } }
    }
}