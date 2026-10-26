# Demo website qua Cloudflare Quick Tunnel

Luồng truy cập: trình duyệt → URL HTTPS `https://….trycloudflare.com` → cloudflared trên máy bạn → Web tại `https://localhost:7167` → SQL Server local. Không cần tài khoản Cloudflare, tên miền, ZIP hoặc deploy GitHub. GitHub vẫn dùng để lưu source code.

## 1. Chuẩn bị

- Chạy các lệnh tại thư mục `D:\RestaurantManagementSystem` bằng PowerShell.
- Máy cần .NET 10 SDK, SQL Server instance đang dùng và database `RestaurantDB`. Giữ connection string local hiện tại; không cần chuyển database lên cloud.
- Đã có `cloudflared.exe` tại thư mục dự án trên máy này. File thực thi đã được Git ignore. Trên máy khác có thể cài bằng `winget install --id Cloudflare.cloudflared --exact`, rồi mở lại PowerShell để nhận PATH.
- Nếu chưa có chứng chỉ phát triển HTTPS, chạy `dotnet dev-certs https --trust` và chấp nhận hộp thoại tin cậy chứng chỉ của Windows.

## 2. Chạy website — cửa sổ PowerShell thứ nhất

```powershell
Set-Location D:\RestaurantManagementSystem
dotnet run --project RestaurantManagement.Web --launch-profile https
```

Đợi Web khởi động, rồi mở `https://localhost:7167`. Kiểm tra trang chủ và đăng nhập trước khi mở tunnel. Nếu Web đang chạy đúng cổng này rồi thì dùng luôn, không chạy thêm một bản trên cùng cổng.

Dữ liệu demo và tài khoản đã nạp được giữ trong SQL Server local. Xem [DEMO_WEBSITE.md](DEMO_WEBSITE.md); không cần nạp lại database mỗi lần mở tunnel.

## 3. Mở tunnel — cửa sổ PowerShell thứ hai

```powershell
Set-Location D:\RestaurantManagementSystem
.\Start-QuickTunnel.ps1
```

Nếu Windows chặn chạy script PowerShell, dùng lệnh chỉ áp dụng cho lần chạy này:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Start-QuickTunnel.ps1
```

Script dùng lệnh tương đương dưới đây. Nếu không dùng script, có thể chạy trực tiếp:

```powershell
.\cloudflared.exe --no-autoupdate tunnel --url https://localhost:7167 --no-tls-verify
```

Copy URL HTTPS có đuôi `trycloudflare.com` do cloudflared in ra để mở trên điện thoại hoặc chia sẻ cho người xem demo. Đợi log báo kết nối tunnel thành công. Trang quản trị là `<URL>/admin`, đăng nhập khách là `<URL>/Account/Login`. App MAUI cũng có thể nhập URL gốc HTTPS này.

Web hiện chuyển HTTP sang HTTPS và dùng cookie đăng nhập Secure. Vì vậy tunnel trỏ thẳng tới **HTTPS cổng 7167** để tránh chuyển hướng về cổng localhost hoặc lặp chuyển hướng. `--no-tls-verify` chỉ bỏ kiểm tra chứng chỉ phát triển của origin trên máy này; địa chỉ công khai vẫn dùng HTTPS của Cloudflare. Không đổi cookie sang HTTP.

## 4. Trong và sau buổi demo

- Giữ máy, mạng Internet, SQL Server, Web và cloudflared chạy; tắt chế độ sleep trong buổi demo.
- URL công khai cho phép người có link truy cập các màn của website theo quyền đăng nhập hiện có. Dùng bộ dữ liệu demo khi chia sẻ link.
- Nhấn `Ctrl+C` trong cửa sổ tunnel để ngừng chia sẻ; nhấn `Ctrl+C` ở cửa sổ Web khi muốn tắt Web. Database vẫn giữ nguyên.
- Mỗi lần tạo tunnel mới, URL sẽ đổi. Cập nhật link gửi người xem và URL trong app mobile.

## 5. Xử lý lỗi thường gặp

- **Script báo chưa chạy Web / connection refused:** chạy bước 2, kiểm tra Web lắng nghe `https://localhost:7167` và SQL Server đã bật.
- **502 Bad Gateway:** kiểm tra lại trang HTTPS local; dùng đúng cổng 7167 và giữ cờ `--no-tls-verify` khi dùng chứng chỉ phát triển.
- **Không đăng nhập được:** mở đúng URL HTTPS công khai, kiểm tra tài khoản trước trên Web local; không đổi tunnel sang origin HTTP 5255.
- **Không kết nối được QUIC trên mạng hiện tại:** dừng tunnel rồi thử `.\cloudflared.exe --no-autoupdate tunnel --protocol http2 --url https://localhost:7167 --no-tls-verify`.
- **Máy đã có cấu hình named tunnel:** nếu cloudflared báo Quick Tunnel bị ảnh hưởng bởi `config.yml`/`config.yaml` trong `%USERPROFILE%\.cloudflared`, kiểm tra cấu hình đó trước; không tự xóa cấu hình tunnel khác.

Quick Tunnel dành cho thử nghiệm/demo, không bảo đảm uptime, giới hạn 200 yêu cầu đang xử lý đồng thời và không hỗ trợ SSE. Tài liệu chính thức: [Cloudflare Quick Tunnels](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/).
