using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;

namespace AnalysisScript.Parser;

public static class ScriptParser
{
    private static bool Is(this IEnumerator<IToken> reader, params TokenType[] types)
    {
        return types.Any(type => type == reader.Current?.Type);
    }

    private static bool MoveNextAndIs(this IEnumerator<IToken> reader, params TokenType[] types)
    {
        if (!reader.MoveNext()) throw new ParserEndOfFileException(types);

        return types.Any(type => type == reader.Current.Type);
    }

    private static bool IsOrEofAndMoveNext(this IEnumerator<IToken> reader, params TokenType[] types)
    {
        return reader.Is(types) && reader.MoveNext();
    }

    private static IEnumerator<IToken> Require(this IEnumerator<IToken> reader, params TokenType[] types)
    {
        if (!reader.Is(types))
            throw new InvalidGrammarException(reader.Current, types);

        return reader;
    }

    private static IEnumerator<IToken> Require(this IEnumerator<IToken> reader, TokenType type)
    {
        if (reader.Current.Type != type)
            throw new InvalidGrammarException(reader.Current, type);

        return reader;
    }

    private static void RequireAndMoveNext(this IEnumerator<IToken> reader, params TokenType[] types)
    {
        reader.Require(types);
        if (!reader.MoveNext()) throw new ParserEndOfFileException(types);
    }

    private static void MoveNextAndRequire(this IEnumerator<IToken> reader, params TokenType[] types)
    {
        if (!reader.MoveNext()) throw new ParserEndOfFileException(types);

        reader.Require(types);
    }

    private static T ReadNextAndRequire<T>(this IEnumerator<IToken> reader, TokenType type) where T : IToken
    {
        if (!reader.MoveNext()) throw new ParserEndOfFileException(type);

        reader.Require(type);
        return ((T)reader.Current)!;
    }

    private static void MoveNextAndRequire(this IEnumerator<IToken> reader, TokenType type)
    {
        if (!reader.MoveNext()) throw new ParserEndOfFileException(type);

        reader.Require(type);
    }


    private static AsIdentity NextAndReadIdentity(this IEnumerator<IToken> reader)
    {
        var token = reader.ReadNextAndRequire<Token.Identity>(TokenType.Identity);

        return new AsIdentity(token);
    }

    private static readonly TokenType[] SkipTypes = [TokenType.NewLine, TokenType.Comment];

    private static IEnumerable<AsPipe> EnumeratePipes(this IEnumerator<IToken> reader)
    {
        while (reader.Is(TokenType.Pipe))
        {
            var lex = reader.Current;
            var identity = reader.NextAndReadIdentity();
            var arguments = reader.ReadArguments(allowReference: true).ToList();
            yield return new AsPipe((Token.Pipe)lex, identity, arguments);
            if (reader.Is(TokenType.Pipe)) continue;
            if (!reader.IsOrEofAndMoveNext(TokenType.NewLine, TokenType.Comment))
                break;
            while (reader.Is(SkipTypes))
                if (!reader.IsOrEofAndMoveNext(TokenType.NewLine, TokenType.Comment))
                    break;
        }
    }

    private static List<AsPipe> ReadPipes(this IEnumerator<IToken> reader)
    {
        if (!reader.MoveNext()) return [];

        return reader.EnumeratePipes().ToList();
    }

    private static IEnumerable<AsObject> ReadArrayObjects(IEnumerator<IToken> reader, bool allowReference = false)
    {
        do
        {
            if (reader.MoveNextAndIs(TokenType.ArrayEnd)) yield break;
            var item = ReadArgument(reader, allowReference);
            yield return item;
        } while (reader.MoveNextAndIs(TokenType.Comma));
    }
    
    private static AsArray ReadArray(IEnumerator<IToken> reader, bool allowReference = false)
    {
        var current = reader.Current;
        var array = ReadArrayObjects(reader, allowReference).ToList();

        if (array.Count == 0) throw new InvalidGrammarException(current, allowReference ? ArgumentTypes : ArgumentTypesWithoutReference);
        return new AsArray((Token.ArrayStart)current, array);
    }

    private static AsObject ReadArgument(IEnumerator<IToken> reader, bool allowReference = false)
    {
        reader.Require(ArgumentTypes);
        return reader.Current.Type switch
        {
            TokenType.Integer => new AsInteger((Token.Integer)reader.Current),
            TokenType.Number => new AsNumber((Token.Number)reader.Current),
            TokenType.String => new AsString((Token.String)reader.Current),
            TokenType.Identity => new AsIdentity((Token.Identity)reader.Current),
            TokenType.ArrayStart => ReadArray(reader, allowReference),
            TokenType.Reference => allowReference
                ? new AsIdentity(((Token.Reference)reader.Current).ToIdentity())
                : throw new InvalidGrammarException(reader.Current, ArgumentTypesWithoutReference),
            _ => throw new InvalidDataException(),
        };
    }

