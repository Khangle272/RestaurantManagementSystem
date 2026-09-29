# Tiến độ, phân công và hướng phát triển

**Cập nhật: 29/09/2026 · Nhánh bàn giao: `feature/auth-ui-fix` · Code Auth đã kiểm chứng: `17d4ece`.**

Đây là **một bản tổng hợp hiện tại**, không phải nhiều báo cáo tuần nối nhau. Việc đã làm, việc còn thiếu và ý tưởng chờ chốt được tách riêng; không ghi mật khẩu thật/dữ liệu khách thật.

## 0. Hướng dẫn cho người nhận và AI của nhóm

- **Trước khi làm:** đọc phần hiện trạng và nhiệm vụ của người đang phụ trách, kiểm tra nhánh/commit/code thực tế. Đọc ghi chú không có nghĩa phải tự triển khai mọi ý tưởng trong file; chỉ làm phạm vi được giao. Khi cần lấy code mới, làm theo mục 4 và giữ nguyên thay đổi local.
- **Sau khi làm xong:** cập nhật ngay nhiệm vụ tương ứng trong file này: đánh dấu `[x]`, ghi ngày, phần đã thêm/sửa, route/file quan trọng, kiểm chứng và việc còn chờ người khác. Chưa xong thì giữ `[ ]`, ghi phần đã có/phần còn thiếu; không đánh dấu xong chỉ vì có bảng hoặc giao diện mẫu.
- **Không chồng nhật ký:** sửa mục hiện tại, không nối thêm một bản báo cáo tuần mới hoặc chép lại nội dung đã có. Khi một chức năng hoàn tất, chuyển kết quả ngắn sang mục 1 và bỏ yêu cầu trùng trong mục 2; giữ bàn giao còn thiếu.
- **Đổi quyết định:** cập nhật nội dung đang áp dụng; quyết định cũ ghi một dòng gạch ngang + ngày + quyết định thay thế ở mục 5. Ý tưởng chưa chốt để mục 3, không tự đổi thành yêu cầu bắt buộc.
- **Đối chiếu sau tích hợp:** cập nhật đúng thực trạng route/schema/quyền, không giữ lỗi cũ nếu đã sửa. Chỉ commit/push/merge khi được người phụ trách cho phép.

Mẫu ghi một việc xong: `[x] 30/09 — Đặt bàn khách: gửi yêu cầu + lịch sử riêng; route ...; đã kiểm tra ...; còn chờ tiếp tân duyệt.` Đây chỉ là **mẫu**, không phải trạng thái đã hoàn thành.

## 1. Phần đã hoàn thành

### Thành viên 1 — Tài khoản, phân quyền và giao diện tài khoản

