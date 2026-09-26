# Bàn giao thành viên 1 — phần đã làm và góp ý tuần tiếp theo

Nhật ký chung để nhóm/AI cập nhật sau mỗi phần hoàn thành: [NHAT_KY_Y_TUONG_VA_TIEN_DO.md](NHAT_KY_Y_TUONG_VA_TIEN_DO.md). File này ghi riêng phần Thành viên 1; không thay cho việc kiểm tra code thật.

Phạm vi đã làm: khách hàng tự đăng ký; đăng nhập/đăng xuất/đổi mật khẩu; khóa tạm sau 5 lần sai trong 15 phút; Admin cấp tài khoản, đổi vai trò và khóa/mở khóa cho nhân viên đã có hồ sơ. Đã bổ sung vai trò Thu ngân cùng trang xem/in hóa đơn và xác nhận nhận đủ tiền mặt. Trang nghiệp vụ hiện hữu được chặn quyền ở controller, không chỉ ẩn nút. Nhân viên ngừng làm hoặc khách hàng ngừng sử dụng bị đăng xuất ở yêu cầu tiếp theo.

**Trạng thái:** code trên nhánh `feature/auth-week6-member1`, chưa merge `master`. Chưa làm màn hình đặt bàn, gọi món, bếp, thanh toán online hay nghiệp vụ nhập/xuất kho hoàn chỉnh; **không được gọi các phần đó là đã xong**. Web dùng cookie Identity; API nghiệp vụ tương lai phải cấu hình xác thực và chặn quyền riêng.

## Cách chạy trên máy từng thành viên

1. Dùng cùng migration và chuỗi kết nối SQL Server của nhóm. Web hiện đọc `ConnectionStrings:DefaultConnection`.
2. Khởi tạo 7 vai trò và tài khoản Admin đầu tiên bằng biến môi trường **riêng trên máy** (PowerShell). Nếu máy đã chạy lệnh này trước đây, chạy lại để bổ sung `ThuNgan`:

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
| Thu ngân | Xem/in hóa đơn; xác nhận đã thu đủ tiền mặt trên bill hợp lệ | Thanh toán online sau khi có cổng thanh toán và xác nhận phía server |
| Bếp | Đăng nhập, đổi mật khẩu | Xem và cập nhật hàng đợi món |
| Kho | Xem/quản lý nguyên liệu theo CRUD hiện có | Nhập/xuất/tồn kho |
| Admin | CRUD nhân viên, danh mục, món, nguyên liệu, bàn; cấp/khóa/đổi vai trò nhân viên | Quản trị nghiệp vụ bổ sung |

`/NhanVien`, `/DanhMuc`, `/MonAn` và `/TaiKhoanNhanVien` chỉ cho Admin. `/BanAn` cho Admin/TiepTan/BoiBan xem, nhưng thêm/sửa/xóa bàn chỉ Admin. `/NguyenLieu` cho Admin/Kho. `/HoaDon` cho Admin/ThuNgan xem và in; POST xác nhận tiền mặt chỉ cho ThuNgan. Khách không vào các trang nhân viên. Các trang chưa có nghiệp vụ (đặt bàn, bếp, giao món) không được quảng cáo là đã xong.

Thu ngân đăng nhập sẽ vào `/HoaDon`. Danh sách chia hóa đơn đang chờ và đã thanh toán (100 bill gần nhất mỗi nhóm); trang chi tiết hiển thị món, size, số lượng, đơn giá, giảm giá, cọc và `TongThanhToan` do SQL Server tính. Nút in dùng trình duyệt. Xác nhận tiền mặt chỉ nhận bill `ChuaThanhToan`, tổng dương và khớp các dòng chưa hủy; dùng `RowVersion` để từ chối bill vừa bị sửa/thu bởi người khác. Hành động lưu `DaThanhToan`, `TienMat`, thời điểm thanh toán và `NhanVienId` của Thu ngân. Không tự xác nhận bill `ThanhToanMotPhan` vì chưa lưu số đã trả từng lần. Thanh toán online chưa tích hợp, không được đổi trạng thái chỉ vì khách bấm nút hoặc tải ảnh chuyển khoản.

## Tích hợp của thành viên 2 và 3

