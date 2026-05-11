namespace RemoteHostApp.DTOs;

/// <summary>
/// DTO sự kiện chuột từ viewer gửi về - giống hệt server
/// </summary>
public class MouseEventDto
{
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // Move, LeftClick, RightClick, DoubleClick, Scroll
    public int X { get; set; }
    public int Y { get; set; }
    public int ScrollDelta { get; set; }
}