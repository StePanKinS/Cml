using Cml.Lexing;
using System.Diagnostics;

namespace Cml.Parsing;

internal static class Parser
{
    public static ParsedFile Process(List<Token> tokens)
    {
        List<Import> imports = [];
        List<Definition> definitions = [];

        int i = 0;
        try
        {
            while (i < tokens.Count)
            {
                Token token = tokens[i++];
                if (token is KeywordToken keywordToken)
                {
                    if (keywordToken.Value == Keywords.Import)
                    {
                        readImport(keywordToken, ref i);
                        continue;
                    }
                    else if (keywordToken.Value == Keywords.Struct)
                    {
                        readStruct(keywordToken, ref i);
                        continue;
                    }
                }
                else if (token is NameToken typeName)
                {
                    readFunction(typeName, ref i);
                    continue;
                }

                Util.Exit(token, $"Expected import, struct or function definition, not {token}");
                throw new UnreachableException();
            }
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine("Something went wrong(");
            Environment.Exit(1);
            return null;
        }

        return new ParsedFile(tokens[0].Location.File, definitions, imports);

        void readImport(KeywordToken keywordToken, ref int i)
        {
            Token token = tokens[i++];
            if (token is not NameToken fileName)
            {
                Util.Exit(token, $"Expected file name, not {token}");
                throw new UnreachableException();
            }

            token = tokens[i++];
            if (token is not SymbolToken semicolonToken || semicolonToken.Value != Symbols.Semicolon)
            {
                Util.Exit(token, $"Expected semicolon, not {token}");
                throw new UnreachableException();
            }

            Location location = new(fileName.Value,
                keywordToken.Location.StartLine, semicolonToken.Location.EndLine,
                keywordToken.Location.StartColumn, semicolonToken.Location.EndColumn);

            Import import = new(fileName.Value, keywordToken.Location, location);

            imports.Add(import);
        }

        void readStruct(KeywordToken keywordToken, ref int i)
        {
            Token token = tokens[i++];
            if (token is not NameToken structName)
            {
                Util.Exit(token, $"Expected struct name, not {token}");
                throw new UnreachableException();
            }

            token = tokens[i++];
            if (token is not SymbolToken CurlyOpenSymbol || CurlyOpenSymbol.Value != Symbols.CurlyOpen)
            {
                Util.Exit(token, $"Expected `{'{'}`, not {token}");
                throw new UnreachableException();
            }

            List<NameTypeTypeName> members = [];
            SymbolToken curlyCloseToken;

            while (true)
            {
                token = tokens[i];

                if (token is SymbolToken curlyClose && curlyClose.Value == Symbols.CurlyClose)
                {
                    curlyCloseToken = curlyClose;
                    break;
                }

                NameToken typeName = readTypeName(tokens, ref i);

                token = tokens[i++];
                if (token is not NameToken memberName)
                {
                    Util.Exit(token, $"Expected member name, not {token}");
                    throw new UnreachableException();
                }

                NameTypeTypeName nttn = new(memberName, typeName);
                members.Add(nttn);

                token = tokens[i++];

                if (token is SymbolToken symbolToken)
                {
                    if (symbolToken.Value == Symbols.Semicolon)
                        continue;

                    if (symbolToken.Value == Symbols.CurlyClose)
                    {
                        curlyCloseToken = symbolToken;
                        break;
                    }
                }

                Util.Exit(token, $"Expected `;` or `{'}'}`, not {token}");
                throw new UnreachableException();
            }

            Location location = new(keywordToken.Location.File,
                keywordToken.Location.StartLine, curlyCloseToken.Location.EndLine,
                keywordToken.Location.StartColumn, curlyCloseToken.Location.EndColumn);

            StructDefinition structDefinition = new(structName.Value, members, location);
            definitions.Add(structDefinition);
        }

        void readFunction(NameToken typeName, ref int i)
        {
            Token token = tokens[i++];
            if (token is not NameToken funcName)
            {
                Util.Exit(token, $"Expected function name, not {token}");
                throw new UnreachableException();
            }

            token = tokens[i++];
            if (token is not SymbolToken circleOpen || circleOpen.Value != Symbols.CircleOpen)
            {
                Util.Exit(token, $"Expected `(`, not {token}");
                throw new UnreachableException();
            }

            List<NameTypeTypeName> args = [];
            SymbolToken circleCloseToken;

            while (true)
            {
                token = tokens[i];

                if (token is SymbolToken circleClose && circleClose.Value == Symbols.CircleClose)
                {
                    i++;
                    circleCloseToken = circleClose;
                    break;
                }

                NameToken argType = readTypeName(tokens, ref i);

                token = tokens[i++];
                if (token is not NameToken memberName)
                {
                    Util.Exit(token, $"Expected member name, not {token}");
                    throw new UnreachableException();
                }

                NameTypeTypeName nttn = new(memberName, argType);
                args.Add(nttn);

                token = tokens[i++];

                if (token is SymbolToken symbolToken)
                {
                    if (symbolToken.Value == Symbols.Comma)
                        continue;

                    if (symbolToken.Value == Symbols.CircleClose)
                    {
                        circleCloseToken = symbolToken;
                        break;
                    }
                }

                Util.Exit(token, $"Expected `,` or ), not {token}");
                throw new UnreachableException();
            }

            Executable code = parseInstruction(tokens, ref i);

            Location location = new(typeName.Location.File,
                typeName.Location.StartLine, circleCloseToken.Location.EndLine,
                typeName.Location.StartColumn, circleCloseToken.Location.EndColumn);

            FunctionDefinition func = new(funcName.Value, typeName.Value, args, code, location);

            definitions.Add(func);
        }
    }

    private static NameToken readTypeName(List<Token> tokens, ref int i)
    {
        Token token = tokens[i++];
        if (token is not NameToken typeName)
        {
            Util.Exit(token, $"Expected type name, not {token}");
            throw new UnreachableException();
        }

        int starCnt = 0;
        while (true)
        {
            token = tokens[i];
            if (token is not SymbolToken symbolToken || symbolToken.Value != Symbols.Star)
                break;
            i++;
            starCnt++;
        }

        for (int j = 0; j < starCnt; j++)
        {
            typeName.Value += '*';
        }

        Location location = new(
            typeName.Location.File,
            typeName.Location.StartLine,
            token.Location.EndLine,
            typeName.Location.StartColumn,
            token.Location.EndColumn
        );

        typeName.Location = location;
        return typeName;
    }

    private static Executable parseInstruction(List<Token> tokens, ref int i)
    {
        int start = i;
        int end;
        Token token = tokens[i++];
        if (token is SymbolToken curlyOpen && curlyOpen.Value == Symbols.CurlyOpen)
        {
            List<Executable> code = [];
            SymbolToken curlyClose;

            while (tokens[i] is not SymbolToken curlyCloseToken || (curlyClose = curlyCloseToken).Value != Symbols.CurlyClose)
            {
                code.Add(parseInstruction(tokens, ref i));
            }
            i++;

            Location location = new(
                curlyOpen.Location.File,
                curlyOpen.Location.StartLine,
                curlyOpen.Location.EndLine,
                curlyClose.Location.EndLine,
                curlyClose.Location.EndColumn
            );

            return new CodeBlock(code, location);
        }

        while (true)
        {
            if (token is SymbolToken semicolonSymbol && semicolonSymbol.Value == Symbols.Semicolon)
            {
                end = i;
                break;
            }
            token = tokens[i++];
        }

        return parseExprssion(tokens, start, end);
    }

    private static Executable parseExprssion(List<Token> tokens, int start, int end)
    {
        return null;
    }
}