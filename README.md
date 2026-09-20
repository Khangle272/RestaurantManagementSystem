# RestaurantManagementSystem

Giao diện quản trị Web có CRUD nhân viên (`/NhanVien`), bàn ăn (`/BanAn`) và
danh mục món ăn (`/DanhMuc`). Các trang hỗ trợ tìm kiếm, lọc trạng thái, phân trang,
kiểm tra dữ liệu đầu vào, mã/tên trùng và xác nhận xóa. Bàn ăn có thêm bộ lọc khu vực.
Bản ghi đang được sử dụng không được xóa; có thể chuyển sang trạng thái ngừng sử dụng/nghỉ việc.
Thông tin đăng nhập liên kết với nhân viên được giữ nguyên khi sửa hồ sơ.

Chạy Web bằng `dotnet run --project RestaurantManagement.Web` sau khi cấu hình
`ConnectionStrings:DefaultConnection`. Web áp dụng migrations và chỉ nạp dữ liệu mẫu
khi database chưa có migration, để các bản ghi đã sửa/xóa không được tạo lại khi khởi động.
Không cần migration mới cho các chức năng CRUD này.

Kiểm thử tích hợp (chạy từ thư mục gốc repository):

```powershell
dotnet run --project tests/Management.SmokeTests/Management.SmokeTests.csproj
```

Bộ kiểm thử yêu cầu .NET 10 và SQL Server cục bộ hỗ trợ Windows Authentication.
Mặc định dùng `.\SQLEXPRESS`; có thể đặt biến môi trường `CRUD_TEST_SQL_SERVER` để đổi instance.
Tài khoản chạy cần quyền tạo/xóa database. Kiểm thử khởi động Web trên cổng tạm,
tạo database `RestaurantCrudTests_<GUID>` riêng và xóa sau khi chạy; không dùng database ứng dụng.
Các ca kiểm thử bao gồm CRUD qua HTTP, xác thực form, dữ liệu trùng, khóa ngoại,
xung đột phiên bản, bộ lọc/phân trang và giữ nguyên kết quả xóa sau khi khởi động lại.