- [x] Đăng ký khách; đăng nhập, đăng xuất, đổi mật khẩu; khóa tạm 15 phút sau 5 lần nhập sai. Có 7 vai trò: Admin, Khách, Tiếp tân, Bồi bàn, Thu ngân, Bếp, Kho.
- [x] Hai cổng trong **cùng một Web**: khách `/Account/Login`, nhân viên/Admin `/Staff/Login`; `/` là khu công khai. Đúng tài khoản nhân viên nhưng nhập ở cổng khách vẫn báo lỗi chung; đăng ký khách không nhận email trùng với bất kỳ tài khoản nào.
- [x] Kiểm tra quyền ở server, không chỉ ẩn menu. Khóa/ngừng sử dụng/đổi vai trò/đặt lại mật khẩu thu hồi phiên cũ theo luồng tương ứng. CSS/JS công khai; trang nghiệp vụ vẫn có bảo vệ.
- [x] `/NhanVien` gộp hồ sơ và thao tác tài khoản: thêm hồ sơ → chọn công việc → lưu → cấp tài khoản. Admin quản lý 5 nhóm nhân viên; mỗi vai trò có quyền cố định, không có bảng chọn quyền thứ hai. Admin được khởi tạo riêng, không cấp quyền Admin qua form nhân viên.
- [x] `/TaiKhoanKhachHang`: Admin tìm, sửa thông tin tài khoản, khóa/mở và đặt lại mật khẩu khách; không chuyển khách thành nhân viên hoặc xóa lịch sử.
- [x] `/Account`: Khách/Admin sửa thông tin riêng qua `/Account/EditProfile`, xác nhận bằng mật khẩu người thao tác; email đăng nhập/username và hồ sơ được lưu đồng bộ. Khách chỉ sửa chính mình. Nhân viên không tự sửa hồ sơ/vai trò nhưng được đổi mật khẩu riêng. Admin chưa liên kết hồ sơ nhân viên hiện sửa được email/số điện thoại, chưa có họ tên riêng.
- [x] `/Account/ResetPassword`: Admin đặt lại mật khẩu nhân viên/khách, phải xác nhận bằng mật khẩu Admin. Không xem mật khẩu cũ, không tự mở khóa, không dùng chức năng này đặt lại mật khẩu Admin khác. Hồ sơ ghi rõ email liên hệ; đổi email đăng nhập ở phần tài khoản.
- [x] Đăng ký có lối “Đã có tài khoản? Đăng nhập”; UI giữ Bootstrap/sidebar xanh của nhóm, nhãn vai trò nhỏ lấy từ quyền thật. Không dùng bộ chuyển vai trò giả.
- [x] Thu ngân vào `/HoaDon`: xem/in và xác nhận đã nhận đủ tiền mặt trên bill hợp lệ; kiểm tra tổng tiền/trạng thái/`RowVersion`, ghi người thu và thời điểm. Admin chỉ xem/in, các vai trò khác không vào hóa đơn. Không tự xác nhận thanh toán online hoặc thu tiếp bill `ThanhToanMotPhan` bằng luồng này.
- [x] Tài khoản demo thật trong database local, lệnh khởi tạo chạy lại không ghi đè tài khoản đã có; hướng dẫn tại mục 4.

**Kiểm chứng bản `17d4ece`:** build 0 cảnh báo/0 lỗi; **366 kiểm tra HTTP/CSDL**, database thử riêng đã xóa. Đợt tài khoản không đổi schema/database nhóm. Kết quả này không xác nhận đặt bàn, thực đơn công khai hay KDS đã hoàn thành.

---

## 2. Việc còn thiếu — bàn giao theo người

### Thành viên 1 — Dữ liệu và tích hợp quyền

- [ ] **Migration chung theo feedback:** định mức gắn `MonAnSizeId` thay vì một lượng cho mọi size; combo chọn size của từng món thành phần; thống nhất dữ liệu thời điểm gọi từng dòng để KDS FIFO đúng. Đối chiếu model/migration hiện tại rồi chốt với nhóm, không đổi schema riêng hoặc thêm bảng chỉ cho đủ số lượng.
- [ ] **Giữ dữ liệu cũ:** định lượng cũ chỉ là giá trị khởi đầu để rà lại từng size; combo cũ thiếu size phải chọn/xác nhận lại, không tự đoán. Không DROP database đang có dữ liệu. Cập nhật model, migration, seed/SQL tạo mới, test và Word 3.1/ERD cùng thiết kế đã chốt.
- [ ] **Nối route thật:** nhận route đặt bàn/tiếp tân/mobile/KDS từ thành viên 2 và thực đơn khách từ thành viên 3, cập nhật menu và chuyển trang theo vai trò. Dùng nền `AccountController`, `AppRoles`, `ActiveAccountMiddleware`; không viết hệ thống tài khoản/role thứ hai.
- [ ] **Kiểm tra sau khi ghép:** đủ 7 vai trò, trang được phép/bị cấm, GET và POST, dữ liệu thuộc đúng khách; nhân viên không sửa hồ sơ/giá trái quyền. Khóa/đổi vai trò/đặt lại mật khẩu làm hết phiên cũ. API nghiệp vụ tạo sau phải có xác thực/phân quyền riêng; cookie Web không tự bảo vệ API khác.

