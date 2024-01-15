using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;

namespace AnalysisScript.Test.Parser;

public static class AssertExtension
{
    public static AsLet IsLet(
        this AsObject asObject,
        Action<AsIdentity>? id = default, Action<AsObject>? arg = default, Action<List<AsPipe>>? pipes = default)
    {
        var let = Assert.IsType<AsLet>(asObject);
        id?.Invoke(let.Name);
        arg?.Invoke(let.Arg);
        pipes?.Invoke(let.Pipes);

        return let;
    }

    public static AsInteger IsInteger(this AsObject asObject, int? num = default)
    {
        var number = Assert.IsType<AsInteger>(asObject);
        if (num.HasValue) Assert.Equal(num.Value, number.Value);

        return number;
    }


    public static AsIdentity IsIdentity(this AsObject asObject, string? id = null)
    {
        var asId = Assert.IsType<AsIdentity>(asObject);
        if (id is not null) Assert.Equal(id, asId.Name);

        return asId;
    }

    public static AsArray IsArray(this AsObject asObject, IEnumerable<string>? matchAllToString = null)
    {
        var asArray = Assert.IsType<AsArray>(asObject);

        if (matchAllToString is not null)
        {
            var items = asArray.Items;
            var matchList = matchAllToString.ToList();
            Assert.Equal(matchList.Count, items.Count);
            
            foreach (var (first, second) in matchList.Zip(items))
            {
                Assert.Equal(first, second.ToString());
            }
        }

        return asArray;
    }

    public static AsString IsString(this AsObject asObject, string? str)
    {
        var asString = Assert.IsType<AsString>(asObject);
        if (str is not null) Assert.Equal(str, asString.RawContent);

        return asString;
    }

    public static AsNumber IsNumber(this AsObject asObject, double? num = default)
    {
        var number = Assert.IsType<AsNumber>(asObject);
        if (num.HasValue) Assert.Equal(num.Value, number.Real);

        return number;
    }

    public static AsComment IsComment(this AsObject asObject, string? comment = default)
    {
        var asComment = Assert.IsType<AsComment>(asObject);
        if (comment is not null) Assert.Equal(comment, asComment.Content);

        return asComment;
    }

    public static AsCall IsCall(this AsObject asObject,
        Action<AsIdentity>? id = default, Action<List<AsObject>>? args = default)
    {
        var asObj = Assert.IsType<AsCall>(asObject);
        id?.Invoke(asObj.Method);
        args?.Invoke(asObj.Args);

        return asObj;
    }

    public static AsReturn IsReturn(this AsObject asObject,
        Action<AsIdentity>? id = default)
    {
        var asObj = Assert.IsType<AsReturn>(asObject);
        id?.Invoke(asObj.Variable);

        return asObj;
    }

    public static AsParam IsParam(this AsObject asObject,
        Action<AsIdentity>? id = default)
    {
        var asObj = Assert.IsType<AsParam>(asObject);
        id?.Invoke(asObj.Variable);

        return asObj;
    }
}
