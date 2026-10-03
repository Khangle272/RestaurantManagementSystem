# Tiến độ, quyết định nghiệp vụ và bàn giao

Cập nhật: **02/10/2026 — Thành viên 1**, nhánh `member1-update`, phát triển trên `master` của nhóm tại `dd834bf`.
File mô tả trạng thái của nhánh đang đọc; không mặc nhiên có nghĩa đã merge vào `master`.

## Cách cập nhật file này

- Thành viên hoặc AI hỗ trợ phải kiểm tra nhánh, commit và code hiện tại trước khi làm. Fetch để đối chiếu bản mới; không pull/đổi nhánh đè lên thay đổi chưa lưu.
- Làm xong thì **sửa đúng mục hiện trạng/bàn giao bên dưới**, cập nhật ngày và kết quả kiểm tra. Không nối thêm hàng loạt mục lặp lại chuyện sửa lỗi cũ.
- Phân biệt `Đã làm`, `Cần tiếp tục` và `Ý tưởng chưa chốt`. Nếu quyết định thay đổi, thay nội dung cũ và ghi ngắn lý do/ngày; không để hai hướng trái nhau cùng được coi là đúng.
- Khi nhận việc, đọc **mục 3 (quyền), 5 (phân công), 6 (điểm tích hợp)** rồi đối chiếu controller/service/model thật. AI của từng thành viên cũng phải sửa mục tương ứng sau khi hoàn thành; không nối nhật ký lỗi hoặc ghi thêm một bản bàn giao trái với bản này. Báo đã làm phần nào, kiểm chứng bằng gì và phần nào còn cần người sau.
- Code trên nhánh cập nhật chưa tự có trong `master`. Khi kiểm tra/tích hợp, phải lấy đúng nhánh hoặc commit đã bàn giao; không suy rằng chỉ pull `master` là đã có tất cả. Đọc tài liệu cùng phiên bản code, giữ thay đổi local trước khi chuyển nhánh; không merge nguyên UI nhánh auth cũ vào UI mới của nhóm.
- Không đánh dấu hoàn thành chỉ vì đã có bảng CSDL hoặc nút giao diện. Không ghi mật khẩu thật; thay đổi công thức/combo/CSDL liên quan phải thống nhất với người làm phần đó trước.

## 1. Nền tảng và giao diện giữ từ master

- Giữ trang chủ nhà hàng, danh sách/chi tiết món, ảnh minh họa, đăng nhập/đăng ký khách, đăng nhập nội bộ và CSS chính của nhóm. Không lấy UI nhánh auth cũ thay giao diện mới.
- `/` là trang công khai cho khách xem nhà hàng/thực đơn. Khách đăng nhập tại `/Account/Login`; nhân viên và Admin tại `/admin`.
- Nhân viên/Admin vẫn xem trang nhà hàng để kiểm tra nội dung nhưng **không thấy nút/link Đặt bàn của khách**, kể cả menu, banner và chi tiết món. Khách và người chưa đăng nhập vẫn thấy; truy cập thẳng `/DatBan` vẫn bị kiểm tra role ở server. Nhân viên nhận yêu cầu cho khách tại khu tiếp tân, không dùng form khách.
- Form khách chỉ nhận tài khoản khách. Nhập đúng tài khoản nhân viên ở form khách vẫn báo sai thông tin chung, không tiết lộ vai trò. Email đăng ký không được trùng tài khoản đã tồn tại, kể cả nhân viên.
- `/Account` là **Thông tin cá nhân** cho mọi vai trò, không phải trang chủ. Các trang tài khoản dùng bố cục/style sẵn có của khu khách hoặc khu quản trị, không dùng khung UI auth cũ.
- `/admin/TrangChu` là trang chủ nội bộ với lối tắt theo quyền; Admin vào đây sau đăng nhập, nhân viên vẫn vào chức năng công việc chính. `/` vẫn là web nhà hàng công khai, có liên kết riêng **Xem trang nhà hàng**.
- Menu nội bộ: **Trang chủ → Tài khoản → Bàn & Đặt bàn → Thực đơn → Vận hành → Đánh giá khách hàng**. Nhóm chỉ hiện mục được phép; nhóm một mục hiện trực tiếp, nhóm đang xem tự mở, Đăng xuất ở cuối. Menu và lối tắt dùng chung danh sách để tránh lệch tên/quyền. Không thêm chức năng chưa có chỉ để lấp menu.
- Khung nhỏ/điện thoại: menu nội bộ mặc định thu gọn bằng nút **Menu**, không đẩy nội dung chức năng ra ngoài màn hình. Khi mở, menu cuộn riêng; chuyển trang thì trở về trạng thái thu gọn. Desktop giữ sidebar theo nhóm.
- Hai trang đăng nhập tách luồng sử dụng, **không thay thế bảo mật phía server**. Giấu đường dẫn không bảo vệ được tài khoản đã lộ mật khẩu.

## 2. Thành viên 1 đã bổ sung trên nhánh cập nhật

### Tài khoản và phân quyền

