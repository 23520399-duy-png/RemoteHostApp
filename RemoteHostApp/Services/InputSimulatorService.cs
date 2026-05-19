using RemoteHostApp.DTOs;
using RemoteHostApp.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using static RemoteHostApp.Helpers.WinApiHelper;

namespace RemoteHostApp.Services
{
    /// <summary>
    /// Mô phỏng sự kiện chuột và bàn phím bằng Windows SendInput API.
    /// Thread-safe: có thể gọi từ bất kỳ thread nào.
    /// 
    /// Keyboard: nhận Key/Code (chuẩn Web API) từ Viewer, map sang Virtual Key
    /// rồi gọi SendInput với wVk — không phụ thuộc layout bàn phím Viewer.
    /// </summary>
    public class InputSimulatorService
    {
        // ─── Mouse ────────────────────────────────────────────────────────────

        public void SimulateMouseEvent(MouseEventDto dto)
        {
            try
            {
                switch (dto.Action)
                {
                    case "Move":        SimulateMove(dto.X, dto.Y);               break;
                    case "LeftClick":   SimulateLeftClick(dto.X, dto.Y);          break;
                    case "LeftDown":    SimulateMouseButton(dto.X, dto.Y, down: true,  flags: MOUSEEVENTF_LEFTDOWN);  break;
                    case "LeftUp":      SimulateMouseButton(dto.X, dto.Y, down: false, flags: MOUSEEVENTF_LEFTUP);    break;
                    case "RightClick":  SimulateRightClick(dto.X, dto.Y);         break;
                    case "RightDown":   SimulateMouseButton(dto.X, dto.Y, down: true,  flags: MOUSEEVENTF_RIGHTDOWN); break;
                    case "RightUp":     SimulateMouseButton(dto.X, dto.Y, down: false, flags: MOUSEEVENTF_RIGHTUP);   break;
                    case "MiddleClick": SimulateMiddleClick(dto.X, dto.Y);        break;
                    case "MiddleDown":  SimulateMouseButton(dto.X, dto.Y, down: true,  flags: MOUSEEVENTF_MIDDLEDOWN);break;
                    case "MiddleUp":    SimulateMouseButton(dto.X, dto.Y, down: false, flags: MOUSEEVENTF_MIDDLEUP);  break;
                    case "DoubleClick": SimulateDoubleClick(dto.X, dto.Y);        break;
                    case "Scroll":      SimulateScroll(dto.X, dto.Y, dto.Delta);  break;
                    default:
                        LoggingHelper.Warning($"MouseEvent không xác định: {dto.Action}");
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
                        dx = ax, dy = ay,
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
                new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE } } },
                new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_LEFTUP   | MOUSEEVENTF_ABSOLUTE } } }
            );
        }

        private static void SimulateRightClick(int x, int y)
        {
            SimulateMove(x, y);
            var (ax, ay) = ToAbsolute(x, y);
            SendInputs(
                new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_ABSOLUTE } } },
                new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_RIGHTUP   | MOUSEEVENTF_ABSOLUTE } } }
            );
        }

        private static void SimulateMiddleClick(int x, int y)
        {
            SimulateMove(x, y);
            var (ax, ay) = ToAbsolute(x, y);
            SendInputs(
                new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_ABSOLUTE } } },
                new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = MOUSEEVENTF_MIDDLEUP   | MOUSEEVENTF_ABSOLUTE } } }
            );
        }

        private static void SimulateMouseButton(int x, int y, bool down, uint flags)
        {
            SimulateMove(x, y);
            var (ax, ay) = ToAbsolute(x, y);
            SendInputs(new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = flags | MOUSEEVENTF_ABSOLUTE } }
            });
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
        /// Mô phỏng KeyDown hoặc KeyUp từ KeyboardEventDto.
        /// 
        /// Chiến lược:
        /// - Dùng Code (vị trí vật lý) làm nguồn chính để map → Virtual Key.
        ///   Code không phụ thuộc layout → đúng phím trên máy Host.
        /// - Modifier (Ctrl/Shift/Alt) được gửi riêng nếu cần,
        ///   nhưng ưu tiên để phím modifier tự xử lý qua Code (ControlLeft/Right...).
        /// - Dùng wVk (Virtual Key) thay wScan để tránh lệch layout.
        /// </summary>
        public void SimulateKeyboardEvent(KeyboardEventDto dto)
        {
            try
            {
                bool isDown = dto.Action == "KeyDown";

                // Map Code → VK. Fallback sang Key nếu Code không nhận ra.
                ushort vk = CodeToVirtualKey(dto.Code);
                if (vk == 0)
                {
                    LoggingHelper.Warning($"Không map được Code=\"{dto.Code}\" Key=\"{dto.Key}\" — bỏ qua");
                    return;
                }

                bool isExtended = IsExtendedKey(vk);

                var inputs = new List<INPUT>();

                // Gửi modifier nếu Viewer báo có nhưng phím hiện tại không phải modifier
                // (tránh gửi Ctrl kép khi Viewer gửi phím ControlLeft/Right)
                bool isModifierKey = IsModifierVk(vk);

                if (isDown && !isModifierKey)
                {
                    if (dto.CtrlKey)  inputs.Add(MakeVkInput(VK_CONTROL, false, false));
                    if (dto.ShiftKey) inputs.Add(MakeVkInput(VK_SHIFT,   false, false));
                    if (dto.AltKey)   inputs.Add(MakeVkInput(VK_MENU,    false, false));
                }

                inputs.Add(MakeVkInput(vk, !isDown, isExtended));

                if (!isDown && !isModifierKey)
                {
                    if (dto.AltKey)   inputs.Add(MakeVkInput(VK_MENU,    true, false));
                    if (dto.ShiftKey) inputs.Add(MakeVkInput(VK_SHIFT,   true, false));
                    if (dto.CtrlKey)  inputs.Add(MakeVkInput(VK_CONTROL, true, false));
                }

                LoggingHelper.Debug($"Key{(isDown ? "Down" : "Up")}: Code=\"{dto.Code}\" → VK=0x{vk:X2} extended={isExtended}");

                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                LoggingHelper.Error($"SimulateKeyboardEvent lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo INPUT dùng Virtual Key (wVk), không dùng scan code.
        /// </summary>
        private static INPUT MakeVkInput(ushort vk, bool keyUp, bool extended)
        {
            uint flags = keyUp ? KEYEVENTF_KEYUP : 0u;
            if (extended) flags |= KEYEVENTF_EXTENDEDKEY;

            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk   = vk,
                        wScan = 0,
                        dwFlags = flags
                    }
                }
            };
        }

        /// <summary>
        /// Một số VK cần KEYEVENTF_EXTENDEDKEY để Windows nhận đúng:
        /// phím bên phải numpad (Insert/Delete/Home/End/PageUp/Down/Arrow),
        /// RControl, RAlt, NumLock, PrintScreen, Pause...
        /// </summary>
        private static bool IsExtendedKey(ushort vk) => vk switch
        {
            0x21 => true, // VK_PRIOR  PageUp
            0x22 => true, // VK_NEXT   PageDown
            0x23 => true, // VK_END
            0x24 => true, // VK_HOME
            0x25 => true, // VK_LEFT
            0x26 => true, // VK_UP
            0x27 => true, // VK_RIGHT
            0x28 => true, // VK_DOWN
            0x2D => true, // VK_INSERT
            0x2E => true, // VK_DELETE
            0x5B => true, // VK_LWIN
            0x5C => true, // VK_RWIN
            0x6F => true, // VK_DIVIDE  Numpad /
            0x90 => true, // VK_NUMLOCK
            0x91 => true, // VK_SCROLL
            0xA3 => true, // VK_RCONTROL
            0xA5 => true, // VK_RMENU  RAlt
            _ => false
        };

        private static bool IsModifierVk(ushort vk) => vk is
            VK_SHIFT or VK_CONTROL or VK_MENU or
            VK_LSHIFT or VK_RSHIFT or
            VK_LCONTROL or VK_RCONTROL or
            VK_LMENU or VK_RMENU;

        // ─── Code → Virtual Key map ───────────────────────────────────────────

        /// <summary>
        /// Map KeyboardEvent.code (chuẩn Web API) sang Windows Virtual Key.
        /// Dùng Code vì nó biểu diễn vị trí vật lý, không phụ thuộc layout.
        /// </summary>
        private static ushort CodeToVirtualKey(string code) => code switch
        {
            // ── Chữ cái ──────────────────────────────────────────────────────
            "KeyA" => 0x41, "KeyB" => 0x42, "KeyC" => 0x43, "KeyD" => 0x44,
            "KeyE" => 0x45, "KeyF" => 0x46, "KeyG" => 0x47, "KeyH" => 0x48,
            "KeyI" => 0x49, "KeyJ" => 0x4A, "KeyK" => 0x4B, "KeyL" => 0x4C,
            "KeyM" => 0x4D, "KeyN" => 0x4E, "KeyO" => 0x4F, "KeyP" => 0x50,
            "KeyQ" => 0x51, "KeyR" => 0x52, "KeyS" => 0x53, "KeyT" => 0x54,
            "KeyU" => 0x55, "KeyV" => 0x56, "KeyW" => 0x57, "KeyX" => 0x58,
            "KeyY" => 0x59, "KeyZ" => 0x5A,

            // ── Số hàng trên ─────────────────────────────────────────────────
            "Digit1" => 0x31, "Digit2" => 0x32, "Digit3" => 0x33, "Digit4" => 0x34,
            "Digit5" => 0x35, "Digit6" => 0x36, "Digit7" => 0x37, "Digit8" => 0x38,
            "Digit9" => 0x39, "Digit0" => 0x30,

            // ── Numpad ───────────────────────────────────────────────────────
            "Numpad0" => 0x60, "Numpad1" => 0x61, "Numpad2" => 0x62,
            "Numpad3" => 0x63, "Numpad4" => 0x64, "Numpad5" => 0x65,
            "Numpad6" => 0x66, "Numpad7" => 0x67, "Numpad8" => 0x68,
            "Numpad9" => 0x69,
            "NumpadMultiply" => 0x6A,
            "NumpadAdd"      => 0x6B,
            "NumpadSubtract" => 0x6D,
            "NumpadDecimal"  => 0x6E,
            "NumpadDivide"   => 0x6F,
            "NumpadEnter"    => 0x0D, // same VK as Enter; EXTENDEDKEY set bởi IsExtendedKey nếu cần

            // ── Function keys ────────────────────────────────────────────────
            "F1"  => 0x70, "F2"  => 0x71, "F3"  => 0x72, "F4"  => 0x73,
            "F5"  => 0x74, "F6"  => 0x75, "F7"  => 0x76, "F8"  => 0x77,
            "F9"  => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,

            // ── Phím điều hướng ──────────────────────────────────────────────
            "ArrowLeft"  => 0x25,
            "ArrowUp"    => 0x26,
            "ArrowRight" => 0x27,
            "ArrowDown"  => 0x28,
            "Home"       => 0x24,
            "End"        => 0x23,
            "PageUp"     => 0x21,
            "PageDown"   => 0x22,
            "Insert"     => 0x2D,
            "Delete"     => 0x2E,

            // ── Phím đặc biệt ────────────────────────────────────────────────
            "Enter"       => 0x0D,
            "Escape"      => 0x1B,
            "Space"       => 0x20,
            "Tab"         => 0x09,
            "Backspace"   => 0x08,
            "CapsLock"    => 0x14,
            "NumLock"     => 0x90,
            "ScrollLock"  => 0x91,
            "Pause"       => 0x13,
            "PrintScreen" => 0x2C,

            // ── Modifier — Left/Right phân biệt ──────────────────────────────
            "ControlLeft"  => VK_LCONTROL,
            "ControlRight" => VK_RCONTROL,
            "ShiftLeft"    => VK_LSHIFT,
            "ShiftRight"   => VK_RSHIFT,
            "AltLeft"      => VK_LMENU,
            "AltRight"     => VK_RMENU,
            "MetaLeft"     => 0x5B, // VK_LWIN
            "MetaRight"    => 0x5C, // VK_RWIN

            // ── Dấu câu / ký tự đặc biệt ────────────────────────────────────
            "Minus"        => 0xBD,
            "Equal"        => 0xBB,
            "BracketLeft"  => 0xDB,
            "BracketRight" => 0xDD,
            "Semicolon"    => 0xBA,
            "Quote"        => 0xDE,
            "Backquote"    => 0xC0,
            "Comma"        => 0xBC,
            "Period"       => 0xBE,
            "Slash"        => 0xBF,
            "Backslash"    => 0xDC,

            // ── Fallback ─────────────────────────────────────────────────────
            _ => 0
        };

        // ─── Utility ──────────────────────────────────────────────────────────

        private static void SendInputs(params INPUT[] inputs)
        {
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}