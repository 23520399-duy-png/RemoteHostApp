using RemoteHostApp.DTOs;
using RemoteHostApp.Helpers;
using RemoteHostApp.Models;
using RemoteHostApp.Services;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

namespace RemoteHostApp.Forms;

/// <summary>
/// Form chính của RemoteHostApp.
/// Quản lý kết nối, đăng ký host, và điều phối các service.
/// </summary>
public partial class MainForm : Form
{
    // ─── Services ─────────────────────────────────────────────────────────────
    private readonly SignalRHostService _signalR;
    private readonly ScreenCaptureService _capture;
    private readonly InputSimulatorService _input;
    private readonly SessionManager _sessionMgr;
    private readonly AppSettings _settings;
    private bool _isClosing;

    public MainForm(AppSettings settings,
                    SignalRHostService signalR,
                    ScreenCaptureService capture,
                    InputSimulatorService input,
                    SessionManager sessionMgr)
    {
        _settings = settings;
        _signalR = signalR;
        _capture = capture;
        _input = input;
        _sessionMgr = sessionMgr;

        InitializeComponent();
        SetupSignalREvents();
        SetupLogging();

        // Giá trị mặc định
        txtServerUrl.Text = _settings.ServerUrl;
        txtHostId.Text = $"{Environment.MachineName}-{Guid.NewGuid():N}".Substring(0, 28);
        txtComputerName.Text = Environment.MachineName;
        txtUserId.Text = $"USER-{Environment.MachineName}";

        UpdateUI();
    }

    // ─── Designer controls ────────────────────────────────────────────────────

    private Label lblHostId = null!;
    private TextBox txtHostId = null!;
    private Label lblComputerName = null!;
    private TextBox txtComputerName = null!;
    private Label lblUserId = null!;
    private TextBox txtUserId = null!;
    private Button btnConnect = null!;
    private Button btnRegister = null!;
    private Button btnStartStop = null!;
    private Button btnEndControl = null!;
    private Label lblStatus = null!;
    private RichTextBox rtbLog = null!;
    private Label lblServerUrl = null!;
    private TextBox txtServerUrl = null!;

    private void InitializeComponent()
    {
        Text = "RemoteHostApp – Remote Controller";
        Size = new Size(700, 600);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 500);
        Font = new Font("Segoe UI", 9);
        FormClosing += MainForm_FormClosing;

