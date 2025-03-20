using Cml;
using Cml.Lexing;
using Cml.Parsing;

string path;

if (args.Length > 0)
{
    path = args[0];
} 
else
{
    path = @"test.cml";
}

List<Token> tokens = Lexer.Process(path);

foreach ((int i, Token token) in tokens.Enumerate())
{
    Console.WriteLine($"{i} {token.Location}: {token}");
}

ParsedFile parsedFile = Parser.Process(tokens);

foreach (var import in parsedFile.Imports)
{
    Console.WriteLine(import.File);
}
foreach (var definition in parsedFile.Definitions.Names.Values)
{
    Console.WriteLine(definition.Name);
}