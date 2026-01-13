namespace Cml.Lexing;

public class Lexer(string path, string fileName)
{
    private readonly EnumerableReader<char> er = new StreamReader(path).ReadToEnd().GetReader();
    private readonly LocationTracker lt = new(fileName);
    private string acum = "";
    private List<Token> tokens = null!;
    private LexerState state = LexerState.Default;

    private static readonly Dictionary<char, Symbols> singleSymbols = new()
    {
        { '(', Symbols.CircleOpen      },
        { ')', Symbols.CircleClose     },
        { '{', Symbols.CurlyOpen       },
        { '}', Symbols.CurlyClose      },
        { '[', Symbols.SquareOpen      },
        { ']', Symbols.SquareClose     },

        { ';', Symbols.Semicolon       },
        { ',', Symbols.Comma           },
        { '.', Symbols.Dot             },

        { '=', Symbols.Equals          },

        { '|', Symbols.Verticalbar     },
        { '^', Symbols.VerticalArrow   },
        { '&', Symbols.Ampersand       },

        { '<', Symbols.Less            },
        { '>', Symbols.Greater         },

        { '+', Symbols.Plus            },
        { '-', Symbols.Minus           },

        { '*', Symbols.Star            },
        { '/', Symbols.Slash           },
        { '%', Symbols.Percent         },

        { '!', Symbols.ExclamationMark },
    };

    private static readonly Dictionary<(char, char), Symbols> doubleSymbols = new()
    {
        { ('|', '|'), Symbols.VerticalbarVerticalbar },
        { ('&', '&'), Symbols.AmpersandAmpersand     },

        { ('=', '='), Symbols.IsEquals               },
        { ('!', '='), Symbols.NotEquals              },

        { ('<', '='), Symbols.LessEquals             },
        { ('>', '='), Symbols.GreaterEquals          },

        { ('<', '<'), Symbols.LeftShift              },
        { ('>', '>'), Symbols.RightShift             },

        { ('+', '+'), Symbols.Increment              },
        { ('-', '-'), Symbols.Decrement              },
    };

    private static readonly Dictionary<char, char> stringEscaping = new()
    {
        { 'n', '\n' },
        { 'r', '\r' },
        { 't', '\t' },
        { '0', '\0' },
        { '"', '"'  },
        { '\'', '\''},
        { '\\', '\\'},
    };

