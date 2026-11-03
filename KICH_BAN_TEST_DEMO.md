# Kịch bản test và demo

Dữ liệu chuẩn bị ngày **02/11/2026**. Các mã có ngày như `WEB-20261102-POS2` chỉ đúng khi nạp dữ liệu trong ngày đó. Nạp lại vào ngày khác thì đổi phần ngày trong mã theo ngày nạp, và mã voucher đổi theo tháng (`WEB2611-…`).

## 1. Chuẩn bị

### Mở website

```powershell
dotnet run --project RestaurantManagement.Web --launch-profile https
```

- Khách hàng: https://localhost:7167/Account/Login
- Nội bộ: https://localhost:7167/admin
- Mật khẩu chung của các tài khoản dưới đây: `Demo@2026!`

| Vai trò | Email | Dùng để |
|---|---|---|
| Khách hàng | `khach.demo@example.test` (SĐT `0999000000`) | Thực đơn, đặt bàn, ví voucher, lịch sử đơn |
| Tiếp tân | `tieptan.demo@example.test` | Xếp bàn, nhận khách |
| Bồi bàn | `boiban.demo@example.test` | Gọi món, xác nhận đã phục vụ |
| Bếp | `bep.demo@example.test` | Màn hình bếp |
| Thu ngân | `thungan.demo@example.test` | Đối chiếu cọc, thu tiền, nộp ca |
| Kho | `kho.demo@example.test` | Tồn kho, nhập, xuất, thanh lý |
| Quản trị | `admin.demo@example.test` | Toàn bộ chức năng, báo cáo |

Mỗi vai trò cần một phiên đăng nhập riêng: dùng cửa sổ thường, cửa sổ ẩn danh hoặc trình duyệt khác. Hai tài khoản cùng một cửa sổ sẽ ghi đè phiên của nhau.

### Dữ liệu có sẵn

| Dữ liệu | Trạng thái ban đầu | Dùng ở bước |
|---|---|---|
| Bàn `WEB-01`, hóa đơn `WEB-20261102-POS1` | Đang phục vụ, 356.000 đ: 1 món đang nấu, 1 món sẵn sàng, 1 món chờ nấu | Demo bước 4 |
| Bàn `WEB-02`, hóa đơn `WEB-20261102-POS2` | Đã phục vụ hết, 356.000 đ, đã cọc 100.000 đ | Demo bước 5 |
| `WEB-20261102-MANGDI`, `WEB-20261102-GIAOHANG` | Đơn mang đi và giao hàng chưa thu tiền, 356.000 đ | Test bổ sung |
| `HD-POSDEMO-01` | Đơn mang đi, 948.000 đ, chưa gắn khách | Test tra số điện thoại |
| `WEB-20261102-LICH1` | Chờ xác nhận, chưa xếp bàn | Demo bước 3 |
| `WEB-20261102-LICH2` | Chờ khách nộp cọc 200.000 đ | Test bổ sung |
| `WEB-20261102-LICH3` | Khách báo đã chuyển 200.000 đ, chờ đối chiếu | Demo bước 2 |
| `WEB-20261102-LICH4`, `LICH5` | Đã xác nhận, cọc 228.000 đ, có món đặt trước | Dự trù nguyên liệu |
| `WEB-20261102-LICH6`, `LICH7` | Đã hủy, không đến | Bộ lọc danh sách |
| Nước mắm, Ớt | Chạm ngưỡng cảnh báo | Tồn kho |
| Salad rau sốt mè (chờ duyệt demo) | Món chưa duyệt | Duyệt món |

Mã voucher tháng 11/2026:

| Mã | Ưu đãi | Điều kiện | Ví của Khách thử |
|---|---|---|---|
| `WEB2611-P10` | Giảm 10%, tối đa 50.000 đ | Đơn từ 100.000 đ | Có |
| `WEB2611-10K` | Giảm 10.000 đ | Đơn từ 50.000 đ | Có |
| `WEB2611-50K` | Giảm 50.000 đ | Đơn từ 300.000 đ | Nhập mã trực tiếp |
| `WEB2611-MON20` | Giảm 20% món Gỏi ngó sen, tối đa 50.000 đ | Chỉ tính món thuộc mã | Nhập mã trực tiếp |