- Đăng nhập, đăng xuất, đổi mật khẩu; từ chối sai mật khẩu, tài khoản khóa/ngừng sử dụng, nhân viên nghỉ việc và truy cập chưa đăng nhập. Giữ khóa tạm 15 phút sau 5 lần sai mật khẩu.
- Gộp cấp tài khoản, sửa thông tin đăng nhập, khóa/mở khóa và đặt lại mật khẩu vào hồ sơ **Nhân viên**. Không còn hai màn hình chọn lại quyền riêng cho cùng một nhân viên.
- Mỗi nhân viên có **một vai trò công việc**, ánh xạ sang một vai trò Identity với bộ quyền cố định. Admin đổi vai trò trong hồ sơ; phiên cũ bị thu hồi và nhân viên phải đăng nhập lại. Server không nhận quyền tùy ý từ dữ liệu gửi lên.
- Khách được sửa thông tin của mình; Admin sửa thông tin của mình và tài khoản khách/nhân viên được quản lý. Nhân viên không tự sửa hồ sơ hay quyền, nhưng vẫn tự đổi mật khẩu.
- Admin quản lý tài khoản khách: tìm kiếm, sửa thông tin, khóa/mở khóa, đặt lại mật khẩu. Không đổi khách thành nhân viên hoặc xóa lịch sử của khách.
- Sửa thông tin nhạy cảm/đặt lại mật khẩu yêu cầu mật khẩu xác nhận của người thao tác. Không xem mật khẩu cũ; đặt lại thu hồi phiên cũ và **không tự mở khóa**. Tài khoản Admin khác không nằm trong luồng đặt lại mật khẩu này.
- Kiểm tra quyền thật ở controller/server, không chỉ menu. Các trang nhân viên hiển thị nhãn vai trò nhỏ; đăng nhập chuyển tới đúng phần việc.

### Sơ đồ bàn và tiếp nhận đặt bàn

- `/SoDoBan` hiển thị ô bàn theo khu vực/tầng, sức chứa, trạng thái hiện tại và lịch trùng trong khoảng đang xếp. Cập nhật mỗi 10 giây; không thêm framework realtime khi chưa cần.
- Tiếp tân/Admin xem yêu cầu chờ và thông tin yêu cầu đang chọn cạnh sơ đồ; trên màn hình nhỏ có nút tới yêu cầu/sơ đồ, không cần đổi trang mất thông tin. Panel chờ hiển thị tối đa 30 yêu cầu gần nhất theo giờ đến; danh sách đầy đủ và lịch sử vẫn ở `/QuanLyDatBan`.
- Bấm mã bàn hoặc **Đặt lịch tại bàn này** để nhập yêu cầu, điền sẵn bàn/khu vực. Lưu chỉ tạo `ChoXacNhan`, chưa giữ bàn; bàn hợp lệ được chọn dự kiến trên sơ đồ, vẫn cần bấm xác nhận. Nếu lịch thay đổi, chọn bàn khác; nhóm đông có thể chọn thêm bàn cùng khu vực.
- Gợi ý một bàn đơn còn lịch phù hợp, đủ sức chứa/VIP; ưu tiên khu vực yêu cầu rồi số chỗ ít dư nhất. Với lịch sắp đến, chỉ gợi ý bàn đang sẵn sàng. **Chọn bàn gợi ý** không tự duyệt. Ghép bàn/vị trí đặc biệt do tiếp tân quyết định; chưa có dữ liệu để tự suy vị trí cửa sổ hoặc bàn sát nhau. Server kiểm tra lại khi xác nhận, không tin gợi ý cũ.
- Admin cấu hình khu vực/tầng và bàn vật lý. Tiếp tân **không tạo bàn vật lý**: nhận yêu cầu qua điện thoại/tại quầy, nhập thông tin khách rồi xếp vào các bàn đã có. Khách vãng lai không bị ép tạo tài khoản.
- Khách đã đăng nhập gửi yêu cầu ngày giờ, số người, khu vực ưu tiên. Server gắn đúng khách hiện tại; lịch sử, xác nhận gửi yêu cầu và đánh giá chỉ truy cập đặt bàn thuộc khách đó, không suy quyền từ số điện thoại/mã đặt bàn gửi lên.
- Tiếp tân/Admin chọn một hoặc nhiều bàn cùng khu vực: kiểm tra sức chứa, VIP, lịch chồng nhau và trạng thái sử dụng. Xác nhận/xếp/nhận/hủy dùng chung logic, transaction và kiểm soát phiên bản dữ liệu; hai yêu cầu đồng thời không chiếm cùng bàn cùng lịch.
- Nhận khách: lịch đã xác nhận, đã xếp bàn, trong thời gian nhận và bàn sẵn sàng; các bàn ghép chuyển sang `DangPhucVu`. Hủy một lịch tương lai không giải phóng bàn của lượt khách đang phục vụ.
- **Bồi bàn chủ động bấm Kết thúc phục vụ** sau khi thanh toán và xử lý xong các món. Hệ thống không theo dõi/đoán khách đã rời nhà hàng. Thanh toán không tự chuyển bàn thành trống.
- Chỉ Admin/Bồi bàn thao tác kết thúc/dọn bàn cả trên UI và server; Tiếp tân tạo, xếp và nhận khách, không dùng thao tác phục vụ này.
- Kết thúc phục vụ chuyển toàn bộ bàn ghép sang `CanDon`; từng bàn bấm Dọn xong mới về `SanSang`. Danh sách đặt bàn chuyển lượt đã kết thúc sang lịch sử, không còn ghi đang phục vụ.
- Nếu lượt khách chưa gọi món, không có tiền/chi tiết món phải thu, cho phép kết thúc và hủy bill trống; không ép tạo giao dịch thanh toán giả.
- Migration mới thêm tầng, khu vực ưu tiên và thời điểm kết thúc lượt phục vụ, không xóa bảng/dữ liệu cũ. Chỉ khép lịch sử cũ đủ điều kiện rõ ràng: quá giờ, bill đã trả/hủy và không còn bàn đang phục vụ; dữ liệu đang phục vụ không bị tự khép vì đã trả tiền.

### Phân chia thực đơn — chốt ngày 02/10

