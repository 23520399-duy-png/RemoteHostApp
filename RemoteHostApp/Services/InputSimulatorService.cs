using RemoteHostApp.DTOs;
using RemoteHostApp.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;   // ← đúng chỗ: ĐẦU file
using System.Threading;
using static RemoteHostApp.Helpers.WinApiHelper;

namespace RemoteHostApp.Services
{
    /// <summary>
    /// Mô phỏng sự kiện chuột và bàn phím bằng Windows SendInput API.
    /// Thread-safe: có thể gọi từ bất kỳ thread nào.
    /// </summary>
    public class InputSimulatorService
    {
        // ─── Mouse ────────────────────────────────────────────────────────────

        /// <summary>
        /// Mô phỏng sự kiện chuột dựa theo EventType.
        /// </summary>
        public void SimulateMouseEvent(MouseEventDto dto)
        {
            try
            {
                switch (dto.EventType)
                {
                    case "Move": SimulateMove(dto.X, dto.Y); break;
                    case "LeftClick": SimulateLeftClick(dto.X, dto.Y); break;
                    case "RightClick": SimulateRightClick(dto.X, dto.Y); break;
                    case "DoubleClick": SimulateDoubleClick(dto.X, dto.Y); break;
                    case "Scroll": SimulateScroll(dto.X, dto.Y, dto.ScrollDelta); break;
                    default:
                        LoggingHelper.Warning($"MouseEvent không xác định: {dto.EventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.Error($"SimulateMouseEvent lỗi: {ex.Message}");
            }
        }

        private static void SimulateMove(int x, int y)
        {
            var (ax, ay) = ToAbsolute(x, y);
            SendInputs(new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = ax,
                        dy = ay,
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                    }
                }
            });
        }

        private static void SimulateLeftClick(int x, int y)
        {
            SimulateMove(x, y);
            var (ax, ay) = ToAbsolute(x, y);
            SendInputs(
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE } }
                },
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE } }
                }
            );
        }

        private static void SimulateRightClick(int x, int y)
        {
            SimulateMove(x, y);
            var (ax, ay) = ToAbsolute(x, y);
            SendInputs(
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_ABSOLUTE } }
                },
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE } }
                }
            );
        }

        private static void SimulateDoubleClick(int x, int y)
        {
            SimulateLeftClick(x, y);
            Thread.Sleep(50);
            SimulateLeftClick(x, y);
        }

        private static void SimulateScroll(int x, int y, int delta)
        {
            SimulateMove(x, y);
            // delta dương = scroll lên, âm = scroll xuống.
            // Cast sang uint là đúng – Windows API dùng two's complement cho giá trị âm.
            SendInputs(new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        mouseData = (uint)(delta * 120),
                        dwFlags = MOUSEEVENTF_WHEEL
                    }
                }
            });
        }

        // ─── Keyboard ─────────────────────────────────────────────────────────

        /// <summary>
        /// Mô phỏng sự kiện bàn phím, hỗ trợ tổ hợp Ctrl/Shift/Alt.
        /// </summary>
        public void SimulateKeyboardEvent(KeyboardEventDto dto)
        {
            try
            {
                bool isKeyDown = dto.EventType == "KeyDown";
                var inputs = new List<INPUT>();

                // Nhấn modifier trước (chỉ khi KeyDown)
                if (isKeyDown)
                {
                    if (dto.IsCtrl) inputs.Add(MakeKeyInput(VK_CONTROL, false));
                    if (dto.IsShift) inputs.Add(MakeKeyInput(VK_SHIFT, false));
                    if (dto.IsAlt) inputs.Add(MakeKeyInput(VK_MENU, false));
                }

                // Phím chính
                inputs.Add(MakeKeyInput((ushort)dto.KeyCode, !isKeyDown));

                // Thả modifier khi KeyUp (thứ tự ngược)
                if (!isKeyDown)
                {
                    if (dto.IsAlt) inputs.Add(MakeKeyInput(VK_MENU, true));
                    if (dto.IsShift) inputs.Add(MakeKeyInput(VK_SHIFT, true));
                    if (dto.IsCtrl) inputs.Add(MakeKeyInput(VK_CONTROL, true));
                }

                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                LoggingHelper.Error($"SimulateKeyboardEvent lỗi: {ex.Message}");
            }
        }

        private static INPUT MakeKeyInput(ushort vk, bool keyUp) => new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                }
            }
        };

        // ─── Utility ──────────────────────────────────────────────────────────

        private static void SendInputs(params INPUT[] inputs)
        {
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}