**Bàn giao xong khi:** nhóm dùng cùng schema, route/quyền khớp màn hình thật, giữ dữ liệu và test cả chức năng được phép lẫn bị cấm.

### Thành viên 2 — Đặt bàn, phục vụ và bếp

**Hiện chưa có trang/controller đặt bàn khách trong bản bàn giao. Tiếp tân mới xem danh sách bàn; thiếu “Tạo đặt bàn” không phải thiếu tài khoản.**

- [ ] **Khách đặt bàn:** nối Đặt bàn/Lịch sử vào khu khách. Nhập ngày/giờ, số người, liên hệ, ghi chú → gửi yêu cầu chờ xử lý → xem lịch sử/trạng thái của mình. Lấy `KhachHang` qua `TaiKhoanId` của phiên Identity; không nhận ID khách/trạng thái tùy ý từ form. Đổi ID trên URL không đọc/sửa được yêu cầu người khác.
- [ ] **Tiếp tân đặt hộ:** có nút **Tạo đặt bàn** → nhập thông tin khách/giờ/số người → chọn bàn có sẵn, phù hợp khung giờ → lưu phiếu. Có danh sách yêu cầu để duyệt/từ chối, xếp bàn và nhận bàn. Tiếp tân không thêm bàn vật lý B01/B02…; việc đó thuộc Admin. Khách vãng lai không bắt buộc đăng ký.
- [ ] **Lịch bàn:** dùng `DatBan`, `ChiTietDatBan`, `BanAn`, `KhachHang`. Kiểm tra ngày/giờ hợp lệ, sức chứa và khoảng `GioDen`–`GioKetThucDuKien`, không chỉ trạng thái bàn hiện tại; kiểm tra lại khi lưu/duyệt để tránh nhận trùng. Ghi người thao tác/thời điểm đúng trường hiện có; đối chiếu enum trước khi chọn trạng thái hủy/từ chối.
- [ ] **Bồi bàn mobile/tablet:** bàn đang phục vụ → chọn món/size/lượng/ghi chú → gửi bếp; không sửa giá. Với khách đến trực tiếp, dùng phiếu `DatBan` nội bộ gắn bàn, `KhachHangId` có thể rỗng, không ép khách tạo tài khoản. Khớp bảng hóa đơn/chi tiết hiện có, không tạo bill thứ hai chỉ để in.
- [ ] **KDS:** chỉ hiện món đang được gọi/chế biến, bàn, size, lượng, ghi chú và thứ tự gọi. FIFO theo thời điểm từng dòng, kể cả gọi thêm vào cùng bill; không dùng mỗi thời điểm tạo hóa đơn. Bếp chuyển `ChoCheBien → DangCheBien → SanSang`; Bồi bàn nhận thông báo/trạng thái sẵn sàng và xác nhận `DaPhucVu` khi mang ra. Không trả/hiện giá hoặc toàn bộ CRUD thực đơn cho Bếp.
- [ ] **Nối thanh toán:** lưu đơn giá tại thời điểm bán vào chi tiết hóa đơn, lấy/tính giá ở server, không tin giá từ form. Đồng bộ `HoaDon.TongTien` với dòng món trước khi Thu ngân thu tiền. Khách bấm nút/đưa ảnh chuyển khoản không tự đổi bill thành `DaThanhToan`; thanh toán online cần xác nhận giao dịch server. Đặt cọc cũng không tự đánh dấu đã thu khi chưa có xác nhận.

**Bàn giao xong khi:** khách gửi/xem đúng yêu cầu; tiếp tân đặt hộ/duyệt/xếp/nhận được; không trùng lịch hay lộ yêu cầu người khác; sai vai trò bị chặn. Với phần phục vụ/bếp, thử gọi thêm nhiều lần trên cùng bill và luồng bếp xong → phục vụ nhận món. Gửi route/quyền thật cho thành viên 1 nối điều hướng.

