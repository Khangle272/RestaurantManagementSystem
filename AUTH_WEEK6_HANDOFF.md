# Bàn giao tuần 6 — đăng nhập và phân quyền

Phạm vi đã làm: khách hàng tự đăng ký; đăng nhập/đăng xuất/đổi mật khẩu; khóa tạm sau 5 lần sai trong 15 phút; Admin cấp tài khoản, đổi vai trò và khóa/mở khóa cho nhân viên đã có hồ sơ. Trang CRUD hiện hữu được chặn ở controller, không chỉ ẩn nút. Nhân viên ngừng làm hoặc khách hàng ngừng sử dụng bị đăng xuất ở yêu cầu tiếp theo.

## Cách chạy trên máy từng thành viên

1. Dùng cùng migration và chuỗi kết nối SQL Server của nhóm. Web hiện đọc `ConnectionStrings:DefaultConnection`.
2. Khởi tạo 6 vai trò và tài khoản Admin đầu tiên bằng biến môi trường **riêng trên máy** (PowerShell):

```powershell
$env:AuthBootstrap__AdminEmail='admin@example.test'
$env:AuthBootstrap__AdminPassword='<mat-khau-rieng-du-8-ky-tu-co-chu-hoa-so>'
dotnet run --project RestaurantManagement.Web -- --init-auth
```

Lệnh chạy lại không đổi mật khẩu Admin và không tự nâng quyền tài khoản đã tồn tại. Không commit hoặc nhắn mật khẩu thật vào Git. Sau đó chạy `dotnet run --project RestaurantManagement.Web`; Admin đăng nhập và cấp tài khoản tại `/TaiKhoanNhanVien` cho hồ sơ nhân viên đang làm việc. Khách đăng ký ở `/Account/Register`. Nếu dữ liệu nhân viên chưa có, tạo hồ sơ qua CRUD trước rồi mới cấp tài khoản. Gửi mật khẩu khởi tạo cho người thử qua kênh riêng; họ đổi mật khẩu sau lần đăng nhập đầu.

## Bảng quyền hiện tại

| Vai trò | Quyền hiện có trong tuần 6 | Chức năng sẽ tích hợp sau |
| --- | --- | --- |
| Khách hàng | Tự đăng ký, đăng nhập, đổi mật khẩu | Xem thực đơn, đặt bàn, đánh giá |
| Tiếp tân | Xem danh sách bàn | Tiếp nhận/xác nhận đặt bàn |
| Bồi bàn | Xem danh sách bàn | Ghi món, theo dõi bàn và nhận món từ bếp |
| Bếp | Đăng nhập, đổi mật khẩu | Xem và cập nhật hàng đợi món |
| Kho | Xem/quản lý nguyên liệu theo CRUD hiện có | Nhập/xuất/tồn kho |
| Admin | CRUD nhân viên, danh mục, món, nguyên liệu, bàn; cấp/khóa/đổi vai trò nhân viên | Quản trị nghiệp vụ bổ sung |

`/NhanVien`, `/DanhMuc`, `/MonAn` và `/TaiKhoanNhanVien` chỉ cho Admin. `/BanAn` cho Admin/TiepTan/BoiBan xem, nhưng thêm/sửa/xóa bàn chỉ Admin. `/NguyenLieu` cho Admin/Kho. Khách không vào các trang nhân viên. Các trang chưa có nghiệp vụ (đặt bàn, bếp, giao món) không được quảng cáo là đã xong.

## Tích hợp của thành viên 2 và 3

- Gắn `[Authorize(Roles = AppRoles.KhachHang + "," + AppRoles.TiepTan + "," + AppRoles.Admin)]` hoặc danh sách vai trò phù hợp **trên controller/action server**, không dựa vào việc ẩn nút. Thêm quyền khác nhau cho thao tác xem và cập nhật nếu cần.
- Kiểm tra quyền sở hữu theo dữ liệu, không chỉ theo vai trò: khách chỉ được xem/sửa đặt bàn của mình. Lấy tài khoản bằng `UserManager<TaiKhoan>.GetUserAsync(User)` rồi tra `KhachHang.TaiKhoanId`.
- Web hiện dùng cookie Identity. API hiện chưa có endpoint nghiệp vụ cho khách/nhân viên; khi thêm API, cần cấu hình xác thực cho API và gắn `[Authorize]` + kiểm tra sở hữu tương tự. Cookie/Web không tự bảo vệ một API mới nếu API được mở riêng.
- Dùng trạng thái dữ liệu hiện có; không tạo thêm hệ thống vai trò hay bảng tài khoản riêng.

Tài khoản thử không được hard-code vào repo. Mẫu cần chia sẻ riêng cho nhóm: `admin@example.test` (Admin do người chạy khởi tạo), một khách tự đăng ký, bốn nhân viên tương ứng TiepTan/BoiBan/Bep/Kho do Admin cấp. Mỗi máy có database local riêng nên tài khoản tạo trên máy này không tự xuất hiện khi thành viên khác pull Git.

Kiểm chứng: `dotnet build tests/Management.SmokeTests/Management.SmokeTests.csproj`; `dotnet run --project tests/Management.SmokeTests --no-build`. Bộ smoke test tạo database tên ngẫu nhiên, kiểm tra HTTP và xóa database thử sau khi hoàn tất. Không chạy lên database thật.
