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
`PopulateIngredientStock.sql` tạo ba phiếu nhập minh họa cho 15 nguyên liệu, cập nhật tồn kho và đơn giá tham khảo theo đúng chứng từ; chạy lại không cộng tồn hai lần.
`SeedPosKdsPaymentDemo.sql` thêm hai hóa đơn đang mở để minh họa thanh toán thường và thanh toán có voucher; chạy sau các script trên, không tự chạy khi khởi động ứng dụng.
Script ghi tài khoản `admin.mau@example.test` và năm nhân viên NV004/NV005/NV006/NV008/NV012. Mỗi nhân viên có đúng một vai trò ứng dụng: `ThuNgan`, `TiepTan`, `BoiBan`, `Bep` hoặc `Kho`. Trên database mới, sáu tài khoản nội bộ được tạo ở trạng thái hoạt động với mật khẩu riêng đã băm bằng ASP.NET Identity; đăng nhập sau khi nạp SQL. Script giữ nguyên mật khẩu của tài khoản đã hoạt động; không tạo lại vị trí Thực đơn/Danh mục riêng. Với tài khoản mẫu bị khóa do SQL cũ, chỉ kích hoạt khi đúng hồ sơ/quyền và chưa có mật khẩu.
Bảng đăng nhập ngoài, token và claim không được chèn dữ liệu giả; chúng dành cho chức năng xác thực.

