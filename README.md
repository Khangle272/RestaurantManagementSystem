# RestaurantManagementSystem

Đọc [nhật ký ý tưởng và tiến độ](NHAT_KY_Y_TUONG_VA_TIEN_DO.md) để biết cách chạy tài khoản thử, quyền hiện có, quyết định đã làm và phần còn dự kiến. Sau mỗi lượt hoàn thành, cập nhật cùng file này để nhóm chỉ cần theo dõi một nơi.

Giao diện quản trị Web có CRUD nhân viên (`/NhanVien`), bàn ăn (`/BanAn`) và
danh mục món ăn (`/DanhMuc`). Các trang hỗ trợ tìm kiếm, lọc trạng thái, phân trang,
kiểm tra dữ liệu đầu vào, mã/tên trùng và xác nhận xóa. Bàn ăn có thêm bộ lọc khu vực.
Bản ghi đang được sử dụng không được xóa; có thể chuyển sang trạng thái ngừng sử dụng/nghỉ việc.
Thông tin đăng nhập liên kết với nhân viên được giữ nguyên khi sửa hồ sơ.

Chạy Web bằng `dotnet run --project RestaurantManagement.Web` sau khi cấu hình
`ConnectionStrings:DefaultConnection`. Web áp dụng migrations và khởi tạo vai trò, không tự nạp dữ liệu mẫu,
kể cả khi khởi tạo database mới. Các trang CRUD đọc và ghi trực tiếp vào SQL Server.
Dữ liệu được nhập qua giao diện hoặc bằng script SQL chủ động.
`RestaurantDB` là tên database minh họa; mỗi máy tự tạo/nạp dữ liệu theo hướng dẫn bên dưới.
Không chạy lệnh API `--seed` trên database dùng dữ liệu thật vì lệnh đó nạp dữ liệu minh họa.
Bản cập nhật người 1 có migrations `TableFloorAndReservationLifecycle` và `MenuApprovalAndKitchenOwnership`; Web tự áp dụng khi khởi động.
Chúng bổ sung tầng/khu vực ưu tiên/thời điểm kết thúc lượt bàn và cột duyệt món/lý do ngừng kinh doanh, không tạo lại hay xóa database.
Sao lưu database có dữ liệu quan trọng trước khi nâng cấp; không chạy rollback xóa cột mới tùy tiện.

Bộ dữ liệu cho ba CRUD nằm tại `RestaurantManagement.API/Scripts/PopulateCrudData.sql`.
Đây là thông tin tự soạn để thực hành, gồm 12 nhân viên, 18 bàn và 10 danh mục,
kèm khu vực cần thiết. Các tên/thông tin liên hệ không đại diện cho nhân sự thực tế;
email dùng miền `.test`. Dữ liệu được lưu trong SQL Server và có thể sửa/xóa qua giao diện.
Để nạp trên máy khác, mở script trong SSMS, chọn database ứng dụng đã có schema rồi Execute.
Script chỉ thêm mã/tên chưa có, không sửa/xóa bản ghi cũ và không tự chạy khi mở ứng dụng.
Connection string mẫu dùng `.\SQLEXPRESS`; sửa tên instance/database cho đúng máy chạy, không mặc nhiên dùng cấu hình máy của người khác.

Để bổ sung dữ liệu liên kết cho các bảng nghiệp vụ còn lại, chạy
`RestaurantManagement.API/Scripts/PopulateRestaurantData.sql` sau script trên.
Bộ dữ liệu tự soạn bao gồm thực đơn/size/công thức/combo, khách hàng, đặt bàn,
món đặt trước, hóa đơn/chi tiết, nhà cung cấp, nhập/xuất kho, khuyến mãi/voucher và đánh giá.
Script dùng transaction, kiểm tra tổng hóa đơn và lượng xuất theo lô; chạy lại không tạo trùng.
Script ghi tài khoản `admin.mau@example.test` và năm nhân viên NV004/NV005/NV006/NV008/NV012. Mỗi nhân viên có đúng một vai trò ứng dụng: `ThuNgan`, `TiepTan`, `BoiBan`, `Bep` hoặc `Kho`. Trên database mới, sáu tài khoản nội bộ được tạo ở trạng thái hoạt động với mật khẩu riêng đã băm bằng ASP.NET Identity; đăng nhập sau khi nạp SQL. Script giữ nguyên mật khẩu của tài khoản đã hoạt động; không tạo lại vị trí Thực đơn/Danh mục riêng. Với tài khoản mẫu bị khóa do SQL cũ, chỉ kích hoạt khi đúng hồ sơ/quyền và chưa có mật khẩu.
Bảng đăng nhập ngoài, token và claim không được chèn dữ liệu giả; chúng dành cho chức năng xác thực.

