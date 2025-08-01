using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Z3
{
    internal class MetadataAssemblyInfo: MetadataInfo
    {
        private static Dictionary<string, MetadataAssemblyInfo?> allLoadedAssemblies = new();

        private FileStream fileStream;
        private PEReader portableExecutableReader;
        private MetadataReader reader;
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

        private MetadataAssemblyInfo(string assemblyName, int depthToLoad)
        {
            Name = Path.GetFileNameWithoutExtension(assemblyName);

            fileStream = new FileStream(assemblyName!, FileMode.Open, FileAccess.Read, FileShare.Read);
            portableExecutableReader = new PEReader(fileStream); //.BaseStream);
            reader = portableExecutableReader.GetMetadataReader();

            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);

                // If it's namespace is blank, it's not a user-defined type
                if (string.IsNullOrEmpty(reader.GetString(typeDef.Namespace)))
                    continue;

                // If it's BaseType is null, it is not something we are interested in
                if (typeDef.BaseType.IsNil)
                    continue;

                var typeInfo = new MetadataClassInfo(typeDef, reader);

                classesByName[typeInfo.FullName] = typeInfo;
                classesByHandle[typeDefHandle] = typeInfo;
            }

            AllClassesLoaded(this, depthToLoad);
        }

        public ReadOnlyDictionary<string, MetadataClassInfo> ClassesByName
        {
            get
            {
                return classesByName.AsReadOnly();
            }
        }

        public ReadOnlyDictionary<TypeDefinitionHandle, MetadataClassInfo> ClassesByHandle
        {
            get
            {
                return classesByHandle.AsReadOnly();
            }
        }

        public override void AllClassesLoaded(MetadataInfo? metadataInfo, int depthToLoad)
        {
            if (depthToLoad > loadedDepth)
            {
                loadedDepth = depthToLoad;
                // Now all classes are loaded, we give all classes
                // the chance to get a link to the classes they reference.
                foreach (var classInfo in classesByHandle.Values)
                {
                    classInfo.AllClassesLoaded(this, depthToLoad - 1);
                }
            }
        }
    }
}