### Đưa dữ liệu về trạng thái ban đầu

Dừng website trước, rồi chạy lệnh tương ứng.

Trong ngày 02/11/2026, khôi phục bản sạch đã sao lưu:

```powershell
sqlcmd -S .\SQLEXPRESS -E -C -Q "ALTER DATABASE RestaurantDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE RestaurantDB FROM DISK=N'RestaurantDB_demo_ready_20261102.bak' WITH REPLACE; ALTER DATABASE RestaurantDB SET MULTI_USER;"
```

Vào ngày khác, tạo lại từ đầu để dữ liệu mang đúng ngày mới. Lệnh này xóa toàn bộ `RestaurantDB`:

```powershell
sqlcmd -S .\SQLEXPRESS -E -C -Q "ALTER DATABASE RestaurantDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE RestaurantDB;"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AuthBootstrap__DemoPassword = 'Demo@2026!'
dotnet run --project RestaurantManagement.Web --no-launch-profile -- --init-website-demo
```

## 2. Kịch bản demo chính (khoảng 15 phút)

Chạy đúng thứ tự để các con số khớp với cột "Kết quả đúng". Luồng trọng tâm là bước 5 và 6: thu ngân nhập số điện thoại, chọn voucher, trừ cọc, ra hóa đơn, xem báo cáo nộp ca.

### Bước 1. Khách đặt bàn (Khách hàng)

1. Mở trang chủ, gõ "goi ngo sen" không dấu vào ô tìm món: **Gỏi ngó sen tôm thịt** có trong kết quả.
2. Thêm **Gỏi ngó sen tôm thịt, Nhỏ (S)**, số lượng 2 vào giỏ.
3. Mở **Đặt bàn**, chọn ngày mai, ca tối, 4 khách, khu vực Sảnh chung, chọn bàn, điền liên hệ rồi gửi yêu cầu.

**Kết quả đúng:** tiền món tham khảo 178.000 đ, tiền cọc 89.000 đ (50% tiền món). Khi xóa hết món khỏi giỏ, phần món chỉ còn dòng tiền cọc và ghi chú ngắn; cọc giữ chỗ là 200.000 đ (phòng VIP 500.000 đ).

### Bước 2. Đối chiếu cọc (Thu ngân)

1. Mở **Đối chiếu tiền đặt cọc**, bấm **Đối chiếu** ở dòng `WEB-20261102-LICH3`.
2. Tích ô đã kiểm tra tiền vào ngân hàng, bấm **Xác nhận nhận 200.000 đ**.

**Kết quả đúng:** lịch biến khỏi danh sách chờ đối chiếu và tiền cọc đã nhận là 200.000 đ.

### Bước 3. Xếp bàn (Tiếp tân)

1. Mở **Danh sách đặt bàn**, thẻ **Chờ xác nhận**.
2. Ở `WEB-20261102-LICH1`, bấm **Xếp bàn**, chọn một bàn trống trên sơ đồ và xác nhận.

**Kết quả đúng:** lịch chuyển sang thẻ **Đã xác nhận**, hiện tên bàn đã xếp.

### Bước 4. Gọi món và bếp (Bồi bàn, Bếp)

1. Bồi bàn mở **Quản lý đơn hàng**, mở `WEB-20261102-POS1`, bấm **Gọi thêm món**, thêm 1 món.
2. Bếp mở **Màn hình bếp (KDS)**: món mới hiện trong hàng chờ. Món đang nấu bấm **Nấu xong**, món chờ bấm **Bắt đầu nấu** rồi **Nấu xong**.
3. Bồi bàn mở lại đơn, bấm **✓ Đã phục vụ** từng món.

**Kết quả đúng:** món đi theo thứ tự Chờ nấu → Đang nấu → Sẵn sàng → Đã phục vụ; tiền hóa đơn tăng theo món thêm; màn hình bếp không hiện giá tiền. Trên màn hình bếp, bàn `WEB-01` hiện là "Sảnh demo website 1".

