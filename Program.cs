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

                    if (0 == options.ClassNames!.Count())
                    {
                        options.AutoFind = true;
                    }

                    return (Options?)options;
                },
                (DefaultOptions options) =>
                {
                    Logger.SetVerbosity(options.Verbosity);

                    if (0 == options.ClassNames!.Count())
                    {
                        options.AutoFind = true;
                    }
                    return (Options?)options;
                },
                _ => { return null; }
            );

            if (null != cmdLine)
            {
                List<string> classNames = [..cmdLine.ClassNames!];

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

                    if (assemblyInfo!.ClassesByName.TryGetValue(className, out var classInfo))
                    {
                        // For this class we do load more information.
                        classInfo.AllClassesLoaded(assemblyInfo, 2);
                        foreach (var language in cmdLine.Languages!)
                        {
                            switch (language)
                            {
                                case Language.TypeScript:
                                {
                                    var fileName = TypeScriptFormatter.GetFileNameFromClass(classInfo);
                                    if (!string.IsNullOrEmpty(cmdLine.OutputFolder))
                                    {
                                        var dir = Path.GetDirectoryName(Path.Combine(cmdLine.OutputFolder, fileName));
                                        if (!Directory.Exists(dir!))
                                        {
                                            Directory.CreateDirectory(dir!);
                                        }
                                    }
                                    using var writer = GetOutput(fileName);
                                    (new TypeScriptFormatter(assemblyInfo, writer)).FormatClass(classInfo);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.LogMessage($"Could not find class {className}. Did you include the full namespace?");
                    }
                }

                if (cmdLine is DemoOptions)
                {
                    foreach (var language in cmdLine.Languages!)
                    {
                        switch (language)
                        {
                            case Language.TypeScript:
                                TypeScriptTypeConverter.DemoSerialization();
                                break;
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
                Console.Out.WriteLine($"********** Output file would be {fileName} **********");
                Console.Out.WriteLine();
                return Console.Out;
            }

            return new StreamWriter(Path.Combine(cmdLine.OutputFolder, fileName), false);
        }
    }
}