namespace AnalysisScript.Interpreter.Variables;

public interface IContainer
{
    public Type UnderlyingType { get; }

    public static IContainer Of<T>(T value) => new Container<T>(value);

    public T? As<T>() => ((Container<T>)this).Value;

    public async ValueTask<T> ValueCastToAsync<T>() => (await ExprTreeHelper.GetValueCastToDelegateAsync<T>(this))();
    public T ValueCastTo<T>() => ExprTreeHelper.GetValueCastToDelegate<T>(this)();

    public string? UnderlyingToString() => ExprTreeHelper.GetExprToStringDelegate(this);

    public object? BoxedUnderlyingValue() => ExprTreeHelper.GetBoxUnderlyingValueDelegate(this)();

    public async ValueTask AwaitIfUnderlyingIsKnownAwaitable(CancellationToken cancellationToken = default)
    {
        if (UnderlyingType == typeof(Task) && As<Task>() is {} task)
        {
            await task.WaitAsync(cancellationToken);
        }
        else if (UnderlyingType == typeof(ValueTask))
        {
            await As<ValueTask>();
        }
    }
}

public readonly struct Container<T>(T? value) : IContainer
{
    public Type UnderlyingType => typeof(T);
    public T? Value => value;
}