### Bước 5. Thu tiền: voucher và trừ cọc (Thu ngân)

1. Mở **Hóa đơn & thanh toán**, tìm `POS2`, mở `WEB-20261102-POS2`.
2. Ở **Nhập số điện thoại khách hàng**, nhập `0999000000`, bấm **Tra cứu ví voucher**.
3. Ở `WEB2611-P10`, bấm **Áp dụng**.
4. Chọn **Tiền mặt**, bấm nút nhanh **500.000 đ**, bấm **✓ Xác nhận đã thu tiền mặt**.

| Dòng trên hóa đơn | Kết quả đúng |
|---|---|
| Tiền món | 356.000 đ |
| Giảm giá / Voucher | - 35.600 đ |
| Trừ tiền đặt cọc đã nhận | - 100.000 đ |
| Đã thu khi thanh toán | 220.400 đ |
| Tiền trả lại | 279.600 đ |
| Tổng khách đã trả (cọc + thu) | 320.400 đ |

Sau khi thu: hóa đơn **Đã thanh toán**; bàn `WEB-02` chuyển sang **Chờ dọn** (bồi bàn bấm **Đã dọn xong** thì về **Sẵn sàng**); khách được cộng **32 điểm** (320.400 đ / 10.000 đ, làm tròn xuống); mã `P10` không còn trong ví.

### Bước 6. Báo cáo nộp ca (Thu ngân)

Mở **Báo cáo nộp ca**, chọn hôm nay. Nếu đã làm đủ bước 2 và 5, và chưa thu thêm hóa đơn nào khác:

| Chỉ tiêu | Kết quả đúng |
|---|---|
| Tiền mặt phải nộp | 526.400 đ (306.000 đ có sẵn + 220.400 đ) |
| Chuyển khoản đã vào tài khoản | 756.000 đ (556.000 đ có sẵn + 200.000 đ) |
| Tổng thực thu | 1.282.400 đ |

Tiền cọc của khách ở bước 1 chỉ được cộng khi thu ngân đã xác nhận. Giao dịch trước 17:00 tính vào ca trưa, từ 17:00 tính vào ca tối.

### Bước 7. Báo cáo quản trị (Quản trị)

Mở **Tổng quan kinh doanh**, **Doanh thu tháng / năm**, **Món bán chạy**, **Hiệu quả khuyến mãi**, chọn nút nhanh **30 ngày qua**.

**Kết quả đúng:** biểu đồ có dữ liệu 28 ngày gần nhất; **Đánh giá khách hàng** có điểm trung bình và danh sách nhận xét.

## 3. Danh sách test bổ sung

Đánh dấu vào cột cuối khi test. Nên làm sau phần demo, hoặc khôi phục dữ liệu trước khi demo lại.

### Khách hàng

| Mã | Thao tác | Kết quả đúng | Đạt |
|---|---|---|---|
| K1 | Mở **Voucher & ví của tôi** | Mã dùng được nằm đầu danh sách, mã đã dùng nằm cuối | ☐ |
| K2 | Mở **Đơn hàng & tích điểm** trên điện thoại (hoặc thu hẹp cửa sổ) | Bảng thành các thẻ, không phải cuộn ngang; đơn mang đi hiện "Mang đi" | ☐ |
| K3 | Mở `WEB-20261102-MANGDI`, áp mã `WEB2611-10K` | Tiền giảm 10.000 đ, phải trả 346.000 đ | ☐ |
| K4 | Mở **Lịch sử đặt bàn** | Thấy các lịch `LICH1` đến `LICH7` cùng trạng thái | ☐ |
| K5 | Ở `LICH2`, mở **Thanh toán / xem trạng thái cọc**, báo đã chuyển khoản | Lịch sang chờ thu ngân đối chiếu | ☐ |

### Tiếp tân

