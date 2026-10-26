# Demo toàn bộ website

Bộ dữ liệu mô phỏng cho thực hành, được nạp vào `RestaurantDB` trên `.\SQLEXPRESS`. Không phải đơn hàng, tiền thanh toán hoặc thông tin người thật.

## Mở website và đăng nhập

- Website: **https://localhost:7167/**
- Nội bộ: **https://localhost:7167/admin**
- Khách hàng: **https://localhost:7167/Account/Login**
- Mật khẩu của cả 7 tài khoản `.demo` bên dưới: **`Demo@2026!`**. Tài khoản `.mau` hoặc tài khoản riêng đã có vẫn giữ nguyên mật khẩu.

| Vai trò | Email |
|---|---|
| Quản trị | `admin.demo@example.test` |
| Khách hàng | `khach.demo@example.test` |
| Tiếp tân | `tieptan.demo@example.test` |
| Bồi bàn | `boiban.demo@example.test` |
| Thu ngân | `thungan.demo@example.test` |
| Bếp | `bep.demo@example.test` |
| Kho | `kho.demo@example.test` |

Dùng cửa sổ thường cho Admin, cửa sổ ẩn danh hoặc trình duyệt khác cho Khách/Bếp/Thu ngân để không ghi đè phiên đăng nhập của nhau. Dùng URL HTTPS để cookie đăng nhập hoạt động đúng.

## Kịch bản demo khoảng 15–20 phút

### 1. Trang chủ, thực đơn và tài khoản khách

Mở trang chủ, tìm tên món có/không dấu; xem danh mục, món và size. Đăng nhập `khach.demo@example.test`. Menu **Đơn hàng & tích điểm** đã có lịch sử mua trong bốn tuần, gồm tiền mặt/chuyển khoản/thẻ, giảm giá, đánh giá và các dòng tích điểm. Có giao dịch trong ngày để trang doanh thu không bị trống.

Mở **Voucher & ví của tôi**. Chương trình **Demo toàn website · 10/2026** có các mã:

| Mã trong tháng 10/2026 | Ưu đãi | Điều kiện |
|---|---|---|
| `WEB2610-P10` | Giảm 10% hóa đơn, tối đa 50.000đ | Đơn từ 100.000đ |
| `WEB2610-10K` | Giảm trực tiếp 10.000đ | Đơn từ 50.000đ; mã không giới hạn |
| `WEB2610-50K` | Giảm trực tiếp 50.000đ | Đơn từ 300.000đ |
| `WEB2610-MON20` | Giảm 20% món gỏi áp dụng, tối đa 50.000đ | Chỉ tính món thuộc mã |

Ví khách đã lưu sẵn `P10` và `10K`, chưa dùng. Hai mã còn lại có thể nhập trực tiếp; một số mã đã có lịch sử dùng nên không còn nút thu thập cho cùng khách đó. Đổi tháng chạy demo, tiền tố mã tự đổi theo `WEByyMM`; xem mã thực tế trên `/KhuyenMai`.

### 2. Chương trình cha có nhiều mã con

Đăng nhập Admin → **Chương trình khuyến mãi** → mở **Demo toàn website · 10/2026**. Bốn mã khác nhau nằm trong cùng một form, có thời hạn, số vé, giới hạn lượt, món và điều kiện riêng. Có thêm chương trình hết hạn và sắp diễn ra để trình bày bộ lọc trạng thái. Các mã bắt đầu trước ngày hiện tại và đều dùng được ngay.

### 3. Sơ đồ bàn → POS → KDS → thanh toán

Mở **Sơ đồ bàn**, chọn khu vực **Sảnh demo website**:

- **WEB-01**: khách đang dùng bữa, hóa đơn có món đang nấu/sẵn sàng/chờ chế biến. Bồi bàn gọi thêm món qua POS. Bếp mở KDS, chuyển món sang Sẵn sàng; bồi bàn xác nhận đã phục vụ.
- **WEB-02**: món đã phục vụ, có **100.000đ cọc đã ghi sổ**. Phù hợp demo thanh toán ngay: thu ngân mở hóa đơn có mã kết thúc `-POS2`, chọn voucher trong ví hoặc nhập mã trực tiếp; kiểm tra giảm giá rồi cấn cọc, số tiền còn phải thu. Đơn thuộc khách `khach.demo@example.test`, nên khách cũng có thể chọn mã từ trang đơn của mình.
- **WEB-03…WEB-06**: để trình bày bàn trống và lịch sắp tới; một số bàn đã có lịch cho ngày mai/ngày kia.

Các hóa đơn chuẩn bị trong ngày có tiền tố `WEB-yyyyMMdd`: `-POS1`, `-POS2`, `-MANGDI`, `-GIAOHANG`. Ví dụ ngày 26/10/2026: **`WEB-20261026-POS2`**. Tìm bằng tiền tố `WEB-` ở danh sách đơn/hóa đơn.