- **Bỏ hai role riêng `ThucDon` và `DanhMucMon`**. Chỉ có 6 vai trò nội bộ gồm Admin, Tiếp tân, Phục vụ, Thu ngân, Quản lý bếp, Kho; thêm Khách hàng là 7 role đăng nhập.
- `Bep` là **Quản lý bếp/điều phối**, không chỉ người trực tiếp nấu. Có hai màn hình tách biệt: `/Bep` chỉ các món đang gọi (không tiền, không danh sách toàn thực đơn); `/MonAn` chuẩn bị món lẻ/thức uống, nội dung kỹ thuật, size và công thức cơ sở.
- **Admin** quản lý danh mục, giá từng size, combo và trạng thái kinh doanh. Bếp không tạo/sửa combo, xóa món, đặt giá hoặc tự duyệt mở bán. Không chỉ ẩn ô: server bỏ qua giá/trạng thái/duyệt do Bếp giả gửi lên; endpoint xóa/danh mục và combo bị chặn thật.
- Bếp tạo/sửa → `DaDuyet=false` → Admin kiểm tra, nhập giá các size đang dùng > 0 và chọn **Duyệt mở bán**. Bếp sửa món đã duyệt thì cần duyệt lại; giữ giá cũ nhưng tạm ẩn món khỏi web khách. Dòng món/giá chốt trên bill cũ không bị sửa theo thực đơn.
- `DaDuyet` và `TrangThai` là hai việc khác nhau: tạo mới luôn `DangPhucVu`, nhưng món chưa duyệt **chưa được mở bán**. Danh sách quản lý có nhãn/bộ lọc Chờ duyệt, không tính món chờ duyệt vào số món đang phục vụ.
- Trang chủ và URL chi tiết công khai đều kiểm tra duyệt, danh mục, trạng thái và giá hợp lệ. Combo có món thành phần chưa duyệt/ngừng kinh doanh cũng không được hiển thị công khai. TV2 phải giữ cùng điều kiện khi ghi order, không tin việc món đã có trên màn hình từ trước.
- Cho đổi sang `TamHet` dù món còn nằm trên hóa đơn chưa thanh toán: không sửa/hủy dòng món, giá chốt, trạng thái bếp hay trạng thái thanh toán cũ. Chặn nhận order mới cho món tạm hết là trách nhiệm của luồng gọi món tiếp theo.
- Chuyển `NgungKinhDoanh` phải nhập lý do; chỉ Admin thao tác. Server còn chặn khi món có dòng chưa hủy trên bill chưa thanh toán/thanh toán một phần; không đồng nhất điều kiện này với `TamHet`. Combo chọn món lẻ có sẵn, không nhập lại nguyên liệu thô. Công thức theo size và size của món thành phần combo **chưa hoàn tất**, xem bàn giao ở mục 5.
- Quyết định này thay yêu cầu cũ “chỉ Admin chuẩn bị toàn bộ thực đơn”: Admin vẫn quyết định kinh doanh/giá; Quản lý bếp chuẩn bị chuyên môn. Nhóm cần cập nhật đặc tả/ma trận quyền theo hướng đã chốt, không khôi phục hai role cũ.

## 3. Vai trò hiện tại

| Vai trò | Phần được dùng/quản lý | Không được làm |
| --- | --- | --- |
| Admin | Quản lý nhân viên/tài khoản khách, thực đơn/danh mục, cấu hình bàn/khu vực, giám sát nghiệp vụ và xem/in bill | Không xem mật khẩu cũ; không tự đóng dấu thanh toán online thành công |
| Khách hàng | Trang công khai, tài khoản của mình, gửi/xem yêu cầu đặt bàn của mình, đánh giá đủ điều kiện | Không vào quản trị hoặc xem lịch của khách khác |
| Tiếp tân | Nhận yêu cầu đặt bàn, tạo yêu cầu cho khách, xếp/nhận/hủy lịch và sơ đồ bàn | Không tạo bàn vật lý, sửa thực đơn/giá hoặc thu tiền |
| Bồi bàn | Sơ đồ bàn, kết thúc phục vụ và xác nhận dọn xong | Không sửa cấu hình bàn, giá, công thức hoặc xác nhận thu tiền |
| Thu ngân | Xem/in bill, xác nhận tiền mặt qua luồng hiện có | Không sửa giá thực đơn/công thức; không xác nhận online chỉ từ nút bấm/ảnh chuyển khoản |
| Quản lý bếp (`Bep`) | KDS món đang gọi; màn hình riêng chuẩn bị thông tin món lẻ/thức uống, size, công thức cơ sở và gửi duyệt | Không tiền trên màn hình bếp, không đặt giá/duyệt/xóa món, không CRUD danh mục/combo |
| Kho | Danh mục nguyên liệu, ngưỡng cảnh báo, xem tồn từ chứng từ và cảnh báo Cần bổ sung tại danh sách | Không tự quyết định công thức món hoặc giá bán |

### Những phần chỉ Admin quản lý — không phải đang thiếu người phụ trách

| Phần chỉ Admin được sửa | Lý do và ranh giới với nhân viên |
| --- | --- |
| Nhân viên, tài khoản khách, khóa/mở khóa và đặt lại mật khẩu | Kiểm soát quyền và thông tin tài khoản; nhân viên chỉ tự đổi mật khẩu, khách tự sửa hồ sơ của mình |
| Danh mục món | Thống nhất cách phân loại. Bếp chọn danh mục đã có khi chuẩn bị món, không cần một nhân viên/role Danh mục riêng |
| Giá theo size, combo, duyệt mở bán, ngừng kinh doanh/xóa món | Quyết định kinh doanh. Bếp làm thông tin kỹ thuật, size và công thức rồi gửi duyệt; không được sửa giá |
| Thêm/sửa/xóa bàn vật lý, khu vực và tầng | Cấu hình nhà hàng. Tiếp tân xếp khách vào bàn có sẵn; Bồi bàn xử lý phục vụ/dọn bàn, không tự tạo bàn vật lý |

