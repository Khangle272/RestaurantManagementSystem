# RestaurantManagementSystem

Hệ thống quản lý nhà hàng (C# .NET 10, ASP.NET Core MVC, SQL Server, .NET MAUI). Gồm:

| Project | Vai trò |
| --- | --- |
| `RestaurantManagement.Web` | Website khách + trang quản trị nội bộ, đồng thời chứa REST API `/api` cho mobile |
| `RestaurantManagement.API` | Tầng dữ liệu dùng chung: entity, `RestaurantDbContext`, migrations, script SQL, lệnh `--seed` |
| `RestaurantManagement.Shared` | DTO hợp đồng dữ liệu giữa API và app mobile |
| `RestaurantManagement.Mobile` | App MAUI native (Android/Windows) gọi `/api` bằng bearer token |
| `tests/Management.SmokeTests` | Kiểm thử tích hợp HTTP + database trên database tạm |

Tài liệu khác:

- [NHAT_KY_Y_TUONG_VA_TIEN_DO.md](NHAT_KY_Y_TUONG_VA_TIEN_DO.md): ma trận quyền, quyết định nghiệp vụ, tiến độ và bàn giao. Cập nhật file này sau mỗi lượt làm việc.
- [DESIGN.md](DESIGN.md): thiết kế giao diện và luồng màn hình.
- [API_MOBILE.md](API_MOBILE.md): kiến trúc REST API, danh sách endpoint, phân quyền và cách chạy app mobile.
- [DEMO_WEBSITE.md](DEMO_WEBSITE.md), [KICH_BAN_TEST_DEMO.md](KICH_BAN_TEST_DEMO.md): bộ dữ liệu và kịch bản trình bày.
- [HUONG_DAN_CLOUDFLARE.md](HUONG_DAN_CLOUDFLARE.md): chia sẻ demo qua Cloudflare Quick Tunnel.
- [RestaurantManagement.API/README.md](RestaurantManagement.API/README.md): database, migration và dữ liệu minh họa.

## Chạy website

Yêu cầu .NET 10 SDK và SQL Server (mặc định `.\SQLEXPRESS`, Windows Authentication). Đặt chuỗi kết nối `ConnectionStrings:DefaultConnection` theo máy của mình rồi chạy:

```powershell
dotnet run --project RestaurantManagement.Web --launch-profile https
```

Khi khởi động, Web tự áp dụng migrations và tạo vai trò; **không tự nạp dữ liệu mẫu**. Sao lưu database có dữ liệu quan trọng trước khi nâng cấp.

- Trang nhà hàng cho khách: `https://localhost:7167/`, khách đăng nhập tại `/Account/Login`.
- Cổng nội bộ cho nhân viên/Admin: `https://localhost:7167/admin`.
- Profile `http` dùng `http://localhost:5255`; cookie đăng nhập là Secure nên demo nên dùng HTTPS.

## Nạp dữ liệu (chỉ Development)

Cách nhanh nhất là bộ demo toàn website. Lệnh nâng schema, nạp các script SQL còn thiếu, tạo tài khoản đủ 7 vai trò và giao dịch mẫu (hóa đơn 28 ngày, ví voucher, điểm, lịch cọc, bàn đang phục vụ, KDS, chứng từ kho). Có thể chạy lại mà không tạo trùng:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AuthBootstrap__DemoPassword = 'Demo@2026!'
dotnet run --project RestaurantManagement.Web --no-launch-profile -- --init-website-demo
```

Tài khoản `.demo@example.test` dùng mật khẩu `Demo@2026!`; danh sách và kịch bản xem tại [DEMO_WEBSITE.md](DEMO_WEBSITE.md).

Các lệnh Development khác của Web:

| Tham số | Tác dụng |
| --- | --- |
| `--import-sql-data` | Chạy lần lượt các script trong `RestaurantManagement.API/Scripts` (nhân viên, bàn, thực đơn, khách, hóa đơn, kho, khuyến mãi) và tạo sáu tài khoản nội bộ mẫu |
| `--init-demo-accounts` | Chỉ tạo tài khoản demo đủ vai trò |
| `--init-week10-demo` | Khu vực và hai bàn riêng kèm lịch đặt bàn có món đặt trước theo ca (W10) |
| `--init-auth` | Chỉ đồng bộ schema và vai trò |
| `--remove-retired-menu-roles` | Dọn vai trò/tài khoản mẫu cũ đã ngừng dùng, có kiểm tra bảo toàn lịch sử |

Sau `--import-sql-data`, sáu tài khoản nội bộ mẫu dùng mật khẩu riêng (chỉ cho dữ liệu Development; chạy lại script không đặt lại mật khẩu đã đổi):

| Chức vụ | Email | Mật khẩu mẫu |
| --- | --- | --- |
| Quản trị | `admin.mau@example.test` | `AdminSen2026!` |
| Tiếp tân | `letan.minhhoa@nhahang.example.test` | `TiepTanHoa2026!` |
| Bồi bàn | `phucvu.minhhoa@nhahang.example.test` | `BoiBanNang2026!` |
| Thu ngân | `thungan.minhhoa@nhahang.example.test` | `ThuNganCafe2026!` |
| Quản lý bếp | `bep.minhhoa@nhahang.example.test` | `BepCom2026!` |
| Kho | `kho.minhhoa@nhahang.example.test` | `KhoGao2026!` |

Dữ liệu là thông tin tự soạn để thực hành; email dùng miền `.test`. Khi chạy script bằng `sqlcmd`, thêm `-f 65001 -I` để giữ đúng tiếng Việt. Không chạy `RestaurantManagement.API -- --seed` hay các lệnh trên với database dữ liệu thật.

## Chức năng chính

- **Tài khoản và phân quyền:** 6 vai trò nội bộ (Admin, Tiếp tân, Bồi bàn, Thu ngân, Quản lý bếp, Kho) và Khách hàng; mỗi tài khoản đúng một vai trò. `/Account` là trang Thông tin cá nhân; Admin cấp tài khoản nhân viên trong hồ sơ `/NhanVien` và quản lý khách tại `/TaiKhoanKhachHang`. Menu nội bộ chỉ hiện chức năng đúng vai trò; quyền vẫn được kiểm tra ở server.
- **Thực đơn:** món nhiều size, định mức nguyên liệu, combo, ảnh PNG/JPEG/WebP tối đa 5 MB. Bếp soạn nội dung kỹ thuật, Admin duyệt và đặt giá trước khi mở bán.
- **Bàn và đặt bàn:** sơ đồ bàn `/SoDoBan` theo tầng/khu vực, gợi ý bàn theo lịch/sức chứa/VIP, tiếp nhận yêu cầu tại `/QuanLyDatBan`, đặt cọc và đặt món trước. Thanh toán không tự trả bàn: bồi bàn bấm **Kết thúc phục vụ** rồi **Dọn xong**.
- **Gọi món và bếp:** POS `/DonHang` cho đơn tại bàn, mang đi, giao hàng; gọi thêm món; KDS `/Bep` chế biến theo thứ tự vào trước (`ChoCheBien` → `DangCheBien` → `SanSang`); bồi bàn xác nhận mang ra (`DaPhucVu`) hoặc hủy món chưa nấu.
- **Thanh toán** tại `/HoaDon/Details/{id}`: tiền mặt (tính tiền thối) hoặc chuyển khoản (hiện thông tin tài khoản, số tiền, nội dung và lưu mã giao dịch). Tự trừ tiền cọc, áp dụng khuyến mãi/voucher, in hóa đơn K80/A5.
- **Khuyến mãi và khách hàng thân thiết:** chương trình nhiều mã tại `/KhuyenMai` (giảm %/tiền, phạm vi hóa đơn/món, trần giảm, giới hạn phát hành/lượt dùng); khách thu thập voucher vào ví hoặc đổi điểm tại `/KhachHang/Vouchers`. Cứ 10.000đ sau giảm được 1 điểm. Giờ hiệu lực theo UTC+7, chính xác tới giây.
- **Kho:** nhà cung cấp, nhập/xuất/thanh lý; tồn kho = tổng nhập − tổng xuất đã ghi sổ.
- **Báo cáo:** doanh thu, món bán chạy, tồn kho, hiệu quả ưu đãi, thống kê đặt bàn.
- **App mobile:** nhân viên xem sơ đồ bàn, gọi món, phục vụ, thanh toán, tiếp nhận đặt bàn; khách theo dõi đơn, lịch sử và điểm. Thu thập/đổi voucher vẫn làm trên website. Chi tiết: [API_MOBILE.md](API_MOBILE.md).

Bản ghi đang được dùng không bị xóa cứng; chuyển sang ngừng sử dụng/nghỉ việc. Quy tắc nghiệp vụ chi tiết và lý do từng quyết định nằm trong nhật ký.

## Kiểm thử

```powershell
dotnet run --project tests/Management.SmokeTests/Management.SmokeTests.csproj
```

Bộ kiểm thử khởi động Web trên cổng tạm với database riêng `RestaurantCrudTests_<GUID>` và xóa database đó sau khi chạy; không đụng database ứng dụng. Tài khoản Windows cần quyền tạo/xóa database; đổi instance bằng biến `CRUD_TEST_SQL_SERVER`. Chạy riêng một nhóm:

```powershell
dotnet run --project tests/Management.SmokeTests -- --orders-only
dotnet run --project tests/Management.SmokeTests -- --deposits-only
dotnet run --project tests/Management.SmokeTests -- --promotions-only
dotnet run --project tests/Management.SmokeTests -- --customer-promotions-only
dotnet run --project tests/Management.SmokeTests -- --website-demo-only
dotnet run --project tests/Management.SmokeTests -- --mobile-api-only
```

Nếu môi trường không cho MSBuild chạy song song, biên dịch với `-m:1 -p:BuildInParallel=false`. Lần kiểm chứng gần nhất (06/11/2026): **PASS 1170 HTTP/database checks**.

## Chia sẻ demo

Chạy Web bằng profile `https`, rồi mở Cloudflare Quick Tunnel trỏ tới `https://localhost:7167` theo [HUONG_DAN_CLOUDFLARE.md](HUONG_DAN_CLOUDFLARE.md). Database và ảnh vẫn nằm trên máy chạy demo; mỗi lần mở tunnel có URL mới.
