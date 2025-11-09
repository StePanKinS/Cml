global using Cml.Parsing.Executables;
global using Cml.Parsing.Definitions;
global using Cml.CodeGenerating;
global using Cml.NameContexts;
global using Cml.Parsing;
global using Cml.Lexing;
global using Cml.Errors;
using System.Diagnostics;

string path = @"test2.cml";

string build_folder = args.Length > 0 ? args[0] : "/tmp";

Lexer lexer = new(path);

// foreach (var l in lexer.GetTokens())
// {
//     Console.WriteLine($"{l.Location}\t{l}");
// }

var glbNmsp = NamespaceDefinition.NewGlobal();
StructDefinition.AddStandartTypes(glbNmsp);
ErrorReporter errorer = new();

Parser parser = new(glbNmsp, errorer);
parser.ParseDefinitions(lexer);
parser.ParseCode();

if (errorer.Count != 0)
{
    Console.WriteLine($"{errorer.Count} Errors");
    foreach (var e in errorer)
    {
        Console.WriteLine(e);
    }
    
    return;
}

FasmCodeGen codeGen = new(glbNmsp);
string code = codeGen.Generate();

postProcess(code);


void printDefs(NamespaceDefinition nmsp)
{
    foreach (var d in nmsp)
    {
        Console.WriteLine($"{d.GetType().Name}: {d.FullName}");
        if (d is NamespaceDefinition ns)
            printDefs(ns);
    }
}

void postProcess(string code)
{
    StreamWriter sw = new($"{build_folder}/test.asm");
    sw.Write(code);
    sw.Close();

    Process.Start(new ProcessStartInfo()
    {
        UseShellExecute = false,
        FileName = "fasm",
        Arguments = $"{build_folder}/test.asm {build_folder}/test.o",
        RedirectStandardOutput = true,
    })?.WaitForExit();
    Process.Start(new ProcessStartInfo()
    {
        UseShellExecute = false,
        FileName = "gcc",
        Arguments = $"{build_folder}/test.o -o {build_folder}/test -no-pie",
        RedirectStandardOutput = true
    })?.WaitForExit();
}