| Mã | Thao tác | Kết quả đúng | Đạt |
|---|---|---|---|
| T1 | **Đặt bàn cho khách** tại quầy | Lịch mới xuất hiện trong danh sách | ☐ |
| T2 | **Đổi giờ** một lịch | Giờ mới được lưu; nếu trùng lịch thì báo lỗi | ☐ |
| T3 | Xem thẻ **Đã hủy**, **Hoàn tất** | Thấy `LICH6` ở Đã hủy và `LICH7` ở Hoàn tất | ☐ |
| T4 | **Sơ đồ bàn**, lọc khu vực và trạng thái | Sơ đồ chỉ còn bàn phù hợp | ☐ |
| T5 | Thu tiền xong một bàn, đặt lại đúng bàn đó (giờ đến sau hiện tại ít nhất 30 phút) | Bàn hiện **Còn trống** và đặt được | ☐ |
| T6 | Đặt một bàn đã có lịch, giờ đến cách lịch đó dưới 2 tiếng | Bàn hiện **Đã kín**, không đặt được | ☐ |
| T7 | Đặt cùng bàn đó, giờ đến cách lịch cũ đúng 2 tiếng | Bàn hiện **Còn trống** và đặt được | ☐ |
| T8 | Đặt online lúc 21:15; tiếp tân nhận khách lúc 21:15 | Online bị từ chối (muộn nhất 21:00); tại quầy vẫn nhận (muộn nhất 21:30) | ☐ |
| T9 | **Đặt bàn cho khách**, tích **Khách đến ăn ngay**, bỏ trống tên và số điện thoại | Lưu xong bàn **Đang phục vụ** ngay, tên hiện **Khách lẻ** | ☐ |
| T10 | Bấm **Nhận bàn** một lịch đã xếp bàn, giờ hẹn còn hơn 30 phút | Nhận được nếu bàn trống và không vướng lịch khác | ☐ |
| T11 | Bấm **Nhận bàn** một lịch **Chờ cọc** đã xếp bàn | Nhận được; phần cọc chưa nộp không còn phải thu trước | ☐ |
| T12 | Lịch đã quá giờ hẹn 15 phút, bấm **Không đến** | Lịch sang **Hoàn tất**, trạng thái **Không đến**, bàn được trả | ☐ |
| T13 | **Sơ đồ bàn**: bấm **Xác nhận hoàn tất bàn**, rồi **Đã dọn xong** | Bàn lần lượt sang **Chờ dọn**, rồi **Sẵn sàng** | ☐ |

### Thu ngân

| Mã | Thao tác | Kết quả đúng | Đạt |
|---|---|---|---|
| C1 | Nhập số điện thoại chưa đăng ký, ví dụ `0900000000` | Báo chưa là thành viên, vẫn thanh toán được | ☐ |
| C2 | Nhập mã voucher sai | Báo lỗi, tiền không đổi | ☐ |
| C3 | Thu `WEB-20261102-GIAOHANG` bằng **Chuyển khoản ngân hàng** | Thành công; báo cáo nộp ca tăng phần chuyển khoản | ☐ |
| C4 | Tìm theo mã trong **Hóa đơn & thanh toán** | Danh sách chỉ còn hóa đơn phù hợp | ☐ |
| C5 | Mở **Món & cọc** của lượt đang ngồi còn cọc dư, ghi **Đã hoàn cho khách** | Thu ngân lưu được, không cần quản trị | ☐ |
| C6 | Đăng nhập **Bồi bàn**, mở một hóa đơn chưa thu | Ô giảm giá thủ công chỉ đọc, không sửa được | ☐ |

### Kho

| Mã | Thao tác | Kết quả đúng | Đạt |
|---|---|---|---|
| W1 | **Tồn kho & cảnh báo**, bật lọc chạm ngưỡng | Thấy Nước mắm và Ớt | ☐ |
| W2 | Bấm **Nhập thêm** ở Nước mắm | Phiếu nhập mở với đúng Nước mắm được chọn sẵn | ☐ |
| W3 | Lập phiếu nhập, bấm lưu | Có hộp xác nhận; sau khi lưu tồn kho tăng đúng số lượng | ☐ |
| W4 | Mở phiếu xuất mới | Phiếu bắt đầu bằng dòng trống, không tự điền nguyên liệu | ☐ |
| W5 | Xuất vượt tồn kho | Báo không đủ tồn kho, phiếu không được lưu | ☐ |
| W6 | Lập phiếu **Thanh lý nguyên liệu** | Tồn giảm; báo cáo nhập – xuất – tồn có chứng từ tương ứng | ☐ |