Sau xác nhận thanh toán, kiểm tra đơn chuyển Đã thanh toán, điểm được cộng đúng một lần và tiền cọc không dùng lại. QR chỉ phục vụ minh họa giao diện; không cần chuyển tiền thật để demo. Bản ghi demo đã thanh toán hoặc món đã đổi trạng thái sẽ được giữ nguyên khi nạp lại.

### 4. Đặt bàn, cọc và dự trù nguyên liệu

Tiếp tân → **Yêu cầu đặt bàn** hoặc khách → **Lịch sử đặt bàn**. Các lịch tiền tố `WEB-yyyyMMdd-LICH` gồm:

- `LICH1`: chờ xác nhận/xếp bàn.
- `LICH2`: chờ khách nộp cọc giữ bàn.
- `LICH3`: khách đã báo chuyển khoản, đang đợi thu ngân đối chiếu. Mở **Đối chiếu cọc đặt bàn** để ghi nhận.
- `LICH4`, `LICH5`: đã xác nhận, có cọc và món đặt trước theo size, ngày mai/ngày kia. Dùng để xem **Đặt bàn & dự trù nguyên liệu**.
- `LICH6`: đã hủy; `LICH7`: khách không đến. Chọn bộ lọc tương ứng để thấy lịch sử.

Các lịch tự động chờ cọc áp dụng hạn 60 phút như luồng thật. Nếu để lâu, lịch chưa báo chuyển có thể tự hết hạn. Lịch đã báo chuyển tiếp tục đợi nhân viên đối chiếu.

### 5. Khách hàng và tích điểm

Admin → **Quản lý khách hàng** → tìm **Khách thử**. Xem lịch sử đơn, trạng thái món, tổng mua và sổ điểm. Quy tắc hiện tại: **10.000đ sau giảm = 1 điểm**, làm tròn xuống, có tính phần đã trả bằng cọc.

Có thêm ba hồ sơ khách demo chưa có tài khoản để trình bày cấp tài khoản vào hồ sơ cũ. Các tài khoản khác không xem được lịch sử đơn của Khách thử.

### 6. Kho, nhà cung cấp và chứng từ

Đăng nhập Kho → Tồn kho/Nhập kho/Xuất kho/Thanh lý:

- Phiếu `WEB-yyyyMMdd-NHAP`: nhập đã ghi sổ, có lô và hạn sử dụng để đủ nguyên liệu demo.
- Phiếu `WEB-yyyyMMdd-NHAPNHAP`: phiếu nháp để thực hành ghi sổ.
- Các phiếu xuất chế biến gắn với hóa đơn demo; xem ghi chú chứa mã `WEB-`.
- Phiếu `WEB-yyyyMMdd-THANHLY` và phiếu xuất `-HUY`: minh họa thanh lý có chứng từ tương ứng.

Tồn kho thay đổi theo phiếu; chạy nạp lại trong cùng ngày không cộng tồn lần hai. Menu có món **Salad rau sốt mè (chờ duyệt demo)** để Admin/Bếp trình bày luồng duyệt món.

### 7. Báo cáo

Admin → **Tổng quan kinh doanh**, **Doanh thu**, **Món bán chạy**, **Hiệu quả khuyến mãi**. Chọn khoảng **28 ngày vừa qua** để thấy nhiều điểm trên biểu đồ. Bộ demo có 29 hóa đơn đã thanh toán (28 ngày trước + hôm nay), snapshot ưu đãi và đánh giá, không cộng điểm giả vào hóa đơn cũ trước khi nạp.

## Nạp lại hoặc chuẩn bị vào ngày khác

Chạy tại thư mục repository:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AuthBootstrap__DemoPassword = 'Demo@2026!'
$env:Logging__LogLevel__Default = 'Warning'
dotnet run --project RestaurantManagement.Web --no-launch-profile -- --init-website-demo
dotnet run --project RestaurantManagement.Web --no-build --launch-profile https
```

Lệnh đầu nâng schema và bổ sung dữ liệu mẫu còn thiếu. Không xóa database, không đặt lại hóa đơn đã thanh toán, không hoàn tác món đã phục vụ và không đổi mật khẩu tài khoản đang có. Nếu tài khoản `.demo` đã đổi mật khẩu hoặc vai trò, lệnh sẽ báo để đối chiếu thay vì tự ghi đè.

Mỗi ngày tạo thêm nhóm đơn/lịch ngày đó và thêm phần lịch sử còn thiếu. Chương trình voucher dùng khóa theo tháng. Các bàn đang phục vụ từ buổi demo cũ không bị tự chiếm lại; hãy hoàn tất đúng lượt khách cũ trên giao diện. Bản sao lưu trước khi nạp ngày 26/10 nằm trong thư mục Backup mặc định của SQL Server, tên `RestaurantDB_before_demo_20261026_085330.bak`.