### Thành viên 3 — Thực đơn và giao diện khách

- [ ] **Khu công khai `/`:** giới thiệu nhà hàng, món/danh mục, hình, mô tả, size, giá, combo, trạng thái. Khách/chưa đăng nhập được xem; không mở form CRUD Admin. Khu khách dùng `_Layout`, nội bộ `_AdminLayout`, giữ vibe UI nhóm; chỉ hiện nút khi có route xử lý thật, bỏ chữ ghi chú phát triển khỏi trang hoàn thiện.
- [ ] **Form món theo feedback:** thông tin chung → chọn tập nguyên liệu một lần → thêm size với giá và lượng từng nguyên liệu → bảng tạm sửa/xóa size → một nút Lưu ghi món/size/định mức cùng transaction. Các size dùng chung tập nguyên liệu nhưng khác lượng/giá; chờ migration chung của thành viên 1 trước khi đổi form.
- [ ] **Set/combo riêng:** chọn món lẻ đã có + size + số lượng, không nhập lại nguyên liệu thô. Giá trọn gói do Admin đặt, không tự suy ra voucher/giảm giá. Ví dụ tổng món lẻ 180.000đ, giá combo có thể đặt 150.000đ.
- [ ] **Trạng thái món:** tạo mới cưỡng chế `DangPhucVu` phía server. Admin cập nhật `TamHet` khi thiếu nguyên liệu; chặn order mới nhưng không xóa/chặn xử lý món đã gọi. `NgungKinhDoanh` phải có lý do và cách xử lý order đang mở.
- [ ] **Món mới/nổi bật:** chốt tiêu chí trong code và báo cáo. Có thể chọn thời hạn “mới” và đặc sản do Admin đánh dấu; nếu gọi best-seller thì phải có dữ liệu bán hàng, không dùng checkbox tùy ý làm bằng chứng.
- [ ] **Giữ phần đã có:** lọc danh mục, giá theo size và transaction lưu món đã có ở bản đối chiếu; tận dụng, không dựng lại. Chỉ Admin quản lý thực đơn/giá/size/combo/định mức. Bếp không được quyền CRUD món chỉ để nhập công thức.

**Bàn giao xong khi:** lọc đúng món; giá/size/combo đúng dữ liệu; khách không sửa giá/món; trạng thái phục vụ đúng; mỗi size lưu lượng riêng và combo chọn đúng size. Đồng bộ báo cáo với schema cuối.

---

## 3. Ý tưởng/quy tắc cần chốt — chưa triển khai

- [ ] **Đặt bàn:** khách chọn bàn cụ thể hay chỉ gửi nhu cầu; thời gian giữ bàn; hạn sửa/hủy và đặt cọc. Thống nhất rồi mới mở thao tác tương ứng.
- [ ] **Công thức của Bếp:** feedback hiện yêu cầu Admin quản lý định mức; Bếp cung cấp công thức. Nếu muốn Bếp tự nhập, xin cô xác nhận rồi làm quyền/màn hình công thức riêng không có giá, không mở toàn bộ quản lý món.
- [ ] **Tài khoản khi triển khai thật:** thay tài khoản/mật khẩu demo; chốt quên mật khẩu, xác minh email/điện thoại và xác thực thêm cho Admin nếu cần. Hiện đổi email có xác nhận mật khẩu, chưa xác minh email mới bằng thư; chưa có OTP/MFA.
- [ ] **Kho:** chưa tự trừ nguyên liệu theo món bán trước khi định mức theo size và quy tắc phiếu xuất được chốt. Theo dõi nhập/xuất/tồn, báo thiếu và báo cáo theo phân công nhóm; không để Kho tự quyết công thức món.
- [ ] **Thanh toán mở rộng:** thu một phần, hoàn/hủy, đối soát ca, voucher/ưu đãi kết hợp cần quy tắc và dữ liệu tương ứng; chưa coi là đã hoàn thành từ màn hình xem/in bill.