### Quản trị

| Mã | Thao tác | Kết quả đúng | Đạt |
|---|---|---|---|
| A1 | **Món ăn**, lọc **Chờ duyệt**, sửa Salad rau sốt mè, tích ô đã duyệt rồi lưu | Món hết trạng thái Chờ duyệt | ☐ |
| A2 | **Khuyến mãi & voucher**, bấm **Ngừng** một chương trình | Có hộp xác nhận, có thông báo sau khi đổi | ☐ |
| A3 | **Khu vực & tiền đặt cọc**, đổi mức cọc | Lưu có thông báo; lịch mới dùng mức mới | ☐ |
| A4 | **Nhà cung cấp**, tìm một tên, bấm sửa rồi lưu | Về danh sách vẫn còn từ khóa tìm kiếm | ☐ |
| A5 | **Khách hàng thành viên**, mở Khách thử | Thấy lịch sử đơn và sổ điểm | ☐ |
| A6 | **Nhân viên**, sửa một nhân viên có tài khoản, bấm **Khóa tài khoản** | Có hộp xác nhận; tài khoản đó không đăng nhập được. Mở khóa lại sau khi test | ☐ |
| A7 | Báo cáo doanh thu, chọn "đến ngày" trước "từ ngày" | Báo "Đến ngày không được trước từ ngày." ngay trên form | ☐ |
| A8 | Báo cáo doanh thu theo ngày, chọn khoảng dài hơn một năm | Trang lỗi của hệ thống ghi rõ giới hạn một năm, có nút quay lại | ☐ |

### Phân quyền và hiển thị

| Mã | Thao tác | Kết quả đúng | Đạt |
|---|---|---|---|
| P1 | Đăng nhập Bếp, gõ địa chỉ `/HoaDon` | Trang không có quyền truy cập | ☐ |
| P2 | Đăng nhập Bồi bàn, gõ địa chỉ `/BaoCao` | Trang không có quyền truy cập | ☐ |
| P3 | Chưa đăng nhập, mở `/DatBan` | Chuyển đến trang đăng nhập | ☐ |
| P4 | Xem các trang có tiền | Tiền đều dạng `139.000 đ` | ☐ |
| P5 | Thu hẹp cửa sổ ở trang có từ 3 ô lọc | Bộ lọc thu vào nút **Bộ lọc** | ☐ |

## 4. Lưu ý khi test

- **Thu tiền khi món chưa phục vụ:** hệ thống vẫn cho thu. Bàn và lượt khách chỉ được giải phóng khi mọi món đã phục vụ hoặc đã hủy. Vì vậy ở bước 4 nên phục vụ hết món rồi mới thu `POS1`.
- **Lịch quá giờ tự đóng:** lịch chưa nhận bàn, đã hết 2 tiếng giữ bàn sẽ tự chuyển **Không đến** (trừ lịch đang chờ đối chiếu chuyển khoản). Vì vậy dữ liệu mẫu nên tạo lại đúng ngày demo.
- **Voucher dùng một lần:** sau khi dùng `P10` ở bước 5, mã này không còn trong ví. Muốn lặp lại phải khôi phục dữ liệu.
- **Hạn cọc:** lịch mẫu `LICH2`, `LICH3` giữ được đến 10:05 ngày 03/11/2026. Lịch khách tạo mới vẫn theo quy định 60 phút, quá hạn chưa báo chuyển thì tự hết hạn.
- **Phiếu nhập nháp:** `WEB-20261102-NHAPNHAP` chỉ xem được, giao diện chưa có nút ghi sổ phiếu nháp, nên đừng dùng phiếu này khi demo.
- **Tồn kho:** nguyên liệu tính theo g hoặc ml nên số trông lớn, ví dụ 104.010 ml nước mắm. Ngưỡng cảnh báo của Nước mắm và Ớt được đặt bằng đúng tồn hiện tại để có dữ liệu cảnh báo.
- **QR chuyển khoản:** chỉ minh họa, không cần chuyển tiền thật.
