namespace Multicaster.SourceGenerator.Tests;

/// <summary>
/// Test receiver interface for chat functionality.
/// </summary>
public interface IChatReceiver
{
    void OnMessage(string user, string message);
    void OnUserJoined(string user);
    void OnUserLeft(string user);
}

/// <summary>
/// Test receiver interface for game functionality.
/// </summary>
public interface IGameReceiver
{
    void OnGameStarted(int gameId);
    void OnPlayerMoved(int playerId, int x, int y);
    void OnGameEnded(int winnerId);
}

/// <summary>
/// Test receiver interface with client results (async methods).
/// </summary>
public interface IClientResultReceiver
{
    Task<bool> ConfirmAsync(string message, CancellationToken cancellationToken = default);
    Task NotifyAsync(string message, CancellationToken cancellationToken = default);
}
