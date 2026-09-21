# RestaurantManagementSystem

Giao diện quản trị Web có CRUD nhân viên (`/NhanVien`), bàn ăn (`/BanAn`) và
danh mục món ăn (`/DanhMuc`). Các trang hỗ trợ tìm kiếm, lọc trạng thái, phân trang,
kiểm tra dữ liệu đầu vào, mã/tên trùng và xác nhận xóa. Bàn ăn có thêm bộ lọc khu vực.
Bản ghi đang được sử dụng không được xóa; có thể chuyển sang trạng thái ngừng sử dụng/nghỉ việc.
Thông tin đăng nhập liên kết với nhân viên được giữ nguyên khi sửa hồ sơ.

Chạy Web bằng `dotnet run --project RestaurantManagement.Web` sau khi cấu hình
`ConnectionStrings:DefaultConnection`. Web chỉ áp dụng migrations, không tự nạp dữ liệu mẫu,
kể cả khi khởi tạo database mới. Các trang CRUD đọc và ghi trực tiếp vào SQL Server.
Dữ liệu được nhập qua giao diện hoặc bằng script SQL chủ động.
Dữ liệu mẫu đã nạp trước đây vẫn còn trong database; thay đổi này không tự xóa dữ liệu đang có.
Không chạy lệnh API `--seed` trên database dùng dữ liệu thật vì lệnh đó nạp dữ liệu minh họa.
Không cần migration mới cho các chức năng CRUD này.

Bộ dữ liệu cho ba CRUD nằm tại `RestaurantManagement.API/Scripts/PopulateCrudData.sql`.
Đây là thông tin tự soạn để thực hành, gồm 12 nhân viên, 18 bàn và 10 danh mục,
kèm khu vực cần thiết. Các tên/thông tin liên hệ không đại diện cho nhân sự thực tế;
email dùng miền `.test`. Dữ liệu được lưu trong SQL Server và có thể sửa/xóa qua giao diện.
Để nạp trên máy khác, mở script trong SSMS, chọn database ứng dụng đã có schema rồi Execute.
Script chỉ thêm mã/tên chưa có, không sửa/xóa bản ghi cũ và không tự chạy khi mở ứng dụng.
Trên máy hiện tại, Web kết nối `.\SQLEXPRESS`, database `RestaurantDB`.

Để bổ sung dữ liệu liên kết cho các bảng nghiệp vụ còn lại, chạy
`RestaurantManagement.API/Scripts/PopulateRestaurantData.sql` sau script trên.
Bộ dữ liệu tự soạn bao gồm thực đơn/size/công thức/combo, khách hàng, đặt bàn,
món đặt trước, hóa đơn/chi tiết, nhà cung cấp, nhập/xuất kho, khuyến mãi/voucher và đánh giá.
Script dùng transaction, kiểm tra tổng hóa đơn và lượng xuất theo lô; chạy lại không tạo trùng.
Các tài khoản minh họa liên kết nhân viên không có mật khẩu và bị khóa.
Bảng đăng nhập ngoài, token và claim không được chèn dữ liệu giả; chúng dành cho chức năng xác thực.

Trang món ăn có thêm/sửa/xóa, giá theo nhiều size, định mức, thành phần combo và tải ảnh
PNG/JPEG/WebP tối đa 5 MB. Món/size có lịch sử liên quan được bảo vệ khi xóa.
Trang nguyên liệu lấy tồn kho từ tổng nhập trừ tổng xuất đã ghi sổ; không cho đổi đơn vị
khi nguyên liệu đã có công thức hoặc chứng từ.
Thống kê hai trang tính trên toàn bộ dữ liệu, bộ lọc lấy từ database.
Các nút chưa có chức năng, chi nhánh/tài khoản/thông báo giả đã được gỡ khỏi giao diện chung.

Kiểm thử tích hợp (chạy từ thư mục gốc repository):

```powershell
dotnet run --project tests/Management.SmokeTests/Management.SmokeTests.csproj
```

Bộ kiểm thử yêu cầu .NET 10 và SQL Server cục bộ hỗ trợ Windows Authentication.
Mặc định dùng `.\SQLEXPRESS`; có thể đặt biến môi trường `CRUD_TEST_SQL_SERVER` để đổi instance.
Tài khoản chạy cần quyền tạo/xóa database. Kiểm thử khởi động Web trên cổng tạm,
tạo database `RestaurantCrudTests_<GUID>` riêng và xóa sau khi chạy; không dùng database ứng dụng.
Dữ liệu mẫu được bộ kiểm thử nạp riêng sau khi xác nhận Web khởi động với database trống.
Các ca kiểm thử bao gồm CRUD qua HTTP, xác thực form, dữ liệu trùng, khóa ngoại,
xung đột phiên bản, bộ lọc/phân trang và giữ nguyên kết quả xóa sau khi khởi động lại.
