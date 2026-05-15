namespace RemoteHostApp.DTOs;

/// <summary>
/// DTO đăng ký host với server - phải giống hệt server
/// </summary>
public class HostRegisterDto
{
    public string HostId { get; set; } = string.Empty;

    public string ComputerName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}