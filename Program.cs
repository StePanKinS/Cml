using Cml.Lexing;

string path;

if (args.Length > 0)
{
    path = args[0];
} 
else
{
    path = @"C:\Users\stepa\Code\cml\Test.cml";
}

List<Token> tokens = Lexer.Process(path);

foreach (Token token in tokens)
{
    Console.WriteLine(token);
}