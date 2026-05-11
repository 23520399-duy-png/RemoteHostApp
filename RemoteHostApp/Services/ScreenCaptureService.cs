using RemoteHostApp.DTOs;
using RemoteHostApp.Helpers;
using RemoteHostApp.Models;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System;

namespace RemoteHostApp.Services;

/// <summary>
/// Chụp màn hình và gửi frame tới viewer qua SignalR
/// </summary>
public class ScreenCaptureService : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly AppSettings _settings;
    private readonly SignalRHostService _signalR;
    private readonly SemaphoreSlim _captureLock = new(1, 1);

    private ActiveSession? _activeSession;
    private bool _isRunning;
    private bool _disposed;

    // Kích thước màn hình thực tế (lấy 1 lần khi khởi động)
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    public ScreenCaptureService(AppSettings settings, SignalRHostService signalR)
    {
        _settings = settings;
        _signalR = signalR;

        var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        _screenWidth = screen.Width;
        _screenHeight = screen.Height;

        // Timer nhưng chưa chạy ngay
        _timer = new System.Threading.Timer(OnTimerTick, null,
            Timeout.Infinite, Timeout.Infinite);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Bắt đầu vòng lặp capture</summary>
    public void StartCapture(ActiveSession session)
    {
        _activeSession = session;
        _isRunning = true;
        _timer.Change(0, _settings.CaptureIntervalMs);
        LoggingHelper.Info($"ScreenCapture bắt đầu – interval {_settings.CaptureIntervalMs}ms");
    }

    /// <summary>Dừng vòng lặp capture</summary>
    public void StopCapture()
    {
        _isRunning = false;
        _activeSession = null;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        LoggingHelper.Info("ScreenCapture đã dừng.");
    }

    // ─── Timer callback ───────────────────────────────────────────────────────

    private async void OnTimerTick(object? state)
    {
        // Chỉ gửi khi session đang ở trạng thái Accepted
        if (!_isRunning || _activeSession?.Status != SessionStatus.Accepted)
            return;

        // Tránh frame overlap: nếu frame trước chưa xong → bỏ qua tick này
        if (!_captureLock.Wait(0))
            return;

        try
        {
            using var bitmap = CaptureScreen();
            var base64 = ImageHelper.ResizeAndCompress(
                bitmap, _settings.MaxFrameWidth, _settings.JpegQuality);

            // Lấy vị trí chuột hiện tại
            var cursor = Cursor.Position;

            var frame = new ScreenFrameDto
            {
                SessionId = _activeSession.SessionId,
                ImageBase64 = base64,
                ScreenWidth = _screenWidth,
                ScreenHeight = _screenHeight,
                FrameWidth = _settings.MaxFrameWidth,
                FrameHeight = (int)(_screenHeight * ((double)_settings.MaxFrameWidth / _screenWidth)),
                MouseX = cursor.X,
                MouseY = cursor.Y,
                SentAt = DateTime.UtcNow
            };

            await _signalR.SendScreenFrame(frame);
        }
        catch (Exception ex)
        {
            LoggingHelper.Error($"CaptureScreen lỗi: {ex.Message}");
        }
        finally
        {
            _captureLock.Release();
        }
    }

    // ─── Screen capture ───────────────────────────────────────────────────────

    /// <summary>Chụp toàn bộ màn hình chính</summary>
    public Bitmap CaptureScreen()
    {
        var bmp = new Bitmap(_screenWidth, _screenHeight);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(Point.Empty, Point.Empty,
            new Size(_screenWidth, _screenHeight));
        return bmp;
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopCapture();
        _timer.Dispose();
        _captureLock.Dispose();
        _disposed = true;
    }
}