using AnalysisScript.Lexical;

namespace AnalysisScript
{
    public static class LexicalAnalyzer
    {
        public static IEnumerable<IToken> Analyze(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) yield break;

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
                    yield return new Token.Pipe(pos);
                }
                else if (current == '=')
                {
                    yield return new Token.Equal(pos);
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
                    yield return new Token.Number(real, pos);
                }
                else if (IsIdentityStart(current))
                {
                    string val = $"{current}";
                    while (HasMore() && IsIdentity(Peek()))
                    {
                        val += current;
                    }
                    if (val == "let") yield return new Token.Let(pos);
                    else if (val == "param") yield return new Token.Param(pos);
                    else if (val == "ui") yield return new Token.Ui(pos);
                    else yield return new Token.Identity(val, pos);
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
                            else throw new UnknownTokenException(pos, $"\\{current}");
                        }
                        else val += current;
                    }
                    yield return new Token.String(val, pos);
                }
                else if (char.IsWhiteSpace(current)) continue;
                else throw new UnknownTokenException(pos, current.ToString());
            } while (HasMore());
        }
    }
}
