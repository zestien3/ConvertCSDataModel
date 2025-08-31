using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Zestien3;

namespace Z3
{
    /// <summary>
    /// The main class for this application, containing the entry point.
    /// </summary>
    public static class Program
    {
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
                    Logger.SetVerbosity(options.Verbosity);

                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ConvertCSDataModel.Demo.cs")!;
                    var reader = new StreamReader(stream);
                    Console.Out.WriteLine(reader.ReadToEnd());

                    options.Languages = [Language.TypeScript];
                    return (Options?)options;
                },
                (DefaultOptions options) =>
                {
                    Logger.SetVerbosity(options.Verbosity);
                    return (Options?)options;
                },
                _ => { return null; }
            );

            if (null != cmdLine)
            {
                List<string> classNames = [.. cmdLine.ClassNames!];

                Logger.LogMessage($"Opening assembly {cmdLine.AssemblyName!}");
                assemblyInfo = MetadataAssemblyInfo.Factory(cmdLine.AssemblyName!);

                if (cmdLine.AutoFind)
                {
                    foreach (var classInfo in assemblyInfo.ClassesByName.Values)
                    {
                        if (classInfo.Attributes.ContainsKey(nameof(Zestien3.UseInFrontendAttribute)))
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
                    Logger.LogMessage($"Processing class {className}");

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
                                    (new TypeScriptFormatter(assemblyInfo, writer)).FormatClass(classInfo);
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
                Logger.LogMessage($"Output file would be {fileName}");
                return Console.Out;
            }

            return new StreamWriter(Path.Combine(cmdLine.OutputFolder, fileName), false);
        }
    }
}