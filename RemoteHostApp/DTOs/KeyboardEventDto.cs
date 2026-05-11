namespace RemoteHostApp.DTOs;

/// <summary>
/// DTO sự kiện bàn phím từ viewer gửi về - giống hệt server
/// </summary>
public class KeyboardEventDto
{
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // KeyDown, KeyUp
    public int KeyCode { get; set; }
    public bool IsCtrl { get; set; }
    public bool IsShift { get; set; }
    public bool IsAlt { get; set; }
}