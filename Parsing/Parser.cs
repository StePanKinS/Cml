using System.Diagnostics.CodeAnalysis;
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

    public void ParseDefinitions(string fileName)
        => ParseDefinitions(new Lexer(fileName));

    public void ParseDefinitions(Lexer l)
    {
        EnumerableReader<Token> er = l.GetReader();
        parseDefinitions(er, globalNamespace);
    }

    private Location parseDefinitions(EnumerableReader<Token> er, NamespaceDefinition nmsp)
    {
        // TODO: check for name collisions
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
        List<(Token<string> type, Token<string> name)> members = [];

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

            if (members.Select(m => m.name).Count() != 0)
                errorer.Append($"Struct member with this name `{memName.Value}` already exists", memName.Location);
            else
                members.Add((memType, memName));

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
        Location loc = parseDefinitions(er, namespaceDefinition);
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
        List<(Token<string>, Token<string>)> args = [];

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

            var argName = (Token<string>)token;

            args.Add((argType, argName));

            if (!er.Read(out token))
            {
                Location loc = new(typeName, argName);
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
            typeName,
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

    public void ParseCode()
    {
        setReferences(globalNamespace);
        parseCode(globalNamespace);
    }

    private void setReferences(NamespaceDefinition nmsp)
    {
        foreach (var d in nmsp)
        {
            if (d is NamespaceDefinition nmspDef)
            {
                setReferences(nmspDef);
                continue;
            }
            if (d is StructDefinition structDef)
            {
                setStructreferences(structDef);
                continue;
            }
            if (d is FunctionDefinition funcDef)
            {
                setFunctionReferences(funcDef);
                continue;
            }
            // TODO: variable definitions
            throw new Exception($"Unknown definition {d} in setReferences");
        }
    }

    private void setStructreferences(StructDefinition structDef)
    {
        var nmsp = (NamespaceDefinition)structDef.Parent;
        foreach (var m in structDef.Members)
        {
            if (!nmsp.TryGetType(m.TypeName.Value, out StructDefinition? def))
            {
                errorer.Append($"Can not resolve type name `{m.TypeName.Value}`", m.TypeName.Location);
                continue;
            }
            m.Type = def;
        }
    }

    private void setFunctionReferences(FunctionDefinition funcDef)
    {
        NamespaceDefinition nmsp = (NamespaceDefinition)funcDef.Parent;

        if (!nmsp.TryGetType(funcDef.ReturnTypeName.Value, out StructDefinition? type))
            errorer.Append($"Can not resolve type name `{funcDef.ReturnTypeName.Value}`", funcDef.ReturnTypeName.Location);
        else
            funcDef.ReturnType = type;

        funcDef.Args = new(nmsp.NameContext);
        foreach (var arg in funcDef.NotypeArgs)
        {
            Location loc = new(arg.type, arg.name);

            if (!nmsp.TryGetType(arg.type.Value, out type))
            {
                errorer.Append($"Can not resolve type name `{arg.type.Value}`", arg.type.Location);
                continue;
            }

            VariableDefinition varDef = new(arg.name.Value, type, funcDef, [], loc);
            if (!funcDef.Args.Append(varDef))
                errorer.Append($"Function argument with name `{varDef.Name}` already exists", arg.name.Location);
        }
    }

    private void parseCode(NamespaceDefinition nmsp)
    {
        foreach (var d in nmsp)
        {
            if (d is NamespaceDefinition nmspDef)
                parseCode(nmspDef);
            if (d is FunctionDefinition funcDef)
                parseCode(funcDef);
        }
    }

    private void parseCode(FunctionDefinition funcDef)
    {
        if (funcDef.Modifyers.Contains(Keywords.External) ||
            funcDef.Modifyers.Contains(Keywords.Export))
            return;

        funcDef.Code = parseInstruction(funcDef.UnparsedCode, funcDef.Args, funcDef);
    }

    private Executable parseInstruction(Token[] tokens, NameContext nameCtx, FunctionDefinition funcDef)
    {
        EnumerableReader<Token> er = tokens.GetReader();
        Token? token, t;
        if (!er.Peek(out token))
            throw new Exception("Empty instruction");
        if (token is Token<Symbols> symbolToken && symbolToken.Value == Symbols.CurlyOpen)
        {
            List<Executable> code = [];
            NameContext locals = new(nameCtx);

            er.Read(out _);
            while (true)
            {
                if (!er.Peek(out t))
                {
                    errorer.Append("Code block isnt closed", t!.Location);
                    break;
                }
                if (t is Token<Symbols> symToken && symToken.Value == Symbols.CurlyClose)
                {
                    er.Read(out _);
                    break;
                }
                Token[] instructionTokens = readUntilSymbol(er, Symbols.Semicolon);
                er.Read(out t);
                if (instructionTokens.Length == 0)
                {
                    Location loc = new(token, t!);
                    errorer.Append("Empty instruction", loc);
                    continue;
                }
                code.Add(parseInstruction(instructionTokens, locals, funcDef));
            }

            return new CodeBlock([.. code], locals, funcDef.ReturnType!, new(token!, t!));
        }

        Executable? ret = parseExpression(tokens, nameCtx, funcDef);
        if (ret == null)
        {
            nameCtx.TryGetType("void", out var vd);
            Location location = new(tokens[0], tokens[^1]);
            ret = new Nop(vd!, location);
            errorer.Append("Can not parse expression", location);
        }
        return ret;
    }

    private Executable? parseExpression(Token[] tokens, NameContext nameCtx, FunctionDefinition funcDef)
        => parseExpression(new Stack<Token>(tokens.Reverse()), nameCtx, funcDef, int.MinValue, false, false);

    private static readonly Dictionary<Symbols, UnaryOperationTypes> PrefixOperations = new()
    {
        { Symbols.Minus, UnaryOperationTypes.Minus },
        { Symbols.Star, UnaryOperationTypes.Dereference },
        { Symbols.Ampersand, UnaryOperationTypes.GetReference },
        { Symbols.ExclamationMark, UnaryOperationTypes.Inverse },
    };

    private static readonly Dictionary<Symbols, BinaryOperationTypes> BinaryOperations = new()
    {
        { Symbols.Equals, BinaryOperationTypes.Assign },
    };

    private static readonly Dictionary<BinaryOperationTypes, (int left, int right)> BinaryOpBPs = new()
    {
        { BinaryOperationTypes.Assign, (2, 1) },
    };

    private const int PrefixOpBP = 10;

    private Executable? parseExpression(Stack<Token> tokens, NameContext nameCtx, FunctionDefinition funcDef, int minBP, bool expectClosingPrant, bool expectComma)
    {
        Token? token, t;
        if (!tokens.TryPop(out token))
            return null;

        Executable? lhs = null;

        if (tryGetImmidiateValue(token, tokens, nameCtx, funcDef, out lhs))
        {
            if (lhs == null)
                return null;
        }
        else if (token.Type == TokenType.Symbol)
        {
            var symbolToken = (Token<Symbols>)token;
            if (symbolToken.Value == Symbols.CircleOpen)
            {
                if (!tokens.TryPeek(out t))
                    return null;
                if (t.Type == TokenType.Identifier &&
                    nameCtx.TryGetType(((Token<string>)t).Value, out var castType))
                {
                    tokens.Pop();
                    lhs = parseTypeCast(tokens, nameCtx, funcDef, expectClosingPrant, expectComma, castType, symbolToken.Location);
                }
                else
                {
                    lhs = parseExpression(tokens, nameCtx, funcDef, int.MinValue, true, false);
                    if (!tokens.TryPop(out t) || t.Type != TokenType.Symbol || ((Token<Symbols>)t).Value != Symbols.CircleClose)
                    {
                        // errorer.Append("Expected `)`", loc);
                        return null;
                    }
                }
            }
            else if (PrefixOperations.ContainsKey(symbolToken.Value))
                lhs = parseExpression(tokens, nameCtx, funcDef, PrefixOpBP, expectClosingPrant, expectComma);
            else
                return null;
        }

        if (lhs == null)
            return null;

        while (true)
        {
            if (!tokens.TryPeek(out token))
                return lhs;
            if (token.Type != TokenType.Symbol)
                return null;

            var symbolToken = (Token<Symbols>)token;
            if (symbolToken.Value == Symbols.CircleOpen)
            {
                lhs = parseFuncCall(lhs, tokens, nameCtx, funcDef);
                if (lhs == null)
                    return null;
            }
            else if (BinaryOperations.ContainsKey(symbolToken.Value))
            {
                var op = BinaryOperations[symbolToken.Value];
                // if (op == BinaryOperationTypes.Assign)
                // {
                //     if (lhs is Identifyer lhsIdent)
                //         returnType = lhsIdent.ReturnType;
                //     else if (lhs is UnaryOperation { OperationType: UnaryOperationTypes.Dereference } lhsDeref)
                //         returnType = lhsDeref.ReturnType;
                //     if (returnType == null)
                //     {
                //         // errorer.Append("Left side of assignment must be a variable or a dereference", lhs.Location);
                //         return null;
                //     }
                // }
                var (leftBP, rightBP) = BinaryOpBPs[op];
                if (leftBP <= minBP)
                    return lhs;
                tokens.Pop();
                Executable? rhs = parseExpression(tokens, nameCtx, funcDef, rightBP, expectClosingPrant,expectComma);
                if (rhs == null)
                    return null;

                StructDefinition? returnType = getBinaryOpReturnType(op, lhs.ReturnType, rhs.ReturnType);
                if (returnType == null)
                    return null;
                Location loc = new(lhs.Location, rhs.Location);

                lhs = new BinaryOperation(op, lhs, rhs, returnType, loc);
            }
            else if (expectClosingPrant && symbolToken.Value == Symbols.CircleClose)
            {
                // tokens.Pop();
                return lhs;
            }
            else if (expectComma && symbolToken.Value == Symbols.Comma)
            {
                tokens.Pop();
                return lhs;
            }
            else
                return null;
        }
    }

    private StructDefinition? getBinaryOpReturnType(BinaryOperationTypes op, StructDefinition left, StructDefinition right)
    {
        return left;
    }

    private Executable? parseFuncCall(Executable lhs, Stack<Token> tokens, NameContext nameCtx, FunctionDefinition funcDef)
    {
        tokens.Pop();
        if (lhs.ReturnType is not FunctionPointer funcPtr)
        {
            // errorer.Append("Can not call non-function value", lhs.Location);
            return null;
        }
        Location loc = funcPtr.Location;
        List<Executable> args = [];
        while (true)
        {
            if (!tokens.TryPeek(out var token))
                return null;
            if (token.Type == TokenType.Symbol && ((Token<Symbols>)token).Value == Symbols.CircleClose)
            {
                loc = new(loc, token.Location);
                tokens.Pop();
                break;
            }
            Executable? e = parseExpression(tokens, nameCtx, funcDef, 0, true, true);
            if (args.Count == funcPtr.Args.Length)
                return null;
            if (e == null || e.ReturnType != funcPtr.Args[args.Count])
                return null;
            args.Add(e);
        }
        return new FunctionCall(lhs, [.. args], funcPtr.ReturnType, loc);
    }

    private bool tryGetImmidiateValue(Token token, Stack<Token> tokens, NameContext nameCtx, FunctionDefinition funcDef, out Executable? executable)
    {
        executable = null;
        if (token.Type == TokenType.Literal)
        {
            executable = getLiteralExecutable(token, nameCtx);
            return true;
        }
        if (token.Type != TokenType.Identifier)
            return false;

        var ident = (Token<string>)token;
        Definition? def;
        if (!nameCtx.TryGetName(ident.Value, out def))
            return false;

        if (def is StructDefinition)
        {
            Location location = token.Location;
            nameCtx.TryGetType(((Token<string>)token).Value, out var type);
            if (!tokens.TryPop(out token!))
                return true;
            if (token.Type != TokenType.Identifier)
                return true;

            location = new(location, token.Location);
            VariableDefinition varDef = new(((Token<string>)token).Value, type!, funcDef, [], location);
            if (!nameCtx.Append(varDef))
                return true;
            executable = new Identifyer(varDef, varDef.ValueType, token.Location);
            return true;
        }

        StructDefinition? retType;
        if (def is FunctionDefinition functionDef)
            retType = new FunctionPointer(functionDef);
        else if (def is VariableDefinition varDef)
            retType = varDef.ValueType;
        else
            return false;

        executable = new Identifyer(def, retType, token.Location);
        return true;
    }

    private Executable? parseTypeCast(Stack<Token> tokens, NameContext nameCtx, FunctionDefinition funcDef, bool expectClosingPrant, bool expectComma, StructDefinition type, Location startLoc)
    {
        int ptrCnt = 0;
        Token? token;

        while (true)
        {
            if (!tokens.TryPop(out token))
                return null;
            if (token is Token<Symbols> symbolToken)
            {
                if (symbolToken.Value == Symbols.Star)
                {
                    ptrCnt++;
                    continue;
                }
                if (symbolToken.Value != Symbols.CurlyClose)
                    return null;
                break;
            }
            return null;
        }

        for (int i = 0; i < ptrCnt; i++)
        {
            type = new Pointer(type);
        }

        Executable? e = parseExpression(tokens, nameCtx, funcDef, PrefixOpBP, expectClosingPrant, expectComma);
        if (e == null)
            return null;
        Location loc = new(startLoc, e.Location);

        return new UnaryOperation(UnaryOperationTypes.Cast, e, type, loc);
    }

    private Executable getLiteralExecutable(Token token, NameContext nameCtx)
    {
        StructDefinition? litType = null;
        if (token is Token<char> charToken)
        {
            nameCtx.TryGetType("char", out litType);
            return new Literal<char>(charToken.Value, litType!, token.Location);
        }
        else if (token is Token<string> stringToken)
        {
            nameCtx.TryGetType("char", out litType);
            litType = new Pointer(litType!);
            return new Literal<string>(stringToken.Value, litType!, token.Location);
        }
        else if (token is Token<ulong> intToken)
        {
            nameCtx.TryGetType("int", out litType);
            return new Literal<ulong>(intToken.Value, litType!, token.Location);
        }

        throw new Exception($"Unknown literal type {token}");
    }
}