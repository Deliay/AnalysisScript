namespace AnalysisScript.Lexical
{
    public static class LexicalAnalyzer
    {
        public static IEnumerable<IToken> Analyze(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) yield break;
            int line = -1;
            int pos = 0;
            char current;

            bool HasMore() => pos < source.Length;
            char Peek() => current = source[pos++];
            bool IsIdentityStart(char ch) => char.IsLetter(ch) || ch == '_';
            bool IsIdentity(char ch) => IsIdentityStart(ch) || char.IsNumber(ch);
            bool IsQuote(char ch) => ch == '"';

            do
            {
                Peek();
                if (current == '|')
                {
                    yield return new Token.Pipe(pos, line);
                }
                else if (current == '=')
                {
                    yield return new Token.Equal(pos, line);
                }
                else if (char.IsNumber(current))
                {
                    double real = char.GetNumericValue(current);
                    while (HasMore() && char.IsNumber(Peek()))
                    {
                        real *= 10;
                        real += char.GetNumericValue(current);
                    }
                    if (current == '.' && HasMore())
                    {
                        double d = 10;
                        while (HasMore() && char.IsNumber(Peek()))
                        {
                            real += char.GetNumericValue(current) / d;
                            d *= 10;
                        }
                    }
                    yield return new Token.Number(real, pos, line);
                }
                else if (IsIdentityStart(current))
                {
                    string val = $"{current}";
                    while (HasMore() && IsIdentity(Peek()))
                    {
                        val += current;
                    }
                    if (val == "let") yield return new Token.Let(pos, line);
                    else if (val == "param") yield return new Token.Param(pos, line);
                    else if (val == "ui") yield return new Token.Ui(pos, line);
                    else if (val == "return") yield return new Token.Return(pos, line);
                    else yield return new Token.Identity(val, pos, line);
                }
                else if (IsQuote(current))
                {
                    string val = "";
                    while (HasMore() && !IsQuote(Peek()))
                    {
                        if (current == '\\')
                        {
                            if (Peek() == '"') val += '\"';
                            else if (current == 't') val += '\t';
                            else if (current == 's') val += ' ';
                            else if (current == 'b') val += '\b';
                            else throw new InvalidTokenException(pos, $"\\{current}");
                        }
                        else val += current;
                    }
                    yield return new Token.String(val, pos, line);
                }
                else if (current == '#')
                {
                    string val = "";
                    while (HasMore() && Peek() != '\n')
                    {
                        val += current;
                    }
                    yield return new Token.Comment(val, pos, ++line);
                    line += 1;
                    yield return new Token.NewLine(pos, ++line);
                }
                else if (current == '\n') yield return new Token.NewLine(pos, ++line);
                else if (char.IsWhiteSpace(current)) continue;
                else throw new InvalidTokenException(pos, current.ToString());
            } while (HasMore());

            yield return new Token.NewLine(pos, ++line);
        }
    }
}
