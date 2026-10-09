# RestaurantManagementSystem

## Bổ sung tuần 8 — nhánh member1-week8-update

Khách chọn món từ thực đơn có ảnh, chọn size/số lượng/ghi chú vào giỏ rồi đặt bàn; hoặc mở lịch đã đặt để bổ sung món. Món chọn trước chỉ gửi bếp khi nhận khách hoặc nhân viên xác nhận chuẩn bị sớm sau khi đủ cọc thỏa thuận. Chi tiết lịch hiển thị món, trạng thái và lịch sử cọc; khách yêu cầu hủy để nhà hàng đối chiếu.

Tiếp tân chọn ngày/giờ trên sơ đồ và xem khoảng bắt đầu–kết thúc từng bàn. POS chọn bàn rồi mở rộng vùng chọn món ngay tại trang; sidebar desktop có thể ẩn/hiện. Form dùng AJAX giữ dữ liệu khi lỗi.

Lịch online mới tự tính cọc: **50% tiền món đặt trước**, hoặc **299.000đ/lịch nếu chưa chọn món**, học cách tính từ chính sách công khai của CoCo Saigon/Nhà Bè Khánh Hào (nguồn trong nhật ký). Kiểm tra còn bàn rồi giữ tạm **1 tiếng** để báo thanh toán. Quá hạn chưa báo chuyển/nhận cọc thì worker tự chuyển **Đã hủy**, giữ lịch sử và nhường chỗ, không để tồn trong danh sách chờ. Lịch đã báo chuyển đúng hạn được giữ tạm chờ thu ngân đối chiếu, không tự hủy nhầm tiền; chưa chốt cho tới khi xác nhận nhận cọc. Mốc đã lưu của lịch cũ không tự reset. Mã lịch mới `DB-XXXXXX`, có kiểm tra trùng; mã/lịch/giao dịch cũ không đổi.

Ở Development, QR chỉ tượng trưng và ghi rõ không chuyển tiền thật. Khách bấm **Tôi đã chuyển khoản** chỉ báo chờ đối chiếu. Thu ngân đăng nhập `/admin`, vào **Vận hành → Đối chiếu cọc đặt bàn**, kiểm tra tiền thực nhận/nội dung mã lịch, tích Đã đối chiếu rồi xác nhận; không phải nhập mã giao dịch ngân hàng. Vẫn lưu người xử lý, số tiền, thời điểm và chống gửi lặp. Đủ cọc mới chốt lịch; có thể từ chối thông báo không khớp để khách kiểm tra lại. Cọc bổ sung trước nhận bàn chỉ tính phần thiếu; sau nhận bàn không thu cọc mới. Chưa kết nối ngân hàng thật/callback tự xác nhận.

Lịch mới dự kiến tối đa 3 tiếng. Bồi bàn vẫn kết thúc ngay khi khách xong; nếu quên, worker mỗi phút tự kết thúc lượt quá 3 tiếng từ lúc nhận bàn, chỉ khi đã thanh toán và xử lý xong món/cọc. Bàn chuyển **Cần dọn**, chỉ bấm **Dọn xong** mới **Sẵn sàng**; lượt còn nợ/món/cọc phải xử lý thì cảnh báo, không tự thanh toán hoặc bỏ qua.

Kiểm chứng cập nhật 09/10: build 0 lỗi/cảnh báo, **787 kiểm tra HTTP/SQL đạt** trên database tạm đã dọn; giỏ/POS/AJAX/trạng thái cọc đạt. Có ca tự hủy sau hạn, bảo vệ lịch đang đối chiếu, không nhân đôi tiền và không hủy cọc thủ công. Worker thật đã kết thúc ba lượt demo cũ đủ điều kiện, không xóa lịch sử. Hai migrations `AutomaticReservationDeposits` và `PaymentNoticeReconciliation` thêm trường, giữ dữ liệu/thỏa thuận cũ; thay đổi thời hạn/tự hủy/bỏ ô mã ngân hàng không thêm migration.

