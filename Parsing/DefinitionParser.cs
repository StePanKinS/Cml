using System.Text;

namespace Cml.Parsing;

public class Parser(NamespaceDefinition globalNamespace, ErrorReporter errorer)
{
    public const Symbols StructMemberSeparator = Symbols.Comma;
    private static readonly IEnumerable<Keywords> Modifyers = [Keywords.External, Keywords.Export];
    private static readonly Dictionary<Symbols, Symbols> BracketPairs = new()
    {
        { Symbols.CurlyClose, Symbols.CurlyOpen },
        { Symbols.CircleClose, Symbols.CircleOpen },
        { Symbols.SquareClose, Symbols.SquareOpen }
    };
    private NamespaceDefinition globalNamespace = globalNamespace;
    private ErrorReporter errorer = errorer;

    public void Parse(string fileName)
        => Parsedefinitions(new Lexer(fileName));

    public void Parsedefinitions(Lexer l)
    {
        EnumerableReader<Token> er = l.GetReader();
        parse(er, globalNamespace);
    }

    private Location parse(EnumerableReader<Token> er, NamespaceDefinition nmsp)
    {
        List<Token<Keywords>> modifyers = [];
        Token? token;

        while (true)
        {
            if (!er.Peek(out token))
            {
                if (modifyers.Count != 0)
                {
                    Location loc = new(modifyers[0].Location, modifyers[^1].Location);
                    errorer.Append("After modifyer keywrds expected definition", loc);
                }
                break;
            }
            switch (token.Type)
            {
                case TokenType.Keyword:
                    var kwdToken = (Token<Keywords>)token;
                    if (Modifyers.Contains(kwdToken.Value))
                    {
                        if ((from m in modifyers select m.Value).Contains(kwdToken.Value))
                        {
                            Location loc = new(modifyers.Find((m) => m.Value == kwdToken.Value)!.Location, kwdToken.Location);
                            errorer.Append($"Several {kwdToken.Value.ToString().ToLower()} modifyers found", loc);
                        }
                        else
                            modifyers.Add(kwdToken);

                        er.Read(out token);
                        continue;
                    }
                    if (kwdToken.Value == Keywords.Struct)
                    {
                        if (modifyers.Count != 0)
                            errorer.Append("Modifyers are not applicable to structs", new(modifyers[0], modifyers[^1]));

                        modifyers.Clear();
                        readStruct(er, nmsp);
                        continue;
                    }
                    if (kwdToken.Value == Keywords.Namespace)
                    {
                        if (modifyers.Count != 0)
                            errorer.Append("Modifyers are not applicable to namespaces", new(modifyers[0], modifyers[^1]));

                        modifyers.Clear();
                        readNamespace(er, nmsp);
                        continue;
                    }
                    errorer.Append($"Unexpected keyword {kwdToken.Value.ToString().ToLower()}", kwdToken.Location);
                    er.Read(out _);
                    break;
                case TokenType.Identifier:
                    readFunction(er, nmsp, [.. from m in modifyers select m.Value]);
                    modifyers.Clear();
                    continue;
                case TokenType.Symbol:
                    var symbolToken = (Token<Symbols>)token;
                    if (symbolToken.Value == Symbols.CurlyClose)
                    {
                        er.Read(out _);

                        if (nmsp == globalNamespace)
                        {
                            errorer.Append("Unexpected `}`", symbolToken.Location);
                            continue;
                        }
                        return symbolToken.Location;
                    }
                    goto default;
                default:
                    errorer.Append($"Unexpected token: {token}", token.Location);
                    er.Read(out _);
                    continue;
            }
        }

        return null!;
    }

