# 🖥️ RemoteHostApp – WinForms Host Application

> **Phase 5** của hệ thống **Remote Controller App** — Ứng dụng Host chạy trên máy tính được điều khiển từ xa.

RemoteHostApp là ứng dụng WinForms (.NET 8.0) chạy trên máy **Host** (máy bị điều khiển). Ứng dụng kết nối tới SignalR Server, cho phép Viewer yêu cầu điều khiển, sau đó liên tục chụp màn hình và stream tới Viewer, đồng thời nhận và thực thi các sự kiện chuột/bàn phím từ xa thông qua Windows API `SendInput`.

---

## 📋 Mục lục

- [Tổng quan dự án](#-tổng-quan-dự-án)
- [Chức năng chính](#-chức-năng-chính)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Cấu trúc thư mục](#-cấu-trúc-thư-mục)
- [Cách cài đặt & chạy](#-cách-cài-đặt--chạy)
- [Cấu hình (appsettings.json)](#-cấu-hình-appsettingsjson)
- [Hướng dẫn sử dụng chi tiết](#-hướng-dẫn-sử-dụng-chi-tiết)
- [Các tính năng đã implement](#-các-tính-năng-đã-implement)
- [Tích hợp với Server](#-tích-hợp-với-server)
- [Lưu ý quan trọng](#-lưu-ý-quan-trọng)
- [Troubleshooting](#-troubleshooting)
- [Phase tiếp theo](#-phase-tiếp-theo)

---

## 🔍 Tổng quan dự án

### Kiến trúc hệ thống

```
┌──────────────┐     SignalR (WebSocket)      ┌──────────────┐     SignalR (WebSocket)      ┌──────────────┐
│              │ ◄──────────────────────────►  │              │ ◄──────────────────────────►  │              │
│  Viewer App  │   Mouse/Keyboard Events       │   Server     │   Screen Frames (Base64)      │  Host App    │
│  (Phase 6)   │   Screen Frames               │  (Phase 1-4) │   Control Request/Response    │  (Phase 5)   │
│              │ ◄──────────────────────────►  │              │ ◄──────────────────────────►  │  ← BẠN ĐANG  │
└──────────────┘                               └──────────────┘                               │    Ở ĐÂY     │
                                                                                              └──────────────┘
```

### Vai trò của RemoteHostApp

RemoteHostApp đóng vai trò là **máy bị điều khiển** trong hệ thống. Cụ thể:

1. **Đăng ký** với SignalR Server bằng Host ID duy nhất
2. **Chờ** Viewer gửi yêu cầu điều khiển
3. **Hiển thị popup** để Host chấp nhận hoặc từ chối
4. **Stream màn hình** liên tục tới Viewer sau khi chấp nhận
5. **Thực thi** sự kiện chuột/bàn phím mà Viewer gửi về

---

## ⚡ Chức năng chính

| Chức năng | Mô tả |
|---|---|
| 🔌 **Kết nối SignalR** | Kết nối tới server với auto-reconnect (2s → 5s → 10s → 30s), tự động re-register host sau reconnect |
| 📋 **Đăng ký Host** | Đăng ký Host ID và tên máy tính với server để Viewer có thể tìm thấy |
| 🔔 **Nhận yêu cầu điều khiển** | Hiển thị popup khi Viewer yêu cầu, cho phép chấp nhận hoặc từ chối (có lý do) |
| 📸 **Chụp & stream màn hình** | Chụp toàn bộ màn hình → resize → nén JPEG → encode Base64 → gửi qua SignalR |
| 🖱️ **Mô phỏng chuột** | Move, Left Click, Right Click, Double Click, Scroll |
| ⌨️ **Mô phỏng bàn phím** | KeyDown, KeyUp, hỗ trợ tổ hợp Ctrl + Shift + Alt |
| 💓 **Ping định kỳ** | Gửi ping tới server mỗi 10 giây để duy trì trạng thái online |
| 📊 **Log realtime** | Hiển thị log color-coded trên giao diện (Info, Warning, Error, Debug) |

---

## 🛠️ Công nghệ sử dụng

| Công nghệ | Version | Mục đích |
|---|---|---|
| **.NET** | 8.0 | Runtime & SDK |
| **WinForms** | .NET 8.0-windows | Giao diện người dùng |
| **SignalR Client** | 8.0.0 | Giao tiếp realtime với server |
| **Windows API (P/Invoke)** | `user32.dll` | `SendInput` – mô phỏng chuột/bàn phím |
| **GDI+ / System.Drawing** | Built-in | Chụp và xử lý ảnh màn hình |
| **Microsoft.Extensions.Configuration** | 8.0.0 | Đọc cấu hình từ `appsettings.json` |
| **Newtonsoft.Json** | 13.0.3 | Serialization/Deserialization JSON |

---

## 📁 Cấu trúc thư mục

```
RemoteHostApp/
├── RemoteHostApp.sln                 # Solution file
└── RemoteHostApp/
    ├── Program.cs                    # Entry point – khởi tạo services và chạy MainForm
    ├── RemoteHostApp.csproj          # Project file (.NET 8.0 SDK-style)
    ├── app.manifest                  # Yêu cầu quyền Administrator
    ├── appsettings.json              # Cấu hình ứng dụng
    │
    ├── DTOs/                         # Data Transfer Objects (giống hệt server)
    │   ├── ControlResponseDto.cs     #   Phản hồi Accept/Reject
    │   ├── HostRegisterDto.cs        #   Đăng ký host
    │   ├── KeyboardEventDto.cs       #   Sự kiện bàn phím
    │   ├── MouseEventDto.cs          #   Sự kiện chuột
    │   └── ScreenFrameDto.cs         #   Frame màn hình (Base64 JPEG)
    │
    ├── Models/                       # Domain models
    │   ├── ActiveSession.cs          #   Thông tin phiên điều khiển + enum SessionStatus
    │   ├── AppSettings.cs            #   POCO cho cấu hình
    │   └── HostInfo.cs               #   Thông tin máy host
    │
    ├── Services/                     # Business logic
    │   ├── SignalRHostService.cs     #   Quản lý kết nối SignalR, gửi/nhận events
    │   ├── ScreenCaptureService.cs   #   Chụp màn hình + stream (Timer-based)
    │   ├── InputSimulatorService.cs  #   Mô phỏng mouse/keyboard via SendInput
    │   └── SessionManager.cs         #   Quản lý trạng thái phiên hiện tại
    │
    ├── Forms/                        # WinForms UI
    │   ├── MainForm.cs               #   Form chính – điều phối toàn bộ
    │   └── AcceptControlForm.cs      #   Popup chấp nhận/từ chối điều khiển
    │
    └── Helpers/                      # Tiện ích
        ├── ImageHelper.cs            #   Resize + JPEG compress + Base64 encode
        ├── LoggingHelper.cs          #   Hệ thống log tập trung
        └── WinApiHelper.cs           #   P/Invoke structs & constants cho SendInput
```

---

## 🚀 Cách cài đặt & chạy

### Yêu cầu hệ thống

- **Hệ điều hành**: Windows 10 / 11 (bắt buộc – sử dụng Windows API)
- **.NET SDK**: 8.0 trở lên ([Tải tại đây](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Quyền Administrator**: Ứng dụng yêu cầu chạy với quyền Admin (để `SendInput` hoạt động)
- **Server đang chạy**: RemoteControllerServer (Phase 1-4) phải đang chạy và accessible

### Cài đặt

```bash
# 1. Clone hoặc copy project
git clone <repository-url>
cd RemoteHostApp

# 2. Restore NuGet packages
dotnet restore

# 3. Build project
dotnet build
```

### Chạy ứng dụng

```bash
# Chạy trực tiếp (sẽ yêu cầu quyền Admin qua UAC prompt)
dotnet run --project RemoteHostApp
```

Hoặc mở `RemoteHostApp.sln` bằng **Visual Studio 2022** → nhấn **F5** (chạy Debug) hoặc **Ctrl+F5** (chạy không Debug).

> ⚠️ **Lưu ý**: Nếu chạy bằng `dotnet run`, terminal/IDE cũng cần được chạy với quyền Administrator. Nếu không, ứng dụng sẽ hiển thị UAC prompt hoặc bị từ chối quyền.

---

## ⚙️ Cấu hình (appsettings.json)

```json
{
  "AppSettings": {
    "ServerUrl": "http://localhost:5271",
    "HubEndpoint": "/remoteHub",
    "CaptureIntervalMs": 100,
    "MaxFrameWidth": 1280,
    "JpegQuality": 75,
    "PingIntervalSeconds": 10
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Chi tiết các tham số

| Tham số | Giá trị mặc định | Mô tả |
|---|---|---|
| `ServerUrl` | `http://localhost:5271` | URL của SignalR Server. Có thể thay đổi qua giao diện. |
| `HubEndpoint` | `/remoteHub` | Endpoint của SignalR Hub trên server. |
| `CaptureIntervalMs` | `100` | Khoảng cách giữa các frame (ms). `100` = ~10 FPS. |
| `MaxFrameWidth` | `1280` | Chiều rộng tối đa của frame gửi đi (pixel). Chiều cao tự tính theo tỉ lệ. |
| `JpegQuality` | `75` | Chất lượng nén JPEG (1-100). Giảm → file nhỏ hơn, ảnh mờ hơn. |
| `PingIntervalSeconds` | `10` | Chu kỳ ping server (giây). Server dùng ping để biết host còn online. |

### Khuyến nghị điều chỉnh hiệu năng

| Tình huống | CaptureIntervalMs | MaxFrameWidth | JpegQuality |
|---|---|---|---|
| **Mạng LAN tốc độ cao** | `80` | `1920` | `85` |
| **Mạng bình thường** | `100` | `1280` | `75` |
| **Mạng yếu / 3G/4G** | `200` | `960` | `50` |
| **Chỉ xem (không cần rõ)** | `300` | `800` | `40` |

---

## 📖 Hướng dẫn sử dụng chi tiết

### Bước 1: Khởi động Server

Đảm bảo **RemoteControllerServer** đang chạy:

```bash
# Trong thư mục server
cd RemoteControllerServer
dotnet run
# Server chạy tại http://localhost:5271
```

### Bước 2: Khởi động RemoteHostApp

```bash
# Trong thư mục RemoteHostApp (chạy với quyền Admin)
dotnet run --project RemoteHostApp
```

Giao diện chính sẽ hiển thị với các trường:
- **Server URL**: Địa chỉ server (mặc định `http://localhost:5271`)
- **Host ID**: ID duy nhất của máy host (tự động sinh)
- **Computer**: Tên máy tính (tự động lấy)

### Bước 3: Kết nối tới Server

1. Kiểm tra **Server URL** đúng địa chỉ server
2. Nhấn nút **🔌 Kết nối**
3. Chờ trạng thái chuyển sang **"● Online – Đã kết nối"** (màu xanh dương)
4. Log panel sẽ hiển thị: `Kết nối SignalR thành công!`

### Bước 4: Đăng ký Host

1. Kiểm tra **Host ID** (có thể sửa lại nếu muốn)
2. Nhấn nút **📋 Đăng ký Host**
3. Trạng thái chuyển sang **"● Online – Đã đăng ký"** (màu xanh lá)
4. Lúc này Viewer có thể tìm thấy Host của bạn trên server

### Bước 5: Chấp nhận yêu cầu điều khiển

Khi một Viewer gửi yêu cầu điều khiển:

1. **Popup** sẽ hiện lên trên cùng (TopMost) với thông tin Viewer
2. Bạn có 2 lựa chọn:
   - ✅ **Chấp nhận**: Bắt đầu session điều khiển, tự động stream màn hình
   - ❌ **Từ chối**: Có thể nhập lý do từ chối (tuỳ chọn)
3. Sau khi chấp nhận, trạng thái chuyển sang **"● In Session – Đang điều khiển"** (màu cam)

> ⚠️ **Quan trọng**: Streaming màn hình chỉ bắt đầu **sau khi Host chấp nhận** yêu cầu. Viewer không thể xem/điều khiển máy mà không có sự đồng ý.

### Bước 6: Trong phiên điều khiển

Khi session đang hoạt động:

- **Màn hình** được chụp và gửi liên tục (mặc định 10 FPS)
- **Chuột & bàn phím** từ Viewer được thực thi trên máy Host
- Bạn có thể:
  - Nhấn **⏹ Stop Stream** để tạm dừng stream (vẫn giữ session)
  - Nhấn **▶ Start Stream** để tiếp tục stream
  - Nhấn **🛑 End Control** để kết thúc toàn bộ phiên

### Bước 7: Kết thúc phiên

Phiên điều khiển kết thúc khi:
- Host nhấn **🛑 End Control**
- Viewer chủ động kết thúc
- Mất kết nối (timeout)

Sau khi kết thúc, trạng thái trở về **"● Online – Đã đăng ký"** và Host sẵn sàng nhận session mới.

---

## ✅ Các tính năng đã implement

### Kết nối & Mạng

- [x] Kết nối SignalR với WebSocket transport
- [x] Auto-reconnect với exponential backoff (2s → 5s → 10s → 30s)
- [x] Tự động re-register host sau khi reconnect
- [x] Ping định kỳ để duy trì trạng thái online
- [x] Fire-and-forget cho gửi frame (`SendAsync` thay `InvokeAsync`)
- [x] Lifecycle events: Reconnecting, Reconnected, Closed

### Screen Capture & Streaming

- [x] Chụp toàn bộ màn hình chính bằng GDI+ (`CopyFromScreen`)
- [x] Resize giữ tỉ lệ khung hình
- [x] Nén JPEG với quality tuỳ chỉnh
- [x] Encode Base64 và gửi qua SignalR
- [x] Gửi kèm vị trí chuột hiện tại trong mỗi frame
- [x] Bảo vệ chống frame overlap bằng SemaphoreSlim
- [x] Timer-based với interval tuỳ chỉnh

### Input Simulation

- [x] Di chuyển chuột (tọa độ absolute 0-65535)
- [x] Click trái / Click phải
- [x] Double click
- [x] Scroll (lên/xuống với delta)
- [x] KeyDown / KeyUp
- [x] Tổ hợp phím: Ctrl + Key, Shift + Key, Alt + Key
- [x] Tổ hợp phím kết hợp: Ctrl + Shift + Key, v.v.

### Giao diện & UX

- [x] Form chính với các nút chức năng rõ ràng
- [x] Popup chấp nhận/từ chối với TopMost
- [x] Trạng thái color-coded (Offline/Online/In Session/Reconnecting)
- [x] Log panel với color-coded theo severity
- [x] Giới hạn log 2000 dòng (tránh memory leak)
- [x] Enable/Disable nút theo trạng thái
- [x] Lock input fields khi đã kết nối
- [x] Graceful shutdown khi đóng form (async cleanup)

---

## 🔗 Tích hợp với Server

### SignalR Hub Methods (Host gọi lên Server)

| Method | Tham số | Mô tả |
|---|---|---|
| `RegisterHost` | `HostRegisterDto` | Đăng ký host với server |
| `PingHost` | `string hostId` | Ping giữ host online |
| `AcceptControl` | `ControlResponseDto` | Chấp nhận yêu cầu điều khiển |
| `RejectControl` | `ControlResponseDto` | Từ chối yêu cầu điều khiển |
| `EndControl` | `string sessionId` | Kết thúc phiên điều khiển |
| `SendScreenFrame` | `ScreenFrameDto` | Gửi frame màn hình tới Viewer |

### SignalR Events (Server gửi xuống Host)

| Event | Tham số | Mô tả |
|---|---|---|
| `ReceiveControlRequest` | `sessionId, viewerId, viewerName` | Viewer yêu cầu điều khiển |
| `ReceiveMouseEvent` | `MouseEventDto` | Sự kiện chuột từ Viewer |
| `ReceiveKeyboardEvent` | `KeyboardEventDto` | Sự kiện bàn phím từ Viewer |
| `ControlAccepted` | `sessionId` | Xác nhận session accepted |
| `ControlRejected` | `sessionId` | Xác nhận session rejected |
| `ControlEnded` | `sessionId` | Viewer kết thúc phiên |

### Test với test-signalr.html

Nếu server có sẵn file **test-signalr.html**, bạn có thể dùng nó để test trực tiếp:

1. Mở `http://localhost:5271/test-signalr.html` trên trình duyệt
2. Kết nối tới Hub tại endpoint `/remoteHub`
3. Gọi `RegisterHost` để mô phỏng Viewer gửi `ControlRequest`
4. Quan sát RemoteHostApp nhận được popup yêu cầu

Hoặc dùng REST API của server:

```bash
# Lấy danh sách host online
curl http://localhost:5271/api/hosts

# Gửi yêu cầu điều khiển (giả lập Viewer)
curl -X POST http://localhost:5271/api/control/request \
  -H "Content-Type: application/json" \
  -d '{"hostId": "<HOST_ID>", "viewerName": "TestViewer"}'
```

---

## ⚠️ Lưu ý quan trọng

### 🔐 Quyền Administrator

Ứng dụng **bắt buộc** chạy với quyền Administrator. Đây là yêu cầu của Windows để:
- `SendInput` API hoạt động đúng (mô phỏng chuột/bàn phím)
- `CopyFromScreen` chụp được màn hình đầy đủ
- Tránh bị UIPI (User Interface Privilege Isolation) chặn

Cấu hình quyền nằm trong `app.manifest`:
```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

### 📡 Hiệu năng mạng

- Mỗi frame JPEG 1280px ≈ **50–150 KB** (raw) → **70–200 KB** (Base64)
- Ở 10 FPS → bandwidth **700 KB – 2 MB/s** cho mỗi session
- Trên mạng LAN: hoạt động tốt
- Trên mạng internet: cân nhắc giảm `MaxFrameWidth` và `JpegQuality`

### 🔒 Bảo mật (Phiên bản hiện tại)

Phiên bản hiện tại chưa có:
- Authentication (JWT/API Key)
- Mã hóa end-to-end cho dữ liệu màn hình
- Rate limiting cho input events
- Validation bounds cho tọa độ chuột

> 💡 Các tính năng bảo mật này sẽ được bổ sung trong các phase sau. Hiện tại phù hợp cho môi trường **phát triển và demo**.

### 🖥️ Đa màn hình

Phiên bản hiện tại chỉ chụp **màn hình chính** (`Screen.PrimaryScreen`). Hỗ trợ đa màn hình sẽ được xem xét trong tương lai.

---

## 🔧 Troubleshooting

### Lỗi thường gặp và cách khắc phục

| Lỗi | Nguyên nhân | Cách khắc phục |
|---|---|---|
| `Không thể kết nối` | Server chưa chạy hoặc URL sai | Kiểm tra server đang chạy. Kiểm tra URL (mặc định: `http://localhost:5271`) |
| `Kết nối thất bại: Connection refused` | Firewall chặn port | Mở port 5271 trên Windows Firewall hoặc tắt firewall tạm thời |
| `SendInput không hoạt động` | Không có quyền Admin | Chạy lại ứng dụng với quyền Administrator (Right-click → Run as Administrator) |
| `CopyFromScreen lỗi` | DPI scaling hoặc Secure Desktop | Kiểm tra DPI settings. Đảm bảo không ở Secure Desktop (login screen) |
| `SignalR đang reconnect` liên tục | Server restart hoặc mạng không ổn định | Kiểm tra kết nối mạng. Ứng dụng sẽ tự reconnect (2s → 5s → 10s → 30s) |
| `Ping lỗi` | Mất kết nối tạm thời | Tự động recovery. Nếu kéo dài, kiểm tra mạng và restart server |
| Ứng dụng **treo** khi đóng | Async cleanup chưa hoàn thành | Chờ vài giây. Nếu không đóng được, dùng Task Manager |
| **Lag / FPS thấp** | Bandwidth không đủ hoặc CPU quá tải | Giảm `MaxFrameWidth` (960), giảm `JpegQuality` (50), tăng `CaptureIntervalMs` (200) |

### Kiểm tra kết nối

```bash
# Kiểm tra server có accessible không
curl http://localhost:5271/api/hosts

# Kiểm tra SignalR Hub
# Mở trình duyệt → http://localhost:5271/remoteHub → nếu thấy response là OK
```

### Log Level

Nếu cần debug chi tiết, thay đổi log level trong `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

---

## 🚀 Phase tiếp theo

### Vị trí trong Roadmap

| Phase | Thành phần | Trạng thái |
|---|---|---|
| Phase 1 | SignalR Hub Server | ✅ Hoàn thành |
| Phase 2 | REST API (Host management) | ✅ Hoàn thành |
| Phase 3 | Session management & routing | ✅ Hoàn thành |
| Phase 4 | Server optimization & logging | ✅ Hoàn thành |
| **Phase 5** | **WinForms Host App** | **✅ Hoàn thành** |
| Phase 6 | Viewer App (WinForms / Web) | 🔲 Tiếp theo |
| Phase 7 | Authentication & Security | 🔲 Kế hoạch |
| Phase 8 | Delta Frame & Performance | 🔲 Kế hoạch |

### Phase 6: Viewer App (dự kiến)

Viewer App sẽ cần implement:
- Kết nối SignalR và gửi `ControlRequest` tới Host
- Nhận và hiển thị `ScreenFrameDto` (decode Base64 → render)
- Capture sự kiện chuột/bàn phím từ user → gửi `MouseEventDto` / `KeyboardEventDto`
- Giao diện hiển thị màn hình từ xa với khả năng tương tác

### Phase 7-8: Tối ưu hóa (dự kiến)

- **Authentication**: JWT token cho cả Host và Viewer
- **HTTPS/WSS**: Mã hóa truyền tải
- **Delta Frame**: Chỉ gửi vùng màn hình thay đổi (giảm ~70-90% bandwidth)
- **Binary Protocol**: Chuyển từ Base64/JSON sang MessagePack (giảm ~33% overhead)
- **Multi-monitor**: Hỗ trợ chọn và chụp nhiều màn hình
- **File Transfer**: Truyền file giữa Host và Viewer

---

## 📄 Thông tin bổ sung

### Dependencies (NuGet Packages)

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="8.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### Build & Publish

```bash
# Build Debug
dotnet build

# Build Release
dotnet build -c Release

# Publish thành file .exe độc lập
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

---

**RemoteHostApp** – Phase 5 of Remote Controller App System  
Developed for NT106 – Computer Networks Course
