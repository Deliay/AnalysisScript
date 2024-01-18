using System.Linq.Expressions;
using System.Reflection;
using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Interpreter.Variables.Method;

namespace AnalysisScript.Test.Interpreter;

public static class StaticRegistry
{
    [AsMethod(Name = "id")]
    [AsMethod(Name = "id2")]
    public static T Identity<T>(T t) => t;

    public readonly static MethodInfo ActualMethod = typeof(StaticRegistry).GetMethod(nameof(Identity))!;
}

public class InstanceRegistry
{
    [AsMethod(Name = "id")]
    [AsMethod(Name = "id2")]
    public static T Identity<T>(T t) => t;
    public readonly static MethodInfo ActualStaticMethod = typeof(InstanceRegistry).GetMethod(nameof(Identity))!;

    [AsMethod(Name = "foo")]
    [AsMethod(Name = "bar")]
    public T Foo<T>(T t) => Identity(t);


    [AsMethod(Name = "foo")]
    [AsMethod(Name = "bar")]
    public ValueTask Bar() => ValueTask.CompletedTask;

    public const string MethodFoo = "foo";
    public const string MethodBar = "bar";

    public readonly static MethodInfo ActualInstanceMethodFoo = typeof(InstanceRegistry).GetMethod(nameof(Foo))!;
    public readonly static MethodInfo ActualInstanceMethodBar = typeof(InstanceRegistry).GetMethod(nameof(Bar))!;
}

public class MethodContextTest
{
    [Fact]
    public void WillScanStaticRegistryAndReturnRightResult()
    {
        var registrations = MethodContext.ScanClassForStaticMethod(typeof(StaticRegistry)).ToList();

        Assert.Equal(2, registrations.Count);
        Assert.All(registrations, reg => Assert.Equal(StaticRegistry.ActualMethod, reg.method));
        Assert.Contains(registrations, method => method.name == "id");
        Assert.Contains(registrations, method => method.name == "id2");
    }
    [Fact]
    public void WillScanRegistryAndReturnStaticMethods()
    {
        var registrations = MethodContext.ScanClassForStaticMethod(typeof(InstanceRegistry)).ToList();

        Assert.Equal(2, registrations.Count);
        Assert.All(registrations, reg => Assert.Equal(InstanceRegistry.ActualStaticMethod, reg.method));
        Assert.Contains(registrations, method => method.name == "id");
        Assert.Contains(registrations, method => method.name == "id2");
        Assert.DoesNotContain(registrations, reg => reg.name == "foo");
        Assert.DoesNotContain(registrations, reg => reg.name == "bar");
    }
    [Fact]
    public void WillScanRegistryAndReturnInstanceDelegates()
    {
        var instance = new InstanceRegistry();
        var registrations = MethodContext.ScanInstanceForInstanceMethod(instance).ToList();

        Assert.Equal(4, registrations.Count);
        Assert.Contains(registrations, reg => reg.name == "foo"
                                            && reg.callInfo.method == InstanceRegistry.ActualInstanceMethodFoo);
        Assert.Contains(registrations, reg => reg.name == "bar"
                                            && reg.callInfo.method == InstanceRegistry.ActualInstanceMethodFoo);
        Assert.Contains(registrations, reg => reg.name == "foo"
                                            && reg.callInfo.method == InstanceRegistry.ActualInstanceMethodBar);
        Assert.Contains(registrations, reg => reg.name == "bar"
                                            && reg.callInfo.method == InstanceRegistry.ActualInstanceMethodBar);
        Assert.DoesNotContain(registrations, reg => reg.name == "id");
        Assert.DoesNotContain(registrations, reg => reg.name == "id2");
    }
    private class TestContext : MethodContext
    {

    }
    [Fact]
    public void WillRegisterResolvedStaticMethodToRegistry()
    {
        var ctx = new MethodContext();

        ctx.ScanAndRegisterStaticFunction(typeof(InstanceRegistry));
        Assert.Equal(2, ctx.AllRawMethod.Count);
        Assert.All(ctx.AllRawMethod, registry =>
        {
            var (method, @this) = Assert.Single(registry.Value);
            Assert.Equal(InstanceRegistry.ActualStaticMethod, method);
            Assert.Null(@this);
        });
    }
    [Fact]
    public void WillRegisterResolvedInstanceMethodToRegistry()
    {
        var ctx = new MethodContext();
        var inst = new InstanceRegistry();

        ctx.ScanAndRegisterInstanceFunction(inst);
        Assert.Equal(2, ctx.AllRawMethod.Count);
        Assert.All(ctx.AllRawMethod, registry =>
        {
            var methods = registry.Value.ToList();
            Assert.Equal(2, methods.Count);
            Assert.Contains(methods, method =>
                method.Item2?.Value == inst && method.Item1 == InstanceRegistry.ActualInstanceMethodFoo);
            Assert.Contains(methods, method =>
                method.Item2?.Value == inst && method.Item1 == InstanceRegistry.ActualInstanceMethodBar);
        });
    }
    [Fact]
    public void WillCompileAndCacheMethodToMethodCallExpression()
    {
        var inst = new InstanceRegistry();
        var ctx = new MethodContext();

        ctx.ScanAndRegisterInstanceFunction(inst);

        var methodExpr = ctx.GetMethod(null, InstanceRegistry.MethodBar, []);
        Assert.Equal(InstanceRegistry.ActualInstanceMethodBar, methodExpr.Method);
        var @this = Assert.IsType<ConstantExpression>(methodExpr.Object);
        Assert.Equal(inst, @this.Value);
    }
    [Fact]
    public void WillMakeGenericMethodAndBuildMethodExpr()
    {
        var inst = new InstanceRegistry();
        var ctx = new MethodContext();
        var arg = ExprTreeHelper.GetConstantValueLambda(1);
        ctx.ScanAndRegisterInstanceFunction(inst);

        var methodExpr = ctx.GetMethod(null, InstanceRegistry.MethodBar, [arg]);

        var @this = Assert.IsType<ConstantExpression>(methodExpr.Object);
        Assert.Equal(inst, @this.Value);

        Assert.True(methodExpr.Method.IsGenericMethod);
        
        Assert.Equal(InstanceRegistry.ActualInstanceMethodFoo, methodExpr.Method.GetGenericMethodDefinition());
        Assert.Equal(typeof(int), Assert.Single(methodExpr.Method.GetGenericArguments()));
    }
    [Fact]
    public void WillMakeDifferentGenericMethodAndBuildMethodExpr()
    {
        var inst = new InstanceRegistry();
        var ctx = new MethodContext();
        var arg1 = ExprTreeHelper.GetConstantValueLambda(1);
        var arg2 = ExprTreeHelper.GetConstantValueLambda(1.1);
        ctx.ScanAndRegisterInstanceFunction(inst);

        var methodExpr1 = ctx.GetMethod(null, InstanceRegistry.MethodBar, [arg1]);

        var @this1 = Assert.IsType<ConstantExpression>(methodExpr1.Object);
        Assert.Equal(inst, @this1.Value);

        Assert.True(methodExpr1.Method.IsGenericMethod);
        
        Assert.Equal(InstanceRegistry.ActualInstanceMethodFoo, methodExpr1.Method.GetGenericMethodDefinition());
        Assert.Equal(typeof(int), Assert.Single(methodExpr1.Method.GetGenericArguments()));


        var methodExpr2 = ctx.GetMethod(null, InstanceRegistry.MethodBar, [arg2]);

        var @this2 = Assert.IsType<ConstantExpression>(methodExpr2.Object);
        Assert.Equal(inst, @this2.Value);

        Assert.True(methodExpr2.Method.IsGenericMethod);
        
        Assert.Equal(InstanceRegistry.ActualInstanceMethodFoo, methodExpr2.Method.GetGenericMethodDefinition());
        Assert.Equal(typeof(double), Assert.Single(methodExpr2.Method.GetGenericArguments()));
    }

