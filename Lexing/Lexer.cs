using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Cml.Lexing;

public class Lexer(string fileName) : IEnumerable<Token>
{
    private readonly EnumerableReader<char> er = new StreamReader(fileName).ReadToEnd().GetReader();
    private readonly LocationTracker lt = new(fileName);
    private string acum = "";


    private Token? readNextToken()
    {
        while (true)
        {
            Token? token;
            char c, d;

            if (!er.Peek(out c))
                return endAcum(out token) ? token : null;

            if (c == '_' || char.IsLetterOrDigit(c))
            {
                acum += c;
                lt.NextCol();
                er.Read(out c);
                continue;
            }

            if (endAcum(out token))
                return token;

            lt.NextCol();

            switch (c)
            {
                case '\r':
                    if (er.Peek(out d) && d == '\n')
                        goto case '\n';
                    return new Token<string>("Unexpected \\r symbol", TokenType.Unknown, lt.GetLocation(true));
                case '\n':
                    lt.NextLine(true);
                    if (!er.Read(out _))
                        return null;
                    continue;
                case '"':
                    token = readString();
                    break;
                case '\'':
                    token = readChar();
                    break;
                default:
                    if (char.IsWhiteSpace(c))
                    {
                        if (!er.Read(out _))
                            return null;
                        lt.UpdateStart();
                        continue;
                    }
                    if (char.IsSymbol(c) || char.IsPunctuation(c))
                    {
                        token = readSymbol();
                        break;
                    }
                    return new Token<string>($"Unexpected character `{c}`", TokenType.Unknown, lt.GetLocation(true));
            }

            lt.UpdateStart();
            return token;
        }
    }

    private Token readSymbol()
    {
        er.Read(out char c);
        Symbols symbol = Symbols.Unknown;

        switch (c)
        {
            case '(':
                symbol = Symbols.CircleOpen;
                break;
            case ')':
                symbol = Symbols.CircleClose;
                break;
            case '{':
                symbol = Symbols.CurlyOpen;
                break;
            case '}':
                symbol = Symbols.CurlyClose;
                break;
            case '[':
                symbol = Symbols.SquareOpen;
                break;
            case ']':
                symbol = Symbols.SquareClose;
                break;
            case ';':
                symbol = Symbols.Semicolon;
                break;
            case ',':
                symbol = Symbols.Comma;
                break;
            case '*':
                symbol = Symbols.Star;
                break;
            case '=':
                if (er.Read(out c) && c == '=')
                    symbol = Symbols.IsEquals;
                else
                    symbol = Symbols.Equals;
                break;
        }

        if (symbol == Symbols.Unknown)
            return new Token<string>("Unknown symbol", TokenType.Unknown, lt.GetLocation());

        return new Token<Symbols>(symbol, TokenType.Symbol, lt.GetLocation());
    }

    private Token readChar()
    {
        er.Read(out _);
        er.Read(out char c);
        lt.NextCol();
        if (c == '\\')
        {
            if (!er.Read(out c))
                return new Token<string>("Unclosed char literal", TokenType.Unknown, lt.GetLocation());

            lt.NextCol();
            char? d = c switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '0' => '\0',
                '\\' or '\'' => c,
                _ => null,
            };

            if (!er.Peek(out c) || c != '\'')
                return new Token<string>("Unclosed char literal", TokenType.Unknown, lt.GetLocation());

            lt.NextCol();

            if (!d.HasValue)
                return new Token<string>("Incorrect escaping", TokenType.Unknown, lt.GetLocation());

            c = d.Value;
        }

        if (!er.Read(out char b) || b != '\'')
            return new Token<string>("Unclosed char literal", TokenType.Unknown, lt.GetLocation());

        lt.NextCol();

        return new Token<char>(c, TokenType.Literal, lt.GetLocation());
    }

    private Token readString()
    {
        StringBuilder sb = new();
        er.Read(out _);
        while (true)
        {
            if (!er.Read(out char c))
                return new Token<string>("Unclosed string literal", TokenType.Unknown, lt.GetLocation());

            if (c == '\n')
            {
                Token token = new Token<string>("Unclosed string literal", TokenType.Unknown, lt.GetLocation());
                lt.NextLine();
                return token;
            }
            lt.NextCol();

            if (c == '"')
                return new Token<string>(sb.ToString(), TokenType.Literal, lt.GetLocation());

            if (c == '\\')
            {
                if (!er.Read(out c))
                    return new Token<string>("Unclosed string literal", TokenType.Unknown, lt.GetLocation());

                char? d = c switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '0' => '\0',
                    '\\' or '\"' => c,
                    _ => null,
                };

                if (!d.HasValue)
                    return new Token<string>("Incorrect escaping", TokenType.Unknown, lt.GetLocation());

                c = d.Value;
            }

            sb.Append(c);
        }
    }

    private bool endAcum([MaybeNullWhen(false)] out Token token)
    {
        token = null;

        if (acum.Length == 0)
        {
            lt.UpdateStart();
            return false;
        }

        if (char.IsDigit(acum[0]))
        {
            if (!processDigit(out token))
                token = new Token<string>($"Invalid digit literal {acum}", TokenType.Unknown, lt.GetLocation());
        }
        else if (Enum.TryParse<Keywords>(acum, true, out var result))
        {
            token = new Token<Keywords>(result, TokenType.Keyword, lt.GetLocation());
        }
        else
        {
            token = new Token<string>(acum, TokenType.Identifier, lt.GetLocation());
        }

        acum = "";
        lt.UpdateStart();

        return true;
    }

    private bool processDigit([MaybeNullWhen(false)] out Token token)
    {
        if (ulong.TryParse(acum, out ulong num))
        {
            token = new Token<ulong>(num, TokenType.Literal, lt.GetLocation());
            return true;
        }
        token = null;
        return false;
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

    public IEnumerator<Token> GetEnumerator()
        => new TokenEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    private class TokenEnumerator(Lexer l) : IEnumerator<Token>
    {
        public Token Current { get; private set; } = default!;

        object IEnumerator.Current => Current;

        private readonly Lexer l = l;

        public void Dispose()
            => Reset();

        public bool MoveNext()
        {
            Token? token = l.readNextToken();
            if (token == null)
                return false;

            Current = token;
            return true;
        }

        public void Reset()
        {
            l.er.Reset();
            l.lt.Reset();
        }
    }
}
