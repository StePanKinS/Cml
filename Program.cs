using Cml.Lexing;

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

foreach (Token token in tokens)
{
    Console.WriteLine(token);
}