    public Token[] GetTokens()
    {
        if (tokens != null)
            return [.. tokens];
        tokens = [];

        while (true)
        {
            if (!er.Read(out char c))
            {
                endAcum();
                return [.. tokens];
            }

            switch (state)
            {
                case LexerState.Default:
                    endAcum();
                    lt.UpdateStart();
                    lt.NextCol();

                    if (char.IsLetter(c) || c == '_')
                    {
                        acum += c;
                        state = LexerState.Identifyer;
                        continue;
                    }
                    if (char.IsDigit(c))
                    {
                        acum += c;
                        state = LexerState.Number;
                        continue;
                    }
                    if (c == '\n')
                    {
                        lt.NextLine();
                        state = LexerState.Default;
                        continue;
                    }
                    if (c == '\r')
                    {
                        state = LexerState.CarretReturn;
                        continue;
                    }
                    if (char.IsWhiteSpace(c))
                    {
                        state = LexerState.Default;
                        continue;
                    }
                    if (c == '"')
                    {
                        state = LexerState.String;
                        continue;
                    }
                    if (c == '\'')
                    {
                        state = LexerState.Char;
                        continue;
                    }
                    if (singleSymbols.ContainsKey(c))
                    {
                        acum += c;
                        state = LexerState.Symbol;
                        continue;
                    }
                    tokens.Add(new Token<string>($"Unexpected character `{c}`", TokenType.Unknown, lt.GetLocation()));
                    state = LexerState.Default;
                    continue;

                case LexerState.Identifyer:
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        lt.NextCol();
                        acum += c;
                        continue;
                    }
                    goto case LexerState.Default;

                case LexerState.Number:
                    if (char.IsDigit(c))
                    {
                        acum += c;
                        lt.NextCol();
                        continue;
                    }
                    if (c == '.')
                    {
                        acum += c;
                        lt.NextCol();
                        state = LexerState.NumberDot;
                        continue;
                    }
                    goto case LexerState.Default;

                case LexerState.NumberDot:
                    if (char.IsDigit(c))
                    {
                        acum += c;
                        lt.NextCol();
                        state = LexerState.NumberAfterDot;
                        continue;
                    }
                    Location loc = lt.GetLocation();
                    Location numloc = new(loc.File, loc.StartLine, loc.StartColumn, loc.EndLine, loc.EndColumn - 1);
                    Location dotloc = new(loc.File, loc.EndLine, loc.EndColumn - 1, loc.EndLine, loc.EndColumn);

                    tokens.Add(new Token<ulong>(ulong.Parse(acum[..^1]), TokenType.Literal, numloc));
                    tokens.Add(new Token<Symbols>(Symbols.Dot, TokenType.Symbol, dotloc));

                    acum = "";

                    goto case LexerState.Default;

                case LexerState.NumberAfterDot:
                    if (char.IsDigit(c))
                    {
                        acum += c;
                        lt.NextCol();
                        continue;
                    }
                    goto case LexerState.Default;

                case LexerState.CarretReturn:
                    if (c == '\n')
                    {
                        lt.NextLine(true);
                        state = LexerState.Default;
                        continue;
                    }
                    tokens.Add(new Token<string>("Unexpected \\r symbol", TokenType.Unknown, lt.GetLocation()));
                    goto case LexerState.Default;

                case LexerState.Symbol:
                    if (doubleSymbols.ContainsKey((acum[0], c)))
                    {
                        lt.NextCol();
                        acum += c;
                        endAcum();
                        state = LexerState.Default;
                        continue;
                    }
                    endAcum();
                    goto case LexerState.Default;

                case LexerState.String:
                    lt.NextCol();
                    if (c == '"')
                    {
                        endAcum();
                        state = LexerState.Default;
                        continue;
                    }
                    // if (c == '\n' || c == '\r')
                    // {
                    //     tokens.Add(new Token<string>($"Unclosed string `{acum}`", TokenType.Unknown, lt.GetLocation()));
                    //     acum = "";
                    //     goto case LexerState.Default;
                    // }
                    if (c == '\\')
                    {
                        state = LexerState.StringAfterBackslash;
                        continue;
                    }
                    acum += c;
                    continue;

                case LexerState.StringAfterBackslash:
                    lt.NextCol();
                    if (stringEscaping.TryGetValue(c, out var d))
                        acum += d;
                    else
                        acum += c;
                    state = LexerState.String;
                    continue;

                default:
                    throw new Exception($"{state} not implemented");
            }
        }
    }
    
    private void endAcum()
    {
        if (acum.Length == 0)
            return;

        switch (state)
        {
            case LexerState.Number:
                if (ulong.TryParse(acum, out var num))
                    tokens.Add(new Token<ulong>(num, TokenType.Literal, lt.GetLocation()));
                else
                    tokens.Add(new Token<string>($"Invalid digit literal {acum}", TokenType.Unknown, lt.GetLocation()));
                break;

            case LexerState.NumberAfterDot:
                if (double.TryParse(acum, out var fnum))
                    tokens.Add(new Token<double>(fnum, TokenType.Literal, lt.GetLocation()));
                else
                    tokens.Add(new Token<string>($"Invalid floating point literal {acum}", TokenType.Unknown, lt.GetLocation()));
                break;

            case LexerState.Identifyer:
                if (Enum.TryParse<Keywords>(acum, true, out var kwd))
                {
                    if (kwd == Keywords.True || kwd == Keywords.False)
                        tokens.Add(new Token<bool>(kwd == Keywords.True, TokenType.Literal, lt.GetLocation()));
                    else
                        tokens.Add(new Token<Keywords>(kwd, TokenType.Keyword, lt.GetLocation()));
                }
                else
                    tokens.Add(new Token<string>(acum, TokenType.Identifier, lt.GetLocation()));
                break;

            case LexerState.String:
                tokens.Add(new Token<string>(acum, TokenType.Literal, lt.GetLocation()));
                break;

            case LexerState.Symbol:
                if (acum.Length == 1)
                    tokens.Add(new Token<Symbols>(singleSymbols[acum[0]], TokenType.Symbol, lt.GetLocation()));
                else if (acum.Length == 2)
                    tokens.Add(new Token<Symbols>(doubleSymbols[(acum[0], acum[1])], TokenType.Symbol, lt.GetLocation()));
                else
                    throw new Exception($"{acum.Length}-length symbol");

                break;

            default:
                throw new Exception($"Unknown state {state}");
        }

        lt.UpdateStart();
        acum = "";
    }


    private enum LexerState
    {
        Default,

        String,
        StringAfterBackslash,

        Identifyer,

        Number,
        NumberDot,
        NumberAfterDot,
        CarretReturn,
        Char,
        Symbol,
    }

    private class LocationTracker(string fileName)
    {
        private readonly string FileName = fileName;
        private int CurrentCol;
        private int CurrentLine;
        private int StartCol;
        private int StartLine;

        public void NextCol()
            => CurrentCol++;

        public void NextLine(bool update = false)
        {
            CurrentCol = 0;
            CurrentLine++;

            if (update)
                UpdateStart();
        }

        public void UpdateStart()
        {
            StartCol = CurrentCol;
            StartLine = CurrentLine;
        }

        public Location GetLocation(bool update = false)
        {
            Location loc = new(FileName, StartLine, StartCol, CurrentLine, CurrentCol);
            if (update)
                UpdateStart();
            return loc;
        }

        public void Reset()
        {
            CurrentCol = 0;
            CurrentLine = 0;
            StartCol = 0;
            StartLine = 0;
        }
    }
}
