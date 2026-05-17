namespace RemoteHostApp.DTOs;

/// <summary>
/// DTO sự kiện bàn phím từ viewer gửi về - giống hệt server
/// </summary>
public class KeyboardEventDto
{
    public string SessionId { get; set; } = string.Empty;
    
    // KeyDown/KeyUp
    public string Action { get; set; } = string.Empty;

    // Tên phím logic
    public string Key { get; set; } = string.Empty;

    // Mã vị trí vật lý
    public string Code { get; set; } = string.Empty;

    public bool CtrlKey  { get; set; }
    public bool ShiftKey { get; set; }
    public bool AltKey   { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}