Bộ quyền cố định theo vai trò công việc, không có bộ quyền thứ hai để chọn lại. Không thêm role hoặc mở quyền chỉ để chia bớt menu của Admin. Admin giám sát không đồng nghĩa làm thay mọi thao tác: xác nhận thu tiền mặt hiện thuộc Thu ngân; công thức thuộc chuyên môn Quản lý bếp; chứng từ và tồn thuộc Kho. Chưa có chức năng gọi món/nhập–xuất hoàn chỉnh là phần cần phát triển, không phải quyền đã giao hết cho Admin.

## 4. Tài khoản để nhóm chạy thử

Sáu tài khoản nội bộ từ SQL của nhóm gồm Admin và 5 loại nhân viên vẫn dùng mật khẩu riêng: xem **README.md**. Seed không còn tạo hai tài khoản thực đơn/danh mục. Pull code không mang theo database; mỗi máy phải cấu hình connection string và nạp dữ liệu/khởi tạo tài khoản trên đúng database local.

Nếu cần bộ demo đủ cả Khách và 6 vai trò nội bộ, chạy từ thư mục repo sau khi cấu hình database:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:AuthBootstrap__DemoPassword='Demo@2026!'
dotnet run --project RestaurantManagement.Web -- --init-demo-accounts
dotnet run --project RestaurantManagement.Web --launch-profile http
```

| Vai trò | Email đăng nhập | Mật khẩu demo công khai |
| --- | --- | --- |
| Admin | `admin.demo@example.test` | `Demo@2026!` |
| Khách hàng | `khach.demo@example.test` | `Demo@2026!` |
| Tiếp tân | `tieptan.demo@example.test` | `Demo@2026!` |
| Bồi bàn | `boiban.demo@example.test` | `Demo@2026!` |
| Thu ngân | `thungan.demo@example.test` | `Demo@2026!` |
| Quản lý bếp | `bep.demo@example.test` | `Demo@2026!` |
| Kho | `kho.demo@example.test` | `Demo@2026!` |

Lệnh chỉ chạy ở Development, không tự chạy khi mở Web. Chạy lại không đổi mật khẩu đã có hay ghi đè hồ sơ trùng/sai; nếu tài khoản đã đổi mật khẩu, dùng mật khẩu mới hoặc luồng Admin đặt lại. Không dùng các mật khẩu công khai này khi triển khai thật.

## 5. Bàn giao còn thiếu — đối chiếu code, không làm lại phần đã có

Phân công hiện tại: **TV1 — sơ đồ/tiếp nhận bàn + nền tài khoản; TV2 — gọi món, đơn hàng, thanh toán; TV3 — nhà cung cấp, nhập/xuất/tồn kho**. Phần thực đơn từng là TV3 ở đợt trước; nhóm cần phân người nối tiếp các góp ý chưa hoàn tất dưới đây, không mặc nhiên coi TV3 đã phải làm cả hai mảng cùng lúc.

| Phần | Đã có trên bản nhóm/nhánh này | Cần tiếp tục |
| --- | --- | --- |
| Trang khách | Trang chủ, thực đơn/chi tiết, yêu cầu đặt bàn và lịch sử có kiểm tra sở hữu | Hoàn thiện nội dung nhà hàng và trải nghiệm đặt bàn khi nhóm có nội dung thật; không làm lại cổng đăng nhập |
| Nguyên liệu theo size — phần thực đơn đợt trước | Đã có món, các size/giá và công thức chung của món; Bếp chuẩn bị món, Admin duyệt bán | Chưa nhập/lưu lượng nguyên liệu riêng cho size nhỏ, vừa, lớn. Cần bổ sung như ví dụ ngay dưới bảng |
| Size của món trong combo | Đã chọn được món lẻ, số lượng và giá bán trọn combo | Chưa chọn được size của từng món trong combo. Cần bổ sung như ví dụ ngay dưới bảng |
| Trạng thái/nhãn món | Tạo `DangPhucVu`, duyệt mở bán riêng, `TamHet` không đổi order cũ, ngừng kinh doanh phải có lý do | Món mới/nổi bật chưa có tiêu chí chính thức: cần chốt số ngày “mới”, tiêu chí bestseller/đặc sản; chưa tự thêm thuật toán xếp hạng |
| Gọi món — TV2 | Hóa đơn/chi tiết và trạng thái chế biến đã có trong CSDL; tên món/giá có dữ liệu chốt lúc bán | Luồng mobile từ bàn đang phục vụ: món + size + lượng + ghi chú → gửi bếp; kiểm tra trạng thái món/giá ở server, giữ tổng bill đúng. Bổ sung snapshot tên size và dùng trên bill/KDS; không đọc tên size đang sửa trong thực đơn làm lịch sử. Tiếp tục đơn mang đi/giao hàng theo đề cương |
| Bếp — TV2 | KDS lọc món chờ/đang chế biến, hiện bàn/lượng/ghi chú, không tiền; không hiển thị/cập nhật món trên bill đã hủy | FIFO đang theo thời điểm lập bill, chưa đúng khi gọi thêm món trên bill cũ. Cần thời điểm từng lần gọi/dòng order và hiển thị size. Bồi bàn thấy món sẵn sàng và xác nhận đã mang ra (`SanSang` → `DaPhucVu`). KDS không gộp với màn hình chuẩn bị món của Quản lý bếp |
| Thanh toán — TV2 | Thu ngân xem/in và xác nhận tiền mặt qua server | Bổ sung hình thức khác theo đề cương; chỉ xác nhận online từ kết quả đáng tin cậy ở server. Không dùng ảnh chuyển khoản hay nút khách bấm để tự coi là đã thu tiền |
| Kho — TV3 | CRUD nguyên liệu/đơn vị/ngưỡng cảnh báo, nhãn Cần bổ sung khi tồn ≤ ngưỡng; dữ liệu nhà cung cấp/chứng từ và tồn tính từ nhập trừ xuất đã ghi sổ | Hoàn thiện UI/nghiệp vụ nhà cung cấp, nhập/xuất, xử lý cảnh báo, thanh lý và báo cáo. Chưa tự trừ kho theo món khi định mức từng size chưa được chốt |
| Báo cáo Word — cả nhóm | Luồng/vai trò hiện tại ghi trong file này | Cập nhật sơ đồ, đặc tả, ma trận quyền, luồng bàn/bếp, công thức giá combo và các trạng thái theo code thật; không ghi phần bàn giao còn thiếu là đã làm |

### Hai góp ý thực đơn cần làm tiếp — giải thích bằng ví dụ

**Nguyên liệu theo size:** Cùng một món nhưng phần nhỏ và phần lớn sẽ dùng lượng nguyên liệu khác nhau. Ví dụ Gỏi size S dùng 100g tôm, size L dùng 200g tôm. Hiện code mới lưu công thức chung cho món Gỏi, chưa phân biệt lượng theo size. Người tiếp tục phần thực đơn cần sửa form: chọn các nguyên liệu của món một lần, rồi nhập lượng riêng cho từng size; bấm Lưu món mới lưu toàn bộ. Bếp nhập size/công thức, Admin nhập giá và duyệt bán. Đây là dữ liệu để sau này tính đúng lượng xuất kho, không phải Kho tự quyết định công thức nấu.

**Size của món trong combo:** Combo phải ghi rõ gồm món gì, size nào và bao nhiêu phần. Ví dụ Combo gia đình gồm 1 Gỏi size L và 2 Canh size S. Hiện code chọn được món và số lượng nhưng chưa chọn size, nên chưa biết phải làm phần nhỏ hay lớn. Người tiếp tục phần thực đơn cần thêm lựa chọn Món → Size → Số lượng cho từng dòng combo. Không nhập lại nguyên liệu vì đã lấy từ công thức của món/size được chọn; giá bán trọn combo vẫn do Admin đặt, không bắt buộc bằng tổng giá từng món lẻ.

Hai mục này là **phần thực đơn chưa hoàn tất theo góp ý của cô**, không phải yêu cầu làm lại tài khoản/phân quyền hoặc sơ đồ bàn. Nhóm phân người tiếp tục thực đơn; không tự giao toàn bộ việc này cho TV3 khi TV3 đang làm kho. Khi triển khai phải bổ sung cách lưu dữ liệu tương ứng, không chỉ thêm ô trên giao diện; công thức/combo cũ thiếu size cần được kiểm tra và chọn lại, không tự đoán.

### Hướng dẫn triển khai cho người hoặc AI nhận phần thực đơn

**Trạng thái hiện tại:** Đây là yêu cầu cần làm tiếp, không phải mô tả tính năng đã hoàn thành. Đã có size, giá theo size và duyệt mở bán. Chưa có lượng nguyên liệu theo từng size, chưa có size của từng món trong combo. Không ghi “đã xong” hai mục này khi chỉ mới chỉnh form hoặc sửa tài liệu.

**Duyệt mở bán, không phải “duyệt giá” riêng:** Bếp tạo/sửa món thì món chuyển về Chờ duyệt. Admin nhập hoặc kiểm tra giá của các size rồi chọn Duyệt mở bán cho món. Hệ thống kiểm tra giá hợp lệ, không tự đánh giá giá đó đắt/rẻ hay tạo một quy trình xin duyệt giá riêng. Cách mô tả đúng là: “Đã có size, giá từng size và duyệt mở bán; còn thiếu lượng nguyên liệu theo từng size và size của món thành phần combo.”

**1. Đọc đúng chỗ trước khi sửa**

- `RestaurantManagement.API/Models/Entities.cs`: `MonAnSize` đã có tên/giá; `DinhMucMon` hiện lưu `MonAnId + NguyenLieuId + SoLuong`, tức lượng chung cho món; `ChiTietCombo` chỉ có món thành phần và số lượng, chưa có size.
- `RestaurantManagement.API/Data/RestaurantDbContext.cs`: khóa định mức đang theo món/nguyên liệu, khóa combo theo combo/món. Không chỉ thêm thuộc tính rồi quên quan hệ, khóa và các truy vấn dùng khóa cũ.
- `RestaurantManagement.Web/Models/MonAnViewModels.cs`, `Controllers/MonAnController.cs`, `Views/MonAn/Form.cshtml`, `wwwroot/js/dish-form.js`: form hiện có danh sách size, một danh sách định mức chung và danh sách món trong combo. Sửa trên các phần này, giữ giao diện nhóm và cách kiểm tra quyền hiện tại; không tạo ứng dụng hoặc bộ form song song.
- Tìm các chỗ đọc/ghi `DinhMucMon`, `ChiTietCombo`, `ChiTietComboSnapshot` trong seed, trang khách, bill, đặt trước, KDS và kiểm thử. Đặc biệt `HomeController` phải hiển thị/kiểm tra size của thành phần combo; các kiểm tra xóa size/xóa món cũng phải tính quan hệ mới. Phân biệt phần cần sửa trong lượt này với điểm cần bàn giao cho TV2/TV3; không nối trừ kho tự động chỉ vì đã thêm công thức.

**2. Làm nguyên liệu theo size thành một luồng đầy đủ**

- Thông tin chung vẫn gồm tên món, danh mục có sẵn, ảnh và mô tả. Chọn tập nguyên liệu của món một lần; mọi size đang dùng đều có cùng tập nguyên liệu này, nhưng lượng của mỗi nguyên liệu khác nhau.
- Với từng size, nhập tên size và lượng của các nguyên liệu đã chọn. Ví dụ Gỏi S dùng 100g tôm, Gỏi L dùng 200g tôm. Lượng lưu phải theo đơn vị của nguyên liệu: nếu đơn vị là kg thì hai lượng tương ứng là 0,1kg và 0,2kg, không lưu số 100 vào cột đang hiểu là kg. Hiện chưa có hệ thống đổi đơn vị tự động; phải hướng dẫn đơn vị ngay trên form, không tự suy đoán.
- Bếp nhập nội dung/size/công thức, không nhập giá. Admin nhập giá của từng size và duyệt mở bán. Giữ việc Bếp sửa món thì món cần được duyệt lại; giá cũ không bị dữ liệu Bếp gửi lên ghi đè, món chưa duyệt không public.
- Các size đang nhập nằm trong bảng tạm trên form: thêm/sửa/xóa trước khi lưu, chưa ghi từng dòng xuống DB. Khi bấm Lưu món, server kiểm tra toàn bộ rồi lưu món, size và định lượng trong một transaction; lỗi ở một phần thì không giữ lại món hoặc size lưu dở. Tái sử dụng transaction, RowVersion, chống giả mạo form và xử lý ảnh hiện có.
- Dữ liệu đích phải xác định được **mỗi size → mỗi nguyên liệu → lượng sử dụng**. Cập nhật cả model, quan hệ/khóa, migration/snapshot, dữ liệu gửi từ form và truy vấn đọc lại. Tên field/index khi thêm hoặc xóa dòng bằng JS phải đúng để server nhận đủ các size và lượng tương ứng.
- Kiểm tra phía server: size thuộc đúng món; nguyên liệu tồn tại và hợp lệ; lượng của nguyên liệu đã chọn lớn hơn 0; không lặp nguyên liệu trong cùng size; không nhận size ID của món khác; tập nguyên liệu giữa các size đang dùng không bị lệch. Không chỉ kiểm tra bằng JavaScript.

**3. Làm size của món trong combo**

- Chỉ Admin cấu hình combo. Mỗi dòng chọn một món lẻ/thức uống đã có, sau đó chọn size của chính món đó và số lượng nguyên. Ví dụ 1 Gỏi L + 2 Canh S. Không chọn combo làm thành phần của combo khác, không nhập nguyên liệu thô vào form combo.
- Khi đổi món ở một dòng, phải chọn lại size hợp lệ; server từ chối size thuộc món khác, size ngừng dùng, lượng ≤ 0 hoặc món không đủ điều kiện bán. Giá trọn gói của combo do Admin đặt; giá các món lẻ chỉ phục vụ tham khảo nếu cần, không tự thay giá combo thành tổng giá lẻ.
- Lưu đầy đủ món, size và số lượng của từng dòng. Rà lại khóa/truy vấn cũ chỉ theo `MonAnId`: không để việc thay size sửa nhầm dòng hoặc mất thành phần. Nếu cho phép cùng một món với hai size khác nhau thì cả khóa và form phải phân biệt được hai dòng; không mở lựa chọn đó trên UI khi server vẫn coi là dòng trùng.
- Màn hình chi tiết combo phải đọc lại đúng size đã chọn. Bàn giao TV2 cùng cấu trúc này để lúc gọi combo biết cần làm món gì/size nào; khi công thức theo size đã hoàn tất, nhu cầu nguyên liệu lấy từ size thành phần × số phần × số combo, không tạo công thức nguyên liệu độc lập cho combo. Việc xuất kho thật vẫn thuộc lượt tích hợp kho.

**4. Giữ dữ liệu cũ và kiểm tra trước khi bàn giao**

- Thêm migration, không chạy lại `InitialSchema.sql`, xóa database hoặc xóa định mức/combo cũ để “làm mới”. Giữ công thức cũ để đối chiếu. Một công thức chung không chứng minh S/L dùng cùng lượng; combo cũ thiếu size cũng không chứng minh phải chọn size đầu tiên. Dữ liệu chưa xác định phải được người phụ trách kiểm tra/chọn lại; không tự đoán rồi dùng để trừ kho.
- Không sửa ngược hóa đơn, giá chốt, đặt trước hoặc chứng từ kho cũ khi cập nhật thực đơn. Giữ snapshot thành phần combo đã chốt trong đơn cũ; phần tên size trên bill/KDS vẫn là điểm TV2 phải nối như bảng bàn giao. Nếu cần bỏ cột/bảng cũ sau chuyển đổi, phải xác minh không còn nơi sử dụng và xin chốt riêng, không làm ngầm trong lượt bổ sung này.
- Cập nhật seed và kiểm thử theo dữ liệu mới; không xóa ca phân quyền, dữ liệu cũ hoặc tranh chấp chỉ để test đạt. Cả `DbSeeder` và `PopulateRestaurantData.sql` còn ghi công thức/combo kiểu cũ. SQL còn tạo phiếu xuất minh họa từ công thức theo món; đó là dữ liệu demo, không phải tính năng tự trừ kho đang chạy. Khi đổi seed, chạy lại không được đổi chứng từ đã ghi sổ. Kiểm tra tối thiểu: lưu/mở lại Gỏi S và L vẫn đúng lượng khác nhau; sửa lượng S không đổi L; form lỗi không lưu dở; size/nguyên liệu giả bị từ chối; Bếp không đặt giá/duyệt/tạo combo; Admin duyệt rồi khách mới xem được món; combo lưu đúng món/size/số lượng; nâng schema giữ dữ liệu lịch sử.
- Hoàn thành thì cập nhật đúng bảng “Đã có/Cần tiếp tục” phía trên, ngày và kết quả kiểm chứng. Ghi rõ TV2/TV3 cần dùng dữ liệu nào và phần nào chưa tích hợp. Không ghi trọn luồng gọi món/xuất kho đã xong chỉ vì lưu được định lượng và combo.

### Thứ tự nối tiếp để demo được trọn luồng

1. **TV2 — bàn → gọi món → bếp → phục vụ → thanh toán → kết thúc/dọn bàn.** Hiện Bếp chỉ cập nhật tới `SanSang`, chưa có thao tác Bồi bàn xác nhận `DaPhucVu`. Với bill có món, đây là điểm còn thiếu để kết thúc bàn qua giao diện, dù đã thu tiền. Phải bổ sung bước phục vụ thật; không bỏ kiểm tra của `TableService`, tự đánh dấu món đã mang ra khi bếp làm xong hoặc sửa thẳng CSDL để coi là demo hoàn chỉnh.
2. **Người tiếp tục thực đơn — làm hai mục ở ví dụ trên trước khi nối kho.** Kiểm tra lưu và mở lại món vẫn đúng lượng của từng size; combo vẫn đúng món/size/số lượng đã chọn. Không coi đã xong chỉ vì form có thêm ô. Các yêu cầu chuyển dữ liệu và giữ lịch sử nằm ở mục 6.
3. **TV3 — chứng từ nhập/xuất và kiểm soát tồn.** Luồng gọn dự kiến: Kho thấy thiếu → lập nhu cầu nhập → Admin duyệt → người phụ trách liên hệ nhà cung cấp → Kho nhận hàng/ghi chứng từ. Đề xuất/duyệt mua chưa có trên bản hiện tại; nhóm chốt phạm vi trước khi bổ sung, không cần hệ thống mua hàng/email tự động khi đề cương chưa yêu cầu.

Chưa chốt: số ngày để gọi là món mới, tiêu chí món nổi bật và hình thức thanh toán ngoài tiền mặt sẽ triển khai thật. Chưa làm OTP/MFA hay cơ chế giấu đường dẫn như một lớp bảo mật. Nhóm thống nhất trước rồi cập nhật đúng mục, không thêm tính năng giả để lấp chỗ trống.

## 6. Điểm tích hợp cần giữ

- TV2 dùng `DatBan.Id` và các dòng `ChiTietDatBan` để biết bàn của lượt phục vụ; một lượt có thể ghép nhiều bàn. Khách đến trực tiếp dùng yêu cầu nội bộ, không bắt buộc `KhachHangId`.
- Khi nhận bàn, nhân viên có hồ sơ hợp lệ được gắn vào bill theo luồng hiện có. Admin không có hồ sơ nhân viên vẫn nhận bàn được, nhưng không tạo bill với FK nhân viên giả; nhân viên ghi order phải tạo/gắn bill hợp lệ khi khách gọi món. Tránh tạo hai bill chỉ để in hoặc vì refresh trang.
- Chỉ Thu ngân xác nhận đã thu tiền mặt. Bồi bàn kết thúc phục vụ sau khi thanh toán và xử lý xong món; các bàn ghép cùng chuyển cần dọn. Không giải phóng bàn ngay khi Thu ngân bấm thanh toán.
- Dùng cùng `TableService` cho xếp/nhận/hủy/kết thúc bàn, không viết nhánh cập nhật trạng thái riêng bỏ qua transaction/RowVersion. Chuẩn ngày giờ phục vụ theo Việt Nam (UTC+7).
- Các endpoint nghiệp vụ hiện nằm trong Web và được kiểm tra quyền server. Nếu sau này tách API nghiệp vụ, phải cấu hình xác thực/quyền/sở hữu riêng; không coi việc ẩn menu Web là đã bảo vệ API.
- Công thức và size thành phần combo là thay đổi dữ liệu liên quan nhiều phần: thống nhất migration, cách giữ lịch sử/chuyển định lượng cũ, seed và kiểm tra trước khi ghép. Bếp cung cấp công thức; không tự mở quyền sửa giá cho Bếp/Kho.
- Khi TV2 ghi order, lấy món/size/giá từ DB, kiểm tra `DaDuyet`, `DangPhucVu`, danh mục/size đang dùng, số lượng và thành phần combo. Snapshot tên/size/giá tại lúc gọi; đổi thực đơn sau đó không cập nhật ngược bill. Hiện bill có tên món/giá chốt nhưng tên size còn đọc từ thực đơn, chưa có snapshot riêng; phải bổ sung cột và cách chuyển lịch sử không đoán dữ liệu. Gọi thêm món có thời điểm riêng để FIFO đúng; một request lặp không tạo thêm bill/nhân đôi món ngoài ý muốn.
- Khi TV3 làm kho, chỉ chứng từ `DaGhiSo` mới làm thay đổi tồn. Xuất phải kiểm tra lượng khả dụng, đúng đơn vị và lô nhập, tránh xuất âm khi hai người cùng thao tác. Không trừ kho lần nữa khi bếp chỉ đổi trạng thái món. Công thức từng size/combo phải được chốt trước khi nối tự động xuất chế biến.
- Chưa bổ sung OTP/MFA, phân quyền tùy biến, engine xếp hạng món hay thanh toán online giả. Chỉ làm khi có nhu cầu/điều kiện triển khai rõ ràng.

### Các điểm code cần đọc trước khi tích hợp

| Điểm giữ chung | File/nhóm file | Tránh sửa đè |
| --- | --- | --- |
| Vai trò, phiên, khởi tạo | `Security/AppRoles.cs`, `ActiveAccountMiddleware.cs`, `AuthSetup.cs`, `Program.cs` | Không khôi phục role cũ hoặc cấp quyền từ checkbox tùy ý; CSS/JS phải tải được trước đăng nhập |
| Cổng khách/nội bộ, hồ sơ | `AccountController`, `AdminController`, `NhanVienController`, `TaiKhoanKhachHangController` | Giữ phân biệt 2 cổng, sở hữu hồ sơ và thu hồi phiên khi khóa/đổi role/reset |
| Menu/UI | `Views/Shared/_ManagementNavigation.cshtml`, `_AdminLayout.cshtml`, `_Layout.cshtml` | Menu và lối tắt dùng chung ma trận; màn hình nhỏ phải còn thấy nội dung, không chỉ menu; không thay UI master bằng khung auth cũ |
| Trạng thái và lịch bàn | `Services/TableService.cs`, `SoDoBanController`, `QuanLyDatBanController` | Dùng service chung/transaction/RowVersion; thanh toán khác với kết thúc phục vụ, lịch tương lai khác trạng thái bàn hiện tại |
| Chuẩn bị và bán món | `MonAnController`, `HomeController`, `Models/MonAnViewModels.cs` | Bếp không sửa giá/duyệt; món nháp không public; định mức hiện tại còn theo món, không giả định đã theo size |
| Thanh toán/KDS | `HoaDonController`, `BepController` | Không đổi bill cũ khi sửa món; thanh toán chỉ theo quyền và nguồn đáng tin; món bill hủy không tiếp tục chế biến |
| CSDL | `RestaurantManagement.API/Models/Entities.cs`, `Data/RestaurantDbContext.cs`, `Migrations`, `Scripts` | Đổi model phải có migration/snapshot và cập nhật seed; không chạy lại InitialSchema trên DB đang có dữ liệu để “sửa nhanh” |
| Kiểm chứng | `tests/Management.SmokeTests` | Chạy trên DB tạm; thêm ca cho hành vi mới, không xóa ca phân quyền/race chỉ để test xanh |

### Chuyển database cũ và dọn role đã bỏ

- Migration `TableFloorAndReservationLifecycle` thêm tầng/khu vực ưu tiên/thời điểm kết thúc; migration `MenuApprovalAndKitchenOwnership` chỉ thêm `DaDuyet` (món cũ mặc định đã duyệt) và `LyDoNgung`. Không xóa món, size, bill hoặc công thức cũ.
- Role/tài khoản mẫu đã bỏ không được tự nâng thành Admin/Bếp. Nhánh này không tạo lại chúng và không cho đăng nhập bằng role đã bỏ.
- Dọn database Development cũ bằng lệnh bên dưới **sau khi kiểm tra connection string và sao lưu nếu có dữ liệu cần giữ**. Lệnh kiểm tra tài khoản dùng chung, lịch bàn, hóa đơn, phiếu nhập/xuất; nếu có liên kết thì dừng toàn bộ, không xóa lịch sử. Khi đó Admin đổi phân công hồ sơ thay vì xóa.

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project RestaurantManagement.Web -- --remove-retired-menu-roles
```

