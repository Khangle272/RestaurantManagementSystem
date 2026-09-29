# Góp ý của cô và nhật ký quyết định khi làm đồ án (từ 27/09/2026)

Giữ file này làm nơi ghi **góp ý → quyết định → phần đã làm → phần còn chờ**, để nhóm và AI khác đọc lại không nhầm ý tưởng với chức năng đã chạy. Khi tích hợp, đối chiếu với code và nhánh mới nhất; ghi chú này không tự tạo/sửa database. Các thành viên cần thống nhất thay đổi CSDL với người phụ trách dữ liệu trước khi code form.

**Cách dùng cho các lần làm sau (cả thành viên và AI):** trước khi sửa hãy đọc mục liên quan rồi đối chiếu code/nhánh hiện tại. Sau mỗi phần đã làm xong, thêm một mục có ngày, người/nhánh phụ trách, file hoặc chức năng đã đổi, kết quả build/test, và việc còn chờ người khác. Ghi rõ `Đã làm`, `Đang làm` hoặc `Dự kiến`; không đổi một ý tưởng thành “đã hoàn thành” chỉ vì nó đã có bảng CSDL. Giữ các quyết định cũ để nhìn lại vì sao nhóm đổi hướng; nếu quyết định thay đổi, ghi mục mới thay thế và lý do. Không ghi mật khẩu thật hoặc dữ liệu khách hàng thật vào file.

**Quy trình đang dùng:** xem README để tạo database và tài khoản mẫu từ SQL. Các lệnh khởi tạo nhân viên/mật khẩu mẫu ghi ở các mục cũ bên dưới là lịch sử thay đổi, đã được gỡ khỏi Web.

## 0. Nền tảng đã làm: tài khoản, phân quyền và Thu ngân

**Đã làm:** khách tự đăng ký; đăng nhập, đăng xuất, đổi mật khẩu; khóa tạm 15 phút sau 5 lần nhập sai. Admin cấp tài khoản, đổi vai trò, khóa/mở khóa cho nhân viên đã có hồ sơ. Trang hiện hữu kiểm tra quyền ở controller/server, không chỉ ẩn menu; nhân viên ngừng làm hoặc khách ngừng sử dụng bị đăng xuất ở yêu cầu tiếp theo. Web dùng cookie Identity; API nghiệp vụ tạo về sau phải cấu hình xác thực và kiểm tra quyền riêng.

