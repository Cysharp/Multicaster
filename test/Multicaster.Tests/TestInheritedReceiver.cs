namespace Multicaster.Tests;

public interface ITestInheritedReceiver
{
    void Parameter_Zero();
    Task ClientResult_Parameter_Zero_NoReturnValue();
}

public interface ITestInheritedReceiver2 : ITestInheritedReceiver
{
    void Parameter_One(int arg1);
}

public interface ITestInheritedReceiver3 : ITestInheritedReceiver2
{
    void Parameter_Many(int arg1, string arg2, bool arg3);
    Task<int> ClientResult_Parameter_Two_WithReturnValue(int arg1, bool arg2);
}

public class TestInheritedReceiver : ITestInheritedReceiver3
{
    public static readonly object ParameterZeroArgument = new();

    public List<(string Name, object? Arguments)> Received { get; } = new();

    void ITestInheritedReceiver.Parameter_Zero()
        => Received.Add((nameof(ITestInheritedReceiver.Parameter_Zero), ParameterZeroArgument));

    Task ITestInheritedReceiver.ClientResult_Parameter_Zero_NoReturnValue()
    {
        Received.Add((nameof(ITestInheritedReceiver.ClientResult_Parameter_Zero_NoReturnValue), ParameterZeroArgument));
        return Task.CompletedTask;
    }

    void ITestInheritedReceiver2.Parameter_One(int arg1)
    {
        Received.Add((nameof(ITestInheritedReceiver2.Parameter_One), arg1));
    }

    void ITestInheritedReceiver3.Parameter_Many(int arg1, string arg2, bool arg3)
    {
        Received.Add((nameof(ITestInheritedReceiver3.Parameter_Many), (arg1, arg2, arg3)));
    }

    Task<int> ITestInheritedReceiver3.ClientResult_Parameter_Two_WithReturnValue(int arg1, bool arg2)
    {
        Received.Add((nameof(ITestInheritedReceiver3.ClientResult_Parameter_Two_WithReturnValue), (arg1, arg2)));
        return Task.FromResult(arg1 * 10);
    }
}