Để nạp cả hai script theo đúng thứ tự bằng ứng dụng (môi trường Development), chạy:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=RestaurantDB;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet run --project RestaurantManagement.Web -- --import-sql-data
```

Lệnh có thể chạy lại; mỗi script chỉ bổ sung bản ghi còn thiếu. Sáu tài khoản nội bộ mẫu có thể đăng nhập ngay sau khi nạp; bộ demo đủ cả khách ở mục 4 của nhật ký.

Nếu thích dùng SSMS: tạo `RestaurantDB`, chọn database đó và chạy lần lượt `InitialSchema.sql`, `PopulateCrudData.sql`, `PopulateRestaurantData.sql`. Sau đó chạy Web bằng:

```powershell
dotnet run --project RestaurantManagement.Web --launch-profile http
```

Trang chủ ở `http://localhost:5255/`, cổng quản trị/nhân viên ở `http://localhost:5255/admin`. Sáu tài khoản nội bộ mẫu sử dụng các mật khẩu riêng sau đây chỉ cho dữ liệu Development:

| Chức vụ | Email | Mật khẩu mẫu |
| --- | --- | --- |
| Quản trị | `admin.mau@example.test` | `AdminSen2026!` |
| Tiếp tân | `letan.minhhoa@nhahang.example.test` | `TiepTanHoa2026!` |
| Bồi bàn | `phucvu.minhhoa@nhahang.example.test` | `BoiBanNang2026!` |
| Thu ngân | `thungan.minhhoa@nhahang.example.test` | `ThuNganCafe2026!` |
| Quản lý bếp | `bep.minhhoa@nhahang.example.test` | `BepCom2026!` |
| Kho | `kho.minhhoa@nhahang.example.test` | `KhoGao2026!` |

Mật khẩu trong bảng dành cho tài khoản mẫu mới nạp. Nếu đã đổi mật khẩu thì dùng mật khẩu mới; chạy lại SQL không đặt lại mật khẩu của tài khoản đang hoạt động.
Người dùng mở `/Account` (**Thông tin cá nhân**) để xem tài khoản, sửa thông tin nếu được phép và đổi mật khẩu. Trang chủ nội bộ ở `/admin/TrangChu`; menu nhóm chỉ hiện chức năng đúng vai trò, còn `/` vẫn là web nhà hàng cho khách.

Nếu dùng `sqlcmd` thay SSMS để đọc các file SQL UTF-8, thêm **`-f 65001 -I`**; thiếu bảng mã có thể lưu sai tiếng Việt vào database. Lệnh nạp qua ứng dụng đọc file UTF-8.

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

Admin quản lý hồ sơ và tài khoản đăng nhập chung tại `/NhanVien`: tạo nhân viên, chọn vai trò công việc rồi cấp tài khoản trong hồ sơ; không chọn thêm một bộ quyền riêng. `/TaiKhoanNhanVien` cũ chuyển về màn hình này.
Quản lý tài khoản khách tại `/TaiKhoanKhachHang`. Nhân viên không tự sửa hồ sơ/quyền; khách và Admin được sửa thông tin trong phạm vi cho phép, với mật khẩu xác nhận.

Sơ đồ bàn ở `/SoDoBan`; Tiếp tân tạo yêu cầu đặt bàn cho khách tại `/QuanLyDatBan/Create` và xếp vào bàn có sẵn. Chỉ Admin cấu hình bàn/khu vực vật lý.
Sơ đồ có danh sách yêu cầu bên cạnh và gợi ý bàn phù hợp theo lịch/sức chứa/khu vực/VIP. Bấm mã bàn để tạo yêu cầu điền sẵn bàn; gợi ý và chọn bàn chưa giữ chỗ, vẫn cần tiếp tân xác nhận để server kiểm tra lại.
Thanh toán không tự trả bàn: bồi bàn bấm **Kết thúc phục vụ** sau khi xử lý xong và đủ điều kiện thanh toán, sau đó bấm **Dọn xong** để bàn sẵn sàng.

Xem **NHAT_KY_Y_TUONG_VA_TIEN_DO.md** để biết ma trận 7 vai trò (6 nội bộ + Khách), bộ demo đủ vai trò, các phần đã bổ sung và bàn giao cần tiếp tục. Không còn hai role `ThucDon`/`DanhMucMon`: Admin quản lý danh mục/giá/combo/duyệt mở bán, Quản lý bếp chuẩn bị nội dung kỹ thuật trên màn hình riêng, không có quyền sửa giá. Món Bếp tạo/sửa phải được Admin duyệt lại trước khi public.
Nhân viên xem trang nhà hàng được nhưng không thấy nút đặt bàn của khách. Dọn role/tài khoản mẫu cũ trên database Development bằng `--remove-retired-menu-roles` theo hướng dẫn và điều kiện bảo toàn lịch sử trong nhật ký; không tự xóa lúc khởi động.

Kiểm chứng bản cập nhật ngày 02/10/2026: build **0 cảnh báo, 0 lỗi**, **PASS: 481 HTTP/database checks** trên database tạm đã dọn. Xem mục 5–7 của nhật ký để biết phần đã có và điểm còn thiếu; hiện chưa có UI xác nhận món đã mang ra (`SanSang` → `DaPhucVu`), nên TV2 cần nối bước này trước khi demo trọn luồng kết thúc bàn có món. Không coi build/test đạt là toàn bộ đồ án đã hoàn thành.
