namespace Linksoft.CameraWall.Wpf.App;

public partial class CameraWallApp
{
    [LoggerMessage(Level = LogLevel.Information, Message = "App initializing")]
    private partial void LogAppInitializing();

    [LoggerMessage(Level = LogLevel.Error, Message = "CurrentDomain Unhandled Exception: {ExceptionMessage}")]
    private partial void LogCurrentDomainUnhandledException(string exceptionMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dispatcher Unhandled Exception: {ExceptionMessage}")]
    private partial void LogDispatcherUnhandledException(string exceptionMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "App starting")]
    private partial void LogAppStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "App started")]
    private partial void LogAppStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "App closing")]
    private partial void LogAppClosing();

    [LoggerMessage(Level = LogLevel.Information, Message = "All recordings stopped")]
    private partial void LogAllRecordingsStopped();

    [LoggerMessage(Level = LogLevel.Information, Message = "App closed")]
    private partial void LogAppClosed();
}