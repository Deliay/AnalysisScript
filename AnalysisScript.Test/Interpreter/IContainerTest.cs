using AnalysisScript.Interpreter.Variables;

namespace AnalysisScript.Test.Interpreter;

public class IContainerTest
{
    private class Dummy {}

    [Fact]
    public void WillWrapVariableInContainerAndResolveUseAsMethod()
    {
        var obj = new Dummy();
        var container = IContainer.Of(obj);
        Assert.Equal(obj, container.As<Dummy>());
        Assert.Equal(typeof(Dummy), container.UnderlyingType);
    }
    
    private class DummySub : Dummy {}
    private class DummySub2 : DummySub
    {
        public bool IsToStringCalled { get; private set; }
        
        public override string ToString()
        {
            IsToStringCalled = true;
            return ToStringResult;
        }

        public const string ToStringResult = "1";
    }
    [Fact]
    public void WillCastUnderlyingValueUseValueCastToMethod()
    {
        var obj = new DummySub();
        var container = IContainer.Of(obj);
        Assert.Throws<InvalidCastException>(() => Assert.Equal(obj, container.As<Dummy>()));
        Assert.Equal(obj, container.ValueCastTo<Dummy>());
        Assert.Equal(obj, container.ValueCastTo<Dummy>());
    }
    
    [Fact]
    public void WillCastUnderlyingValueUseRightClass()
    {
        var obj = new DummySub2();
        var container = IContainer.Of(obj);
        Assert.Throws<InvalidCastException>(() => Assert.Equal(obj, container.As<Dummy>()));
        Assert.Throws<InvalidCastException>(() => Assert.Equal(obj, container.As<DummySub>()));
        Assert.Equal(obj, container.ValueCastTo<Dummy>());
        Assert.Equal(obj, container.ValueCastTo<DummySub>());
    }
    
    [Fact]
    public void WillCallToStringUseExprHelper()
    {
        var obj = new DummySub2();
        var container = IContainer.Of(obj);

        var underlyingToString = container.UnderlyingToString();
        
        Assert.Equal(DummySub2.ToStringResult, underlyingToString);
        Assert.True(obj.IsToStringCalled);
        
    }
    
    [Fact]
    public void WillBoxCorrectValueToContainer()
    {
        var obj = new DummySub2();
        var container = IContainer.Of(obj);

        var underlyingToString = container.BoxedUnderlyingValue();
        
        Assert.Equal(obj, underlyingToString);
        
    }
}