using System.Diagnostics;
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
        EnumerableReader<Token> er = l.GetTokens().GetReader();
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

            if (members.Where(m => m.name.Value == memName.Value).Any())
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

        FunctionDefinition fd = new(
            funcName.Value,
            typeName,
            nmsp,
            modifyers
        );

        Token t = token;
        FunctionArguments args = new(nmsp.NameContext, fd);

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

            if (!args.Append(argType, argName))
            {
                errorer.Append($"Function argument with name `{argName.Value}` already exists", argName.Location);
                return;
            }

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

        fd.Arguments = args;
        fd.UnparsedCode = body;
        fd.Location = location;

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

        if (errorer.Count != 0)
            return;

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
        List<StructType.StructMember> members = [];
        foreach (var m in structDef.Members)
        {
            if (!nmsp.TryGetType(m.type.Value, out Typ? type))
            {
                errorer.Append($"Can not resolve type name `{m.type.Value}`", m.type.Location);
                continue;
            }
            members.Add(new (type, m.type.Value));
        }
        structDef.Type = new StructType(structDef.Name, [.. members]);
    }

    private void setFunctionReferences(FunctionDefinition funcDef)
    {
        NamespaceDefinition nmsp = (NamespaceDefinition)funcDef.Parent;

        if (!nmsp.TryGetType(funcDef.ReturnTypeName.Value, out Typ? type))
            errorer.Append($"Can not resolve type name `{funcDef.ReturnTypeName.Value}`", 
                funcDef.ReturnTypeName.Location);
        else
            funcDef.ReturnType = type;

        foreach (var arg in funcDef.Arguments.Variables)
        {
            if (!nmsp.TryGetType(arg.TypeName!, out type))
            {
                errorer.Append($"Can not resolve type name `{arg.TypeName}`", arg.Location);
                continue;
            }

            arg.Type = type;
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
        if (funcDef.Modifyers.Contains(Keywords.External)
            // || funcDef.Modifyers.Contains(Keywords.Export)
            )
            return;
        EnumerableReader<Token> er = funcDef.UnparsedCode.GetReader();
        funcDef.Code = parseInstruction(er, funcDef.Arguments, funcDef, out var returnType);

        if (er.Read())
            throw new Exception("Not all code is parsed");

        funcDef.ContainsReturn = returnType != null;

        if (funcDef.ReturnType != DefaultType.Void && funcDef.ReturnType != returnType)
            errorer.Append($"Function return type `{funcDef.ReturnType.Name}` does not match actual return type `{returnType?.Name}`", funcDef.Location);
    }

    private Executable parseInstruction(EnumerableReader<Token> tokens, INameContainer nameCtx, FunctionDefinition funcDef, out Typ? returnType)
    {
        returnType = null;

        Token? token, t;
        if (!tokens.Peek(out token))
            throw new Exception("Empty instruction");
        if (token is Token<Symbols> symbolToken && symbolToken.Value == Symbols.CurlyOpen)
        {
            List<Executable> code = [];
            LocalVariables locals = new(nameCtx);

            tokens.Read();
            while (true)
            {
                if (!tokens.Peek(out t))
                {
                    errorer.Append("Code block isnt closed", t!.Location);
                    break;
                }
                if (t is Token<Symbols> symToken && symToken.Value == Symbols.CurlyClose)
                {
                    tokens.Read();
                    break;
                }
                code.Add(parseInstruction(tokens, locals, funcDef, out var innerRT));

                if (innerRT != null && returnType == null)
                    returnType = innerRT;
            }

            return new CodeBlock([.. code], locals, new(token!, t!));
        }
        else if (token is Token<Keywords> kwdToken)
        {
            tokens.Read();

            if (kwdToken.Value == Keywords.Return)
            {
                Executable? e = parseExpression(tokens, nameCtx, funcDef);
                if (e == null)
                {
                    errorer.Append("Can not parse expression", kwdToken.Location);
                    returnType = DefaultType.Void;
                    return new Return(new Nop(kwdToken.Location), kwdToken.Location);
                }

                if (e.ReturnType != funcDef.ReturnType)
                {
                    var promoted = promoteToType(e, funcDef.ReturnType);
                    if (promoted == null)
                        errorer.Append($"Function return type `{funcDef.ReturnType}` does not match actual return type `{e.ReturnType}`", e.Location);
                    else
                        e = promoted;
                }

                returnType = e.ReturnType;
                return new Return(e, new Location(kwdToken.Location, e.Location));
            }
            else if (kwdToken.Value == Keywords.While)
            {
                if (!tokens.Read(out token)
                    || token.Type != TokenType.Symbol
                    || ((Token<Symbols>)token).Value != Symbols.CircleOpen)
                {
                    errorer.Append("Expected start of condition expression `(`", token!.Location);
                    return new Nop(kwdToken.Location);
                }

                Executable? cond = parseExpression(tokens, nameCtx, funcDef, int.MinValue, [Symbols.CircleClose], false);
                if (cond == null)
                {
                    errorer.Append("Con not parse condition", kwdToken.Location);
                    cond = new Nop(kwdToken.Location);
                }
                else if (cond.ReturnType != DefaultType.Bool)
                    errorer.Append($"Expected bool type, not {cond.ReturnType}", cond.Location);
                    
                tokens.Read();

                Executable body = parseInstruction(tokens, nameCtx, funcDef, out var _);
                // if (innerRT != null && returnType == null) // body can be not executed
                //     returnType = innerRT;

                return new WhileLoop(cond, body, new Location(kwdToken.Location, body.Location));
            }
            else if (kwdToken.Value == Keywords.If)
            {
                if (!tokens.Read(out token)
                    || token.Type != TokenType.Symbol
                    || ((Token<Symbols>)token).Value != Symbols.CircleOpen)
                {
                    errorer.Append("Expected start of condition expression `(`", token!.Location);
                    return new Nop(kwdToken.Location);
                }

                Executable? cond = parseExpression(tokens, nameCtx, funcDef, int.MinValue, [Symbols.CircleClose], false);
                if (cond == null)
                {
                    errorer.Append("Con not parse condition", kwdToken.Location);
                    cond = new Nop(kwdToken.Location);
                }
                else if (cond.ReturnType != DefaultType.Bool)
                {
                    errorer.Append($"Expected bool type, not {cond.ReturnType}", cond.Location);
                }
                tokens.Read();

                Executable body = parseInstruction(tokens, nameCtx, funcDef, out var bodyRT);
                Executable? elseBody = null;
                Location location = new(kwdToken.Location, body.Location);

                if (tokens.Peek(out token)
                    && token.Type == TokenType.Keyword
                    && ((Token<Keywords>)token).Value == Keywords.Else)
                {
                    tokens.Read();
                    elseBody = parseInstruction(tokens, nameCtx, funcDef, out var elseRT);
                    location = new Location(location, elseBody.Location);
                    if (bodyRT != null && elseRT != null)
                    {
                        if (bodyRT != elseRT)
                            return new Nop(location);
                        returnType = bodyRT;
                    }
                }

                return new ControlFlow(cond, body, elseBody, location);
            }

            errorer.Append($"Unexpected keyword `{kwdToken.Value.ToString().ToLower()}`", kwdToken.Location);
            return new Nop(kwdToken.Location);
        }

        Executable? ret = parseExpression(tokens, nameCtx, funcDef);
        if (ret == null)
        {
            tokens.Peek(out t);
            Location location = new(token, t!);
            ret = new Nop(location);
            errorer.Append("Can not parse expression", location);
            while (true)
            {
                if (!tokens.Peek(out token))
                    break;

                if (token.Type == TokenType.Symbol)
                {
                    var val = ((Token<Symbols>)token).Value;
                    if (val == Symbols.Semicolon)
                    {
                        tokens.Read();
                        break;
                    }
                    if (val == Symbols.CurlyClose)
                        break;
                }

                tokens.Read();
            }
        }
        return ret;
    }

    private Executable? parseExpression(EnumerableReader<Token> tokens, INameContainer nameCtx, FunctionDefinition funcDef)
        => parseExpression(tokens, nameCtx, funcDef, int.MinValue, [Symbols.Semicolon], true);

    private static readonly Dictionary<Symbols, UnaryOperationTypes> PrefixOperations = new()
    {
        { Symbols.Minus,           UnaryOperationTypes.Minus        },
        { Symbols.Star,            UnaryOperationTypes.Dereference  },
        { Symbols.Ampersand,       UnaryOperationTypes.GetReference },
        { Symbols.ExclamationMark, UnaryOperationTypes.Inverse      },
    };

    private static readonly Dictionary<Symbols, BinaryOperationTypes> BinaryOperations = new()
    {
        { Symbols.Equals,                 BinaryOperationTypes.Assign        },
   
        { Symbols.VerticalbarVerticalbar, BinaryOperationTypes.LogicalOr     },
        { Symbols.AmpersandAmpersand,     BinaryOperationTypes.LogicalAnd    },
   
        { Symbols.Verticalbar,            BinaryOperationTypes.BitwiseOr     },
        { Symbols.VerticalArrow,          BinaryOperationTypes.BitwiseXor    },
        { Symbols.Ampersand,              BinaryOperationTypes.BitwiseAnd    },

        { Symbols.IsEquals,               BinaryOperationTypes.IsEquals      },
        { Symbols.NotEquals,              BinaryOperationTypes.NotEquals     },
         
        { Symbols.Less,                   BinaryOperationTypes.Less          },
        { Symbols.LessEquals,             BinaryOperationTypes.LessEquals    },
        { Symbols.Greater,                BinaryOperationTypes.Greater       },
        { Symbols.GreaterEquals,          BinaryOperationTypes.GreaterEquals },

        { Symbols.RightShift,             BinaryOperationTypes.RightShift    },
        { Symbols.LeftShift,              BinaryOperationTypes.LeftShift     },
         
        { Symbols.Plus,                   BinaryOperationTypes.Add           },
        { Symbols.Minus,                  BinaryOperationTypes.Subtract      },

        { Symbols.Slash,                  BinaryOperationTypes.Divide        },
        { Symbols.Percent,                BinaryOperationTypes.Remainder     },
        { Symbols.Star,                   BinaryOperationTypes.Multiply      },
    };

    private static readonly Dictionary<BinaryOperationTypes, (int left, int right)> BinaryOpBPs = new()
    {
        { BinaryOperationTypes.Assign,        (1,   0) },
   
        { BinaryOperationTypes.LogicalOr,     (2,   3) },
        { BinaryOperationTypes.LogicalAnd,    (4,   5) },
   
        { BinaryOperationTypes.BitwiseOr,     (6,   7) },
        { BinaryOperationTypes.BitwiseXor,    (8,   9) },
        { BinaryOperationTypes.BitwiseAnd,    (10, 11) },

        { BinaryOperationTypes.IsEquals,      (12, 13) },
        { BinaryOperationTypes.NotEquals,     (12, 13) },
         
        { BinaryOperationTypes.Less,          (14, 15) },
        { BinaryOperationTypes.LessEquals,    (14, 15) },
        { BinaryOperationTypes.Greater,       (14, 15) },
        { BinaryOperationTypes.GreaterEquals, (14, 15) },

        { BinaryOperationTypes.RightShift,    (16, 17) },
        { BinaryOperationTypes.LeftShift,     (16, 17) },

        { BinaryOperationTypes.Add,           (20, 21) },
        { BinaryOperationTypes.Subtract,      (20, 21) },

        { BinaryOperationTypes.Divide,        (22, 23) },
        { BinaryOperationTypes.Remainder,     (22, 23) },
        { BinaryOperationTypes.Multiply,      (22, 23) },
    };

    private const int PrefixOpBP = 100;

    private Executable? parseExpression(
        EnumerableReader<Token> tokens,
        INameContainer nameCtx,
        FunctionDefinition funcDef,
        int minBP,
        IEnumerable<Symbols> endSymbols,
        bool consumeEndSymbol
    )
    {
        Token? token, t;
        if (!tokens.Read(out token))
            return null;

        Executable? lhs;

        if (token.Type == TokenType.Unknown)
        {
            errorer.Append(((Token<string>)token).Value, token.Location);
            return null;
        }
        else if (tryGetImmidiateValue(token, tokens, nameCtx, funcDef, endSymbols, out lhs))
        {
            if (lhs == null)
                return null;
        }
        else if (token.Type == TokenType.Symbol)
        {
            var symbolToken = (Token<Symbols>)token;
            if (symbolToken.Value == Symbols.Semicolon
                && endSymbols.Contains(Symbols.Semicolon))
                return new Nop(symbolToken.Location);
            else if (symbolToken.Value == Symbols.CircleOpen)
            {
                if (!tokens.Peek(out t))
                {
                    errorer.Append("Unexpected file end", t!.Location);
                    return null;
                }
                if (t.Type == TokenType.Identifier &&
                    nameCtx.TryGetType(((Token<string>)t).Value, out var castType))
                {
                    tokens.Read();
                    lhs = parseTypeCast(tokens, nameCtx, funcDef, endSymbols, castType, symbolToken.Location);
                }
                else
                {
                    lhs = parseExpression(tokens, nameCtx, funcDef, int.MinValue, [Symbols.CircleClose], false);
                    if (!tokens.Read(out t) || t.Type != TokenType.Symbol || ((Token<Symbols>)t).Value != Symbols.CircleClose)
                    {
                        errorer.Append("Closing bracket `)` is not found", new Location(symbolToken, t!));
                        return null;
                    }
                }
            }
            else if (PrefixOperations.TryGetValue(symbolToken.Value, out UnaryOperationTypes value))
            {
                Executable? rhs = parseExpression(tokens, nameCtx, funcDef, PrefixOpBP, endSymbols, false);
                if (rhs == null)
                    return null;
                Typ? retType = getPrefixOperationReturnType(value, rhs);
                if (retType == null)
                    return null;
                lhs = new UnaryOperation(value, rhs, retType, new Location(symbolToken.Location, rhs.Location));
            }
            else
            {
                errorer.Append($"Unexpected symbol {symbolToken}", symbolToken.Location);
                return null;
            }
        }
        else
        {
            errorer.Append($"Unexpected token {token}", token.Location);
            return null;
        }

        if (lhs == null)
            return null;

        while (true)
        {
            if (!tokens.Peek(out token))
                return lhs;
            if (token.Type != TokenType.Symbol)
            {
                errorer.Append($"Expected symbol. `{token}` is found", token.Location);
                return null;
            }

            var symbolToken = (Token<Symbols>)token;
            if (symbolToken.Value == Symbols.CircleOpen)
            {
                lhs = parseFuncCall(lhs, tokens, nameCtx, funcDef);
                if (lhs == null)
                    return null;
            }
            else if (symbolToken.Value == Symbols.Dot)
            {
                lhs = parseDot(lhs, tokens, nameCtx, funcDef);
                if (lhs == null)
                    return null;
            }
            else if (BinaryOperations.TryGetValue(symbolToken.Value, out BinaryOperationTypes op))
            {
                var (leftBP, rightBP) = BinaryOpBPs[op];
                if (leftBP <= minBP)
                    return lhs;

                tokens.Read();
                Executable? rhs = parseExpression(tokens, nameCtx, funcDef, rightBP, endSymbols, false);
                if (rhs == null)
                    return null;

                lhs = verifyBinaryOperation(lhs, op, rhs);
                if (lhs == null)
                    return null;
            }
            else if (endSymbols.Contains(symbolToken.Value))
            {
                if (consumeEndSymbol)
                    tokens.Read();
                return lhs;
            }
            else
            {
                errorer.Append($"Expected symbol of binary operation, end symbol or `(` for function call. `{token}` is found", token.Location);
                return null;
            }
        }
    }

    private Typ? getPrefixOperationReturnType(UnaryOperationTypes op, Executable operand)
    {
        switch (op)
        {
            case UnaryOperationTypes.Cast:
                throw new UnreachableException("Cast is special case");
            case UnaryOperationTypes.Dereference:
                if (operand.ReturnType is not Pointer ptr)
                {
                    errorer.Append($"Dereferencing is applicable only to pointers, not `{operand.ReturnType}`", operand.Location);
                    return null;
                }
                return ptr.PointsTo;
            case UnaryOperationTypes.GetReference:
                if (operand is not Identifyer ident)
                {
                    errorer.Append("Can take reference only from identifier", operand.Location);
                    return null;
                }
                return new Pointer(ident.ReturnType);
            case UnaryOperationTypes.Inverse:
                if (operand.ReturnType != DefaultType.Bool)
                {
                    errorer.Append($"Type missmatch: expected bool, given {operand.ReturnType}", operand.Location);
                    return null;
                }
                return DefaultType.Bool;
            case UnaryOperationTypes.Minus:
                if (operand.ReturnType is not DefaultType.Integer
                    && operand.ReturnType is not DefaultType.FloatingPoint)
                {
                    errorer.Append($"Inverse expects integer or floating point type, not {operand.ReturnType}", operand.Location);
                    return null;
                }
                return operand.ReturnType;
            default:
                throw new Exception($"Unknown prefix operation {op}");
        }
    }

    private Executable? verifyBinaryOperation(Executable left, BinaryOperationTypes op, Executable right)
    {
        Typ? returnType;
        switch (op)
        {
            case BinaryOperationTypes.Assign:
                Typ targetType;
                if (left is Identifyer lhsIdent
                    && lhsIdent.Definition is VariableDefinition varDef)
                    targetType = varDef.Type;
                else if (left is GetMember gm)
                    targetType = gm.ReturnType;
                else
                {
                    errorer.Append($"Unexpected lhs {left}", left.Location);
                    return null;
                }
                
                if (targetType != right.ReturnType)
                {
                    var promoted = promoteToType(right, targetType);
                    if (promoted == null)
                    {
                        errorer.Append($"Type missmatch: expected {targetType}, got {right.ReturnType}", right.Location);
                        return null;
                    }
                    right = promoted;
                }

                returnType = targetType;
                break;
                
            case BinaryOperationTypes.Add:
            case BinaryOperationTypes.Subtract:
            case BinaryOperationTypes.Multiply:
            case BinaryOperationTypes.Divide:
                var t = getLeastCommonType(left.ReturnType, right.ReturnType);
                var l = promoteToType(left, t);
                var r = promoteToType(right, t);
                if (l == null || r == null)
                {
                    errorer.Append($"Cant do math with types {left.ReturnType} and {right.ReturnType}", new Location(left.Location, right.Location));
                    return null;
                }
                left = l;
                right = r;
                returnType = t!;
                break;

            case BinaryOperationTypes.Remainder:
                if (left.ReturnType is not DefaultType.Integer li
                    || right.ReturnType is not DefaultType.Integer ri)
                {
                    errorer.Append("Remainder operation can be applyed to only integer types", new Location(left.Location, right.Location));
                    return null;
                }
                int size = Math.Max(li.Size, ri.Size);
                if (li.Size != size)
                    left = promoteToType(left, DefaultType.Integer.GetObject(size, li.IsSigned))!;
                if (ri.Size != size)
                    right = promoteToType(right, DefaultType.Integer.GetObject(size, ri.IsSigned))!;
                returnType = li;
                break;
                
            case BinaryOperationTypes.BitwiseAnd:
            case BinaryOperationTypes.BitwiseOr:
            case BinaryOperationTypes.BitwiseXor:
                if (left.ReturnType is not DefaultType.Integer lint
                    || right.ReturnType is not DefaultType.Integer rint
                    || lint != rint)
                {
                    errorer.Append("Bitwise operations can be applyed only to same integer types", new Location(left.Location, right.Location));
                    return null;
                }
                
                returnType = lint;
                break;

            case BinaryOperationTypes.Less:
            case BinaryOperationTypes.LessEquals:
            case BinaryOperationTypes.Greater:
            case BinaryOperationTypes.GreaterEquals:
            case BinaryOperationTypes.IsEquals:
            case BinaryOperationTypes.NotEquals:
                var type = getLeastCommonType(left.ReturnType, right.ReturnType);
                var le = promoteToType(left, type);
                var re = promoteToType(right, type);
                if (le == null || re == null)
                {
                    errorer.Append($"Cant compare types {left.ReturnType} and {right.ReturnType}", new Location(left.Location, right.Location));
                    return null;
                }
                left = le;
                right = re;
                returnType = DefaultType.Bool;
                break;

            default:
                throw new Exception($"Binary operation {op} not implemented in verifyBinaryOperation");
        }
        return new BinaryOperation(op, left, right, returnType, new Location(left.Location, right.Location));
    }

    private Typ? getLeastCommonType(Typ a, Typ b)
    {
        if (a is DefaultType.Integer inta
            && b is DefaultType.Integer intb)
        {
            if (a == DefaultType.Integer.Lit)
                return b;
            if (b == DefaultType.Integer.Lit)
                return a;

            if (inta.IsSigned != intb.IsSigned)
                return null;

            return inta.Size >= intb.Size ? inta : intb;
        }

        if (a is DefaultType.FloatingPoint fa
            && b is DefaultType.FloatingPoint fb)
            return fa.Size > fb.Size ? fa : fb;

        if (a is DefaultType.FloatingPoint
            && b is DefaultType.Integer)
            return a;

        if (a is DefaultType.Integer
            && b is DefaultType.FloatingPoint)
            return b;

        return null;
    }

    private Executable? promoteToType(Executable e, Typ? targetType)
    {
        if (targetType == null)
            return null;
        if (e.ReturnType == targetType)
            return e;

        bool allowed = false;

        if (e.ReturnType is DefaultType.Integer eint
            && targetType is DefaultType.Integer tint
            && (eint.IsSigned == tint.IsSigned
                && eint.Size < tint.Size
                || eint == DefaultType.Integer.Lit))
            allowed = true;

        if (e.ReturnType is DefaultType.Integer
            && targetType is DefaultType.FloatingPoint)
            allowed = true;

        if (e.ReturnType is DefaultType.FloatingPoint ef
            && targetType is DefaultType.FloatingPoint tf
            && tf.Size >= ef.Size)
            allowed = true;
        
        if (allowed)
            return new UnaryOperation(UnaryOperationTypes.Cast, e, targetType, e.Location);
        else
            return null;
    }

    private Executable? parseDot(Executable lhs, EnumerableReader<Token> tokens, INameContainer nameCtx, FunctionDefinition funcDef)
    {
        tokens.Read();
        if (!tokens.Read(out var token) || token.Type != TokenType.Identifier)
        {
            errorer.Append($"Expected member name, not {token}", token!.Location);
            return null;
        }
        var nameToken = (Token<string>)token;
        Typ retType;
        if (lhs is Identifyer ident 
            && ident.Definition is NamespaceDefinition nmsp)
        {
            if (!nmsp.TryGetName(nameToken.Value, out Definition? def))
            {
                errorer.Append($"Namespace `{nmsp.FullName}` does not contain definition for `{nameToken.Value}`", nameToken.Location);
                return null;
            }
            retType = def switch
            {
                VariableDefinition varDef => varDef.Type,
                FunctionDefinition fnDef => new FunctionPointer(fnDef),
                StructDefinition typeDef => typeDef.Type,
                NamespaceDefinition => DefaultType.Void,
                _ => throw new Exception($"Unsupported definition {def.GetType().Name}"),
            };
        }
        else 
        {
            if (lhs.ReturnType is not StructType st)
            {
                errorer.Append($"Expected composite type, not {lhs.ReturnType}", lhs.Location);
                return null;
            }
            var member = st.GetStructMember(nameToken.Value);
            if (member == null)
            {
                errorer.Append($"Member with name {nameToken.Value} doesnt exist in type {lhs.ReturnType}", nameToken.Location);
                return null;
            }
            retType = member.Type;
        }

        return new GetMember(lhs, nameToken, retType, new Location(lhs.Location, nameToken.Location));
    }

    private Executable? parseFuncCall(Executable lhs, EnumerableReader<Token> tokens, INameContainer nameCtx, FunctionDefinition funcDef)
    {
        tokens.Read();
        if (lhs.ReturnType is not FunctionPointer funcPtr)
        {
            errorer.Append("Can not call non-function value", lhs.Location);
            return null;
        }
        Location loc = lhs.Location;
        List<Executable> args = [];
        while (true)
        {
            if (!tokens.Peek(out var token))
            {
                errorer.Append("Unexpected end of file", token!.Location);
                return null;
            }
            if (token.Type == TokenType.Symbol && ((Token<Symbols>)token).Value == Symbols.CircleClose)
            {
                loc = new(loc, token.Location);
                tokens.Read();
                break;
            }
            Executable? e = parseExpression(tokens, nameCtx, funcDef, 0, [Symbols.CircleClose, Symbols.Comma], false);
            if (e == null)
                return null;
                
            if (args.Count == funcPtr.Args.Length)
            {
                errorer.Append($"Too many arguments provided. {funcPtr.Args.Length} expected", lhs.Location);
                return null;
            }

            Executable? exe = promoteToType(e, funcPtr.Args[args.Count]);
            if (exe == null)
            {
                errorer.Append($"Type missmatch: {e.ReturnType}, {funcPtr.Args[args.Count]}", e.Location);
                return null;
            }
            args.Add(exe);

            if (!tokens.Peek(out token))
            {
                errorer.Append("Unexpected end of file", token!.Location);
                return null;
            }
            if (token.Type == TokenType.Symbol && ((Token<Symbols>)token).Value == Symbols.Comma)
                tokens.Read();
        }
        return new FunctionCall(lhs, [.. args], funcPtr.ReturnType, loc);
    }

    private bool tryGetImmidiateValue(Token token, EnumerableReader<Token> tokens, INameContainer nameCtx, FunctionDefinition funcDef, IEnumerable<Symbols> endSymbols, out Executable? executable)
    {
        executable = null;
        if (token.Type == TokenType.Literal)
        {
            executable = getLiteralExecutable(token);
            return true;
        }
        if (token.Type != TokenType.Identifier)
            return false;

        var ident = (Token<string>)token;
        Definition? def;
        if (!nameCtx.TryGetName(ident.Value, out def))
            return false;

        if (def is StructDefinition structDef)
        {
            Typ type = structDef.Type;
            Location location = token.Location;
            
            if (!tokens.Read(out token!)
                || token.Type != TokenType.Identifier)
            {
                errorer.Append($"Expected variable name. {token} is found", token.Location);
                return true;
            }

            location = new(location, token.Location);
            VariableDefinition varDef = new(((Token<string>)token).Value, type, funcDef, [], location);
            if (!nameCtx.Append(varDef))
            {
                errorer.Append($"Variable with name {varDef.Name} already exists", varDef.Location);
                return true;
            }

            executable = new Identifyer(varDef, varDef.Type, token.Location);
            return true;
        }

        Typ? retType;
        if (def is FunctionDefinition functionDef)
            retType = new FunctionPointer(functionDef);
        else if (def is VariableDefinition varDef)
            retType = varDef.Type;
        else if (def is NamespaceDefinition nmspDef)
            retType = DefaultType.Void;
        else
            return false;

        executable = new Identifyer(def, retType, token.Location);
        return true;
    }

    private Executable? parseTypeCast(
        EnumerableReader<Token> tokens,
        INameContainer nameCtx,
        FunctionDefinition funcDef,
        IEnumerable<Symbols> endSymbols,
        Typ type,
        Location startLoc
        )
    {
        int ptrCnt = 0;
        Token? token;

        while (true)
        {
            if (!tokens.Read(out token))
                return null;
            if (token is Token<Symbols> symbolToken)
            {
                if (symbolToken.Value == Symbols.Star)
                {
                    ptrCnt++;
                    continue;
                }
                if (symbolToken.Value != Symbols.CircleClose)
                    return null;
                break;
            }
            return null;
        }

        for (int i = 0; i < ptrCnt; i++)
        {
            type = new Pointer(type);
        }

        Executable? e = parseExpression(tokens, nameCtx, funcDef, PrefixOpBP, endSymbols, false);
        if (e == null)
            return null;
        if (!checkTypeCast(e.ReturnType, type))
        {
            errorer.Append($"Can not cast `{e.ReturnType}` to `{type}`", e.Location);
            return null;
        }

        Location loc = new(startLoc, e.Location);

        return new UnaryOperation(UnaryOperationTypes.Cast, e, type, loc);
    }

    private bool checkTypeCast(Typ from, Typ to)
    {
        if (from == to)
            return true;
        if (from is DefaultType.Integer && to is DefaultType.Integer)
            return true;
        if (from is Pointer && to is Pointer)
            return true;
        if (from is DefaultType.Integer i
            && i.Size == 8
            && i.IsSigned == false 
            && to is Pointer)
            return true;
        if (from is Pointer
            && to is DefaultType.Integer it
            && it.Size == 8
            && it.IsSigned == false)
            return true;
        if (from is DefaultType.FloatingPoint
            && to is DefaultType.FloatingPoint)
            return true;
        if (from is DefaultType.FloatingPoint
            && to is DefaultType.Integer
            || from is DefaultType.Integer
            && to is DefaultType.FloatingPoint)
            return true;
        return false;
    }

    private Executable getLiteralExecutable(Token token)
    {
        if (token is Token<char> charToken)
            return new Literal<char>(charToken.Value, DefaultType.Char, token.Location);

        else if (token is Token<string> stringToken)
            return new Literal<string>(stringToken.Value, new Pointer(DefaultType.Char), token.Location);

        else if (token is Token<ulong> intToken)
            return new Literal<ulong>(intToken.Value, DefaultType.Integer.Lit, token.Location);

        else if (token is Token<bool> boolToken)
            return new Literal<bool>(boolToken.Value, DefaultType.Bool, token.Location);

        else if (token is Token<double> doubleToken)
            return new Literal<double>(doubleToken.Value, DefaultType.FloatingPoint.Lit, token.Location);

        throw new Exception($"Unknown literal type {token}");
    }
}