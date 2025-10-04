global using Cml.Parsing.Executables;
global using Cml.Parsing.Definitions;
global using Cml.CodeGenerating;
global using Cml.NameContexts;
global using Cml.Parsing;
global using Cml.Lexing;
global using Cml.Errors;
global using Cml;

string path;

if (args.Length > 0)
    path = args[0];
else
    path = @"test2.cml";


Lexer lexer = new(path);

foreach (var (index, value) in lexer.Enumerate())
{
    Console.WriteLine($"{index}) {value.Location}: {value}");
}

var glbNmsp = NamespaceDefinition.NewGlobal();
StructDefinition.AddStandartTypes(glbNmsp);
ErrorReporter errorer = new();

Parser parser = new(glbNmsp, errorer);
parser.ParseDefinitions(lexer);
parser.ParseCode();

FasmCodeGen codeGen = new(glbNmsp);
string code = codeGen.Generate();

StreamWriter sw = new(expandUser("~/test.fasm"));
sw.Write(code);
sw.Close();

printDefs(glbNmsp);

Console.WriteLine($"{errorer.Count} Errors");
foreach (var e in errorer)
{
    Console.WriteLine(e);
}


void printDefs(NamespaceDefinition nmsp)
{
    foreach (var d in nmsp)
    {
        Console.WriteLine($"{d.GetType().Name}: {d.FullName}");
        if (d is NamespaceDefinition ns)
            printDefs(ns);
    }
}


string expandUser(string path)
    => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);