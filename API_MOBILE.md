# REST API và ứng dụng mobile

Cập nhật 06/11/2026. Đáp ứng mục "Xây dựng API hệ thống" và "Cài đặt chức năng trên nền tảng mobile" của đề cương.

## Kiến trúc

| Thành phần | Vai trò |
| --- | --- |
| `RestaurantManagement.API` | Tầng dữ liệu dùng chung: entity, `RestaurantDbContext`, migrations (SQL Server). |
| `RestaurantManagement.Web` | Website MVC **và** REST API `/api/...` (thư mục `Api/`). API gọi lại đúng các service nghiệp vụ của website (`OrderService`, `TableService`, `IPromotionService`, `CustomerHistoryService`), nên quy tắc trên web và mobile luôn giống nhau. |
| `RestaurantManagement.Shared` | DTO dùng chung giữa API và app (`Mobile/MobileContracts.cs`). |
| `RestaurantManagement.Mobile` | App .NET MAUI native (Android, Windows), gọi API bằng `HttpClient`. |

**Xác thực:** website giữ cookie. API dùng bearer token của ASP.NET Core Identity (hạn 8 giờ). Một policy scheme chọn theo đường dẫn: request `/api` chỉ đọc token, không bao giờ đọc cookie (không có rủi ro CSRF) và trả `401/403` dạng JSON thay vì chuyển sang trang đăng nhập. `ActiveAccountMiddleware` vẫn kiểm tra tài khoản còn hoạt động với cả token. Lỗi nghiệp vụ trả về `{ "message": "..." }` bằng tiếng Việt.

**Vai trò dùng app:** Quản lý, Tiếp tân, Bồi bàn, Thu ngân, Khách hàng. Bếp và Kho bị từ chối ở bước đăng nhập (`403`), vì hai vai trò này làm việc trên website.

## Danh sách endpoint

| Phương thức | Đường dẫn | Vai trò | Chức năng |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Mọi người | Đăng nhập: `{ email, password }`, trả về `{ tokenType, accessToken, expiresIn, refreshToken }` |
| GET | `/api/auth/me` | Đã đăng nhập | Họ tên, vai trò và các quyền `canOrder/canServe/canPay/canReceive` |
| GET | `/api/staff/tables` | QL, TT, BB, TN | Sơ đồ bàn: Trống / Đã đặt / Đang phục vụ / Chờ dọn, kèm đơn đang mở |
| POST | `/api/staff/tables/{id}/finish` | QL, BB | Kết thúc phục vụ, bàn chuyển sang Chờ dọn |
| POST | `/api/staff/tables/{id}/clean` | QL, BB | Đã dọn xong, bàn về Sẵn sàng |
| GET | `/api/staff/menu` | QL, TT, BB, TN | Thực đơn đang bán, theo danh mục, kèm size và giá |
| GET | `/api/staff/orders?open=true` | QL, TT, BB, TN | Đơn chưa thanh toán (`open=false`: đơn hôm nay) |
| GET | `/api/staff/orders/{id}` | QL, TT, BB, TN | Chi tiết đơn, trạng thái từng món, số phải thu (đã tính ưu đãi và trừ cọc) |
| POST | `/api/staff/orders` | QL, TT, BB, TN | Tạo đơn tại bàn, mang đi hoặc giao hàng |
| POST | `/api/staff/orders/{id}/items` | QL, TT, BB, TN | Gọi thêm món |
| POST | `/api/staff/orders/{id}/lines/{lineId}` | QL, BB | `DaPhucVu` (mang món ra) hoặc `DaHuy` (hủy món chưa nấu xong) |
| POST | `/api/staff/orders/{id}/pay` | QL, TN, BB | Thanh toán tiền mặt hoặc chuyển khoản; server đối chiếu lại số tiền và phiên bản |
| GET | `/api/staff/reservations` | QL, TT | Yêu cầu chờ xác nhận và lịch hôm nay, kèm bàn gợi ý |
| POST | `/api/staff/reservations/{id}/assign` | QL, TT | Xác nhận yêu cầu bằng cách xếp một bàn |
| POST | `/api/staff/reservations/{id}/checkin` | QL, TT | Nhận bàn khi khách đến |
| GET | `/api/customer/summary` | Khách | Họ tên, điểm hiện có, tổng chi tiêu |
| GET | `/api/customer/orders` | Khách | Lịch sử mua hàng, cả đơn đang phục vụ |
| GET | `/api/customer/orders/{id}` | Khách | Trạng thái từng món (chỉ đơn của mình) |
| GET | `/api/customer/points` | Khách | Lịch sử tích điểm và đổi điểm |