- Gắn `[Authorize(Roles = AppRoles.KhachHang + "," + AppRoles.TiepTan + "," + AppRoles.Admin)]` hoặc danh sách vai trò phù hợp **trên controller/action server**, không dựa vào việc ẩn nút. Thêm quyền khác nhau cho thao tác xem và cập nhật nếu cần.
- Kiểm tra quyền sở hữu theo dữ liệu, không chỉ theo vai trò: khách chỉ được xem/sửa đặt bàn của mình. Lấy tài khoản bằng `UserManager<TaiKhoan>.GetUserAsync(User)` rồi tra `KhachHang.TaiKhoanId`.
- Web hiện dùng cookie Identity. API hiện chưa có endpoint nghiệp vụ cho khách/nhân viên; khi thêm API, cần cấu hình xác thực cho API và gắn `[Authorize]` + kiểm tra sở hữu tương tự. Cookie/Web không tự bảo vệ một API mới nếu API được mở riêng.
- Dùng trạng thái dữ liệu hiện có; không tạo thêm hệ thống vai trò hay bảng tài khoản riêng.

Tài khoản thử không được hard-code vào repo. Mẫu cần chia sẻ riêng cho nhóm: `admin@example.test` (Admin do người chạy khởi tạo), một khách tự đăng ký, năm nhân viên tương ứng TiepTan/BoiBan/ThuNgan/Bep/Kho do Admin cấp. `DbSeeder` có hồ sơ Thu ngân mẫu `NV015` nhưng không tạo mật khẩu/tài khoản. `PopulateRestaurantData.sql` cũ có tài khoản `Cashier` minh họa bị khóa và không có mật khẩu; đó không phải tài khoản `ThuNgan` dùng đăng nhập. Nếu `NV004` đã gắn tài khoản cũ, dùng `NV015` hoặc tạo hồ sơ mới, không tự chỉnh mật khẩu bằng SQL. Mỗi máy có database local riêng nên tài khoản tạo trên máy này không tự xuất hiện khi thành viên khác pull Git.

Kiểm chứng: `dotnet build tests/Management.SmokeTests/Management.SmokeTests.csproj`; `dotnet run --project tests/Management.SmokeTests --no-build`. Lần chạy trên nhánh này đạt `PASS: 256 HTTP/database checks` (build 0 cảnh báo, 0 lỗi). Bộ smoke test tạo database tên ngẫu nhiên, kiểm tra HTTP và xóa database thử sau khi hoàn tất. Không chạy lên database thật.

## Góp ý mới của cô: đối chiếu code thật (chưa triển khai các mục dưới đây)

| Nội dung | Đã có | Cần chỉnh |
| --- | --- | --- |
| Phân quyền | Admin quản lý `/MonAn`; Bếp không thấy trang này. Thu ngân có `/HoaDon` và được chuyển vào đó khi đăng nhập. Các trang hiện hữu có chặn quyền server | Khi có KDS/mobile/tiếp tân, chuyển tiếp các vai trò còn lại đến màn hình riêng; KDS Bếp không có giá hay menu quản trị |
| Lọc món theo Danh mục | **Đã có** ở `MonAn/Index` | Kiểm tra sau khi tích hợp, không làm lại |
| Giá theo size | **Đã có** `MonAnSize.GiaBan` | Chỉ Admin sửa; không trả giá cho KDS |
| Lưu Món + Size + Định mức | Controller đã dùng transaction khi nhấn Lưu | Giữ giao dịch này; không cần LocalStorage/Session chỉ để giữ các dòng trước khi submit |
| Định mức nguyên liệu | `DinhMucMon` hiện khóa `(MonAnId, NguyenLieuId)` và có **một lượng cho mọi size** | **Sai trọng tâm góp ý:** chuyển định mức sang `MonAnSizeId`; form chọn tập nguyên liệu một lần, nhập lượng cho từng size |
| Combo | Đã chọn món lẻ + số lượng, có giá combo riêng | `ChiTietCombo` chưa lưu size món thành phần; thêm chọn size, không nhập lại nguyên liệu thô cho combo |
| Trạng thái món | Có `DangPhucVu`, `TamHet`, `NgungKinhDoanh` | Tạo mới phải cưỡng chế `DangPhucVu` ở server; hiện form/POST vẫn cho tạo `TamHet` hay `NgungKinhDoanh`. Ngừng kinh doanh cần lý do. Quy tắc hiện tại còn chặn `TamHet` khi có hóa đơn chưa thanh toán: phải sửa để chỉ chặn order mới, không hủy món đã gọi |
| “Món mới”, “Món nổi bật” | Chỉ là hai checkbox | Chốt tiêu chí rồi mới trình bày/triển khai, ví dụ “mới” trong 30 ngày từ ngày bắt đầu bán; “nổi bật” là đặc sản do Admin đánh dấu. Không gọi best-seller nếu chưa tính doanh số |
| Thứ tự món trong bếp | `ChiTietHoaDon` có món/size/số lượng/ghi chú/trạng thái | Chưa có thời điểm gọi **từng dòng**; cần bổ sung hoặc quy tắc thứ tự tương đương để gọi thêm vào cùng hóa đơn vẫn FIFO. KDS chỉ dùng `ChoCheBien -> DangCheBien -> SanSang`; Bồi bàn xác nhận `DaPhucVu` |

