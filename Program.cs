using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;

namespace Z3
{
    /// <summary>
    /// The main class for this application, containing the entry point.
    /// </summary>
    public static class Program
    {
        internal enum Language
        {
            TypeScript
        }

        internal interface Options
        {
            public bool AutoFind { get; set; }
            public string? AssemblyName { get; set; }
            public IEnumerable<Language>? Languages { get; set; }
            public IEnumerable<string>? ClassNames { get; set; }
            public string? OutputFolder { get; set; }
        }

        [Verb("demo", false, HelpText = "Converts some included test classes as a demonstration")]
        internal class DemoOptions : Options
        {
            [Option('a', "auto", Required = false, HelpText = "If defined, converts all classes with a UseInFrontend attribute")]
            public bool AutoFind { get; set; }

            [Option('f', "file", Required = false, HelpText = "The path to the Assembly which contains the class(es) to convert")]
            public string? AssemblyName { get; set; }

            [Option('l', "language", Required = false, HelpText = "The programming language(s) to convert to")]
            public IEnumerable<Language>? Languages { get; set; }

            [Option('c', "classes", Required = false, HelpText = "The name of the class(es) to convert (including the complete namespace)")]
            public IEnumerable<string>? ClassNames { get; set; }

            [Option('o', "out", Required = false, HelpText = "The folder to which the output files are written. If omitted, Console.Out is used")]
            public string? OutputFolder { get; set; }

            public DemoOptions()
            {
                AutoFind = true;
                AssemblyName = Assembly.GetExecutingAssembly()!.Location;
            }
        }

        [Verb("default", true)]
        internal class DefaultOptions : Options
        {
            [Option('a', "auto", Required = false, HelpText = "If defined, converts all classes with a UseInFrontend attribute")]
            public bool AutoFind { get; set; }

            [Option('f', "file", Required = true, HelpText = "The path to the Assembly which contains the class(es) to convert")]
            public string? AssemblyName { get; set; }

            [Option('l', "language", Required = true, HelpText = "The programming language(s) to convert to")]
            public IEnumerable<Language>? Languages { get; set; }

            [Option('c', "classes", Required = false, HelpText = "The name of the class(es) to convert (including the complete namespace)")]
            public IEnumerable<string>? ClassNames { get; set; }

            [Option('o', "out", Required = false, HelpText = "The folder to which the output files are written. If omitted, Console.Out is used")]
            public string? OutputFolder { get; set; }
        }

        private static MetadataAssemblyInfo? assemblyInfo;

        private static Options? cmdLine;

        /// <summary>
        /// The main entry point of the program.
        /// </summary>
        /// <param name="args">The command line parameters passed to the program.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            var parsed = Parser.Default.ParseArguments<DemoOptions, DefaultOptions>(args);

            if (parsed.Errors.Count() > 0)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.ManifestModule.Name);
                Console.Error.WriteLine($"{assemblyName} converts classes found in the given .NET assembly to different programming languages.");
                Console.Error.WriteLine($"It uses reflection to get the class information from the assembly.");
                Console.Error.WriteLine($"{assemblyName} It uses reflection to get the class information from the assembly.");
                Environment.Exit(1);
            }

            cmdLine = parsed.MapResult(
                (DemoOptions options) =>
                {
                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ConvertCSDataModel.Demo.cs")!;
                    var reader = new StreamReader(stream);
                    Console.Out.WriteLine(reader.ReadToEnd());

                    options.Languages = [Language.TypeScript];
                    return (Options?)options;
                },
                (DefaultOptions options) => { return (Options?)options; },
                _ => { return null; }
            );

            if (null != cmdLine)
            {
                List<string> classNames = new(cmdLine.ClassNames!);

                assemblyInfo = MetadataAssemblyInfo.Factory(cmdLine.AssemblyName!);

                if (cmdLine.AutoFind)
                {
                    foreach (var classInfo in assemblyInfo.ClassesByName.Values)
                    {
                        if (classInfo.Attributes.Contains("UseInFrontendAttribute"))
                        {
                            if (!classNames.Contains(classInfo.FullName))
                            {
                                classNames.Add(classInfo.FullName);
                            }
                        }
                    }
                }

                foreach (var className in classNames)
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
        }

        private static TextWriter GetOutput(string fileName)
        {
            if (string.IsNullOrEmpty(cmdLine!.OutputFolder))
            {
                Console.Out.WriteLine();
                return Console.Out;
            }

            return new StreamWriter(Path.Combine(cmdLine.OutputFolder, fileName), false);
        }
    }
}