    [Fact]
    public void WillBindFirstArgumentToFunction()
    {
        var inst = new InstanceRegistry();
        var ctx = new MethodContext();
        var arg = ExprTreeHelper.GetConstantValueLambda(1);
        ctx.ScanAndRegisterInstanceFunction(inst);

        var methodExpr = ctx.GetMethod(arg, InstanceRegistry.MethodBar, [arg]);

        var @this = Assert.IsType<ConstantExpression>(methodExpr.Object);
        Assert.Equal(inst, @this.Value);

        Assert.True(methodExpr.Method.IsGenericMethod);
        
        Assert.Equal(InstanceRegistry.ActualInstanceMethodFoo, methodExpr.Method.GetGenericMethodDefinition());
        Assert.Equal(typeof(int), Assert.Single(methodExpr.Method.GetGenericArguments()));
    }

    [Fact]
    public void WillThrownIfRegisterStaticFunctionToInstanceRegistry()
    {
        var ctx = new MethodContext();
        var (name, method) = MethodContext.GetMethodInfoForRegistration(InstanceRegistry.Identity<int>).ToList()[0];
        Assert.Throws<InvalidDataException>(() =>
        {
            ctx.RegisterInstanceFunction(name, (new InstanceRegistry(), method));
        });
    }

    [Fact]
    public void WillThrownIfRegisterInstanceMethodMissingThis()
    {
        var ctx = new MethodContext();
        var inst = new InstanceRegistry();
        var (name, (@this, method)) = MethodContext.ScanInstanceForInstanceMethod(inst).First();
        Assert.Throws<NullReferenceException>(() =>
        {
            ctx.RegisterInstanceFunction(name, (null, method));
        });
    }

    [Fact]
    public void WillThrownIfDuplicateRegisterSameClassInstances()
    {
        var ctx = new MethodContext();
        var inst1 = new InstanceRegistry();
        var inst2 = new InstanceRegistry();
        ctx.ScanAndRegisterInstanceFunction(inst1);
        Assert.Throws<InvalidOperationException>(() =>
        {
            ctx.ScanAndRegisterInstanceFunction(inst2);
        });
    }

    [Fact]
    public void WillThrownIfRegisterVoidMethod()
    {
        var ctx = new MethodContext();
        var foo = () => { };
        var bar = () => ValueTask.CompletedTask;
        ctx.RegisterInstanceFunction("bar", bar);
        Assert.Throws<InvalidOperationException>(() =>
        {
            ctx.RegisterInstanceFunction("foo", foo);
        });
    }

    [Fact]
    public void TestInterfaceMatching()
    {
        var guidEqualResultList = typeof(List<int>).GetInterfaces()
            .Where(item => item.GUID == typeof(IEnumerable<>).GUID)
            .ToList();

        var typeEqualResultList = typeof(List<int>).GetInterfaces()
            .Where(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .ToList();
        
        Assert.Equal(guidEqualResultList.Count, typeEqualResultList.Count);
        Assert.Equal(Assert.Single(guidEqualResultList), Assert.Single(typeEqualResultList));
    }
}
