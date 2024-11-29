namespace Cml.Lexing;

internal static class Lexer
{
    internal static List<Token> Process(string path)
    {
        List<Token> tokens = [];

        string source = File.ReadAllText(path);

        int line = 0;
        int col = 0;

        int startLine = 0;
        int startCol = 0;

        string acum = "";



        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];

            if (c == '\n')
            {
                endAcum();

                line++;
                col = 0;

                updateStart();
                continue;
            }

            if (c == '\r' && i + 1 < source.Length && source[i + 1] == '\n')
            {
                endAcum();

                i++;
                line++;
                col = 0;

                updateStart();
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                endAcum();

                col++;

                updateStart();
                continue;
            }

            if (c == '_' || char.IsLetterOrDigit(c))
            {
                acum += c;
                col++;
                continue;
            }

            if (char.IsSymbol(c) || char.IsPunctuation(c))
            {
                endAcum();
                readSymbol(ref i, c);
                continue;
            }


            col++;
        }
        

        return tokens;


        void endAcum()
        {
            Location loc = new(path, startCol, startLine, line, col);

            if (acum.Length == 0)
            {
                updateStart();
                return;
            }

            if (Enum.TryParse<Keywords>(acum, true, out var result))
            {
                tokens.Add(new KeywordToken(result, loc));
            }
            else if (char.IsDigit(acum[0])) 
            {
                processDigit();
            }
            else
            {
                tokens.Add(new NameToken(acum, loc));
            }

            acum = "";
            updateStart();

            void processDigit()
            {
                tokens.Add(new IntLiteralToken(0, loc));
            }
        }

        void updateStart()
        {
            startCol = col;
            startLine = line;
        }

        void readSymbol(ref int pos, char c)
        {
            int i = pos;
            int captured = 0;
            captured++;

            switch (c)
            {
                case '+':
                    if (i + 1 < source.Length)
                    {
                        if (source[i + 1] == '+')
                        {
                            captured++;
                            addSymbolToken(Symbols.Increment);
                            break;
                        }
                        if (source[i + 1] == '=')
                        {
                            captured++;
                            addSymbolToken(Symbols.PlusEquals);
                            break;
                        }
                    }

                    addSymbolToken(Symbols.Plus);
                    break;
                case '-':
                    if (i + 1 < source.Length)
                    {
                        if (source[i + 1] == '-')
                        {
                            captured++;
                            addSymbolToken(Symbols.Decrement);
                            break;
                        }
                        if (source[i + 1] == '=')
                        {
                            captured++;
                            addSymbolToken(Symbols.MinusEquals);
                            break;
                        }
                    }
                    addSymbolToken(Symbols.Minus);
                    break;
                case '*':
                    if (i + 1 < source.Length && source[i+1] == '=')
                    {
                        captured++;
                        addSymbolToken(Symbols.MultiplyEquals);
                        break;
                    }
                    addSymbolToken(Symbols.Star);
                    break;
                case '/':
                    if (i + 1 < source.Length && source[i + 1] == '=')
                    {
                        captured++;
                        addSymbolToken(Symbols.DivideEquals);
                        break;
                    }
                    addSymbolToken(Symbols.Slash);
                    break;
                case '%':
                    if (i + 1 < source.Length && source[i + 1] == '=')
                    {
                        captured++;
                        addSymbolToken(Symbols.ReminderEquals);
                        break;
                    }
                    addSymbolToken(Symbols.Percent);
                    break;
                case '&':
                    if (i + 1 < source.Length)
                    {
                        if (source[i + 1] == '&')
                        {
                            captured++;
                            addSymbolToken(Symbols.AmpersandAmpersand);
                            break;
                        }
                        if (source[i + 1] == '=')
                        {
                            captured++;
                            addSymbolToken(Symbols.AndEquals);
                            break;
                        }
                    }
                    addSymbolToken(Symbols.Ampersand);
                    break;
                case '|':
                    if (i + 1 < source.Length)
                    {
                        if (source[i + 1] == '|')
                        {
                            captured++;
                            addSymbolToken(Symbols.VerticalbarVerticalbar);
                            break;
                        }
                        if (source[i + 1] == '=')
                        {
                            captured++;
                            addSymbolToken(Symbols.OrEquals);
                            break;
                        }
                    }
                    addSymbolToken(Symbols.Verticalbar);
                    break;
                case '^':
                    if (i + 1 < source.Length && source[i + 1] == '=')
                    {
                        captured++;
                        addSymbolToken(Symbols.XorEquals);
                        break;
                    }
                    addSymbolToken(Symbols.UpArrow);
                    break;
                case '~':
                    if (i + 1 < source.Length && source[i + 1] == '=')
                    {
                        captured++;
                        addSymbolToken(Symbols.NegateEquals);
                        break;
                    }
                    addSymbolToken(Symbols.Tilda);
                    break;
                case '<':
                    if (i + 1 < source.Length)
                    {
                        if (source[i + 1] == '<')
                        {
                            captured++;
                            if (i + 2 < source.Length && source[i + 2] == '=')
                            {
                                captured++;
                                addSymbolToken(Symbols.LeftShiftEquals);
                                break;
                            }
                            addSymbolToken(Symbols.LeftShift);
                            break;
                        }
                        if (source[i + 1] == '=')
                        {
                            captured++;
                            addSymbolToken(Symbols.LessEquals);
                            break;
                        }
                    }
                    addSymbolToken(Symbols.Less);
                    break;
                case '>':
                    if (i + 1 < source.Length)
                    {
                        if (source[i + 1] == '>')
                        {
                            captured++;
                            if (i + 2 < source.Length && source[i + 2] == '=')
                            {
                                captured++;
                                addSymbolToken(Symbols.RightShiftEquals);
                                break;
                            }
                            addSymbolToken(Symbols.RightShift);
                            break;
                        }
                        if (source[i + 1] == '=')
                        {
                            captured++;
                            addSymbolToken(Symbols.LessEquals);
                            break;
                        }
                    }
                    addSymbolToken(Symbols.Less);
                    break;
                case '=':
                    if (i + 1 < source.Length && source[i + 1] == '=')
                    {
                        captured++;
                        addSymbolToken(Symbols.IsEquals);
                        break;
                    }
                    addSymbolToken(Symbols.Tilda);
                    break;
                case '!':
                    if (i + 1 < source.Length && source[i + 1] == '=')
                    {
                        captured++;
                        addSymbolToken(Symbols.NotEquals);
                        break;
                    }
                    addSymbolToken(Symbols.ExclamationMark);
                    break;
                case ';':
                    addSymbolToken(Symbols.Semicolon);
                    break;
                case '{':
                    addSymbolToken(Symbols.CurlyOpen);
                    break;
                case '}':
                    addSymbolToken(Symbols.CurlyClose);
                    break;
                case '(':
                    addSymbolToken(Symbols.CircleOpen);
                    break;
                case ')':
                    addSymbolToken(Symbols.CircleClose);
                    break;
                case '[':
                    addSymbolToken(Symbols.SquareOpen);
                    break;
                case ']':
                    addSymbolToken(Symbols.SquareClose);
                    break;
                case '.':
                    addSymbolToken(Symbols.Dot);
                    break;
                default:
                    addSymbolToken(Symbols.Unknown);
                    break;
            }

            pos = i;
            col += captured;
            return;

            void addSymbolToken(Symbols symbol)
            {
                Location location = new(path, startLine, startCol, line, col + captured);

                startCol = col;
                startLine = line;

                SymbolToken symbolToken = new(symbol, location);
                tokens.Add(symbolToken);
            }
        }
    }
}