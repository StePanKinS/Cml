global using Cml.Parsing.Executables;
global using Cml.Parsing.Definitions;
global using Cml.CodeGeneration;
global using Cml.NameContexts;
global using Cml.Parsing;
global using Cml.Lexing;
global using Cml.Errors;
global using Cml.Types;
using Cml;
using System.CommandLine;

// string path = @"test.cml";
// Console.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), path));

// string build_folder = args.Length > 0 ? args[0] : "/tmp";

RootCommand rootCommand = new("Cml™ (crappy modular language) compiler");

var inputsArg = new Argument<string[]>("inputs")
{
    Description = "Input source files",
    Arity = ArgumentArity.ZeroOrMore
};
rootCommand.Arguments.Add(inputsArg);

Option<string> outputOption = new("--output", ["-o"])
{
    DefaultValueFactory = (_) => "a.out",
    Description = "Output file path",
};
rootCommand.Options.Add(outputOption);

Option<bool> printTokensOption = new("--print-tokens", ["-t"])
{
    Description = "Print tokens from lexer",
};
rootCommand.Options.Add(printTokensOption);

Option<string> backendOption = new("--backend", ["-b"])
{
    DefaultValueFactory = (_) => "fasm",
    Description = "Code generation backend: fasm or llvm",
};
rootCommand.Options.Add(backendOption);

ParseResult parseResult = rootCommand.Parse(args);
if (parseResult.Invoke() != 0) Environment.Exit(1);

var inputs = parseResult.GetValue(inputsArg);
if (inputs == null || inputs.Length == 0) // uhhhh...
{
    Console.WriteLine("Error: No input files provided");
    Environment.Exit(1);
}

string backend = parseResult.GetValue(backendOption)!.ToLower();
if (backend != "fasm" && backend != "llvm")
{
    Console.WriteLine("Error: Backend must be 'fasm' or 'llvm'");
    Environment.Exit(1);
}

CmlProject cmlp = new(
    parseResult.GetValue(outputOption)!,
    "./",
    inputs,
    printTokens: parseResult.GetValue(printTokensOption),
    backend: backend
);

ErrorReporter errorer = Compiler.Compile(cmlp);

Console.WriteLine($"{errorer.Count} Errors");
foreach (var e in errorer)
{
    Console.WriteLine(e);
}
if (errorer.Count != 0)
    Environment.Exit(1);
