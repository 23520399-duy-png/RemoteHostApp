using System;

namespace RemoteHostApp.DTOs;

/// <summary>
/// DTO gửi frame màn hình tới viewer
/// </summary>
public class ScreenFrameDto
{
    public string SessionId { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public int MouseX { get; set; }
    public int MouseY { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}