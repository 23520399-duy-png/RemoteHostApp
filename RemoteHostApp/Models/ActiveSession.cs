using System;

namespace RemoteHostApp.Models;

/// <summary>
/// Thông tin phiên điều khiển đang hoạt động
/// </summary>
public class ActiveSession
{
    public string SessionId { get; set; } = string.Empty;
    public string ViewerName { get; set; } = string.Empty;
    public string ViewerId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public SessionStatus Status { get; set; } = SessionStatus.Pending;
}

public enum SessionStatus
{
    Pending,
    Accepted,
    Rejected,
    Ended
}