        // ===== TOP LAYOUT =====
        var pnlTop = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 190,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10),
            AutoSize = false
        };

        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        lblServerUrl = new Label { Text = "Server URL:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        txtServerUrl = new TextBox { Dock = DockStyle.Fill };

        lblHostId = new Label { Text = "Host ID:", AutoSize = true };
        txtHostId = new TextBox { Dock = DockStyle.Fill };

        lblComputerName = new Label { Text = "Computer:", AutoSize = true };
        txtComputerName = new TextBox { Dock = DockStyle.Fill };

        lblUserId = new Label { Text = "User ID:", AutoSize = true };
        txtUserId = new TextBox { Dock = DockStyle.Fill };

        pnlTop.Controls.Add(lblServerUrl, 0, 0);
        pnlTop.Controls.Add(txtServerUrl, 1, 0);

        pnlTop.Controls.Add(lblHostId, 0, 1);
        pnlTop.Controls.Add(txtHostId, 1, 1);

        pnlTop.Controls.Add(lblComputerName, 0, 2);
        pnlTop.Controls.Add(txtComputerName, 1, 2);

        pnlTop.Controls.Add(lblUserId, 0, 3);
        pnlTop.Controls.Add(txtUserId, 1, 3);

        // ===== BUTTON BAR =====
        var pnlButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10, 5, 10, 5)
        };

        btnConnect = new Button
        {
            Text = "🔌 Kết nối",
            Width = 120,
            Height = 32,
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnConnect.Click += BtnConnect_Click;

        btnRegister = new Button
        {
            Text = "📋 Đăng ký Host",
            Width = 140,
            Height = 32,
            BackColor = Color.FromArgb(16, 124, 16),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnRegister.Click += BtnRegister_Click;

        btnStartStop = new Button
        {
            Text = "▶ Start Stream",
            Width = 140,
            Height = 32,
            BackColor = Color.FromArgb(100, 100, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnStartStop.Click += BtnStartStop_Click;

        btnEndControl = new Button
        {
            Text = "🛑 End Control",
            Width = 130,
            Height = 32,
            BackColor = Color.FromArgb(200, 40, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnEndControl.Click += BtnEndControl_Click;

        pnlButtons.Controls.AddRange(new Control[]
        {
        btnConnect,
        btnRegister,
        btnStartStop,
        btnEndControl
        });

        pnlTop.SetColumnSpan(pnlButtons, 2);
        pnlTop.Controls.Add(pnlButtons, 0, 4);

        // ===== STATUS BAR =====
        var pnlStatus = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        lblStatus = new Label
        {
            Text = "● Offline",
            ForeColor = Color.Gray,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        pnlStatus.Controls.Add(lblStatus);

        // ===== LOG AREA =====
        var pnlLog = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var lblLog = new Label
        {
            Text = "Log:",
            Dock = DockStyle.Top,
            Height = 20
        };

        var btnClearLog = new Button
        {
            Text = "Xoá log",
            Dock = DockStyle.Bottom,
            Height = 28
        };
        btnClearLog.Click += (_, _) => rtbLog.Clear();

        rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 8.5f),
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        pnlLog.Controls.Add(rtbLog);
        pnlLog.Controls.Add(btnClearLog);
        pnlLog.Controls.Add(lblLog);

        // ===== FORM ORDER =====
        Controls.Add(pnlLog);
        Controls.Add(pnlStatus);
        Controls.Add(pnlTop);
    }

    // ─── Logging setup ────────────────────────────────────────────────────────

    private void SetupLogging()
    {
        LoggingHelper.OnLog = (msg, level) =>
        {
            // Phải Invoke vì Log có thể gọi từ background thread
            if (rtbLog.IsDisposed) return;
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(() => AppendLog(msg, level));
            }
            else
            {
                AppendLog(msg, level);
            }
        };
    }

    private void AppendLog(string msg, LogLevel level)
    {
        var color = level switch
        {
            LogLevel.Error => Color.OrangeRed,
            LogLevel.Warning => Color.Yellow,
            LogLevel.Debug => Color.DarkGray,
            _ => Color.LightGreen
        };

        rtbLog.SelectionColor = color;
        rtbLog.AppendText(msg + Environment.NewLine);
        rtbLog.ScrollToCaret();

        // Giới hạn log 2000 dòng
        if (rtbLog.Lines.Length > 2000)
        {
            rtbLog.Lines = rtbLog.Lines.TakeLast(1500).ToArray();
        }
    }

    // ─── SignalR events ───────────────────────────────────────────────────────

    private void SetupSignalREvents()
    {
        _signalR.OnConnectionStatusChanged += status =>
            SafeInvoke(() => UpdateStatusLabel(status));

        _signalR.OnControlRequestReceived += (sessionId, viewerId, viewerName) =>
            SafeInvoke(() => HandleControlRequest(sessionId, viewerId, viewerName));

        _signalR.OnMouseEventReceived += dto =>
            _input.SimulateMouseEvent(dto);

        _signalR.OnKeyboardEventReceived += dto =>
            _input.SimulateKeyboardEvent(dto);

        _signalR.OnControlEnded += sessionId =>
            SafeInvoke(() => HandleControlEnded(sessionId));

        _signalR.OnControlAccepted += sessionId =>
            LoggingHelper.Info($"ControlAccepted nhận – session: {sessionId}");

        _signalR.OnControlRejected += sessionId =>
            LoggingHelper.Info($"ControlRejected nhận – session: {sessionId}");
    }

    // ─── Button handlers ──────────────────────────────────────────────────────

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_signalR.IsConnected)
            {
                // Đang kết nối → ngắt
                await _signalR.DisconnectAsync();
                btnConnect.Text = "🔌 Kết nối";
            }
            else
            {
                // Chưa kết nối → kết nối
                _settings.ServerUrl = txtServerUrl.Text.Trim();
                btnConnect.Enabled = false;
                btnConnect.Text = "Đang kết nối...";

                await _signalR.ConnectAsync();
                btnConnect.Text = "⏏ Ngắt kết nối";
            }
        }
        catch (Exception ex)
        {
            LoggingHelper.Error($"Kết nối thất bại: {ex.Message}");
            MessageBox.Show($"Không thể kết nối:\n{ex.Message}", "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnConnect.Text = "🔌 Kết nối";
        }
        finally
        {
            btnConnect.Enabled = true;
            UpdateUI();
        }
    }

    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        try
        {
            btnRegister.Enabled = false;
            var dto = new HostRegisterDto
            {
                HostId = txtHostId.Text.Trim(),
                ComputerName = txtComputerName.Text.Trim(),
                UserId = txtUserId.Text.Trim(),
            };

            if (string.IsNullOrWhiteSpace(dto.HostId))
            {
                MessageBox.Show("Host ID không được để trống!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await _signalR.RegisterHost(dto);
            UpdateStatusLabel("Online");
        }
        catch (Exception ex)
        {
            LoggingHelper.Error($"Đăng ký host thất bại: {ex.Message}");
            MessageBox.Show($"Đăng ký thất bại:\n{ex.Message}", "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnRegister.Enabled = _signalR.IsConnected;
        }
    }

    private void BtnStartStop_Click(object? sender, EventArgs e)
    {
        var session = _sessionMgr.CurrentSession;
        if (session == null) return;

        if (session.Status == SessionStatus.Accepted)
        {
            // Đang stream → dừng
            _capture.StopCapture();
            session.Status = SessionStatus.Ended;
            btnStartStop.Text = "▶ Start Stream";
            LoggingHelper.Info("Đã dừng streaming.");
        }
        else
        {
            // Chưa stream → bắt đầu
            _sessionMgr.UpdateStatus(SessionStatus.Accepted);
            _capture.StartCapture(session);
            btnStartStop.Text = "⏹ Stop Stream";
            LoggingHelper.Info("Đã bắt đầu streaming.");
        }
    }

    private async void BtnEndControl_Click(object? sender, EventArgs e)
    {
        var session = _sessionMgr.CurrentSession;
        if (session == null) return;

        try
        {
            _capture.StopCapture();
            await _signalR.EndControl(session.SessionId);
            _sessionMgr.ClearSession();
            UpdateStatusLabel("Online");
            UpdateUI();
            LoggingHelper.Info("Đã kết thúc phiên điều khiển.");
        }
        catch (Exception ex)
        {
            LoggingHelper.Error($"EndControl lỗi: {ex.Message}");
        }
    }

    // ─── Control request popup ────────────────────────────────────────────────

    private void HandleControlRequest(string sessionId, string viewerId, string viewerName)
    {
        LoggingHelper.Info($"Nhận yêu cầu điều khiển từ: {viewerName} (session: {sessionId})");

        using var popup = new AcceptControlForm(viewerName, sessionId);
        var result = popup.ShowDialog(this);

        // Cache giá trị UI trước khi vào background thread (tránh cross-thread)
        var hostId = txtHostId.Text.Trim();
        var isAccepted = popup.IsAccepted;
        var rejectReason = popup.RejectReason;

        // Xử lý response async (không block UI)
        _ = Task.Run(async () =>
        {
            try
            {
                var dto = new ControlResponseDto
                {
                    SessionId = sessionId,
                    HostId = hostId,
                    IsAccepted = isAccepted,
                    RejectReason = rejectReason
                };

                if (isAccepted)
                {
                    await _signalR.AcceptControl(dto);

                    var session = new ActiveSession
                    {
                        SessionId = sessionId,
                        ViewerId = viewerId,
                        ViewerName = viewerName,
                        Status = SessionStatus.Accepted
                    };
                    _sessionMgr.SetSession(session);
                    _capture.StartCapture(session);

                    SafeInvoke(() =>
                    {
                        UpdateStatusLabel("In Session");
                        UpdateUI();
                    });
                }
                else
                {
                    await _signalR.RejectControl(dto);
                    LoggingHelper.Info($"Đã từ chối: {rejectReason ?? "(không có lý do)"}");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.Error($"Xử lý ControlRequest lỗi: {ex.Message}");
            }
        });
    }

    private void HandleControlEnded(string sessionId)
    {
        if (_sessionMgr.CurrentSession?.SessionId != sessionId) return;

        _capture.StopCapture();
        _sessionMgr.ClearSession();
        UpdateStatusLabel("Online");
        UpdateUI();
        LoggingHelper.Info($"Viewer đã kết thúc phiên: {sessionId}");
    }

    // ─── UI helpers ───────────────────────────────────────────────────────────

    private void UpdateStatusLabel(string status)
    {
        (lblStatus.Text, lblStatus.ForeColor) = status switch
        {
            "Connected" => ("● Online – Đã kết nối", Color.DodgerBlue),
            "Online" => ("● Online – Đã đăng ký", Color.LimeGreen),
            "In Session" => ("● In Session – Đang điều khiển", Color.Orange),
            "Reconnecting" => ("⟳ Đang reconnect...", Color.Yellow),
            "Disconnected" => ("● Offline", Color.Gray),
            _ => ("● " + status, Color.Gray)
        };
    }

    private void UpdateUI()
    {
        bool connected = _signalR.IsConnected;
        bool hasSession = _sessionMgr.CurrentSession != null;

        btnRegister.Enabled = connected && !hasSession;
        btnStartStop.Enabled = hasSession;
        btnEndControl.Enabled = hasSession;

        // Disable input khi đang có session
        txtHostId.ReadOnly = connected;
        txtComputerName.ReadOnly = connected;
        txtServerUrl.ReadOnly = connected;
    }

    private void SafeInvoke(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    // ─── Form closing ─────────────────────────────────────────────────────────

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Nếu đã xử lý closing xong → cho phép đóng
        if (_isClosing)
            return;

        // Lần đầu → cancel để chờ dọn dẹp async
        e.Cancel = true;
        _isClosing = true;

        _capture.StopCapture();

        if (_sessionMgr.CurrentSession != null)
        {
            try { await _signalR.EndControl(_sessionMgr.CurrentSession.SessionId); }
            catch { /* bỏ qua */ }
        }

        await _signalR.DisconnectAsync();

        // Dọn xong → đóng form thật sự
        Close();
    }
}