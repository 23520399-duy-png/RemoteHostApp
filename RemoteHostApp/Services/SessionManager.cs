using RemoteHostApp.Models;

namespace RemoteHostApp.Services;

/// <summary>
/// Quản lý trạng thái phiên điều khiển hiện tại
/// </summary>
public class SessionManager
{
    public ActiveSession? CurrentSession { get; private set; }

    public bool HasActiveSession =>
        CurrentSession?.Status == SessionStatus.Accepted;

    public void SetSession(ActiveSession session)
    {
        CurrentSession = session;
    }

    public void UpdateStatus(SessionStatus status)
    {
        if (CurrentSession != null)
            CurrentSession.Status = status;
    }

    public void ClearSession()
    {
        CurrentSession = null;
    }
}