**Không tự sửa DB thật theo bảng này.** Các thay đổi `DinhMucMon`, `ChiTietCombo` và có thể `ChiTietHoaDon` cần model + migration EF chung, seed/test và cập nhật mô tả CSDL trong Word. Nếu DB đã có dữ liệu, công thức cũ chỉ có thể nhân sang các size làm giá trị **tạm để rà lại**; combo cũ không thể tự đoán size đúng. Sửa quan hệ trong các bảng đang có, không thêm bảng chỉ để tăng số lượng.

### Phân việc và thứ tự ghép code

1. **Thành viên 1 (CSDL, Auth):** thống nhất migration theo size trước để hai bạn không tự sửa schema khác nhau; bổ sung thông tin thời điểm gọi từng món nếu cần FIFO; cập nhật seed/test và mục 3.1 của Word. Khi thành viên 2 có route, chuyển trang sau login theo vai trò, kiểm tra truy cập sai quyền bằng URL trực tiếp và tài khoản thử. Không làm thay chức năng đặt bàn/KDS/thực đơn.
2. **Thành viên 2 (đặt bàn, phục vụ, bếp):** Tiếp tân tiếp nhận/xác nhận/xếp bàn. Bồi bàn trên điện thoại/tablet mở bàn, chọn món **và size**, nhập số lượng/ghi chú rồi gửi order. KDS Bếp chỉ liệt kê món đang chờ/đang chế biến theo thứ tự gọi, bàn, lượng, ghi chú; cập nhật chế biến ở server và báo món sẵn sàng cho Bồi bàn. **Không đưa `DonGia`, tổng tiền, danh sách toàn bộ thực đơn hoặc CRUD quản trị lên KDS.** Với khách đến trực tiếp, có thể tạo bản ghi đặt/nhận bàn nội bộ không bắt buộc `KhachHangId`, để hóa đơn/món vẫn truy được bàn.
3. **Thành viên 3 (thực đơn):** sau migration chung, sửa form Món ăn theo luồng Thông tin -> chọn nguyên liệu -> ma trận Size/Giá/Định lượng -> Lưu một lần. Combo chọn món lẻ + size + số lượng, đặt giá trọn gói; không tạo “công thức nguyên liệu thô” cho combo. Cưỡng chế trạng thái Create, thêm lý do ngừng và xử lý `TamHet` đúng; chốt tiêu chí món mới/nổi bật. Bộ lọc Danh mục và transaction đã có, không làm lại.

### Điểm cần nhớ khi mở rộng quyền/schema

- **Bếp nhập công thức hay Admin nhập?** Bếp biết nguyên liệu/định lượng, nhưng góp ý của cô ghi *chỉ Admin quản lý thực đơn, gồm định mức*. Để bám đúng lời cô: Bếp cung cấp công thức, Admin nhập và chịu trách nhiệm. Nếu muốn Bếp tự nhập, hỏi cô xác nhận rồi làm màn hình công thức riêng **không có giá/không có quyền sửa thực đơn**; tuyệt đối không mở toàn bộ `MonAnController` cho Bếp.
- **Thu ngân đã chốt:** vai trò thứ 7 là `ThuNgan`. Admin cấp cho nhân viên có hồ sơ và chỉ Thu ngân được xác nhận tiền mặt. Khi thành viên khác làm gọi món/thanh toán online, sử dụng chính `HoaDon` và các trạng thái hiện có; không tạo bill trùng hoặc endpoint tự báo thanh toán thành công từ phía khách.