    private void readStruct(EnumerableReader<Token> er, NamespaceDefinition nmsp)
    {
        Token? token, t;
        er.Read(out token);
        var kwdToken = (Token<Keywords>)token!;

        if (!er.Read(out token))
        {
            errorer.Append("Expected a name of a struct, not file END", kwdToken.Location);
            return;
        }

        if (token.Type != TokenType.Identifier)
        {
            errorer.Append($"Expected a nsme of a struct, not {token}", token.Location);
            return;
        }

        var nameToken = (Token<string>)token;

        if (!er.Read(out token))
        {
            Location loc = new(kwdToken.Location, nameToken.Location);
            errorer.Append("Expected `{`, not file END", loc);
            return;
        }


        if (token.Type != TokenType.Symbol || ((Token<Symbols>)token).Value != Symbols.CurlyOpen)
        {
            errorer.Append("Expected `{`, not " + token.ToString(), token.Location);
            return;
        }

        t = token;
        List<(string, string)> members = [];

        while (true)
        {
            Token<string>? memType = readTypeName(er);
            if (memType == null)
                return;

            if (!er.Read(out token))
                {
                    Location loc = new(kwdToken.Location, memType.Location);
                    errorer.Append($"Expected member name. Got file end", loc);
                    return;
                }
            if (token.Type != TokenType.Identifier)
            {
                errorer.Append($"Expected member name. Got {token}", token.Location);
                return;
            }

            var memName = (Token<string>)token;

            if (!er.Read(out token))
            {
                Location loc = new(kwdToken.Location, memName.Location);
                errorer.Append($"Expected {StructMemberSeparator.ToString().ToLower()}. Got file end`", loc);
                return;
            }

            members.Add((memType.Value, memName.Value));

            if (token.Type == TokenType.Symbol && ((Token<Symbols>)token).Value == Symbols.CurlyClose)
            {
                t = token;
                break;
            }

            if (token.Type != TokenType.Symbol || ((Token<Symbols>)token).Value != StructMemberSeparator)
            {
                errorer.Append($"Expected {StructMemberSeparator.ToString().ToLower()}. Got {token}`", token.Location);
                return;
            }

            if (!er.Peek(out t))
            {
                Location loc = new(kwdToken.Location, token.Location);
                errorer.Append("Expected `}` or next member type. Got file end`", loc);
                return;
            }
            if (t.Type == TokenType.Symbol && ((Token<Symbols>)t).Value == Symbols.CurlyClose)
            {
                er.Read(out _);
                break;
            }
        }

        Location location = new(kwdToken, t);
        StructDefinition sd = new(nameToken.Value, members, nmsp, [], location);
        nmsp.Append(sd);
    }

    private void readNamespace(EnumerableReader<Token> er, NamespaceDefinition nmsp)
    {
        Token? token;
        er.Read(out token);
        var kwdToken = (Token<Keywords>)token!;

        if (!er.Read(out token))
        {
            errorer.Append("Expected namespace name. Got file end", kwdToken.Location);
            return;
        }
        if (token.Type != TokenType.Identifier)
        {
            errorer.Append($"Expected namespace name. Got {token}", kwdToken.Location);
            return;
        }

        var nameToken = (Token<string>)token;

        if (!er.Read(out token))
        {
            errorer.Append("Expected `{`. Got file end", new(kwdToken, nameToken));
            return;
        }
        if (token.Type != TokenType.Symbol || ((Token<Symbols>)token).Value != Symbols.CurlyOpen)
        {
            errorer.Append($"Expected `{'{'}`. Got {token}", new(kwdToken, nameToken));
            return;
        }

        NamespaceDefinition namespaceDefinition = new(nameToken.Value, nmsp, [], Location.Nowhere);
        Location loc = parse(er, namespaceDefinition);
        namespaceDefinition.Location = new(kwdToken.Location, loc);
        nmsp.Append(namespaceDefinition);
    }

