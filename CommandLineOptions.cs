using System.Collections.Generic;
using System.Reflection;
using CommandLine;

namespace Z3
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
        public VerbosityLevel Verbosity { get; set; }
    }

    [Verb("demo", false, HelpText = "Converts some included test classes as a demonstration")]
    internal class DemoOptions : Options
    {
        [Option('a', "auto", Required = false, HelpText = "If defined, converts all classes with a UseInFrontend attribute.")]
        public bool AutoFind { get; set; } = false;

        [Option('f', "file", Required = false, HelpText = "The path to the Assembly which contains the class(es) to convert.")]
        public string? AssemblyName { get; set; } = Assembly.GetExecutingAssembly()!.Location;

        [Option('l', "language", Required = false, HelpText = "The programming language(s) to convert to.")]
        public IEnumerable<Language>? Languages { get; set; }

        [Option('c', "classes", Required = false, HelpText = "The name of the class(es) to convert (including the complete namespace). If omitted, -a is assumed.")]
        public IEnumerable<string>? ClassNames { get; set; }

        [Option('o', "out", Required = false, HelpText = "The folder to which the output files are written. If omitted, Console.Out is used.")]
        public string? OutputFolder { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "The output verbosity level (Silent, Normal, Debug) where the default is Silent.")]
        public VerbosityLevel Verbosity { get; set; }
    }

    [Verb("default", true)]
    internal class DefaultOptions : Options
    {
        [Option('a', "auto", Required = false, HelpText = "If defined, converts all classes with a UseInFrontend attribute.")]
        public bool AutoFind { get; set; } = false;

        [Option('f', "file", Required = true, HelpText = "The path to the Assembly which contains the class(es) to convert.")]
        public string? AssemblyName { get; set; }

        [Option('l', "language", Required = true, HelpText = "The programming language(s) to convert to.")]
        public IEnumerable<Language>? Languages { get; set; }

        [Option('c', "classes", Required = false, HelpText = "The name of the class(es) to convert (including the complete namespace). If omitted, Console.Out is used.")]
        public IEnumerable<string>? ClassNames { get; set; }

        [Option('o', "out", Required = false, HelpText = "The folder to which the output files are written. If omitted, Console.Out is used.")]
        public string? OutputFolder { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "The output verbosity level (Silent, Normal, Debug) where the default is Normal.")]
        public VerbosityLevel Verbosity { get; set; } = VerbosityLevel.Normal;
    }
}