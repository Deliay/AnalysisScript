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
        return types.Any(type => type == reader.Current.Type);
    }

    private static bool MoveNextAndIs(this IEnumerator<IToken> reader, params TokenType[] types)
    {
        if (!reader.MoveNext()) throw new ParserEndOfFileException(types);

        return types.Any(type => type == reader.Current.Type);
    }

    private static bool MoveNextIfIsOrEof(this IEnumerator<IToken> reader, params TokenType[] types)
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
            var arguments = reader.ReadArguments().ToList();
            yield return new AsPipe((Token.Pipe)lex, identity, arguments);
            if (reader.Is(TokenType.Pipe)) continue;
            if (!reader.MoveNextIfIsOrEof(TokenType.NewLine, TokenType.Comment))
                break;
            while (reader.Is(SkipTypes))
                if (!reader.MoveNextIfIsOrEof(TokenType.NewLine, TokenType.Comment))
                    break;
        }
    }

    private static List<AsPipe> ReadPipes(this IEnumerator<IToken> reader)
    {
        if (!reader.MoveNext()) return [];

        return reader.EnumeratePipes().ToList();
    }

    private static AsObject ReadArgument(IEnumerator<IToken> reader)
    {
        reader.Require(TokenType.Number, TokenType.Integer, TokenType.String, TokenType.Identity);
        return reader.Current.Type switch
        {
            TokenType.Integer => new AsInteger((Token.Integer)reader.Current),
            TokenType.Number => new AsNumber((Token.Number)reader.Current),
            TokenType.String => new AsString((Token.String)reader.Current),
            TokenType.Identity => new AsIdentity((Token.Identity)reader.Current),
            _ => throw new InvalidDataException(),
        };
    }

    private static readonly TokenType[] ArgumentTypes =
        [TokenType.Number, TokenType.Integer, TokenType.String, TokenType.Identity];

    private static AsObject MoveNextAndReadArgument(IEnumerator<IToken> reader)
    {
        reader.MoveNextAndRequire(ArgumentTypes);
        return reader.Current.Type switch
        {
            TokenType.Number => new AsNumber((Token.Number)reader.Current),
            TokenType.Integer => new AsInteger((Token.Integer)reader.Current),
            TokenType.String => new AsString((Token.String)reader.Current),
            TokenType.Identity => new AsIdentity((Token.Identity)reader.Current),
            _ => throw new InvalidGrammarException(reader.Current, ArgumentTypes),
        };
    }

    private static IEnumerable<AsObject> ReadArguments(this IEnumerator<IToken> reader)
    {
        while (reader.MoveNextAndIs(ArgumentTypes))
        {
            yield return ReadArgument(reader);
        }
    }

    private static AsLet ReadLet(IEnumerator<IToken> reader)
    {
        var lex = reader.Current;

        var identity = reader.NextAndReadIdentity();

        reader.MoveNextAndRequire(TokenType.Equal);
        var argument = MoveNextAndReadArgument(reader);

        reader.MoveNextAndRequire(TokenType.NewLine);

        var pipes = ReadPipes(reader);

        return new AsLet((Token.Let)lex, identity, argument, pipes);
    }

    private static AsComment ReadComment(IEnumerator<IToken> reader)
    {
        var comment = new AsComment((Token.Comment)reader.Current);
        reader.MoveNextIfIsOrEof(SkipTypes);
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
        while (true)
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
        var reader = tokens.GetEnumerator();

        var commands = ReadCommands(reader).ToList();

        return commands.Count == 0 ? null : new AsAnalysis(commands);
    }
}