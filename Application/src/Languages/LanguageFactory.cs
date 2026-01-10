using System;
using System.IO;
using Zestien3;
using Zestien3.ConvertCSDataModel;

namespace Z3
{
    internal static class LanguageFactory
    {
        public static void TranslateClass(MetadataClassInfo classInfo, string? outputFolder)
        {
            if (null == outputFolder)
            {
                outputFolder = string.Empty;
            }

            foreach (var useInFrontend in classInfo.UseInFrontend)
            {
                string fileName = "";
                switch (useInFrontend.Language)
                {
                    case Language.TypeScript:
                        Logger.LogMessage($"Generating TypeScript code for class {classInfo.Name}");
                        fileName = TypeScriptTypeConverter.GetFileNameFromClass(classInfo, useInFrontend.SubFolder);
                        {
                            using TextWriter writer = GetTextWriter(outputFolder, fileName);
                            new TypeScriptFormatter(classInfo.ContainingAssembly!, writer).FormatClass(classInfo, useInFrontend);
                        }
                        break;
                    case Language.HTML:
                        Logger.LogMessage($"Generating HTML code for class {classInfo.Name}");
                        fileName = HTMLTypeConverter.GetFileNameFromClass(classInfo, useInFrontend.SubFolder);
                        {
                            using TextWriter writer = GetTextWriter(outputFolder, fileName);
                            switch (useInFrontend.DialogType)
                            {
                                case DialogType.Standard:
                                    new HTMLStandardFormatter(classInfo.ContainingAssembly!, writer).FormatClass(classInfo, useInFrontend);
                                    break;
                                case DialogType.Compact:
                                    new HTMLCompactFormatter(classInfo.ContainingAssembly!, writer).FormatClass(classInfo, useInFrontend);
                                    break;
                            }
                        }
                        break;
                }
            }
        }

        private static TextWriter GetTextWriter(string outputFolder, string fileName)
        {
            if (!string.IsNullOrEmpty(outputFolder))
            {
                var dir = Path.GetDirectoryName(Path.Combine(outputFolder, fileName));
                if (!Directory.Exists(dir!))
                {
                    Directory.CreateDirectory(dir!);
                }
            }
            return GetOutput(outputFolder, fileName);
        }

        private static TextWriter GetOutput(string outputFolder, string fileName)
        {
            if (string.IsNullOrEmpty(outputFolder))
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine($"********** Output file would be {fileName} **********");
                Console.Out.WriteLine();
                return Console.Out;
            }

            return new StreamWriter(Path.Combine(outputFolder, fileName), false);
        }
    }
}