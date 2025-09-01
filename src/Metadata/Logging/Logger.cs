using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Z3
{
    internal enum VerbosityLevel
    {
        Silent,
        Normal,
        Debug
    }

    /// <summary>
    /// Simple logger, which for now writes everything to Console.Out.
    /// </summary>
    internal static class Logger
    {
        private static TextWriter output = Console.Out;

        public static VerbosityLevel Verbosity { get; private set; }

        public static void LogMessage(string message)
        {
            if (Verbosity != VerbosityLevel.Silent)
            {
                output.WriteLine(message);
            }
        }

        public static void LogDebug(string message,
            [CallerMemberName] string? callingMethod = "",
            [CallerFilePath] string? callingFileName = "",
            [CallerLineNumber] int? callingLineNumber = 0)
        {
            if (Verbosity == VerbosityLevel.Debug)
            {
                output.WriteLine($"{callingMethod} - {Path.GetFileName(callingFileName)}.{callingLineNumber}: {message}");
            }
        }

        public static void LogFatal(string message,
            [CallerMemberName] string? callingMethod = "",
            [CallerFilePath] string? callingFileName = "",
            [CallerLineNumber] int? callingLineNumber = 0)
        {
            output.WriteLine($"{callingMethod} - {Path.GetFileName(callingFileName)}.{callingLineNumber}: {message}");
            Environment.Exit(-1);
        }

        public static void SetVerbosity(VerbosityLevel verbosity)
        {
            Verbosity = verbosity;
        }
    }
}