Tạo demo ba bàn phục vụ đồng thời (chỉ Development; dùng database thử và đúng instance máy mình):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=RestaurantWeek8Demo;Trusted_Connection=True;TrustServerCertificate=True;'
$env:AuthBootstrap__DemoPassword = 'Demo@2026!'
dotnet run --project RestaurantManagement.Web -- --init-week8-demo
dotnet run --project RestaurantManagement.Web --launch-profile http
```

Lệnh tạo tài khoản demo rõ ràng; chạy web thường không tạo lại mật khẩu. Đăng nhập khách `khach.demo@example.test` tại `/Account/Login`, nội bộ `/admin` với các tài khoản ở nhật ký. Migration bổ sung schema khi khởi động; sao lưu database nhóm trước khi nâng. Định mức mỗi size, size của món trong combo và tự xuất nguyên liệu là phần Người 2 cần nối tiếp; khuyến mãi/báo cáo chuyên sâu thuộc Người 3. Đọc nhật ký cùng nhánh trước khi sửa.

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

Màn hình POS gọi món và quản lý đơn hàng tại `/DonHang`:
- Tạo đơn gọi món tại bàn đang phục vụ, đơn mang đi hoặc giao hàng tận nơi (`/DonHang/Create`) kèm giỏ hàng tức thì, phân loại danh mục, tìm kiếm món, chọn kích cỡ món và ghi chú riêng từng món.
- Gọi thêm món vào đơn đang mở (`/DonHang/AddDishes`), lưu snapshot tên size và thời điểm gọi để bếp chế biến theo thứ tự vào trước ra trước (FIFO).
- Bếp theo dõi và chế biến món tại màn hình KDS `/Bep` (`ChoCheBien` → `DangCheBien` → `SanSang`).
- Bồi bàn / Tiếp tân / Admin xác nhận món đã mang ra bàn (`SanSang` → `DaPhucVu`) tại trang chi tiết `/DonHang/Details/{id}`, giải phóng hoàn toàn điều kiện kết thúc phục vụ bàn. Hỗ trợ hủy món chưa nấu (`ChoCheBien` → `DaHuy`) và tự động khấu trừ tiền hóa đơn.

Thanh toán đa hình thức tại `/HoaDon/Details/{id}`:
- **Tiền mặt**: Nhập số tiền khách đưa, tự động tính tiền thối lại và kiểm tra đủ tiền thanh toán.
- **Chuyển khoản ngân hàng (VietQR)**: Sinh mã QR động chuẩn Napas/VietQR (MBBank 999988889999) kèm nội dung chuyển khoản tự động và lưu mã giao dịch ngân hàng.
- **Quẹt thẻ POS**: Lưu thông tin loại thẻ (Visa, MasterCard, JCB, Napas), 4 số cuối và mã chuẩn chi giao dịch.
- Đối trừ cọc còn khả dụng và giảm giá được thu ngân xác nhận; không dùng lại cọc đã đối trừ/hoàn/giữ. Voucher tự áp dụng theo chương trình là phần Người 3 cần hoàn thiện. Chuyển khoản/thẻ cần người thu ngân đối chiếu thực tế; giao diện in hiện có không thay thế xác nhận của ngân hàng.

Xem **NHAT_KY_Y_TUONG_VA_TIEN_DO.md** để biết ma trận 7 vai trò (6 nội bộ + Khách), bộ demo đủ vai trò, các phần đã bổ sung và bàn giao chi tiết. Không còn hai role `ThucDon`/`DanhMucMon`: Admin quản lý danh mục/giá/combo/duyệt mở bán, Quản lý bếp chuẩn bị nội dung kỹ thuật trên màn hình riêng, không có quyền sửa giá. Món Bếp tạo/sửa phải được Admin duyệt lại trước khi public.
Nhân viên xem trang nhà hàng được nhưng không thấy nút đặt bàn của khách. Dọn role/tài khoản mẫu cũ trên database Development bằng `--remove-retired-menu-roles` theo hướng dẫn và điều kiện bảo toàn lịch sử trong nhật ký; không tự xóa lúc khởi động.

Kiểm chứng bản cập nhật ngày 03/10/2026: build **0 cảnh báo, 0 lỗi**, **PASS: 532 HTTP/database checks** trên database tạm đã dọn. Hệ thống đã hoàn thiện liên thông luồng nghiệp vụ trọn vẹn: Đặt bàn/Xếp bàn → Gọi món POS/Thêm món → KDS Bếp chế biến → Phục vụ bàn → Thanh toán đa phương thức & In bill → Kết thúc phục vụ & Dọn bàn sẵn sàng.
