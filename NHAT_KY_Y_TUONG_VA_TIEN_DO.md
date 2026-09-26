# Góp ý của cô và nhật ký quyết định khi làm đồ án (từ 27/09/2026)

Giữ file này làm nơi ghi **góp ý → quyết định → phần đã làm → phần còn chờ**, để nhóm và AI khác đọc lại không nhầm ý tưởng với chức năng đã chạy. Code xác thực đang ở nhánh `feature/auth-week6-member1`; chưa tự merge hay sửa DB thật từ ghi chú này. Hai bạn làm chức năng cần thống nhất thay đổi CSDL với người phụ trách dữ liệu trước khi code form.

**Cách dùng cho các lần làm sau (cả thành viên và AI):** trước khi sửa hãy đọc mục liên quan rồi đối chiếu code/nhánh hiện tại. Sau mỗi phần đã làm xong, thêm một mục có ngày, người/nhánh phụ trách, file hoặc chức năng đã đổi, kết quả build/test, và việc còn chờ người khác. Ghi rõ `Đã làm`, `Đang làm` hoặc `Dự kiến`; không đổi một ý tưởng thành “đã hoàn thành” chỉ vì nó đã có bảng CSDL. Giữ các quyết định cũ để nhìn lại vì sao nhóm đổi hướng; nếu quyết định thay đổi, ghi mục mới thay thế và lý do. Không ghi mật khẩu thật hoặc dữ liệu khách hàng thật vào file.

## 0. Cập nhật phần Thành viên 1: bổ sung Thu ngân

**Tài khoản đăng nhập dùng để demo trên database local:** chạy lệnh `--init-demo-accounts` theo hướng dẫn trong `AUTH_WEEK6_HANDOFF.md` trên từng máy. Lệnh chỉ cho phép môi trường Development và tạo hồ sơ nhân viên/khách cùng vai trò Identity tương ứng. Mật khẩu dưới đây chỉ dành cho demo, không dùng cho tài khoản thật.

| Vai trò | Email đăng nhập | Mật khẩu |
| --- | --- | --- |
| Admin | `admin.demo@example.test` | `Demo@2026!` |
| Khách hàng | `khach.demo@example.test` | `Demo@2026!` |
| Tiếp tân | `tieptan.demo@example.test` | `Demo@2026!` |
| Bồi bàn | `boiban.demo@example.test` | `Demo@2026!` |
| Thu ngân | `thungan.demo@example.test` | `Demo@2026!` |
| Bếp | `bep.demo@example.test` | `Demo@2026!` |
| Kho | `kho.demo@example.test` | `Demo@2026!` |

Git mang theo lệnh tạo tài khoản, không mang dữ liệu trong SQL Server. Chạy lại lệnh không tạo trùng hoặc đổi mật khẩu đã có; tài khoản/hồ sơ trùng nhưng sai thông tin sẽ được báo lỗi thay vì bị ghi đè.

**Kiểm chứng:** build thành công (0 cảnh báo, 0 lỗi); smoke test đạt `PASS: 279 HTTP/database checks`, bao gồm đăng nhập đủ 7 vai trò và chạy lệnh tạo tài khoản hai lần. Database kiểm thử tạm đã được xóa.

**Quyết định:** tách Thu ngân khỏi Tiếp tân. Hiện có 7 vai trò đăng nhập: `Admin`, `KhachHang`, `TiepTan`, `BoiBan`, `ThuNgan`, `Bep`, `Kho`. Admin cấp tài khoản cho **5 nhóm nhân viên** (Tiếp tân, Bồi bàn, Thu ngân, Bếp, Kho); số lượng người trong từng nhóm không giới hạn. Khách tự đăng ký, Admin được khởi tạo riêng. `NhanVien.ChucVu` là chức danh hồ sơ, còn quyền truy cập thật dựa vào vai trò Identity; không suy quyền từ chuỗi chức danh.

**Đã làm trên nhánh `feature/auth-week6-member1`:** thêm vai trò `ThuNgan` vào khởi tạo Identity và form cấp/đổi vai trò của Admin. Thu ngân đăng nhập được đưa đến `/HoaDon`; chỉ Thu ngân và Admin được xem danh sách, chi tiết và in hóa đơn. Thu ngân xác nhận **đã nhận đủ tiền mặt** trên hóa đơn chưa thanh toán; server đối chiếu trạng thái, tổng tiền với các dòng món và `RowVersion` để tránh ghi đè bill đã thay đổi. Khi xác nhận, hóa đơn lưu `DaThanhToan`, `TienMat`, thời điểm thanh toán và `NhanVienId` của Thu ngân đang thao tác. Admin xem/in nhưng không bấm xác nhận thu tiền; Tiếp tân, Bồi bàn, Bếp, Kho và Khách không vào trang hóa đơn. In dùng chức năng in của trình duyệt, không tạo bảng mới. `DbSeeder` bổ sung hồ sơ mẫu `NV015` (Thu ngân), **không tạo sẵn mật khẩu**.

**Cách thử trên DB local:** chạy lại `dotnet run --project RestaurantManagement.Web -- --init-auth` để tạo vai trò thứ 7; Admin cấp tài khoản Thu ngân cho hồ sơ đang làm việc chưa có tài khoản, rồi đăng nhập bằng tài khoản đó. Script `PopulateRestaurantData.sql` cũ có tài khoản mẫu `Cashier` tiếng Anh nhưng bị khóa và không có mật khẩu: đó **không phải** tài khoản `ThuNgan` dùng để đăng nhập. Nếu nhân viên mẫu `NV004` đã liên kết tài khoản cũ, dùng `NV015` từ `DbSeeder` hoặc tạo hồ sơ nhân viên mới; không đổi quyền/mật khẩu của tài khoản cũ bằng SQL thủ công.

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
