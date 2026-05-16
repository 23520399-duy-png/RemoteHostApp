using Microsoft.AspNetCore.SignalR.Client;
using RemoteHostApp.DTOs;
using RemoteHostApp.Helpers;
using RemoteHostApp.Models;
using System.Threading.Tasks;
using System;

namespace RemoteHostApp.Services;

/// <summary>
/// Singleton service quản lý kết nối SignalR với server.
/// Xử lý tất cả giao tiếp realtime: đăng ký host, ping, gửi frame, nhận event.
/// </summary>
public class SignalRHostService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly AppSettings _settings;
    private System.Threading.Timer? _pingTimer;
    private HostRegisterDto? _lastRegisterDto;

    // ─── Events ra ngoài (MainForm lắng nghe) ────────────────────────────────
    public event Action<string, string, string>? OnControlRequestReceived;  // sessionId, viewerId, viewerName
    public event Action<MouseEventDto>? OnMouseEventReceived;
    public event Action<KeyboardEventDto>? OnKeyboardEventReceived;
    public event Action<string>? OnControlAccepted;   // sessionId
    public event Action<string>? OnControlRejected;   // sessionId
    public event Action<string>? OnControlEnded;      // sessionId
    public event Action<string>? OnConnectionStatusChanged; // "Connected", "Disconnected", "Reconnecting"

    public string? HostId { get; private set; }

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public SignalRHostService(AppSettings settings)
    {
        _settings = settings;
    }

    // ─── Kết nối ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Xây dựng HubConnection và kết nối tới server
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_connection != null)
            await DisconnectAsync();

        LoggingHelper.Info($"Đang kết nối tới {_settings.FullHubUrl} ...");

        _connection = new HubConnectionBuilder()
            .WithUrl(_settings.FullHubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(2),
                                            TimeSpan.FromSeconds(5),
                                            TimeSpan.FromSeconds(10),
                                            TimeSpan.FromSeconds(30) })
            .Build();

        // Đăng ký sự kiện lifecycle
        _connection.Reconnecting += ex =>
        {
            LoggingHelper.Warning($"SignalR đang reconnect: {ex?.Message}");
            OnConnectionStatusChanged?.Invoke("Reconnecting");
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connId =>
        {
            LoggingHelper.Info($"SignalR reconnected – connId: {connId}");
            OnConnectionStatusChanged?.Invoke("Connected");

            // Tự động re-register host sau khi reconnect
            if (_lastRegisterDto != null)
            {
                try
                {
                    await _connection.InvokeAsync("RegisterHost", _lastRegisterDto);
                    LoggingHelper.Info($"Auto re-register host thành công – ID: {_lastRegisterDto.HostId}");
                }
                catch (Exception ex)
                {
                    LoggingHelper.Error($"Auto re-register thất bại: {ex.Message}");
                }
            }
        };

        _connection.Closed += ex =>
        {
            LoggingHelper.Warning($"SignalR đóng kết nối: {ex?.Message}");
            OnConnectionStatusChanged?.Invoke("Disconnected");
            StopPing();
            return Task.CompletedTask;
        };

        // Đăng ký nhận sự kiện từ server
        RegisterServerEvents();

        await _connection.StartAsync();
        LoggingHelper.Info("Kết nối SignalR thành công!");
        OnConnectionStatusChanged?.Invoke("Connected");

        StartPing();
    }

    /// <summary>
    /// Ngắt kết nối và dọn dẹp
    /// </summary>
    public async Task DisconnectAsync()
    {
        StopPing();

        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        LoggingHelper.Info("Đã ngắt kết nối SignalR.");
        OnConnectionStatusChanged?.Invoke("Disconnected");
    }

    // ─── Nhận sự kiện từ server ───────────────────────────────────────────────

    private void RegisterServerEvents()
    {
        if (_connection == null) return;

        // Server yêu cầu host cho phép điều khiển
        _connection.On<string, string, string>("ReceiveControlRequest",
            (SessionId, ViewerId, ViewerName) =>
            {
                LoggingHelper.Info($"[ControlRequest] Viewer '{ViewerName}' ({ViewerId}) – session: {SessionId}");
                OnControlRequestReceived?.Invoke(SessionId, ViewerId, ViewerName);
            });

        // Server gửi sự kiện chuột từ viewer
        _connection.On<MouseEventDto>("ReceiveMouseEvent", dto =>
        {
            LoggingHelper.Debug($"[MouseEvent] {dto.Action} ({dto.X},{dto.Y})");
            OnMouseEventReceived?.Invoke(dto);
        });

        // Server gửi sự kiện bàn phím từ viewer
        _connection.On<KeyboardEventDto>("ReceiveKeyboardEvent", dto =>
        {
            LoggingHelper.Debug($"[KeyEvent] {dto.EventType} VK={dto.KeyCode}");
            OnKeyboardEventReceived?.Invoke(dto);
        });

        // Server xác nhận viewer đã accept (không thường dùng ở host side)
        _connection.On<string>("ControlAccepted", sessionId =>
        {
            LoggingHelper.Info($"[ControlAccepted] session: {sessionId}");
            OnControlAccepted?.Invoke(sessionId);
        });

        // Server thông báo viewer reject
        _connection.On<string>("ControlRejected", sessionId =>
        {
            LoggingHelper.Info($"[ControlRejected] session: {sessionId}");
            OnControlRejected?.Invoke(sessionId);
        });

        // Server/viewer kết thúc phiên
        _connection.On<string>("ControlEnded", sessionId =>
        {
            LoggingHelper.Info($"[ControlEnded] session: {sessionId}");
            OnControlEnded?.Invoke(sessionId);
        });
    }

    // ─── Gọi method lên server ────────────────────────────────────────────────

    /// <summary>Đăng ký host với server</summary>
    public async Task RegisterHost(HostRegisterDto request)
    {
        EnsureConnected();
        try
        {
            await _connection!.InvokeAsync("RegisterHost", request);

            HostId = request.HostId;
            _lastRegisterDto = request;

            LoggingHelper.Info($"Đã đăng ký host – ID: {request.HostId}");
        }
        catch (Microsoft.AspNetCore.SignalR.HubException ex)
        {
            LoggingHelper.Error($"RegisterHost failed: {ex.Message}");
        }
    }

    /// <summary>Ping định kỳ để server biết host còn online</summary>
    public async Task PingHost()
    {
        if (!IsConnected || HostId == null) return;
        try
        {
            await _connection!.InvokeAsync("PingHost", HostId);
            LoggingHelper.Debug("Ping server OK");
        }
        catch (Exception ex)
        {
            LoggingHelper.Warning($"PingHost lỗi: {ex.Message}");
        }
    }

    /// <summary>Chấp nhận yêu cầu điều khiển từ viewer</summary>
    public async Task AcceptControl(ControlResponseDto dto)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("AcceptControl", dto);
        LoggingHelper.Info($"Đã chấp nhận điều khiển – session: {dto.SessionId}");
    }

    /// <summary>Từ chối yêu cầu điều khiển từ viewer</summary>
    public async Task RejectControl(ControlResponseDto dto)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("RejectControl", dto);
        LoggingHelper.Info($"Đã từ chối điều khiển – session: {dto.SessionId}");
    }

    /// <summary>Kết thúc phiên điều khiển</summary>
    public async Task EndControl(string sessionId)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("EndControl", sessionId);
        LoggingHelper.Info($"Đã kết thúc phiên – session: {sessionId}");
    }

    /// <summary>Gửi frame màn hình tới viewer</summary>
    public async Task SendScreenFrame(ScreenFrameDto frame)
    {
        if (!IsConnected) return;
        try
        {
            await _connection!.SendAsync("SendScreenFrame", frame);
        }
        catch (Exception ex)
        {
            LoggingHelper.Warning($"SendScreenFrame lỗi: {ex.Message}");
        }
    }

    // ─── Ping timer ───────────────────────────────────────────────────────────

    private void StartPing()
    {
        var interval = TimeSpan.FromSeconds(_settings.PingIntervalSeconds);
        _pingTimer = new System.Threading.Timer(
            async _ => await PingHost(),
            null, interval, interval);
        LoggingHelper.Debug($"Ping timer bắt đầu – mỗi {_settings.PingIntervalSeconds}s");
    }

    private void StopPing()
    {
        _pingTimer?.Dispose();
        _pingTimer = null;
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("Chưa kết nối tới server SignalR!");
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}