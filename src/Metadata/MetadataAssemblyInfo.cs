using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Z3
{
    internal class MetadataAssemblyInfo: MetadataInfo
    {
        private static Dictionary<string, MetadataAssemblyInfo?> allLoadedAssemblies = new();

        private FileStream fileStream;
        private PEReader portableExecutableReader;
        private Dictionary<string, MetadataClassInfo> classesByName = new();
        private Dictionary<TypeDefinitionHandle, MetadataClassInfo> classesByHandle = new();

        public static MetadataAssemblyInfo Factory(string assemblyName, int depthToLoad = 1)
        {
            if (!allLoadedAssemblies.ContainsKey(assemblyName))
            {
                allLoadedAssemblies[assemblyName] = null;
                allLoadedAssemblies[assemblyName] = new MetadataAssemblyInfo(assemblyName, depthToLoad);
            }
            else
            {
                if (null == allLoadedAssemblies[assemblyName])
                {
                    throw new ApplicationException($"Circular reference encountered when loading {assemblyName}");
                }
            }

            return allLoadedAssemblies[assemblyName]!;
        }

        private MetadataAssemblyInfo(string assemblyName, int depthToLoad) : base(null, null)
        {
            XmlDoc = new XmlDocumentationFile( Path.ChangeExtension(assemblyName, ".xml"));

            Name = Path.GetFileNameWithoutExtension(assemblyName);

            fileStream = new FileStream(assemblyName!, FileMode.Open, FileAccess.Read, FileShare.Read);
            portableExecutableReader = new PEReader(fileStream);
            Reader = portableExecutableReader.GetMetadataReader();

            foreach (var typeDefHandle in Reader.TypeDefinitions)
            {
                AddClassToAssembly(typeDefHandle);
            }

            AllClassesLoaded(this, depthToLoad);
        }

        public void AddClassToAssembly(MetadataClassInfo classInfo)
        {
            if (string.IsNullOrEmpty(classInfo.Namespace))
            {
                classesByName[classInfo.Name!] = classInfo;
            }
            else
            {
                classesByName[classInfo.FullName] = classInfo;
            }
        }

        private void AddClassToAssembly(TypeDefinitionHandle typeDefHandle)
        {
            var typeDef = Reader!.GetTypeDefinition(typeDefHandle);

            // If it's name starts with <>, it's probably an anonymous type or backing field
            if (Reader.GetString(typeDef.Name).StartsWith("<>"))
                return;

            // If it's BaseType is null, it is probably not something we are interested in
            if (typeDef.BaseType.IsNil)
                return;

            var typeInfo = new MetadataClassInfo(this, typeDef, Reader, XmlDoc);

            AddClassToAssembly(typeInfo);
            classesByHandle[typeDefHandle] = typeInfo;

            // Add any nested classes to the list as well.
            // We need to do this after the class itself has been added to the lists,
            // so we can access it from the nested class.
            foreach (var subTypeDefinition in typeDef.GetNestedTypes())
            {
                AddClassToAssembly(subTypeDefinition);
            }
        }

        public IReadOnlyDictionary<string, MetadataClassInfo> ClassesByName
        {
            get
            {
                return classesByName.AsReadOnly();
            }
        }

        public IReadOnlyDictionary<TypeDefinitionHandle, MetadataClassInfo> ClassesByHandle
        {
            get
            {
                return classesByHandle.AsReadOnly();
            }
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > LoadedDepth)
            {
                LoadedDepth = depthToLoad;
                // Now all classes are loaded, we give all classes
                // the chance to get a link to the classes they reference.
                foreach (var classInfo in classesByHandle.Values)
                {
                    classInfo.AllClassesLoaded(this, depthToLoad - 1);
                }
            }
        }
        public static IReadOnlyDictionary<string, MetadataAssemblyInfo?> AllLoadedAssemblies => allLoadedAssemblies.AsReadOnly();
    }
}