Để nạp các script theo đúng thứ tự bằng ứng dụng (môi trường Development), chạy:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=RestaurantDB;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet run --project RestaurantManagement.Web -- --import-sql-data
```

Lệnh có thể chạy lại; mỗi script chỉ bổ sung bản ghi còn thiếu. Sáu tài khoản nội bộ mẫu có thể đăng nhập ngay sau khi nạp; bộ demo đủ cả khách ở mục 4 của nhật ký. Hóa đơn POS/KDS tra cứu theo mã `HD-POSDEMO-01` (không voucher) và `HD-POSDEMO-02` (voucher `POSKDS-DEMO10`).

Nếu thích dùng SSMS: tạo `RestaurantDB`, chọn database đó và chạy lần lượt `InitialSchema.sql`, `PopulateCrudData.sql`, `PopulateRestaurantData.sql`, `PopulateIngredientStock.sql`, `SeedPosKdsPaymentDemo.sql`. Sau đó chạy Web bằng:

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
- **Chuyển khoản ngân hàng (VietQR)**: Hiển thị ảnh QR VietQR tĩnh của nhà hàng (MBBank 999917860000 — LA VAN NAM) kèm số tiền và nội dung chuyển khoản, lưu mã giao dịch ngân hàng.
- **Quẹt thẻ POS**: Lưu thông tin loại thẻ (Visa, MasterCard, JCB, Napas), 4 số cuối và mã chuẩn chi giao dịch.
- Tự động khấu trừ tiền cọc đặt bàn và áp dụng mã giảm giá voucher; in hóa đơn tức thì chuẩn định dạng in nhiệt K80 và khổ A5.

Xem **NHAT_KY_Y_TUONG_VA_TIEN_DO.md** để biết ma trận 7 vai trò (6 nội bộ + Khách), bộ demo đủ vai trò, các phần đã bổ sung và bàn giao chi tiết. Không còn hai role `ThucDon`/`DanhMucMon`: Admin quản lý danh mục/giá/combo/duyệt mở bán, Quản lý bếp chuẩn bị nội dung kỹ thuật trên màn hình riêng, không có quyền sửa giá. Món Bếp tạo/sửa phải được Admin duyệt lại trước khi public.
Nhân viên xem trang nhà hàng được nhưng không thấy nút đặt bàn của khách. Dọn role/tài khoản mẫu cũ trên database Development bằng `--remove-retired-menu-roles` theo hướng dẫn và điều kiện bảo toàn lịch sử trong nhật ký; không tự xóa lúc khởi động.

Kiểm chứng bản cập nhật ngày 03/10/2026: build **0 cảnh báo, 0 lỗi**, **PASS: 532 HTTP/database checks** trên database tạm đã dọn. Hệ thống đã hoàn thiện liên thông luồng nghiệp vụ trọn vẹn: Đặt bàn/Xếp bàn → Gọi món POS/Thêm món → KDS Bếp chế biến → Phục vụ bàn → Thanh toán đa phương thức & In bill → Kết thúc phục vụ & Dọn bàn sẵn sàng.

## Chương trình – voucher và khách hàng (25/10/2026)

- Admin mở `/KhuyenMai`, tạo một chương trình rồi thêm nhiều dòng mã ngay trong form. Mỗi mã có kiểu giảm %, giảm tiền trực tiếp, phạm vi hóa đơn/món, danh sách món, trần giảm, đơn tối thiểu và thời hạn riêng. Lưu chương trình và mã trong một transaction. Các đường dẫn `/Voucher` cũ chuyển về chương trình.
- Chương trình và mã có giới hạn vé phát hành; để trống là không giới hạn. Số vé gồm các lần lưu vào ví và hóa đơn nhập mã trực tiếp chưa có vé trong ví. Mã nhập trực tiếp đang giữ/đã thanh toán vẫn chiếm vé; gỡ mã hoặc hủy hóa đơn giải phóng vé trực tiếp. Vé đã lưu trong ví vẫn được tính phát hành. Mỗi mã còn có giới hạn lượt sử dụng riêng (cũng cho phép để trống); hóa đơn chưa thanh toán giữ một lượt. Một khách lưu một vé cho mỗi mã, vé đó dùng một lần, không dùng đồng thời trên hai hóa đơn.
- Toàn bộ form thời gian dùng **giờ Việt Nam UTC+7**, chính xác tới giây, bao gồm cả hai biên `Bắt đầu <= hiện tại <= Kết thúc`. Voucher phải nằm trong thời hạn chương trình; cả chương trình và mã đều phải hoạt động. Mặc định bắt đầu từ 00:00 hôm nay theo giờ Việt Nam để mã mới có thể dùng ngay. Khi cần kết thúc hết ngày, nhập 23:59:59 của ngày đó.
- Khách đăng nhập qua `/Account/Login`, mở `/KhachHang/Vouchers` để thu thập hoặc `/KhachHang/Vouchers?wallet=true` để xem ví. Trong `/KhachHang/Order/{id}`, chọn mã trong ví hoặc nhập mã trực tiếp. Khách chỉ được xem/thao tác hóa đơn của mình; thu ngân xác nhận thanh toán qua luồng hiện có. Trạng thái món tự kiểm tra mỗi 15 giây khi trang đơn đang hiển thị.
- Thu ngân mở `/HoaDon/Details/{id}` để chọn ví của khách hoặc nhập mã. Hóa đơn chưa gắn khách có thể gắn hồ sơ bằng số điện thoại trước khi chọn voucher; thao tác này chỉ áp dụng khi hóa đơn chưa thanh toán/chưa có voucher và không đổi khách đã gắn.
- `/TaiKhoanKhachHang` quản lý toàn bộ hồ sơ, kể cả khách chưa có tài khoản; có thêm/sửa hồ sơ, cấp tài khoản cho hồ sơ cũ mà không tạo trùng khách, lịch sử đơn, trạng thái chế biến/thanh toán, tổng chi tiêu và lịch sử tích điểm. Hồ sơ có tài khoản tiếp tục dùng màn sửa tài khoản có xác nhận mật khẩu.
- **Quy tắc điểm mặc định:** mỗi 10.000đ giá trị mua sau giảm giá được 1 điểm, làm tròn xuống; tính cả phần thanh toán bằng tiền cọc. Ghi điểm trong cùng transaction thanh toán và ràng buộc duy nhất theo hóa đơn. Không cộng lại hóa đơn cũ; chưa bổ sung đổi điểm hoặc hoàn điểm vì chưa có nghiệp vụ đổi/hoàn tiền trong phạm vi này.

Web tự áp dụng migration `20261025134614_CampaignVoucherWalletAndCustomerPoints` khi khởi động. Migration sao chép thời hạn, điều kiện và danh sách món của mã cũ từ chương trình xuống voucher; giữ nguyên hóa đơn, snapshot giảm giá và giới hạn lượt cũ. Các cột ưu đãi trên chương trình được giữ để tương thích chương trình tự động và script dữ liệu cũ. Ba bảng mới: `VoucherMon`, `VoucherKhachHang`, `LichSuTichDiem`. Nếu nâng cấp thủ công từ schema W9, dùng script chạy lại an toàn `RestaurantManagement.API/Scripts/UpgradeCampaignVoucherWalletAndCustomerPoints.sql`. Script chỉ nâng cấp schema có sẵn, không thay thế `InitialSchema.sql`.

Ứng dụng MAUI hiện dùng **WebView HTTPS** để chia sẻ luồng khách với Web. Mở ứng dụng, nhập địa chỉ gốc HTTPS của Web (thiết bị phải truy cập được và tin cậy chứng chỉ), bấm **Mở tài khoản khách hàng**. Đăng nhập cùng tài khoản website để thu thập mã, chọn mã, theo dõi đơn và xem điểm. Đây là giao diện Web trong app, chưa phải bộ API/mobile native riêng; không giả lập việc thanh toán thành công trên thiết bị.

Kiểm thử các luồng mới và hồi quy khuyến mãi:

```powershell
dotnet run --project tests/Management.SmokeTests -- --customer-promotions-only
dotnet run --project tests/Management.SmokeTests -- --promotions-only
dotnet run --project tests/Management.SmokeTests -- --orders-only
dotnet run --project tests/Management.SmokeTests -- --deposits-only
```

Các bộ test dùng database tạm tự tạo/xóa. Có thể đặt `CRUD_TEST_SQL_SERVER` theo instance cục bộ. Khi môi trường không cho MSBuild chạy song song, biên dịch với `-m:1 -p:BuildInParallel=false`.
