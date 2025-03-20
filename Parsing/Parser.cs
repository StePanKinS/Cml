using Cml.Lexing;
using System.Diagnostics;

namespace Cml.Parsing;

internal static class Parser
{
    public static ParsedFile Process(List<Token> tokens)
    {
        List<Import> imports = [];
        //List<Definition> definitions = [];
        NameContext globalContext = new(null);

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

        return new ParsedFile(tokens[0].Location.File, globalContext, imports);

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

            Location location = new(keywordToken.Location, semicolonToken.Location);

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

                if (!tryReadTypeName(tokens, ref i, out NameToken typeName))
                {
                    Util.Exit(token, $"Expected type name or `{'}'}`, not {token}");
                    throw new UnreachableException();
                }

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

            Location location = new(keywordToken.Location, curlyCloseToken.Location);

            StructDefinition structDefinition = new(structName.Value, members, location);
            globalContext.Names[structName.Value] = structDefinition;
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

                if (!tryReadTypeName(tokens, ref i, out NameToken argType))
                {
                    Util.Exit(token, $"Expected type name or `{'}'}`, not {token}");
                    throw new UnreachableException();
                }

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

            Executable code = parseInstruction(tokens, globalContext, ref i);

            Location location = new(typeName.Location, circleCloseToken.Location);

            FunctionDefinition func = new(funcName.Value, typeName.Value, args, code, location);

            globalContext.Names[funcName.Value] = func;
        }
    }

    private static Executable parseInstruction(List<Token> tokens, NameContext nameCtx, ref int i)
    {
        int start = i;
        int end;
        Token token = tokens[i++];
        if (token is SymbolToken curlyOpen && curlyOpen.Value == Symbols.CurlyOpen)
        {
            NameContext nameContext = new(nameCtx);
            List<Executable> code = [];
            SymbolToken curlyClose;

            while (tokens[i] is not SymbolToken curlyCloseToken || (curlyClose = curlyCloseToken).Value != Symbols.CurlyClose)
            {
                code.Add(parseInstruction(tokens, nameContext, ref i));
            }
            i++;

            Location location = new(curlyOpen.Location, curlyClose.Location);

            return new CodeBlock(code, nameContext, location);
        }
        else if (token is KeywordToken keywordToken)
        {
            if (keywordToken.Value == Keywords.If)
            {
                Location location;
                token = tokens[i++];
                if (token is not SymbolToken circleOpenToken || circleOpenToken.Value != Symbols.CircleOpen)
                {
                    Util.Exit(token, $"Expected `(` at start of condition expression, not `{token}`");
                    throw new UnreachableException();
                }
                int exprStart = i;

                if (!skipAfterSymbol(tokens, Symbols.CircleClose, ref i, setCursorOnSymbol: true))
                {
                    location = new(tokens[exprStart].Location, tokens[^1].Location);
                    Util.Exit(location, "Can not find the end of conditional expression");
                    throw new UnreachableException();
                }
                int exprEnd = i++;

                Executable? condition = parseExprssion(tokens, nameCtx, exprStart, exprEnd);
                if (condition == null)
                {
                    location = new(tokens[exprStart].Location, tokens[exprEnd - 1].Location);
                    Util.Exit(location, "Can not parse conditional expression");
                    throw new UnreachableException();
                }

                Executable body = parseInstruction(tokens, nameCtx, ref i);
                location = new(keywordToken.Location, body.Location);
                Executable? elseBody = null;

                if (tokens[i] is KeywordToken elseKeywordToken && elseKeywordToken.Value == Keywords.Else)
                {
                    i++;
                    elseBody = parseInstruction(tokens, nameCtx, ref i);
                    location = new(location, elseBody.Location);
                }

                return new IfKeyword(condition, body, elseBody, location);
            }
            else if (keywordToken.Value == Keywords.Return)
            {
                Executable value = parseInstruction(tokens, nameCtx, ref i);
                return new ReturnKeyword(value, new Location(keywordToken.Location, value.Location));
            }
        }

        if (!skipAfterSymbol(tokens, Symbols.Semicolon, ref i, setCursorOnSymbol: true, skipInnerPrantecies: false))
        {
            Location loc = new(tokens[start].Location, tokens[tokens.Count - 1].Location);
            Util.Exit(loc, "Can not find semicolon");
            throw new UnreachableException();
        }

        end = i++;

        Executable? expr = parseExprssion(tokens, nameCtx, start, end);
        if (expr == null)
        {
            Location loc = new(tokens[start].Location, tokens[end - 1].Location);
            Util.Exit(loc, "Can not parse instruction");
            throw new UnreachableException();
        }

        return expr;
    }

    private static Executable? parseExprssion(List<Token> tokens, NameContext nameCtx, int start, int end)
    {
        Token token;

        if (end - start <= 0)
            return null;

        if (end - start == 1)
        {
            token = tokens[start];
            if (token is NameToken singleNameToken)
                return new ValueOf(singleNameToken, singleNameToken.Location);

            if (token is IntLiteralToken intLiteralToken)
                return new IntLiteral(intLiteralToken);

            if (token is FloatLiteralToken floatLiteralToken)
                return new FloatLiteral(floatLiteralToken);

            if (token is BoolLiteralToken boolLiteralToken)
                return new BoolLiteral(boolLiteralToken);

            if (token is StringLiteralToken stringLiteralToken)
                return new StringLiteral(stringLiteralToken);

            if (token is CharLiteralToken charLiteralToken)
                return new CharLiteral(charLiteralToken);

            if (token is ILiteralToken)
                return new BoolLiteral(new(false, Location.Nowhere));
        }

        if (end - start >= 2)
            if (tryParseVarDefinition(out Executable? valueOf))
                return valueOf;

        int leastPriorityToken = -1;
        int leastPriority = int.MinValue;

        int endDefaultCase = start - 1;

        for (int i = start; i < end; i++)
        {
            if (endDefaultCase != i - 1)
            {
                i = endDefaultCase;
                if (i >= end)
                    break;
            }
            endDefaultCase = i;

            token = tokens[i];

            int priority = int.MinValue;
            bool isRightToLeft = default;

            if (token is SymbolToken symbolToken)
            {
                (priority, isRightToLeft) = symbolToken.Value switch
                {
                    Symbols.Plus => (Addition.Priority, Addition.IsRightToLeft),
                    Symbols.Minus => checkIfUnary(i) ? (UnaryMinus.Priority, UnaryMinus.IsRightToLeft) : (Subtraction.Priority, Subtraction.IsRightToLeft),
                    Symbols.Star => checkIfUnary(i) ? (Dereference.Priority, Dereference.IsRightToLeft) : (Multiplication.Priority, Multiplication.IsRightToLeft),
                    Symbols.Slash => (Division.Priority, Division.IsRightToLeft),
                    Symbols.Percent => (DivisionReminder.Priority, DivisionReminder.IsRightToLeft),
                    Symbols.Ampersand => checkIfUnary(i) ? (AddressOf.Priority, AddressOf.IsRightToLeft) : (BitwiseAnd.Priority, BitwiseAnd.IsRightToLeft),
                    Symbols.AmpersandAmpersand => (And.Priority, And.IsRightToLeft),
                    Symbols.Verticalbar => (BitwiseOr.Priority, BitwiseOr.IsRightToLeft),
                    Symbols.VerticalbarVerticalbar => (Or.Priority, Or.IsRightToLeft),
                    Symbols.UpArrow => (BitwiseXor.Priority, BitwiseXor.IsRightToLeft),
                    Symbols.Tilda => (BitwiseInverse.Priority, BitwiseInverse.IsRightToLeft),
                    Symbols.ExclamationMark => (Inverse.Priority, Inverse.IsRightToLeft),
                    Symbols.LeftShift => (LeftShift.Priority, LeftShift.IsRightToLeft),
                    Symbols.RightShift => (RightShift.Priority, RightShift.IsRightToLeft),
                    Symbols.IsEquals => (IsEquals.Priority, IsEquals.IsRightToLeft),
                    Symbols.NotEquals => (IsNotEquals.Priority, IsNotEquals.IsRightToLeft),
                    Symbols.LessEquals => (IsLessEquals.Priority, IsLessEquals.IsRightToLeft),
                    Symbols.GreaterEquals => (IsGreaterEquals.Priority, IsGreaterEquals.IsRightToLeft),
                    Symbols.Less => (IsLess.Priority, IsLess.IsRightToLeft),
                    Symbols.Greater => (IsGreater.Priority, IsGreater.IsRightToLeft),
                    Symbols.Equals => (JustAssign.Priority, JustAssign.IsRightToLeft),
                    Symbols.PlusEquals => (AddAssign.Priority, AddAssign.IsRightToLeft),
                    Symbols.MinusEquals => (SubtractAssign.Priority, SubtractAssign.IsRightToLeft),
                    Symbols.MultiplyEquals => (MultiplyAssign.Priority, MultiplyAssign.IsRightToLeft),
                    Symbols.DivideEquals => (DivideAssign.Priority, DivideAssign.IsRightToLeft),
                    Symbols.ReminderEquals => (ReminderAssign.Priority, ReminderAssign.IsRightToLeft),
                    Symbols.LeftShiftEquals => (LeftShiftAssign.Priority, LeftShiftAssign.IsRightToLeft),
                    Symbols.RightShiftEquals => (RightShiftAssign.Priority, RightShiftAssign.IsRightToLeft),
                    Symbols.AndEquals => (AndAssign.Priority, AndAssign.IsRightToLeft),
                    Symbols.OrEquals => (OrAssign.Priority, OrAssign.IsRightToLeft),
                    Symbols.XorEquals => (XorAssign.Priority, XorAssign.IsRightToLeft),
                    Symbols.Decrement => (Decrement.Priority, Decrement.IsRightToLeft),
                    Symbols.Increment => (Increment.Priority, Increment.IsRightToLeft),
                    Symbols.Dot => (Dot.Priority, Dot.IsRightToLeft),

                    _ => processDefault(symbolToken, ref i),

                    // +, -, *, /, %, &, &&, |, ||, ^, ~, !, <<, >>, ==, !=, <=, >=, <, >, =, +=, -=, *=, /=, %=, <<=, >>=, &=, |=, ^=, --, ++, ., ,
                };

                if (priority > leastPriority)
                {
                    leastPriority = priority;
                    leastPriorityToken = i;
                } 
                else if (priority == leastPriority && !isRightToLeft)
                {
                    leastPriority = priority;
                    leastPriorityToken = i;
                }
            }
            else if (token is NameToken)
            {
                // TODO: detect unnamed functions;
                if (i + 1 < end && tokens[i + 1] is SymbolToken circleOpentoken && circleOpentoken.Value == Symbols.CircleOpen)
                {
                    if (FunctionCall.Priority > leastPriority)
                    {
                        leastPriority = FunctionCall.Priority;
                        leastPriorityToken = i + 1;
                    }
                    else if (FunctionCall.Priority == leastPriority && isRightToLeft)
                    {
                        leastPriority = FunctionCall.Priority;
                        leastPriorityToken = i + 1;
                    }

                    endDefaultCase = i + 2;
                    if (!skipAfterSymbol(tokens, Symbols.CircleClose, ref endDefaultCase, end: end))
                    {
                        problem(start, end, "Circle bracket is not closed");
                        throw new UnreachableException();
                    }
                }
            }
            else if (token is ILiteral)
            {
                if (leastPriority < 0)
                {
                    leastPriority = 0;
                    leastPriorityToken = i;
                }
            }
            else if (token is KeywordToken keywordToken)
            {
                Util.Exit(token, $"Unexpected keyword `{keywordToken.Value.ToString().ToLower()}`");
                throw new UnreachableException();
            }
        }

        if (leastPriorityToken == -1)
            return null;

        token = tokens[leastPriorityToken];

        Executable? executable;

        Location binaryLocation = new(tokens[start].Location, tokens[end - 1].Location);
        Location unaryRightLocation = new(tokens[leastPriorityToken].Location, tokens[end - 1].Location);

        if (token is SymbolToken parsingSymbolToken)
        {
            executable = parsingSymbolToken.Value switch
            {
                Symbols.Plus => new Addition(getLeft(), getRight(), binaryLocation),
                Symbols.Minus => leastPriority == UnaryMinus.Priority ?
                    new UnaryMinus(getRight(), unaryRightLocation) :
                    new Subtraction(getLeft(), getRight(), binaryLocation),
                Symbols.Star => leastPriority == Dereference.Priority ?
                    new Dereference(getRight(), unaryRightLocation) :
                    new Multiplication(getLeft(), getRight(), binaryLocation),
                Symbols.Slash => new Division(getLeft(), getRight(), binaryLocation),
                Symbols.Percent => new DivisionReminder(getLeft(), getRight(), binaryLocation),
                Symbols.Ampersand => leastPriority == AddressOf.Priority ?
                    new AddressOf(getRight(), unaryRightLocation) :
                    new BitwiseAnd(getLeft(), getRight(), binaryLocation),
                Symbols.AmpersandAmpersand => new And(getLeft(), getRight(), binaryLocation),
                Symbols.Verticalbar => new BitwiseOr(getLeft(), getRight(), binaryLocation),
                Symbols.VerticalbarVerticalbar => new Or(getLeft(), getRight(), binaryLocation),
                Symbols.UpArrow => new BitwiseXor(getLeft(), getRight(), binaryLocation),
                Symbols.Tilda => new BitwiseInverse(getRight(), binaryLocation),
                Symbols.ExclamationMark => new Inverse(getRight(), binaryLocation),
                Symbols.LeftShift => new LeftShift(getLeft(), getRight(), binaryLocation),
                Symbols.RightShift => new RightShift(getLeft(), getRight(), binaryLocation),
                Symbols.IsEquals => new IsEquals(getLeft(), getRight(), binaryLocation),
                Symbols.NotEquals => new IsNotEquals(getLeft(), getRight(), binaryLocation),
                Symbols.LessEquals => new IsLessEquals(getLeft(), getRight(), binaryLocation),
                Symbols.GreaterEquals => new IsGreaterEquals(getLeft(), getRight(), binaryLocation),
                Symbols.Less => new IsLess(getLeft(), getRight(), binaryLocation),
                Symbols.Greater => new IsGreater(getLeft(), getRight(), binaryLocation),
                Symbols.Equals => new JustAssign(getLeft(), getRight(), binaryLocation),
                Symbols.PlusEquals => new AddAssign(getLeft(), getRight(), binaryLocation),
                Symbols.MinusEquals => new SubtractAssign(getLeft(), getRight(), binaryLocation),
                Symbols.MultiplyEquals => new MultiplyAssign(getLeft(), getRight(), binaryLocation),
                Symbols.DivideEquals => new DivideAssign(getLeft(), getRight(), binaryLocation),
                Symbols.ReminderEquals => new ReminderAssign(getLeft(), getRight(), binaryLocation),
                Symbols.LeftShiftEquals => new LeftShiftAssign(getLeft(), getRight(), binaryLocation),
                Symbols.RightShiftEquals => new RightShiftAssign(getLeft(), getRight(), binaryLocation),
                Symbols.AndEquals => new AndAssign(getLeft(), getRight(), binaryLocation),
                Symbols.OrEquals => new OrAssign(getLeft(), getRight(), binaryLocation),
                Symbols.XorEquals => new XorAssign(getLeft(), getRight(), binaryLocation),
                Symbols.Decrement => isPostRement(leastPriorityToken) ?
                    new Decrement(getLeft(), true, new Location(tokens[start].Location, tokens[leastPriorityToken].Location)) :
                    new Decrement(getRight(), false, unaryRightLocation),
                Symbols.Increment => isPostRement(leastPriorityToken) ?
                    new Increment(getLeft(), true, new Location(tokens[start].Location, tokens[leastPriorityToken].Location)) :
                    new Increment(getRight(), false, unaryRightLocation),
                Symbols.Dot => parseDot(),
                Symbols.CircleClose => parseCast(),
                Symbols.CircleOpen => parseFuncCall(),
                Symbols.CurlyOpen => parseStructInitialization(),
                Symbols.SquareOpen => parseGetArrayElement(),

                _ => null
            };
        }
        else if (token is NameToken parsingNameToken)
        {
            executable = new ValueOf(parsingNameToken, parsingNameToken.Location);
        }
        else if (token is ILiteralToken)
        {
            executable = new BoolLiteral(new BoolLiteralToken(false, Location.Nowhere));
        }
        else
        {
            executable = null;
        }

        return executable;


        bool tryParseVarDefinition(out Executable? valueOf)
        {
            valueOf = null;
            int i = start;

            token = tokens[i];

            if (tokens[i] is not NameToken)
                return false;

            if (!tryReadTypeName(tokens, ref i, out NameToken typeName))
            {
                Util.Exit(token, $"Expected type name, not {token}");
                throw new UnreachableException();
            }

            token = tokens[i++];

            if (i != end)
                return false;

            if (token is not NameToken varName)
                return false;

            Location loc = new(typeName.Location, varName.Location);
            VariableDefinition varDef = new(varName.Value, typeName.Value, loc);

            if (nameCtx.Names.ContainsKey(varName.Value))
            {
                Util.Exit(varName, $"Something with `{varName.Value}` name already exists");
                throw new UnreachableException();
            }

            nameCtx.Names[varName.Value] = varDef;

            valueOf = new ValueOf(varName, loc);

            return true;
        }

        (int, bool) processDefault(SymbolToken symbolToken, ref int i)
        {
            ref int pos = ref endDefaultCase;
            int start = pos;
            if (symbolToken.Value == Symbols.CircleOpen)
            {
                int k = i + 1;
                if (tryReadTypeName(tokens, ref k, out NameToken typeName))
                {
                    if (k < end && tokens[k] is SymbolToken circleCloseToken && circleCloseToken.Value == Symbols.CircleClose)
                    {
                        pos = i = k;
                        return (Cast.Priority, Cast.IsRightToLeft);
                    }
                }
                pos++;
                if (!skipAfterSymbol(tokens, Symbols.CircleClose, ref pos, end))
                {
                    problem(start, pos, "Circle bracket is not closed");
                }

                return (int.MinValue, default);
            }

            if (symbolToken.Value == Symbols.SquareOpen)
            {
                pos++;
                if (!skipAfterSymbol(tokens, Symbols.SquareClose, ref pos, end))
                {
                    problem(start, end, "Square bracket is not closed");
                }

                return (GetArrayElement.Priority, GetArrayElement.IsRightToLeft);
            }

            if (symbolToken.Value == Symbols.CurlyOpen)
            {
                pos++;
                if (!skipAfterSymbol(tokens, Symbols.CurlyClose, ref pos, end))
                {
                    problem(start, end, "Curly bracket is not closed");
                }

                return (StructureInitializer.Priority, StructureInitializer.IsRightToLeft);
            }

            Util.Exit(symbolToken, "Unexpected token in expression :(");
            throw new UnreachableException();
        }

        bool checkIfUnary(int pos)
            => parseExprssion(tokens, nameCtx, start, pos) == null;

        bool isPostRement(int pos)
        {
            Executable? left = parseExprssion(tokens, nameCtx, start, pos);
            if (left != null)
                return true;

            Executable? right = parseExprssion(tokens, nameCtx, pos + 1, end);
            if (right != null) 
                return false;

            problem(start, end, "Cannot identify if post or pre inc/dec-rement");
            throw new UnreachableException();
        }

        void problem(int start, int end, string message = "Cannot parse this expression")
        {
            Location startLoc = tokens[start].Location;
            Location endLoc = tokens[end].Location;

            Location location = new(startLoc, endLoc);

            Util.Exit(location, message);

            while (true) ;
        }

        Executable getLeft()
        {
            Executable? e = parseExprssion(tokens, nameCtx, start, leastPriorityToken);
            if (e is null)
                problem(start, leastPriorityToken);

            return e!;

        }

        Executable getRight()
        {
            Executable? e = parseExprssion(tokens, nameCtx, leastPriorityToken + 1, end);
            if (e is null)
                problem(leastPriorityToken + 1, end - 1);

            return e!;

        }

        Executable parseDot()
        {
            if (leastPriorityToken + 1 >= end || tokens[leastPriorityToken + 1] is not NameToken nameToken)
            {
                problem(leastPriorityToken, leastPriorityToken, "Expected a member name after dot expression");
                throw new UnreachableException();
            }
            return new Dot(getLeft(), nameToken, tokens[leastPriorityToken].Location);
        }

        Executable parseCast()
        {
            Executable? value = parseExprssion(tokens, nameCtx, leastPriorityToken + 1, end);
            if (value == null)
            {
                problem(leastPriorityToken + 1, end);
                throw new UnreachableException();
            }

            int i = leastPriorityToken - 1;
            while (true)
            {
                Token token = tokens[i--];
                if (i < 0)
                    throw new Exception("tokens were changed during parsing");

                if (token is SymbolToken symbolToken && symbolToken.Value == Symbols.CircleOpen)
                    break;
            }
            i += 2;

            if (!tryReadTypeName(tokens, ref i, out NameToken typeName))
                throw new Exception("tokens were changed during parsing");

            Location loc = new(typeName.Location, tokens[leastPriorityToken - 1].Location);
            typeName = new(typeName.Value, loc);

            return new Cast(typeName, value, tokens[leastPriorityToken].Location);
        }

        Executable parseFuncCall()
        {
            Executable ptr = getLeft();

            int argStart = leastPriorityToken + 1;
            int funcEnd = leastPriorityToken + 1;
            if (!skipAfterSymbol(tokens, Symbols.CircleClose, ref funcEnd, end: end, setCursorOnSymbol: true))
            {
                problem(argStart, end, "Cannot find function ending `)`");
                throw new UnreachableException();
            }

            List<Executable> args = [];

            while (true)
            {
                if (argStart == funcEnd)
                    break;

                int i = argStart;
                int argEnd;
                if (skipAfterSymbol(tokens, Symbols.Comma, ref i, end: funcEnd, setCursorOnSymbol: true))
                    argEnd = i++;
                else
                    argEnd = i = funcEnd;

                Executable? arg = parseExprssion(tokens, nameCtx, argStart, argEnd);
                if (arg is null)
                {
                    problem(argStart, argEnd, "Can not parse functioon argument expression");
                    throw new UnreachableException();
                }

                args.Add(arg);
                argStart = i;
            }

            return new FunctionCall(ptr, args, tokens[leastPriorityToken].Location);
        }

        Executable parseStructInitialization()
        {
            Dictionary<string, Executable> values = [];
            int i = leastPriorityToken + 1;

            while (true)
            {
                Token token = tokens[i++];

                if (token is SymbolToken curlyCloseToken && curlyCloseToken.Value == Symbols.CurlyClose)
                    break;

                if (token is not NameToken memberNameToken)
                {
                    Util.Exit(token, $"Expected a name of a struct member, not {token}");
                    throw new UnreachableException();
                }

                token = tokens[i++];
                if (token is not SymbolToken asignSymbol || asignSymbol.Value != Symbols.Equals)
                {
                    Util.Exit(token, $"Expected `=`, not {token}");
                    throw new UnreachableException();
                }
                int start = i;
                skipAfterSymbol(tokens, Symbols.Comma, ref i, setCursorOnSymbol: true);

                Executable? expr = parseExprssion(tokens, nameCtx, start, i);
                if (expr == null)
                {
                    problem(start, i);
                    throw new UnreachableException();
                }
                i++;

                values[memberNameToken.Value] = expr;
            }

            Location loc = new(tokens[leastPriorityToken].Location, tokens[i++].Location);
            return new StructureInitializer(values, loc);
        }

        Executable parseGetArrayElement()
        {
            Executable arrayPtr = getLeft();
            int i = leastPriorityToken + 1;
            int indexStart = i;
            if (!skipAfterSymbol(tokens, Symbols.SquareClose, ref i, end: end, setCursorOnSymbol: true))
            {
                problem(indexStart, end, "Cannot find the end of expression of array index");
                throw new UnreachableException();
            }

            Executable? index = parseExprssion(tokens, nameCtx, indexStart, i);
            if (index == null)
            {
                problem(indexStart, i, "Can not prse expression of array index");
                throw new UnreachableException();
            }

            return new GetArrayElement(arrayPtr, index, new(arrayPtr.Location, tokens[i++].Location));
        }
    }

    private static bool tryReadTypeName(List<Token> tokens, ref int i, out NameToken typeName)
    {
        Token token = tokens[i++];
        if (token is not NameToken nameToken)
        {
            typeName = null!;
            return false;
        }

        nameToken = new(nameToken.Value, nameToken.Location);

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
            nameToken.Value += '*';
        }

        Location location = new(nameToken.Location, tokens[i - 1].Location);
        nameToken.Location = location;

        typeName = nameToken;

        return true;
    }

    private static bool skipAfterSymbol(List<Token> tokens, Symbols symbol, ref int pos, int end = -1, bool setCursorOnSymbol = false, bool skipInnerPrantecies = true)
    {
        if (end == -1)
            end = tokens.Count;

        while (pos < end)
        {
            Token token = tokens[pos++];
            if (token is SymbolToken symbolToken)
            {
                if (symbolToken.Value == symbol)
                {
                    if (setCursorOnSymbol)
                        pos--;
                    return true;
                }
                if (skipInnerPrantecies)
                    if (!skipInnerPrentecies(tokens, ref pos, end, symbolToken))
                        return false;
            }
        }

        return false;
    }

    private static bool skipInnerPrentecies(List<Token> tokens, ref int pos, int end, SymbolToken symbolToken)
    {
        if (symbolToken.Value == Symbols.CircleOpen)
            return skipAfterSymbol(tokens, Symbols.CircleClose, ref pos, end: end);
        if (symbolToken.Value == Symbols.CurlyOpen)
            return skipAfterSymbol(tokens, Symbols.CurlyClose, ref pos, end: end);
        if (symbolToken.Value == Symbols.SquareOpen)
            return skipAfterSymbol(tokens, Symbols.SquareClose, ref pos, end: end);

        return true;
    }
}