namespace RemoteHostApp.DTOs;

/// <summary>
/// DTO sự kiện chuột từ viewer gửi về - giống hệt server
/// </summary>
public class MouseEventDto
{
    public string SessionId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public int ScreenWidth { get; set; }

    public int ScreenHeight { get; set; }

    public int Button { get; set; }

    public int Delta { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}