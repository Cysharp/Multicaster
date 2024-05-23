namespace Multicaster.Tests;

public interface ITestReceiver
{
    void Parameter_Zero();
    void Parameter_One(int arg1);
    void Parameter_Two(int arg1, string arg2);
    void Parameter_Many(int arg1, string arg2, bool arg3, long arg4);
    void Throw();

    Task ClientInvoke_Parameter_Zero_NoReturnValue();
    Task ClientInvoke_Parameter_One_NoReturnValue(int arg1);
    Task ClientInvoke_Parameter_Many_NoReturnValue(int arg1, string arg2, bool arg3, long arg4);
    Task<string> ClientInvoke_Parameter_Zero();
    Task<string> ClientInvoke_Parameter_One(int arg1);
    Task<string> ClientInvoke_Parameter_Many(int arg1, string arg2, bool arg3, long arg4);
}

public class TestReceiver : ITestReceiver
{
    public static readonly object ParameterZeroArgument = new();

    public List<(string Name, object? Arguments)> Received { get; } = new();

    public void Parameter_Zero()
        => Received.Add((nameof(Parameter_Zero), ParameterZeroArgument));

    public void Parameter_One(int arg1)
        => Received.Add((nameof(Parameter_One), (arg1)));

    public void Parameter_Two(int arg1, string arg2)
        => Received.Add((nameof(Parameter_Two), (arg1, arg2)));

    public void Parameter_Many(int arg1, string arg2, bool arg3, long arg4)
        => Received.Add((nameof(Parameter_Many), (arg1, arg2, arg3, arg4)));

    public void Throw()
    {
        Received.Add((nameof(Throw), ParameterZeroArgument));
        throw new Exception("Something went wrong.");
    }

    public async Task ClientInvoke_Parameter_Zero_NoReturnValue()
    {
        Received.Add((nameof(ClientInvoke_Parameter_Zero_NoReturnValue), ParameterZeroArgument));
        await Task.Delay(500);
    }

    public async Task ClientInvoke_Parameter_One_NoReturnValue(int arg1)
    {
        Received.Add((nameof(ClientInvoke_Parameter_One_NoReturnValue), (arg1)));
        await Task.Delay(500);
    }

    public async Task ClientInvoke_Parameter_Many_NoReturnValue(int arg1, string arg2, bool arg3, long arg4)
    {
        Received.Add((nameof(ClientInvoke_Parameter_Many_NoReturnValue), (arg1, arg2, arg3, arg4)));
        await Task.Delay(500);
    }

    public async Task<string> ClientInvoke_Parameter_Zero()
    {
        Received.Add((nameof(ClientInvoke_Parameter_Zero), ParameterZeroArgument));
        await Task.Delay(500);
        return nameof(ClientInvoke_Parameter_Zero);
    }

    public async Task<string> ClientInvoke_Parameter_One(int arg1)
    {
        Received.Add((nameof(ClientInvoke_Parameter_One), (arg1)));
        await Task.Delay(500);
        return $"{nameof(ClientInvoke_Parameter_One)}:{arg1}";
    }

    public async Task<string> ClientInvoke_Parameter_Many(int arg1, string arg2, bool arg3, long arg4)
    {
        Received.Add((nameof(ClientInvoke_Parameter_Many), (arg1, arg2, arg3, arg4)));
        await Task.Delay(500);
        return $"{nameof(ClientInvoke_Parameter_Many)}:{arg1},{arg2},{arg3},{arg4}";
    }
}