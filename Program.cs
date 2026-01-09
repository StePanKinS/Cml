global using Cml.Parsing.Executables;
global using Cml.Parsing.Definitions;
global using Cml.CodeGeneration;
global using Cml.NameContexts;
global using Cml.Parsing;
global using Cml.Lexing;
global using Cml.Errors;
global using Cml.Types;
using Cml;

// string path = @"test.cml";
// Console.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), path));

// string build_folder = args.Length > 0 ? args[0] : "/tmp";

CmlProject cmlp = new(
    "cmltest",
    "./",
    ["test.cml", "test2.cml"],
    printTokens: false
);

ErrorReporter errorer = Compiler.Compile(cmlp);

Console.WriteLine($"{errorer.Count} Errors");
foreach (var e in errorer)
{
    Console.WriteLine(e);
}

