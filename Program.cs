global using Cml.Parsing.Executables;
global using Cml.Parsing.Definitions;
global using Cml.Parsing;
global using Cml.Lexing;
global using Cml;

string path;

if (args.Length > 0)
    path = args[0];
else
    path = @"test.cml";


Lexer lexer = new(path);

foreach (var (index, value) in lexer.Enumerate())
{
    Console.WriteLine($"{index}) {value.Location}: {value}");
}

var glbNmsp = NamespaceDefinition.NewGlobal();
ErrorReporter errorer = new();

Parser parser = new(glbNmsp, errorer);
parser.Parsedefinitions(lexer);

// foreach (var d in glbNmsp)
// {
//     Console.WriteLine($"{d.GetType().Name}: {d.Name}");
// }

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