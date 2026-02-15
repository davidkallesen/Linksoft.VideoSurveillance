namespace Linksoft.VideoEngine;

/// <summary>
/// Event arguments for player state changes.
/// </summary>
public sealed class PlayerStateChangedEventArgs : EventArgs
{
    public PlayerStateChangedEventArgs(
        PlayerState previousState,
        PlayerState newState,
        string? errorMessage = null)
    {
        PreviousState = previousState;
        NewState = newState;
        ErrorMessage = errorMessage;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the previous state.
    /// </summary>
    public PlayerState PreviousState { get; }

    /// <summary>
    /// Gets the new state.
    /// </summary>
    public PlayerState NewState { get; }

    /// <summary>
    /// Gets the error message when transitioning to <see cref="PlayerState.Error"/>.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the UTC timestamp of the state change.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"PlayerStateChanged {{ {PreviousState} -> {NewState} }}";
}