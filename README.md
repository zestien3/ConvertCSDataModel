# ConvertCSDataModel

This tool implements a simple converter for classes defined in C# (dotnet) and outputs them in the format of another language.
For now only TypeScript is implemented.

There is only a CLI with several options, which include a demo of converting some internal classes with various types and member names.

It uses Reflection on the compiled Assembly to do so, using the MetadataReader and it's written in net8.0.
It might also work with .NET Assemblies written in another language, but I have not tested that.

As a tool it might not be that interesting to everyone, but if you want to get started using the MetadataReader, it might be helpfull.

## Architecture
There are a number of Metadata-classes to represent Assemblies, classes, attributes, properties and member fields.
We start by reading the Assembly and using reflection, the classes properties and member fields (and some of their attributes) are created automagically.

For now we ignore any methods, as this tool is ment to be used in a project where the backend is done in C# and the frontend in Angular.
This tool can convert the classes created in C# to TypeScript, so they can be used in the Angular project.
This should make it easier to exchange data between backend and frontend.

After the reflection data is created (which is totally string based and thus cannot be invoked as with the old reflection framework),
there are two kind of classes to generate the output.
One is derived from BaseFormatter, which basically creates the output files.
The other one is a class derived from BaseTypeConverter, which converts names of properties, fields and types.
