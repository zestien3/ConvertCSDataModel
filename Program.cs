using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace Z3
{
    public static class Program
    {
        public enum Language
        {
            TypeScript
        }

        public class Options
        {
            [Option('f', "file", Required = true, HelpText = "The path to the Assembly which contains the class(es) to convert")]
            public string? AssemblyName { get; set; }

            [Option('l', "language", Required = true, HelpText = "The programming language(s) to convert to")]
            public IEnumerable<Language>? Languages { get; set; }

            [Option('c', "classes", Required = true, HelpText = "The name of the class(es) to convert (including the complete namespace)")]
            public IEnumerable<string>? ClassNames { get; set; }

            [Option('o', "out", Required = false, HelpText = "The folder to which the output files are written or blank if Console.Out is to be used")]
            public string? OutputFolder { get; set; }
        }

        private static MetadataAssemblyInfo? assemblyInfo;

        private static Options? cmdLine;

        [STAThread]
        public static void Main(string[] args)
        {
            cmdLine = Parser.Default.ParseArguments<Options>(args).Value;

            assemblyInfo = MetadataAssemblyInfo.Factory(cmdLine.AssemblyName!);

            foreach (var className in cmdLine.ClassNames!)
            {
                var classInfo = assemblyInfo!.ClassesByName[className];

                // For this class we do load more information.
                classInfo.AllClassesLoaded(assemblyInfo, 2);
                foreach (var language in cmdLine.Languages!)
                {
                    switch (language)
                    {
                        case Language.TypeScript:
                        {
                            using var writer = GetOutput(TypeScriptFormatter.GetFileNameFromClass(classInfo));
                            (new TypeScriptFormatter(writer)).FormatClass(classInfo);
                            break;
                        }
                    }
                }
            }
        }

        private static TextWriter GetOutput(string fileName)
        {
            if (string.IsNullOrEmpty(cmdLine.OutputFolder))
            {
                return Console.Out;
            }

            return new StreamWriter(Path.Combine(cmdLine.OutputFolder, fileName), false);
        }
    }
}