using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser
{
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


        private static IEnumerable<AsPipe> EnumeratePipes(this IEnumerator<IToken> reader)
        {
            while (reader.Is(TokenType.Pipe))
            {
                var lex = reader.Current;
                var identity = reader.NextAndReadIdentity();
                var arguments = reader.ReadArguments().ToList();
                yield return new AsPipe((Token.Pipe)lex, identity, arguments);
                reader.RequireAndMoveNext(TokenType.NewLine, TokenType.Comment);
            }
        }

        private static List<AsPipe> ReadPipes(this IEnumerator<IToken> reader)
        {
            if (!reader.MoveNext()) return [];

            return reader.EnumeratePipes().ToList();
        }

        private static AsObject ReadArgument(IEnumerator<IToken> reader)
        {
            reader.Require(TokenType.Number, TokenType.String, TokenType.Identity);
            return reader.Current.Type switch
            {
                TokenType.Number => new AsNumber((Token.Number)reader.Current),
                TokenType.String => new AsString((Token.String)reader.Current),
                TokenType.Identity => new AsIdentity((Token.Identity)reader.Current),
                _ => throw new InvalidDataException(),
            };
        }

        private static AsObject MoveNextAndReadArgument(IEnumerator<IToken> reader)
        {
            reader.MoveNextAndRequire(TokenType.Number, TokenType.String, TokenType.Identity);
            return reader.Current.Type switch
            {
                TokenType.Number => new AsNumber((Token.Number)reader.Current),
                TokenType.String => new AsString((Token.String)reader.Current),
                TokenType.Identity => new AsIdentity((Token.Identity)reader.Current),
                _ => throw new InvalidDataException(),
            };
        }

        private static IEnumerable<AsObject> ReadArguments(this IEnumerator<IToken> reader)
        {
            while (reader.MoveNextAndIs(TokenType.Number, TokenType.String, TokenType.Identity))
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
            reader.MoveNextAndRequire(TokenType.NewLine);
            return comment;
        }

        private static AsUi ReadUi(IEnumerator<IToken> reader)
        {
            var lex = reader.Current;
            reader.MoveNextAndRequire(TokenType.NewLine);
            var pipes = reader.ReadPipes();

            return pipes == null || pipes.Count == 0
                ? throw new InvalidGrammarException(reader.Current, TokenType.Pipe)
                : new AsUi((Token.Ui)lex, pipes);
        }

        private static IEnumerable<AsCommand> ReadCommands(IEnumerator<IToken> reader)
        {
            reader.MoveNextAndRequire(TokenType.Let, TokenType.Comment, TokenType.Ui, TokenType.NewLine, TokenType.Return, TokenType.Param);
            while (true)
            {
                if (reader.Current.Type == TokenType.Let) yield return ReadLet(reader);
                else if (reader.Current.Type == TokenType.Comment) yield return ReadComment(reader);
                else if (reader.Current.Type == TokenType.Ui) yield return ReadUi(reader);
                else if (reader.Current.Type == TokenType.NewLine) { if (!reader.MoveNext()) break; }
                else if (reader.Current.Type == TokenType.Return) yield return reader.ReadReturn();
                else if (reader.Current.Type == TokenType.Param) yield return reader.ReadParam();
                else throw new InvalidGrammarException(reader.Current, TokenType.Let, TokenType.Comment, TokenType.Ui, TokenType.NewLine, TokenType.Return, TokenType.Param);
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

            List<AsCommand> commands = ReadCommands(reader).ToList();

            if (commands == null) return null;
            else return new AsAnalysis(commands);
        }
    }
}
