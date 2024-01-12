namespace AnalysisScript.Lexical;

public static class LexicalAnalyzer
{
    public static IEnumerable<IToken> Analyze(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) yield break;
        var line = 1;
        var pos = 0;
        char current;

        do
        {
            Peek();
            if (current == '\n') line += 1;

            if (current == '|')
            {
                yield return new Token.Pipe(pos, line);
            }
            else if (current == '=')
            {
                yield return new Token.Equal(pos, line);
            }
            else if (current == '\n') yield return new Token.NewLine(pos, line);
            else if (char.IsWhiteSpace(current)) continue;
            else if (char.IsNumber(current))
            {
                var integer = current - '0';
                while (HasMore() && char.IsNumber(Peek()))
                {
                    integer *= 10;
                    integer += current - '0';
                }

                if (current == '.' && HasMore())
                {
                    double real = integer;
                    double d = 10;
                    while (HasMore() && char.IsNumber(Peek()))
                    {
                        real += char.GetNumericValue(current) / d;
                        d *= 10;
                    }

                    if (!char.IsNumber(current)) Back();
                    yield return new Token.Number(real, pos, line);
                }
                else
                {
                    if (!char.IsNumber(current)) Back();
                    yield return new Token.Integer(integer, pos, line);
                }
            }
            else if (IsIdentityStart(current))
            {
                var val = $"{current}";
                while (HasMore() && IsIdentity(Peek()))
                {
                    val += current;
                }

                yield return val switch
                {
                    "let" => new Token.Let(pos, line),
                    "param" => new Token.Param(pos, line),
                    "call" => new Token.Call(pos, line),
                    "return" => new Token.Return(pos, line),
                    _ => new Token.Identity(val, pos, line)
                };
            }
            else if (IsQuote(current))
            {
                var val = "";
                while (HasMore() && !IsQuote(Peek()))
                {
                    if (current == '\\')
                    {
                        Peek();
                        val += current switch
                        {
                            '"' => '\"',
                            't' => '\t',
                            's' => ' ',
                            'b' => '\b',
                            'n' => '\n',
                            _ => throw new InvalidTokenException(pos, $"\\{current}")
                        };
                    }
                    else val += current;
                }

                yield return new Token.String(val, pos, line);
            }
            else if (current == '#')
            {
                var val = "";
                while (HasMore() && Peek() != '\n')
                {
                    val += current;
                }

                yield return new Token.Comment(val, pos, line);
                yield return new Token.NewLine(pos, ++line);
            }
            else throw new InvalidTokenException(pos, current.ToString());
        } while (HasMore());

        yield return new Token.NewLine(pos, ++line);
        yield break;

        char Peek() => current = source[pos++];

        void Back() => pos -= 1;

        bool IsIdentityStart(char ch) => char.IsLetter(ch) || ch == '_';

        bool IsIdentity(char ch) => IsIdentityStart(ch) || char.IsNumber(ch);

        bool IsQuote(char ch) => ch == '"';

        bool HasMore() => pos < source.Length;
    }
}