    private void readFunction(EnumerableReader<Token> er, NamespaceDefinition nmsp, Keywords[] modifyers)
    {
        // TODO: start location is in keywords
        Token? token;
        
        Token<string>? typeName = readTypeName(er);
        if (typeName == null)
            return;

        if (!er.Read(out token))
        {
            errorer.Append("Expected function name. Got file end", typeName.Location);
            return;
        }
        if (token.Type != TokenType.Identifier)
        {
            errorer.Append($"Expected function name. Got {token}", token.Location);
            return;
        }

        var funcName = (Token<string>)token;

        if (!er.Read(out token))
        {
            Location loc = new(typeName.Location, funcName.Location);
            errorer.Append("Expected `(`. Got file name", loc);
            return;
        }

        if (token.Type != TokenType.Symbol || ((Token<Symbols>)token).Value != Symbols.CircleOpen)
        {
            errorer.Append("Expected `(`, not " + token.ToString(), token.Location);
            return;
        }

        Token t = token;
        List<(string, string)> args = [];

        while (true)
        {
            if (!er.Peek(out token))
            {
                Location loc = new(typeName, t);
                errorer.Append($"Expected argument type or `)`. Got file end", loc);
                er.Read(out _);
                return;
            }
            if (token.Type == TokenType.Symbol && ((Token<Symbols>)token).Value == Symbols.CircleClose)
            {
                er.Read(out _);
                break;
            }

            Token<string>? argType = readTypeName(er);
            if (argType == null)
                return;

            if (!er.Read(out token))
            {
                Location loc = new(typeName, argType);
                errorer.Append($"Expected argument name. Got file end", loc);
                return;
            }
            if (token.Type != TokenType.Identifier)
            {
                errorer.Append($"Expected argument name. Got {token}", token.Location);
                return;
            }

            var memName = (Token<string>)token;

            args.Add((argType.Value, memName.Value));

            if (!er.Read(out token))
            {
                Location loc = new(typeName, memName);
                errorer.Append($"Expected `,` or `)`. Got file end`", loc);
                return;
            }

            if (token.Type == TokenType.Symbol && ((Token<Symbols>)token).Value == Symbols.CircleClose)
                break;

            if (token.Type != TokenType.Symbol || ((Token<Symbols>)token).Value != Symbols.Comma)
            {
                errorer.Append($"Expected `,` or `)`. Got {token}`", token.Location);
                return;
            }
        }

        if (!er.Peek(out token))
        {
            errorer.Append("Expected function body. Got file end", new Location(typeName, funcName));
            return;
        }

        Token[] body;
        if (token.Type == TokenType.Symbol && ((Token<Symbols>)token).Value == Symbols.CurlyOpen)
            body = readUntilSymbol(er, Symbols.CurlyClose, skipFirstBracket: true, inclusive: true);
        else
            body = readUntilSymbol(er, Symbols.Semicolon, inclusive: true);

        Location location = new(typeName, body[^1]);

        FunctionDefinition fd = new(
            funcName.Value,
            typeName.Value,
            [.. args],
            body,
            nmsp,
            modifyers,
            location
        );
        nmsp.Append(fd);
    }

    private Token<string>? readTypeName(EnumerableReader<Token> er)
    {
        Token? token;
        if (!er.Read(out token))
        {
            errorer.Append("Expected type name. Got file end", token!.Location);
            return null;
        }
        if (token.Type != TokenType.Identifier)
        {
            errorer.Append($"Expected type name. Got {token}", token.Location);
            return null;
        }
        StringBuilder sb = new();
        Location loc = token.Location;
        sb.Append(((Token<string>)token).Value);
        while (true)
        {
            if (!er.Peek(out token) || token.Type != TokenType.Symbol)
                break;

            var symbolToken = (Token<Symbols>)token;
            if (symbolToken.Value != Symbols.Star)
                break;
            sb.Append('*');
            loc = new(loc, symbolToken.Location);
            er.Read(out token);
        }
        return new Token<string>(sb.ToString(), TokenType.Identifier, loc);
    }

    private Token[] readUntilSymbol(
        EnumerableReader<Token> er,
        Symbols symbol,
        bool skipFirstBracket = false,
        bool inclusive = false,
        bool ignoreBrackets = false
    )
    {
        Token? token;
        List<Symbols> brackets = [];
        List<Token> tokens = [];

        er.Peek(out Token? firstToken);


        if (skipFirstBracket)
        {
            if (!er.Read(out token))
            {
                errorer.Append($"Can not find {symbol.ToString().ToLower()}", firstToken!.Location);
                return [.. tokens];
            }

            tokens.Add(token);
        }

        while (true)
        {
            if (!er.Peek(out token))
            {
                errorer.Append($"Can not find {symbol.ToString().ToLower()}", new Location(firstToken!, token!));
                return [.. tokens];
            }
            if (token.Type != TokenType.Symbol || ignoreBrackets)
            {
                tokens.Add(token);
                er.Read(out _);
                continue;
            }
            var symbolToken = (Token<Symbols>)token;

            if (brackets.Count == 0 && symbolToken.Value == symbol)
            {
                if (inclusive)
                {
                    tokens.Add(token);
                    er.Read(out _);
                }
                return [.. tokens];
            }

            switch (symbolToken.Value)
            {
                case Symbols.CurlyOpen:
                case Symbols.CircleOpen:
                case Symbols.SquareOpen:
                    brackets.Add(symbolToken.Value);
                    goto default;
                case Symbols.CurlyClose:
                case Symbols.CircleClose:
                case Symbols.SquareClose:
                    Symbols open = BracketPairs[symbolToken.Value];
                    if (brackets.Count == 0 || brackets[^1] != open)
                    {
                        errorer.Append("Closing bracket without an opening one", symbolToken.Location);
                        goto default;
                    }

                    brackets.RemoveAt(brackets.Count - 1);
                    goto default;

                default:
                    er.Read(out _);
                    tokens.Add(symbolToken);
                    break;
            }
        }
    }
}