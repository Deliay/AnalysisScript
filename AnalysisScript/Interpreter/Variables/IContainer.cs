﻿namespace AnalysisScript.Interpreter.Variables;

public interface IContainer
{
    public Type UnderlyingType { get; }

    public static IContainer Of<T>(T value) => new Container<T>(value);

    public T? As<T>() => ((Container<T>)this).Value;

    public async ValueTask<T> ValueCastTo<T>() => (await ExprTreeHelper.ValueCastTo<T>(this))();

    public string? UnderlyingToString() => ExprTreeHelper.ExprToString(this);

    public object? BoxedUnderlyingValue() => ExprTreeHelper.BoxUnderlyingValue(this)();
}

public readonly struct Container<T>(T? value) : IContainer
{
    public Type UnderlyingType => typeof(T);
    public T? Value => value;
}