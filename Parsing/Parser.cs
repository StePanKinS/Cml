using Cml.Lexing;
using System.Diagnostics;

namespace Cml.Parsing;

internal static class Parser
{
    private static IEnumerable<Keywords> Modifyers = [Keywords.Extern];

    #region FileProcessing
    public static ParsedFile Process(List<Token> tokens, NameContext globalContext)
    {
        try
        {
            int i = 0;
            readDefinitions(tokens, globalContext, ref i);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine("Something went wrong(");
            Environment.Exit(1);
            return null;
        }

        return new ParsedFile(tokens[0].Location.File, globalContext);
    }

    private static void readDefinitions(List<Token> tokens, NameContext nameCtx, ref int i, bool isTopLevel = true)
    {
        List<KeywordToken> modifyers = [];
        while (i < tokens.Count)
        {
            Token token = tokens[i++];
            if (token is KeywordToken keywordToken)
            {
                if (Modifyers.Contains(keywordToken.Value)) {
                    modifyers.Add(keywordToken);
                    continue;
                }
                else if (keywordToken.Value == Keywords.Import)
                {
                    readImport(keywordToken, ref i);
                    continue;
                }
                else if (keywordToken.Value == Keywords.Struct)
                {
                    readStruct(keywordToken, ref i);
                    continue;
                }
                else if (keywordToken.Value == Keywords.Namespace)
                {
                    readNamespace(keywordToken, ref i);
                    continue;
                }
                else {
                    Util.Exit(keywordToken, $"Unexpected keyword {keywordToken.Value}");
                    throw new UnreachableException();
                }
            }
            else if (token is NameToken typeName)
            {
                i -= 1;
                readFunction(typeName, ref i);
                continue;
            }
            else if (!isTopLevel && token is SymbolToken curlyClose && curlyClose.Value == Symbols.CurlyClose)
                break;

            Util.Exit(token, $"Expected import, struct, namespace or function definition, not {token}");
            throw new UnreachableException();
        }

        void readImport(KeywordToken keywordToken, ref int i)
        {
            if (modifyers.Count != 0) {
                Location loc = new(modifyers[0].Location, modifyers[^1].Location);
                Util.Exit(loc, "Modifyers are not expected for imports");
                throw new UnreachableException();
            }

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

            ImportDefinition import = new(fileName.Value, keywordToken.Location, location);

            if (!nameCtx.Add(import))
                throw new Exception($"Cannot add import {import.Name}");
        }
        
        void readStruct(KeywordToken keywordToken, ref int i)
        {
            if (modifyers.Count != 0) {
                Location loc = new(modifyers[0].Location, modifyers[^1].Location);
                Util.Exit(loc, "Modifyers are not expected for structures");
                throw new UnreachableException();
            }

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
            if (!nameCtx.Add(structDefinition))
                throw new Exception($"Can not add struct definition {structDefinition.Name}");
        }    

        void readFunction(NameToken typeNameToken, ref int i)
        {
            bool isExtern = false;
            foreach (var keywordToken in modifyers) {
                if (keywordToken.Value == Keywords.Extern)
                    isExtern = true;
                else {
                    Util.Exit(keywordToken, $"Unxepected modifyer `{keywordToken.Value}`");
                    throw new UnreachableException();
                }
            }
            modifyers.Clear();

            if (!tryReadTypeName(tokens, ref i, out NameToken typeName)) {
                Util.Exit(typeNameToken, "Can not read type name");
                throw new UnreachableException();
            }

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

            NameContext argsNameCtx = new(nameCtx);
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

                Location argLocation = new(argType.Location, memberName.Location);
                VariableDefinition arg = new(memberName.Value, argType.Value, argLocation);
                if (argsNameCtx.Names.TryGetValue(memberName.Value, out Definition? _))
                {
                    Util.Exit(memberName, $"Function argument with the name {memberName.Value} already exists");
                    throw new UnreachableException();
                }

                argsNameCtx.Names[memberName.Value] = arg;

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

            Executable? code = null;
            NameContext? localContext = null;
            Location location;
            if (!isExtern) {
                localContext = new(argsNameCtx);
                code = parseInstruction(tokens, localContext, ref i);

                location = new(typeName.Location, circleCloseToken.Location);
            }
            else {
                token = tokens[i++];
                if (token is not SymbolToken semicolonToken || semicolonToken.Value != Symbols.Semicolon) {
                    Util.Exit(token, $"Expected `;` but got `{token}`");
                    throw new UnreachableException();
                }
                location = new(typeName.Location, semicolonToken.Location);
            }
            FunctionDefinition func = new(funcName.Value, typeName.Value, argsNameCtx, code, localContext, isExtern, location);

            if (!nameCtx.Add(func))
                throw new Exception($"Can not add function {func.Name}");
        }
        
        void readNamespace(KeywordToken keywordToken, ref int i)
        {
            if (modifyers.Count != 0) {
                Location loc = new(modifyers[0].Location, modifyers[^1].Location);
                Util.Exit(loc, "Modifyers are not expected for namespaces");
                throw new UnreachableException();
            }

            Token token = tokens[i++];
            if (token is not NameToken nameToken) {
                Util.Exit(token, $"Expected namespace name, not {token}");
                throw new UnreachableException();
            }

            token = tokens[i++];
            if (token is not SymbolToken curlyOpen || curlyOpen.Value != Symbols.CurlyOpen) {
                Util.Exit(token, $"Expected `{'{'}`, not {token}");
                throw new UnreachableException();
            }
            
            NameContext localCtx = new(nameCtx);
            readDefinitions(tokens, localCtx, ref i, false);

            Location location = new(keywordToken.Location, tokens[i].Location);
            NamespaceDefinition namespaceDefinition = new(nameToken.Value, localCtx, location);

            if (!nameCtx.Add(namespaceDefinition)) {
                Util.Exit(nameToken, $"Something with name `{nameToken.Value}` already exist");
                throw new UnreachableException();
            }
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
                try {
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

                        _ => processDefault(symbolToken, ref i) ?? throw new NullReferenceException(),

                        // +, -, *, /, %, &, &&, |, ||, ^, ~, !, <<, >>, ==, !=, <=, >=, <, >, =, +=, -=, *=, /=, %=, <<=, >>=, &=, |=, ^=, --, ++, ., ,
                    };
                }
                catch (NullReferenceException) {
                    return null;
                }

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
                        return null;
                    // {
                    //     problem(start, end, "Circle bracket is not closed");
                    //     throw new UnreachableException();
                    // }
                }
            }
            else if (token is ILiteralToken)
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
            // Executable? left = getLeft(); // Plz, plz, plz dont look here...
            // Executable? right = getRight();
            try {
                executable = parsingSymbolToken.Value switch
                {
                    Symbols.Plus => new Addition(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Minus => leastPriority == UnaryMinus.Priority ?
                        new UnaryMinus(getRight() ?? throw new NullReferenceException(), unaryRightLocation) :
                        new Subtraction(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Star => leastPriority == Dereference.Priority ?
                        new Dereference(getRight() ?? throw new NullReferenceException(), unaryRightLocation) :
                        new Multiplication(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Slash => new Division(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Percent => new DivisionReminder(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Ampersand => leastPriority == AddressOf.Priority ?
                        new AddressOf(getRight() ?? throw new NullReferenceException(), unaryRightLocation) :
                        new BitwiseAnd(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.AmpersandAmpersand => new And(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Verticalbar => new BitwiseOr(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.VerticalbarVerticalbar => new Or(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.UpArrow => new BitwiseXor(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Tilda => new BitwiseInverse(getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.ExclamationMark => new Inverse(getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.LeftShift => new LeftShift(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.RightShift => new RightShift(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.IsEquals => new IsEquals(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.NotEquals => new IsNotEquals(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.LessEquals => new IsLessEquals(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.GreaterEquals => new IsGreaterEquals(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Less => new IsLess(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Greater => new IsGreater(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Equals => new JustAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.PlusEquals => new AddAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.MinusEquals => new SubtractAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.MultiplyEquals => new MultiplyAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.DivideEquals => new DivideAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.ReminderEquals => new ReminderAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.LeftShiftEquals => new LeftShiftAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.RightShiftEquals => new RightShiftAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.AndEquals => new AndAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.OrEquals => new OrAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.XorEquals => new XorAssign(getLeft() ?? throw new NullReferenceException(), getRight() ?? throw new NullReferenceException(), binaryLocation),
                    Symbols.Decrement => isPostRement(leastPriorityToken) ?
                        new Decrement(getLeft() ?? throw new NullReferenceException(), true, new Location(tokens[start].Location, tokens[leastPriorityToken].Location)) :
                        new Decrement(getRight() ?? throw new NullReferenceException(), false, unaryRightLocation),
                    Symbols.Increment => isPostRement(leastPriorityToken) ?
                        new Increment(getLeft() ?? throw new NullReferenceException(), true, new Location(tokens[start].Location, tokens[leastPriorityToken].Location)) :
                        new Increment(getRight() ?? throw new NullReferenceException(), false, unaryRightLocation),
                    Symbols.Dot => parseDot(),
                    Symbols.CircleClose => parseCast(),
                    Symbols.CircleOpen => parseFuncCall(),
                    Symbols.CurlyOpen => parseStructInitialization(),
                    Symbols.SquareOpen => parseGetArrayElement(),

                    _ => null
                };
            }
            catch (NullReferenceException) {
                return null;
            }
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

        (int, bool)? processDefault(SymbolToken symbolToken, ref int i)
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

            // Util.Exit(symbolToken, "Unexpected token in expression :(");
            // throw new UnreachableException();
            // throw new NullReferenceException();
            return null;
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

        Executable? getLeft()
            => parseExprssion(tokens, nameCtx, start, leastPriorityToken);

        Executable? getRight()
            => parseExprssion(tokens, nameCtx, leastPriorityToken + 1, end);

        Executable? parseDot()
        {
            if (leastPriorityToken + 1 >= end || tokens[leastPriorityToken + 1] is not NameToken nameToken)
                return null;
            // {
            //     problem(leastPriorityToken, leastPriorityToken, "Expected a member name after dot expression");
            //     throw new UnreachableException();
            // }

            Executable? left = getLeft();
            if (left == null)
                return null;
            return new Dot(left, nameToken, tokens[leastPriorityToken].Location);
        }

        Executable? parseCast()
        {
            Executable? value = parseExprssion(tokens, nameCtx, leastPriorityToken + 1, end);
            if (value == null)
                return null;

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

        Executable? parseFuncCall()
        {
            Executable? ptr = getLeft();
            if (ptr == null)
                return null;

            int argStart = leastPriorityToken + 1;
            int funcEnd = leastPriorityToken + 1;
            if (!skipAfterSymbol(tokens, Symbols.CircleClose, ref funcEnd, end: end, setCursorOnSymbol: true))
                return null;
            // {
            //     problem(argStart, end, "Cannot find function ending `)`");
            //     throw new UnreachableException();
            // }

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
                    return null;
                // {
                //     problem(argStart, argEnd, "Can not parse functioon argument expression");
                //     throw new UnreachableException();
                // }

                args.Add(arg);
                argStart = i;
            }

            return new FunctionCall(ptr, args, tokens[leastPriorityToken].Location);
        }

        Executable? parseStructInitialization()
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
                    return null;
                i++;

                values[memberNameToken.Value] = expr;
            }

            Location loc = new(tokens[leastPriorityToken].Location, tokens[i++].Location);
            return new StructureInitializer(values, loc);
        }

        Executable? parseGetArrayElement()
        {
            Executable? arrayPtr = getLeft();
            if (arrayPtr == null)
                return null;

            int i = leastPriorityToken + 1;
            int indexStart = i;
            if (!skipAfterSymbol(tokens, Symbols.SquareClose, ref i, end: end, setCursorOnSymbol: true))
                return null;

            Executable? index = parseExprssion(tokens, nameCtx, indexStart, i);
            if (index == null)
                return null;
            // {
            //     problem(indexStart, i, "Can not prse expression of array index");
            //     throw new UnreachableException();
            // }

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
    #endregion

    #region ASTProcessing
    public static void SetReferences(NameContext nameCtx)
    {
        foreach (Definition definition in nameCtx.Names.Values)
        {
            if (definition is FunctionDefinition func)
            {
                var returnType = castToType(nameCtx.GetValue(func.ReturnTypeName), func.ReturnTypeName, func.Location);
                func.ReturnType = returnType;
                foreach (var arg in func.Args.Names.Values)
                {
                    VariableDefinition varArg = (VariableDefinition)arg;
                    varArg.ValueType = castToType(func.Args.GetValue(varArg.TypeName), varArg.TypeName, arg.Location);
                }
                if (func.IsExtern)
                    continue;
                if (!processExecutable(func.Code!, func.LocalNameContext!, returnType) && returnType != StructDefinition.Void)
                {
                    Util.Exit(func.Location, "Function does not return value");
                    throw new UnreachableException();
                }
            }
        }
    }

    private static bool processExecutable(Executable executable, NameContext nameCtx, StructDefinition? expectedType = null)
    {
        if (executable is ValueOf valueOf)
        {
            Definition? def = nameCtx.GetValue(valueOf.Name.Value);
            if (def == null)
            {
                Util.Exit(valueOf.Location, $"Variable {valueOf.Name.Value} is not defined");
                throw new UnreachableException();
            }
            if (def is VariableDefinition varDef)
            {
                executable.ReturnType = varDef.ValueType;
                return false;
            }
            if (def is FunctionDefinition funcDef)
            {
                FunctionPointer funcPtr = new(funcDef.ReturnType!, (from s in funcDef.Args.Names.Values select ((VariableDefinition)s).ValueType).ToArray());
                executable.ReturnType = funcPtr;
                return false;
            }
            if (def is StructDefinition)
            {
                executable.ReturnType = StructDefinition.Void;
                return false;
            }
            // TODO: namespaces
            throw new NotImplementedException($"Unknown definition type {def.GetType().Name}");
        }
        if (executable is Cast cast)
        {
            StructDefinition castType = castToType(nameCtx.GetValue(cast.TypeName.Value), cast.TypeName.Value, cast.TypeName.Location);
            cast.ReturnType = castType;
            // TODO: check if can cast
            return processExecutable(cast.Value, nameCtx);
        }
        if (executable is CodeBlock codeBlock)
        {
            codeBlock.ReturnType = StructDefinition.Void;
            bool doesReturn = false;
            foreach (var innerExecutable in codeBlock.Code)
            {
                if (processExecutable(innerExecutable, codeBlock.LocalVariables))
                    doesReturn = true;
            }
            return doesReturn;
        }
        if (executable is Dot dot)
        {
            processExecutable(dot.Left, nameCtx);
            Definition? def = getDotReturnType(dot, nameCtx);
            if (def == null)
            {
                Util.Exit(dot.Location, "Invalid dot expression");
                throw new UnreachableException();
            }
            if (def is StructDefinition returnType)
                dot.ReturnType = returnType;
            else
                dot.ReturnType = StructDefinition.Void;

            return false;
        }
        if (executable is FunctionCall funcCall)
        {
            processExecutable(funcCall.FuncPtr, nameCtx);
            if (funcCall.FuncPtr.ReturnType is not FunctionPointer funcPtr)
            {
                Util.Exit(funcCall.FuncPtr.Location, $"Expected a pointer to a function, not regular expression with return type {funcCall.FuncPtr.ReturnType}");
                throw new UnreachableException();
            }
            funcCall.ReturnType = funcPtr.ReturnType;

            return false;
        }

        throw new NotImplementedException($"Unknown executable type {executable.GetType().Name}");
    }

    private static StructDefinition castToType(Definition? definition, string typeName, Location location)
    {
        if (definition == null)
        {
            Util.Exit(location, $"Type with name {typeName} does not exist");
            throw new UnreachableException();
        }
        if (definition is not StructDefinition structtttt)
        {
            Util.Exit(location, $"{typeName} is a {definition.GetType().Name}, expected struct definition");
            throw new UnreachableException();
        }
        return structtttt;
    }

    private static Definition? getDotReturnType(Dot dotExpr, NameContext nameCtx)
    {
        Definition? def;
        if (dotExpr.Left is Dot dot)
        {
            def = getDotReturnType(dot, nameCtx);
        }
        else if (dotExpr.Left is ValueOf valueOf)
        {
            nameCtx.Names.TryGetValue(valueOf.Name.Value, out def);
        }
        else
            def = dotExpr.Left.ReturnType;

        if (def is StructDefinition structDef)
        {
            foreach (var nttn in structDef.Members)
            {
                if (nttn.Name.Value == dotExpr.Right.Value)
                    return nttn.Type;
            }

            return null;
        }
        if (def is FunctionDefinition)
            return null;
        if (def is VariableDefinition)
            return null;

        throw new NotImplementedException();
    }
    #endregion
}
