using System.Drawing;
using System.Windows.Forms;
using System;

namespace RemoteHostApp.Forms;

/// <summary>
/// Popup hiển thị khi có viewer yêu cầu điều khiển máy.
/// Host có thể chấp nhận hoặc từ chối với lý do tuỳ chọn.
/// </summary>
public partial class AcceptControlForm : Form
{
    public bool IsAccepted { get; private set; }
    public string? RejectReason { get; private set; }

    private readonly string _viewerName;
    private readonly string _sessionId;

    public AcceptControlForm(string viewerName, string sessionId)
    {
        _viewerName = viewerName;
        _sessionId = sessionId;
        InitializeComponent();
        lblMessage.Text = $"Viewer \"{viewerName}\" muốn điều khiển máy của bạn.\n\nSession ID: {sessionId}";
    }

    // ─── Designer ─────────────────────────────────────────────────────────────

    private Label lblMessage = null!;
    private Label lblReasonHint = null!;
    private TextBox txtRejectReason = null!;
    private Button btnAccept = null!;
    private Button btnReject = null!;

    private void InitializeComponent()
    {
        // Form
        Text = "Yêu cầu điều khiển từ xa";
        Size = new Size(420, 240);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;

        // Label thông báo
        lblMessage = new Label
        {
            Bounds = new Rectangle(16, 16, 380, 60),
            Font = new Font("Segoe UI", 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Label gợi ý lý do từ chối
        lblReasonHint = new Label
        {
            Text = "Lý do từ chối (tuỳ chọn):",
            Bounds = new Rectangle(16, 86, 200, 20),
            Font = new Font("Segoe UI", 9)
        };

        // TextBox nhập lý do
        txtRejectReason = new TextBox
        {
            Bounds = new Rectangle(16, 108, 380, 24),
            Font = new Font("Segoe UI", 9),
            PlaceholderText = "Nhập lý do (nếu muốn từ chối)..."
        };

        // Nút Chấp nhận
        btnAccept = new Button
        {
            Text = "✔ Chấp nhận",
            Bounds = new Rectangle(16, 150, 130, 40),
            BackColor = Color.FromArgb(0, 150, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnAccept.Click += BtnAccept_Click;

        // Nút Từ chối
        btnReject = new Button
        {
            Text = "✘ Từ chối",
            Bounds = new Rectangle(160, 150, 130, 40),
            BackColor = Color.FromArgb(200, 40, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnReject.Click += BtnReject_Click;

        Controls.AddRange(new Control[] {
            lblMessage, lblReasonHint, txtRejectReason, btnAccept, btnReject
        });
    }

    // ─── Event handlers ───────────────────────────────────────────────────────

    private void BtnAccept_Click(object? sender, EventArgs e)
    {
        IsAccepted = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnReject_Click(object? sender, EventArgs e)
    {
        IsAccepted = false;
        RejectReason = string.IsNullOrWhiteSpace(txtRejectReason.Text)
            ? null : txtRejectReason.Text.Trim();
        DialogResult = DialogResult.Cancel;
        Close();
    }
}