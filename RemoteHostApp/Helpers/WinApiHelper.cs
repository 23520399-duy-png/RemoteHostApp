using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RemoteHostApp.Helpers;

/// <summary>
/// Windows API wrapper cho SendInput (mô phỏng chuột và bàn phím thật)
/// </summary>
public static class WinApiHelper
{
    // ─── Structs ──────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // ─── Constants ────────────────────────────────────────────────────────────

    public const uint INPUT_MOUSE = 0;
    public const uint INPUT_KEYBOARD = 1;

    // Mouse flags
    public const uint MOUSEEVENTF_MOVE        = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP      = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN   = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP     = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN  = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP    = 0x0040;
    public const uint MOUSEEVENTF_WHEEL       = 0x0800;
    public const uint MOUSEEVENTF_ABSOLUTE    = 0x8000;

    // Keyboard flags
    public const uint KEYEVENTF_KEYUP        = 0x0002;
    public const uint KEYEVENTF_EXTENDEDKEY  = 0x0001;  // cần cho Arrow, Insert, Delete, Home, End, PageUp/Down, NumLock...

    // Virtual keys thường dùng
    public const ushort VK_SHIFT   = 0x10;
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_MENU   = 0x12; // Alt
    public const ushort VK_LSHIFT  = 0xA0;
    public const ushort VK_RSHIFT  = 0xA1;
    public const ushort VK_LCONTROL = 0xA2;
    public const ushort VK_RCONTROL = 0xA3;
    public const ushort VK_LMENU  = 0xA4;
    public const ushort VK_RMENU  = 0xA5;

    // ─── P/Invoke ─────────────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    /// MAPVK_VK_TO_VSC = 0: dùng để lấy scan code từ VK (nếu cần debug)
    /// MAPVK_VSC_TO_VK = 1
    /// </summary>
    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Chuyển toạ độ pixel sang giá trị absolute (0–65535) cho SendInput.
    /// </summary>
    public static (int ax, int ay) ToAbsolute(int x, int y)
    {
        int screenW = Screen.PrimaryScreen?.Bounds.Width  ?? 1920;
        int screenH = Screen.PrimaryScreen?.Bounds.Height ?? 1080;
        return (
            (int)((double)x / screenW * 65535),
            (int)((double)y / screenH * 65535)
        );
    }
}