**Tài khoản demo trên mỗi database local:** từ thư mục repo, chạy PowerShell dưới đây rồi khởi động Web bình thường. Lệnh chỉ chạy trong Development, tạo hồ sơ nhân viên/khách và gán đúng vai trò Identity. Mật khẩu dưới đây là mật khẩu demo công khai, không dùng cho tài khoản thật.

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:AuthBootstrap__DemoPassword='Demo@2026!'
dotnet run --project RestaurantManagement.Web -- --init-demo-accounts
dotnet run --project RestaurantManagement.Web
```

| Vai trò | Email đăng nhập | Mật khẩu |
| --- | --- | --- |
| Admin | `admin.demo@example.test` | `Demo@2026!` |
| Khách hàng | `khach.demo@example.test` | `Demo@2026!` |
| Tiếp tân | `tieptan.demo@example.test` | `Demo@2026!` |
| Bồi bàn | `boiban.demo@example.test` | `Demo@2026!` |
| Thu ngân | `thungan.demo@example.test` | `Demo@2026!` |
| Bếp | `bep.demo@example.test` | `Demo@2026!` |
| Kho | `kho.demo@example.test` | `Demo@2026!` |

Git mang theo lệnh tạo tài khoản, không mang dữ liệu trong SQL Server; mỗi máy phải tự chạy lệnh. Chạy lại không tạo trùng hoặc đổi mật khẩu; tài khoản/hồ sơ trùng nhưng sai thông tin sẽ được báo lỗi thay vì bị ghi đè. Nếu muốn tạo Admin thật thay vì dùng tài khoản demo, đặt riêng `AuthBootstrap__AdminEmail` và `AuthBootstrap__AdminPassword` rồi chạy `dotnet run --project RestaurantManagement.Web -- --init-auth`; không đưa mật khẩu thật vào Git.

| Vai trò | Quyền hiện chạy | Chức năng dự kiến tích hợp |
| --- | --- | --- |
| Admin | CRUD nhân viên, danh mục, món, nguyên liệu, bàn; cấp/khóa/đổi vai trò; xem/in hóa đơn | Quản trị nghiệp vụ bổ sung |
| Khách hàng | Tự đăng ký, đăng nhập, đổi mật khẩu | Xem thực đơn, đặt bàn, đánh giá |
| Tiếp tân | Xem danh sách bàn | Tiếp nhận và xếp bàn |
| Bồi bàn | Xem danh sách bàn | Ghi món, theo dõi bàn và món từ bếp |
| Thu ngân | Xem/in hóa đơn, xác nhận thu đủ tiền mặt trên bill hợp lệ | Thanh toán online khi có cổng thanh toán xác nhận phía server |
| Bếp | Đăng nhập, đổi mật khẩu | Hàng đợi chế biến/KDS |
| Kho | Xem/quản lý nguyên liệu qua CRUD hiện có | Nhập/xuất/tồn kho |

`/NhanVien`, `/DanhMuc`, `/MonAn`, `/TaiKhoanNhanVien` chỉ cho Admin; `/BanAn` cho Admin/Tiếp tân/Bồi bàn xem nhưng chỉ Admin sửa; `/NguyenLieu` cho Admin/Kho; `/HoaDon` cho Admin/Thu ngân xem/in nhưng chỉ Thu ngân xác nhận tiền mặt. Khách không vào trang nhân viên. Khi thêm đặt bàn/API, phải kiểm tra **quyền sở hữu** (khách chỉ xem/sửa đặt bàn của mình), không chỉ kiểm tra vai trò. Không tạo hệ thống tài khoản hay vai trò thứ hai.

**Kiểm chứng:** build thành công (0 cảnh báo, 0 lỗi); smoke test đạt `PASS: 279 HTTP/database checks`, bao gồm đăng nhập đủ 7 vai trò và chạy lệnh tạo tài khoản hai lần. Database kiểm thử tạm đã được xóa.

**Quyết định:** tách Thu ngân khỏi Tiếp tân. Hiện có 7 vai trò đăng nhập: `Admin`, `KhachHang`, `TiepTan`, `BoiBan`, `ThuNgan`, `Bep`, `Kho`. Admin cấp tài khoản cho **5 nhóm nhân viên** (Tiếp tân, Bồi bàn, Thu ngân, Bếp, Kho); số lượng người trong từng nhóm không giới hạn. Khách tự đăng ký, Admin được khởi tạo riêng. `NhanVien.ChucVu` là chức danh hồ sơ, còn quyền truy cập thật dựa vào vai trò Identity; không suy quyền từ chuỗi chức danh.

**Đã làm trên nhánh `feature/auth-week6-member1`:** thêm vai trò `ThuNgan` vào khởi tạo Identity và form cấp/đổi vai trò của Admin. Thu ngân đăng nhập được đưa đến `/HoaDon`; chỉ Thu ngân và Admin được xem danh sách, chi tiết và in hóa đơn. Thu ngân xác nhận **đã nhận đủ tiền mặt** trên hóa đơn chưa thanh toán; server đối chiếu trạng thái, tổng tiền với các dòng món và `RowVersion` để tránh ghi đè bill đã thay đổi. Khi xác nhận, hóa đơn lưu `DaThanhToan`, `TienMat`, thời điểm thanh toán và `NhanVienId` của Thu ngân đang thao tác. Admin xem/in nhưng không bấm xác nhận thu tiền; Tiếp tân, Bồi bàn, Bếp, Kho và Khách không vào trang hóa đơn. In dùng chức năng in của trình duyệt, không tạo bảng mới. `DbSeeder` bổ sung hồ sơ mẫu `NV015` (Thu ngân), **không tạo sẵn mật khẩu**.

**Lưu ý dữ liệu mẫu:** `DbSeeder` có hồ sơ Thu ngân `NV015` nhưng không tạo tài khoản; lệnh demo dùng hồ sơ riêng `DEMO-TN`. Script `PopulateRestaurantData.sql` cũ có tài khoản `Cashier` bị khóa, không có mật khẩu; đó không phải tài khoản `ThuNgan` đăng nhập được. Không sửa mật khẩu/quyền bằng SQL thủ công.

**Chưa làm, cần nhóm nối tiếp:** màn hình gọi món phải tạo/cập nhật `HoaDon` và `ChiTietHoaDon` đúng giá tại thời điểm bán, giữ tổng tiền đồng bộ trước khi Thu ngân xác nhận. Thanh toán online cần cổng thanh toán và xác nhận giao dịch ở server; nút khách bấm hoặc ảnh chuyển khoản không được tự đổi sang `DaThanhToan`. Hóa đơn `ThanhToanMotPhan` chưa có dữ liệu số tiền đã thu từng lần nên không cho xác nhận tiền mặt bằng luồng mới. Việc gán voucher, hoàn/hủy giao dịch và đối soát ca là nghiệp vụ tiếp theo, không được ghi trong báo cáo là đã hoàn thành.

## 1. Chốt vai trò, tránh giao diện chung quá rối

- **Admin/quản lý:** quản lý thực đơn, danh mục, giá theo size, combo, trạng thái món và dữ liệu định mức; duyệt nghiệp vụ quản lý. Giá chỉ có trên màn hình Admin, khách (giá bán) và nơi thanh toán phù hợp; không có trên Kitchen Display.
- **Bếp/Kitchen Display:** chỉ thấy món đã gửi bếp, bàn, số lượng, ghi chú, thời điểm/thứ tự gọi; chuyển `ChoCheBien -> DangCheBien -> SanSang`. Không hiện giá, toàn bộ thực đơn hay CRUD quản lý. Khi xong, Bồi bàn thấy trạng thái để mang món.
- **Bồi bàn (mobile/tablet):** xem bàn được phục vụ, mở bàn, chọn món và size, nhập ghi chú, gửi order. Không sửa giá/định mức.
- **Tiếp tân:** tiếp nhận/xác nhận đặt bàn, xếp bàn; không sửa giá/định mức.
- **Thu ngân:** xem chi tiết bill, in bill, xác nhận đã nhận đủ tiền mặt tại quầy. Không sửa giá món hay định mức; thanh toán online chỉ xác nhận khi có kết quả giao dịch hợp lệ từ server.
- **Kho:** tạo/cập nhật **danh mục nguyên liệu** (tên, đơn vị, ngưỡng), nhập/xuất/tồn và báo thiếu; không tự quyết định món cần nguyên liệu gì.
- **Khách:** xem thực đơn/giá bán, đặt bàn, gọi món theo phần được triển khai; không phải tạo hồ sơ nếu là khách vãng lai.

**Điểm cần chốt với cô:** quản lý bếp là người biết công thức, nhưng góp ý ghi *Admin duy nhất quản lý thực đơn, gồm định mức*. Để bám nguyên văn: Bếp cung cấp công thức/định lượng, Admin nhập và chịu trách nhiệm trên hệ thống; màn hình Bếp chỉ điều phối món. Nếu muốn Bếp tự nhập, phải xin cô xác nhận ngoại lệ rồi làm **màn hình công thức riêng** chỉ cho chỉnh nguyên liệu/số lượng, tuyệt đối không thấy/sửa giá hay các cài đặt thực đơn. Không mở toàn bộ `MonAnController` cho Bếp.

## 2. Đối chiếu code hiện tại: cái nào đã có, cái nào còn sai

| Góp ý | Hiện trạng | Việc cần làm |
| --- | --- | --- |
| Lọc món theo Danh mục | **Đã có** trong `MonAn/Index`; không làm lại | Chỉ kiểm thử hiển thị/lọc trên bản nhóm ghép |
| Giá theo size | **Đã có** `MonAnSize.GiaBan` | Giữ Admin được sửa; không đưa lên KDS |
| Lưu Món + Size + Định mức một lần | **Đã có transaction** khi bấm Lưu | Giữ, sửa cấu trúc dữ liệu và form; không cần LocalStorage/Session nếu bảng tạm JS trong form đủ dùng |
| Định mức theo từng size | **Sai:** `DinhMucMon` đang khóa `(MonAnId, NguyenLieuId)`, chỉ 1 `SoLuong` cho mọi size | Đổi thành định mức gắn `MonAnSizeId`, mỗi size có lượng riêng; form chọn nguyên liệu một lần rồi điền ma trận lượng theo size |
| Combo chọn món lẻ | **Có** chọn món lẻ + số lượng, không nhập nguyên liệu thô | Bổ sung chọn **size** của từng món thành phần; giữ giá combo là giá trọn gói do Admin nhập |
| Món mới mặc định đang phục vụ | Form đang cho chọn `TamHet/NgungKinhDoanh` ngay lúc tạo; POST cũng nhận | Create cưỡng chế `DangPhucVu` ở server, ẩn chọn trạng thái khi tạo |
| Tạm hết/ngừng kinh doanh | `TamHet` đang bị chặn nếu có hóa đơn chưa thanh toán; `NgungKinhDoanh` không có lý do | `TamHet` chặn **order mới**, không xóa/chặn món đã gọi; Admin đổi trạng thái. `NgungKinhDoanh` bắt buộc có lý do và quy tắc xử lý order đang mở |
| Món mới/nổi bật | Hiện chỉ là 2 checkbox, chưa có tiêu chí | Chốt trong báo cáo và code: ví dụ `Món mới = trong 30 ngày kể từ ngày bắt đầu bán`; `Nổi bật = đặc sản do Admin đánh dấu` (không gọi best-seller nếu chưa tính doanh số). Tránh thêm engine xếp hạng khi chưa cần |
| Bếp FIFO | Chưa có màn hình; `ChiTietHoaDon` có trạng thái, lượng, ghi chú nhưng **không có thời điểm gọi từng dòng** | Thêm `ThoiDiemGoi` cho dòng order (hoặc quy tắc thứ tự rõ ràng), sắp xếp FIFO, không hiện tiền |
| Bàn của món trên KDS | `HoaDon` nối `DatBan`, `ChiTietDatBan` nối `BanAn` | Với khách đến trực tiếp, vẫn tạo bản ghi `DatBan` nội bộ (`KhachHangId` có thể rỗng) và gắn bàn, để order/bếp biết bàn nào; không ép đăng ký khách hàng |
| Chuyển trang theo vai trò | Auth đã có; đăng nhập hiện về Home chung | Khi trang KDS/mobile/tiếp tân đã tồn tại, chuyển hướng theo vai trò và ẩn menu không liên quan; bảo vệ controller/action ở server |

Không tạo bảng mới chỉ để "đủ số lượng". Sửa quan hệ trong **bảng hiện có** và tạo EF migration mới. Không chạy DROP tùy tiện lên DB đang có dữ liệu: công thức cũ có thể sao chép làm giá trị khởi đầu cho từng size, nhưng **phải rà lại định lượng**; combo cũ nhiều size thì không thể đoán size chính xác, phải xác nhận/chọn lại. Cập nhật seed, SQL script tạo mới, test và phần 3.1 của Word để khớp migration cuối.

## 3. Ai làm gì, theo thứ tự phụ thuộc

### Thành viên 1 — CSDL + Auth/phân quyền

1. Thống nhất với nhóm thiết kế `DinhMucMon -> MonAnSize` và `ChiTietCombo -> MonAnSize` (size món thành phần). Tạo **một migration chung** có phương án chuyển dữ liệu cũ; không để mỗi bạn tự sửa schema riêng.
2. Chốt dữ liệu cần cho KDS: thời điểm gọi **từng chi tiết hóa đơn**, bàn gắn với đơn tại chỗ; cập nhật model/migration/seed/test và mô tả 3.1 của Word.
3. Chuyển trang Thu ngân đến `/HoaDon` đã có. Sau khi thành viên 2 có route KDS và mobile, cập nhật chuyển trang các vai trò còn lại; Bếp không thấy menu quản trị/giá. Kiểm thử URL trực tiếp sai quyền và tài khoản mẫu từng vai trò. Không tự làm thay màn hình đặt bàn, gọi món hay KDS.
4. Về công thức: theo lời cô, quyền sửa ở Admin. Nếu nhóm muốn Bếp tự sửa thì xin cô xác nhận trước; không mở tràn quyền.

### Thành viên 2 — Đặt bàn, phục vụ, Kitchen Display

1. Tiếp tân: nhận/xác nhận đặt bàn, xếp bàn; khách vãng lai không bắt buộc có tài khoản.
2. Bồi bàn trên điện thoại/tablet: mở bàn, chọn món/size, ghi số lượng/yêu cầu chế biến, gửi order; không được sửa giá.
3. KDS cho Bếp: chỉ truy vấn các dòng đã gửi bếp (`ChoCheBien/DangCheBien`), hiện bàn + món + size + lượng + ghi chú + thứ tự gọi; Bếp cập nhật `DangCheBien/SanSang` ở server. Bồi bàn thấy món `SanSang`; đánh dấu `DaPhucVu` khi mang ra. Không hiện `DonGia`, `TongTien` ở KDS/API trả cho Bếp.
4. Kiểm thử thứ tự khi nhiều lần gọi thêm món vào **cùng hóa đơn**; không xếp chỉ theo `HoaDon.ThoiDiemLap` vì tất cả dòng trong một bill sẽ có cùng thời điểm hóa đơn.
5. Khi hoàn tất order, cập nhật tổng hóa đơn trước khi Thu ngân thu tiền; không tạo hóa đơn thứ hai chỉ để in. Nếu nhóm tích hợp thanh toán online, trạng thái `DaThanhToan` phải do xác nhận giao dịch phía server quyết định.

### Thành viên 3 — Thực đơn, món, size, combo

1. Chờ migration chung của thành viên 1, rồi sửa form theo luồng: thông tin món -> chọn tập nguyên liệu -> mỗi size nhập giá và lượng của **từng nguyên liệu đã chọn** -> một nút Lưu. Có thể dùng bảng tạm trong DOM/JavaScript hiện có; không bắt buộc LocalStorage/Session.
2. Combo có form riêng hoặc nhánh form tách rõ: chọn **món lẻ + size + số lượng**; không nhập nguyên liệu thô cho combo. Giá combo là giá trọn gói do Admin đặt; không tự suy ra giảm giá/voucher.
3. Create luôn lưu `DangPhucVu`; Edit cho Admin đổi `TamHet` hoặc `NgungKinhDoanh` (có lý do). Sửa quy tắc đang chặn `TamHet` vì hóa đơn chưa thanh toán. Giữ bộ lọc Danh mục **đã có**, không làm lại.
4. Chốt tiêu chí món mới/nổi bật và cập nhật form, dữ liệu, Word; không để nhãn tùy ý không giải thích. Kiểm thử mỗi size có lượng nguyên liệu khác nhau và combo chọn đúng size.

## 4. Chỗ chưa nên làm vội

- **Không tự báo thanh toán online thành công:** nhóm đã chốt vai trò `ThuNgan` cho tiền mặt/in bill; xác nhận chuyển khoản hoặc ví điện tử cần chứng cứ giao dịch và xử lý từ server.
- **Không để Bếp sửa toàn bộ món** chỉ để nhập công thức; sẽ lộ giá và quyền kinh doanh.
- **Không tự trừ kho theo món bán** trước khi định mức theo size và quy tắc phiếu xuất được chốt, tránh sai tồn.
- **Không push/merge thay đổi schema khi chưa đối chiếu DB hiện có và nhóm chưa đồng ý.**

## 2026-09-29 — Trang chủ, cổng đăng nhập và phạm vi nhân viên

- Trang `/` hiển thị thực đơn theo danh mục, món mới/nổi bật, giá thấp nhất của size đang dùng, trạng thái còn/tạm hết và các khuyến mãi đang hiệu lực không cần voucher. Trang `/Home/MonAn/{id}` hiện chi tiết món, giá theo size, thành phần set và ưu đãi gắn với món. Dữ liệu đọc trực tiếp từ database; không chèn món/giá giả vào giao diện.
- Khách hàng đăng nhập ở `/Account/Login`; cổng `/admin` dành cho Admin và nhân viên đang làm việc. Tài khoản khách không đăng nhập được ở `/admin`, tài khoản quản trị/nhân viên không đăng nhập được ở form khách. URL quản lý chưa đăng nhập chuyển về `/admin`.
- Mỗi tài khoản nhân viên được cấp đúng một vai trò; Admin có thể cấp/đổi qua `/TaiKhoanNhanVien`. Hai phần việc thực đơn mới là `ThucDon` (món, size, công thức, combo) và `DanhMucMon` (danh mục). Theo cấu trúc hiện có, “một phần việc” có thể cập nhật nhiều bảng con liên kết; đây là ranh giới hợp lý hơn một bảng SQL vật lý. Bồi bàn quản lý bàn ăn, Bếp có màn hình cập nhật trạng thái dòng món, các vai trò khác giữ phần việc hiện có.
- Lệnh `--init-menu-staff` trong Development tạo hai hồ sơ/tài khoản mẫu `MENU-001` và `MENU-002` với email `.test`, chỉ khi cung cấp `AuthBootstrap__MenuStaffPassword` qua biến môi trường. Lệnh lặp lại không ghi đè tài khoản khác.
- Kiểm tra: `dotnet build RestaurantManagement.Web/RestaurantManagement.Web.csproj --no-restore` đạt 0 lỗi/cảnh báo. Smoke test chạy bằng tài khoản Windows của máy đạt `PASS: 303 HTTP/database checks`; database kiểm thử tạm đã được xóa. Web chạy với `RestaurantDB` thật trả HTTP 200 ở `/` và `/admin`. Trên instance `.\SQLEXPRESS`, lệnh nạp đã tạo database `RestaurantDB`, hai nhân viên mẫu `MENU-001`/`MENU-002` và tài khoản Admin mẫu `admin.mau@example.test`. Database mới chưa có món ăn/danh mục; cần nhập qua giao diện hoặc script dữ liệu thực đơn. Tài khoản sandbox không đăng nhập SQL bằng Windows Authentication nên các lệnh nạp/kiểm thử phải chạy với tài khoản Windows có quyền SQL.

## 2026-09-29 — Làm lại giao diện công khai và biểu mẫu tài khoản

- Trang chủ dùng ảnh món ăn minh họa lưu tại `RestaurantManagement.Web/wwwroot/images/hero-vietnamese-table.png` (tạo bằng ImageGen tích hợp), bố cục chia nội dung/ảnh, mục ưu đãi, món nổi bật/món mới, bộ lọc danh mục và trạng thái thực đơn trống. Ảnh chỉ minh họa, không gán thành món trong database.
- Form đăng nhập khách, đăng ký khách và đăng nhập quản trị dùng chung hệ màu xanh, bố cục ảnh/nội dung và trường nhập dễ đọc. Form đăng ký có mô tả yêu cầu mật khẩu, đồng ý nhận ưu đãi tùy chọn; các form có nút hiện/ẩn mật khẩu. Thông báo kiểm tra dữ liệu của form đăng ký đã đổi sang tiếng Việt.
- Sửa `UseStaticFiles()` trước xác thực để khách chưa đăng nhập vẫn tải được CSS, JavaScript và ảnh. Đã kiểm tra giao diện ở kích thước màn hình nhỏ và lớn. Build sạch; smoke test đạt `PASS: 306 HTTP/database checks`, gồm ba kiểm tra mới cho tệp tĩnh công khai; database kiểm thử đã xóa.

## 2026-09-29 — Nạp dữ liệu SQL và đồng bộ tài khoản mẫu

- `PopulateRestaurantData.sql` nay lưu thêm hồ sơ/tài khoản mẫu Admin, Thực đơn (`MENU-001`) và Danh mục (`MENU-002`) đúng vai trò ứng dụng. Tài khoản mới do SQL thêm luôn khóa và không có mật khẩu; lệnh `--init-auth` / `--init-menu-staff` dùng ASP.NET Identity để kích hoạt bằng mật khẩu từ biến môi trường. Tài khoản đã tồn tại không bị đổi mật khẩu.
- Thêm lệnh Development `--import-sql-data` chạy `PopulateCrudData.sql`, sau đó `PopulateRestaurantData.sql` và in số bản ghi chính.
- Đã nạp vào `RestaurantDB` tại `.\SQLEXPRESS` và chạy lại để xác nhận không tạo trùng: 14 nhân viên, 10 danh mục, 16 món, 19 size, 18 bàn, 8 khách, 8 đặt bàn, 4 hóa đơn, 8 tài khoản (3 tài khoản đã kích hoạt từ lượt trước và 5 tài khoản minh họa cũ đang khóa). Build với output riêng đạt 0 lỗi/cảnh báo vì tiến trình Web đang mở giữ file build mặc định.

## 2026-09-29 — Kích hoạt các chức vụ nghiệp vụ còn lại

- Năm tài khoản SQL gắn NV004/NV005/NV006/NV008/NV012 nay dùng vai trò ứng dụng `ThuNgan`/`TiepTan`/`BoiBan`/`Bep`/`Kho`, mỗi tài khoản đúng một vai trò. Script có bước nâng cấp các liên kết vai trò tiếng Anh cũ chỉ khi tài khoản vẫn khóa và chưa có mật khẩu.
- Thêm `--init-other-staff` trong Development, nhận `AuthBootstrap__OtherStaffPassword` từ biến môi trường, yêu cầu đã nạp SQL và kích hoạt qua ASP.NET Identity; không ghi mật khẩu vào repository và không thay đổi mật khẩu tài khoản đã hoạt động.
- Đã nạp lại script vào `RestaurantDB` và kích hoạt cả năm tài khoản. Tổng vẫn là 14 nhân viên và 8 tài khoản; build đạt 0 lỗi/cảnh báo.

## 2026-09-29 — Mật khẩu riêng cho tám tài khoản mẫu

- Thêm lệnh Development `--set-sample-passwords`, yêu cầu tám mật khẩu khác nhau từ biến môi trường, xác minh email/vai trò/hồ sơ mẫu rồi đổi bằng ASP.NET Identity trong một transaction. Lệnh kiểm tra mật khẩu mới và mở khóa tài khoản sau khi đổi; không ghi mật khẩu rõ vào SQL/repository.
- Đã đổi và kiểm tra cả tám tài khoản hiện có trong `RestaurantDB` (Admin, Thực đơn, Danh mục, Tiếp tân, Bồi bàn, Thu ngân, Bếp, Kho). Các mật khẩu cũ đã hết hiệu lực. Build đạt 0 lỗi/cảnh báo.

## 2026-09-29 — SQL tạo tài khoản đăng nhập được ngay

- `PopulateRestaurantData.sql` nay chèn tám mã băm ASP.NET Identity cho tám mật khẩu mẫu riêng; database mới sau khi chạy `InitialSchema.sql`, `PopulateCrudData.sql`, `PopulateRestaurantData.sql` có thể đăng nhập ngay, không cần ba lệnh kích hoạt và lệnh đổi mật khẩu. SQL không ghi đè mật khẩu tài khoản đã hoạt động.
- Đã kiểm tra trên một database tạm tạo từ schema SQL: chạy hai script dữ liệu, chạy lặp script nghiệp vụ, xác nhận 8/8 tài khoản hoạt động và mỗi tài khoản có đúng một vai trò; database tạm đã xóa. Chạy lại script trên `RestaurantDB` hiện tại xác nhận cả tám mật khẩu/trạng thái không đổi.

## 2026-09-29 — Dọn các đường khởi tạo cũ

- Gỡ `--init-menu-staff`, `--init-other-staff`, `--set-sample-passwords`, mã hỗ trợ và hướng dẫn tương ứng vì SQL hiện tạo sẵn tám tài khoản mẫu có thể đăng nhập. Giữ `--init-auth` và `--init-demo-accounts` vì bộ smoke test vẫn sử dụng; giữ `--import-sql-data` cho cách nạp SQL bằng một lệnh.
- Xóa trang Privacy mặc định của template vì chỉ chứa văn bản giữ chỗ và không có liên kết trong giao diện; xóa CSS mặc định của layout vì giao diện không nạp stylesheet cô lập đó; dọn chú thích layout thử nghiệm trong `_ViewStart`. Các trang chủ, đăng nhập, thực đơn, Bếp và ảnh minh họa còn được tham chiếu nên giữ nguyên.