---

## 4. Lấy code và tài khoản để chạy

Khi được yêu cầu cập nhật: kiểm tra đúng remote `Khangle272/RestaurantManagementSystem`, nhánh và `git status` → `git fetch origin` → lấy đúng `feature/auth-ui-fix`. Nếu đang ở nhánh này và checkout sạch, dùng `git pull --ff-only origin feature/auth-ui-fix`. Nhánh chưa có thì `git switch --track origin/feature/auth-ui-fix`; nếu đã có thì `git switch feature/auth-ui-fix`. Không kéo thẳng vào nhánh đang làm của người khác, không reset/xóa/ghi đè thay đổi local. Chỉ merge/push khi được phép. Sau khi nhóm merge vào `master`, lấy `master` đã merge. Xem GitHub không tự cập nhật code local.

Dừng Web cũ của repo rồi chạy lại. Kiểm tra connection string đang trỏ đúng SQL Server/database local; Git không mang database/tài khoản từ máy khác. Nếu chưa có tài khoản demo, từ thư mục repo chạy:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:AuthBootstrap__DemoPassword='Demo@2026!'
dotnet run --project RestaurantManagement.Web -- --init-demo-accounts
dotnet run --launch-profile https --project RestaurantManagement.Web
```

| Vai trò | Email | Mật khẩu demo |
| --- | --- | --- |
| Admin | `admin.demo@example.test` | `Demo@2026!` |
| Khách hàng | `khach.demo@example.test` | `Demo@2026!` |
| Tiếp tân | `tieptan.demo@example.test` | `Demo@2026!` |
| Bồi bàn | `boiban.demo@example.test` | `Demo@2026!` |
| Thu ngân | `thungan.demo@example.test` | `Demo@2026!` |
| Bếp | `bep.demo@example.test` | `Demo@2026!` |
| Kho | `kho.demo@example.test` | `Demo@2026!` |

Khách dùng `/Account/Login`; các tài khoản còn lại dùng `/Staff/Login`. Lệnh demo chỉ chạy Development, chạy lại không đổi mật khẩu/quyền hoặc ghi đè hồ sơ trùng. Hồ sơ `NV015` hoặc tài khoản `Cashier` cũ trong SQL không thay thế tài khoản demo Thu ngân. Không sửa password/hash/quyền bằng SQL thủ công. Admin thật dùng cấu hình `AuthBootstrap__AdminEmail`/`AdminPassword` và `--init-auth`; không lưu mật khẩu thật trong Git. Nếu không chạy/đăng nhập được, báo nhánh/commit, log và database thực tế; không tắt bảo mật máy để ép chạy.

---

## 5. Quyết định đã thay thế — chỉ giữ mốc ngắn

- **28/09/2026:** ~~Một cổng đăng nhập chung~~ → hai cổng Khách/Nhân viên, giữ kiểm tra nhóm tài khoản phía server.
- **28/09/2026:** ~~Menu tài khoản nhân viên và bảng chọn quyền riêng~~ → thao tác tài khoản trong hồ sơ nhân viên, một vai trò công việc với bộ quyền cố định.
- **29/09/2026:** ~~Tài khoản cá nhân chỉ đổi mật khẩu; Admin chỉ khóa/mở khách~~ → bổ sung sửa thông tin và Admin đặt lại mật khẩu nhân viên/khách.

Các thay đổi chi tiết cũ xem trong lịch sử Git; không chép lại vào phần tiến độ hiện tại. Sau mỗi lần làm, cập nhật checkbox/ngày/kiểm chứng ngay tại nhiệm vụ tương ứng; ý tưởng chưa được chốt không tự chuyển thành yêu cầu phải làm.