    private static readonly TokenType[] ArgumentTypes =
        [TokenType.Number, TokenType.Integer, TokenType.String, TokenType.Identity, TokenType.Reference, TokenType.ArrayStart];
    private static readonly TokenType[] ArgumentTypesWithoutReference =
        [TokenType.Number, TokenType.Integer, TokenType.String, TokenType.Identity, TokenType.ArrayStart];

    private static AsObject MoveNextAndReadArgument(IEnumerator<IToken> reader, bool allowReference = false)
    {
        reader.MoveNextAndRequire(ArgumentTypes);
        return reader.Current.Type switch
        {
            TokenType.Number => new AsNumber((Token.Number)reader.Current),
            TokenType.Integer => new AsInteger((Token.Integer)reader.Current),
            TokenType.String => new AsString((Token.String)reader.Current),
            TokenType.ArrayStart => ReadArray(reader, allowReference),
            TokenType.Reference => allowReference
                ? new AsIdentity(((Token.Reference)reader.Current).ToIdentity())
                : throw new InvalidGrammarException(reader.Current, ArgumentTypesWithoutReference),
            TokenType.Identity => new AsIdentity((Token.Identity)reader.Current),
            _ => throw new InvalidGrammarException(reader.Current, ArgumentTypes),
        };
    }

    private static IEnumerable<AsObject> ReadArguments(this IEnumerator<IToken> reader, bool allowReference = false)
    {
        while (reader.MoveNextAndIs(ArgumentTypes))
        {
            yield return ReadArgument(reader, allowReference);
        }
    }

    private static AsLet ReadLet(IEnumerator<IToken> reader)
    {
        var lex = reader.Current;

        var identity = reader.NextAndReadIdentity();

        reader.MoveNextAndRequire(TokenType.Equal);
        var argument = MoveNextAndReadArgument(reader);

        if (!reader.MoveNextAndIs(TokenType.NewLine))
        {
            return new AsLet((Token.Let)lex, identity, argument, []);
        }

        var pipes = ReadPipes(reader);

        return new AsLet((Token.Let)lex, identity, argument, pipes);
    }

    private static AsComment ReadComment(IEnumerator<IToken> reader)
    {
        var comment = new AsComment((Token.Comment)reader.Current);
        reader.IsOrEofAndMoveNext(SkipTypes);
        return comment;
    }

    private static AsCall ReadCall(IEnumerator<IToken> reader)
    {
        var lex = reader.Current;

        var identity = reader.NextAndReadIdentity();

        var arguments = reader.ReadArguments().ToList();

        return new AsCall((Token.Call)lex, identity, arguments);
    }

    private static IEnumerable<AsCommand> ReadCommands(IEnumerator<IToken> reader)
    {
        reader.MoveNextAndRequire(TokenType.Let, TokenType.Comment, TokenType.Call, TokenType.NewLine, TokenType.Return,
            TokenType.Param);
        while (reader.Current is not null)
        {
            
            if (reader.Current.Type == TokenType.Comment)
            {
                ReadComment(reader);
            }
            else if (reader.Current.Type == TokenType.NewLine)
            {
                if (!reader.MoveNext()) break;
            }
            else
                yield return reader.Current.Type switch
                {
                    TokenType.Let => ReadLet(reader),
                    TokenType.Call => ReadCall(reader),
                    TokenType.Return => reader.ReadReturn(),
                    TokenType.Param => reader.ReadParam(),
                    _ => throw new InvalidGrammarException(reader.Current, TokenType.Let, TokenType.Comment,
                        TokenType.Call, TokenType.NewLine, TokenType.Return, TokenType.Param)
                };
        }
    }

    private static AsReturn ReadReturn(this IEnumerator<IToken> reader)
    {
        var ret = new AsReturn((Token.Return)reader.Current, reader.NextAndReadIdentity());
        reader.MoveNextAndRequire(TokenType.NewLine);

        return ret;
    }

    private static AsParam ReadParam(this IEnumerator<IToken> reader)
    {
        var param = new AsParam((Token.Param)reader.Current, reader.NextAndReadIdentity());
        reader.MoveNextAndRequire(TokenType.NewLine);

        return param;
    }

    public static AsAnalysis? Parse(IEnumerable<IToken> tokens)
    {
        using var reader = tokens.GetEnumerator();

        var commands = ReadCommands(reader).ToList();

        return commands.Count == 0 ? null : new AsAnalysis(commands);
    }
}