- Lệnh chỉ dọn role/tài khoản/hồ sơ mẫu cũ không có lịch sử; chạy lại không tác động tài khoản khác. Không tự dọn lúc khởi động, không chạy ở Production. Database preview local đã được dọn theo yêu cầu ngày 02/10; Git không đồng bộ việc xóa sang database máy khác.

## 7. Kiểm chứng ngày 02/10/2026

- Build bộ kiểm tra và Web: **0 cảnh báo, 0 lỗi**.
- Kiểm thử tích hợp bản cuối: **PASS: 481 HTTP/database checks**, trên database tạm đã được xóa sau khi chạy. Có kiểm tra đúng/sai cổng và quyền của 7 role hiện tại; menu; khóa/đổi mật khẩu/thu hồi phiên; sở hữu lịch khách; gợi ý/xếp bàn đồng thời và kết thúc/dọn bàn; Bếp giả gửi giá/duyệt; món chờ duyệt không public; bill hủy không vào KDS; phiên bản thanh toán sai; dọn role cũ có/không có lịch sử. Số ca thay đổi theo bộ vai trò và hành vi hiện tại, không dùng số ca cũ làm tiến độ chức năng.
- Kiểm tra migration sơ đồ bàn trước đó (từ 2 lên 3 migrations) trên dữ liệu SQL của nhóm: giữ nguyên **18 bàn, 16 món, 8 đặt bàn, 4 hóa đơn** và checksum thông tin đặt bàn; khép đúng 4 lượt lịch sử đủ điều kiện. Đây không phải bằng chứng riêng về nâng toàn bộ schema từ 2 lên 4 migrations.
- Migration thứ 4 `MenuApprovalAndKitchenOwnership` thêm cột, đã áp dụng trên database kiểm thử/preview. Kiểm tra EF: **No changes have been made to the model since the last migration**. Kiểm tra cú pháp JS sơ đồ/form món và `git diff --check` đạt; không có thao tác xóa database ứng dụng.
- Đã bấm chuyển trang/menu trên trình duyệt desktop và khung nhỏ; menu thu gọn không che nội dung, chuyển trang đóng menu. Nhân viên xem trang công khai không có link đặt bàn của khách. Polling sơ đồ có giới hạn chờ; hai truy vấn danh sách/lịch sử đặt bàn dùng split query tránh nhân dòng khi tải các bảng con. Chưa kiểm thử tải lớn để kết luận mọi vấn đề hiệu năng đã hết.
- Web local dùng database preview riêng. Các ca trạng thái bàn chuẩn bị dữ liệu tương ứng để kiểm tra guard; không có nghĩa đã có UI gọi món/đã mang món/online/kho hoàn chỉnh. Đặc biệt bước Bồi bàn xác nhận món đã phục vụ vẫn bàn giao ở mục 5, không tự đánh dấu đã xong chỉ vì build/test đạt.