Viết tắt: QL = Quản lý (Admin), TT = Tiếp tân, BB = Bồi bàn, TN = Thu ngân.

Các thao tác ghi gửi kèm `rowVersion` mà server đã trả. Nếu dữ liệu vừa bị người khác đổi, API trả `409` để app tải lại, giống cơ chế chống ghi đè của website.

## Chức năng trên app

**Nhân viên**
- **Sơ đồ bàn:** nhóm theo tầng và khu vực, màu theo trạng thái, kéo xuống để làm mới. Bấm vào bàn để gọi món, xem đơn, kết thúc phục vụ hoặc báo đã dọn.
- **Gọi món:** tìm theo tên, lọc theo danh mục, chọn size, ghi chú cho từng món, giỏ món, gửi bếp. Có đơn mang đi và đơn giao hàng.
- **Đơn hàng:** theo dõi trạng thái món (chờ chế biến → đang chế biến → sẵn sàng → đã phục vụ), xác nhận mang món ra, hủy món, gọi thêm.
- **Thanh toán:** tiền mặt (tính tiền thối) hoặc chuyển khoản (nhập mã giao dịch). Tự áp dụng ưu đãi và trừ cọc. Mobile không cho sửa giảm giá thủ công.
- **Đặt bàn** (tiếp tân, quản lý): xác nhận yêu cầu bằng bàn gợi ý, nhận bàn khi khách đến.

**Khách hàng:** xem điểm hiện có, đơn đang phục vụ (tự cập nhật trạng thái món mỗi 15 giây), lịch sử mua hàng và lịch sử tích điểm. Thu thập voucher và đổi điểm vẫn làm trên website.

## Chạy thử

1. Chạy Web: `dotnet run --project RestaurantManagement.Web --launch-profile https` (cổng `https://localhost:7167`). Dữ liệu demo xem [DEMO_WEBSITE.md](DEMO_WEBSITE.md).
2. Chọn cách chạy app:
   - **Windows:** `dotnet build RestaurantManagement.Mobile -f net10.0-windows10.0.19041.0`, rồi mở `RestaurantManagement.Mobile\bin\Debug\net10.0-windows10.0.19041.0\win-x64\RestaurantManagement.Mobile.exe`. Địa chỉ máy chủ: `https://localhost:7167`.
   - **Android emulator:** chạy từ Visual Studio (target Android). Địa chỉ máy chủ: `https://10.0.2.2:7167`. Chỉ bản Debug chấp nhận chứng chỉ phát triển cho `localhost`, `127.0.0.1` và `10.0.2.2`.
   - **Điện thoại thật:** mở Cloudflare Quick Tunnel theo [HUONG_DAN_CLOUDFLARE.md](HUONG_DAN_CLOUDFLARE.md), rồi nhập URL `https://...trycloudflare.com` (chứng chỉ hợp lệ).
3. Đăng nhập bằng tài khoản demo, ví dụ `boiban.demo@example.test`, `thungan.demo@example.test`, `tieptan.demo@example.test`, `khach.demo@example.test`, mật khẩu `Demo@2026!`.

Kiểm thử tự động API trên database tạm (đăng nhập, phân quyền, gọi món, phục vụ, thanh toán, dọn bàn, đặt bàn, dữ liệu khách):

```powershell
dotnet run --project tests/Management.SmokeTests -- --mobile-api-only
```
