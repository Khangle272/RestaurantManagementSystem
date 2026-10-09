# DESIGN.md - Restaurant Management System (Web Admin Portal)

Tài liệu quy chuẩn thiết kế giao diện người dùng (Design System & UI/UX Specification) dành cho phân hệ Quản trị Web của hệ thống Quản lý nhà hàng. Tài liệu này đóng vai trò là quy chuẩn kỹ thuật phục vụ việc lập trình giao diện (Razor Views, Bootstrap 5, Custom CSS) và cấu hình giao diện trên các công cụ thiết kế (như Stitch).

---

## 1. Triết lý thiết kế & Design Tokens

### 1.1. Định hướng phong cách (Visual Style)

* **Định vị:** Modern F&B SaaS Admin Dashboard – Giao diện quản trị hiện đại, trực quan, tối ưu hóa thao tác bàn phím và chuột cho nhân viên thu ngân, thủ kho và người quản lý.


* **Trải nghiệm thao tác (UX):** Hạn chế chuyển trang liên tục; ưu tiên sử dụng Data Table linh hoạt, bộ lọc tức thời (Instant Filter), và các hộp thoại xác nhận (Modal) rõ ràng để kiểm soát các tác vụ xóa hoặc thay đổi trạng thái.



### 1.2. Bảng mã màu chuẩn (Color Palette)

| Loại màu | Tên Token | Mã HEX | Mục đích sử dụng |
| --- | --- | --- | --- |
| **Primary** | `brand-primary` | `#E11D48` (Rose 600) | Nút hành động chính (CTA), mục điều hướng đang kích hoạt, điểm nhấn thương hiệu |
| **Primary Dark** | `brand-primary-dark` | `#BE123C` (Rose 700) | Trạng thái hover/active của nút bấm chính |
| **Secondary** | `brand-secondary` | `#2C7A3E` (Slate 900) | Nền thanh điều hướng bên (Sidebar), tiêu đề trang chính, màu chữ đậm |
| **Accent / Warm** | `brand-accent` | `#F59E0B` (Amber 500) | Cảnh báo tồn kho chạm định mức tối thiểu, nhãn trạng thái chờ xử lý

 |
| **Success** | `state-success` | `#10B981` (Emerald 500) | Trạng thái "Đang phục vụ", "Còn hàng", thông báo tác vụ hoàn tất

 |
| **Warning** | `state-warning` | `#F97316` (Orange 500) | Cảnh báo nguyên liệu sắp hết, món ăn tạm ngưng nhận thêm

 |
| **Danger** | `state-danger` | `#EF4444` (Red 500) | Nút Xóa, trạng thái "Hết hàng", "Ngừng kinh doanh", thông báo lỗi ràng buộc

 |
| **Neutral Dark** | `text-primary` | `#1E293B` (Slate 800) | Màu chữ cho toàn bộ nội dung văn bản chính |
| **Neutral Muted** | `text-secondary` | `#64748B` (Slate 500) | Màu chữ phụ, văn bản gợi ý (placeholder), nhãn mô tả phụ |
| **Surface / Card** | `bg-surface` | `#FFFFFF` (White) | Nền thẻ hiển thị (Card), bảng dữ liệu, hộp thoại Modal |
| **Background** | `bg-app` | `#F8FAFC` (Slate 50) | Nền tổng thể của toàn bộ khung làm việc quản trị |
| **Border / Line** | `border-subtle` | `#E2E8F0` (Slate 200) | Đường phân chia bảng, đường viền ô nhập liệu, đường kẻ phân cách |

### 1.3. Typography (Quy chuẩn kiểu chữ)

* **Phông chữ chủ đạo:** `Inter`, `Roboto` hoặc hệ phông sans-serif chuẩn hệ thống (`system-ui, -apple-system, sans-serif`).
* **Hệ thống phân cấp kiểu chữ:**
* **Tiêu đề trang (H1):** `24px` – Bold (`font-weight: 700`), màu `#0F172A`.
* **Tiêu đề nhóm/phần (H2):** `18px` – Semi-bold (`font-weight: 600`), màu `#1E293B`.
* **Văn bản thông thường (Body):** `14px` – Regular (`font-weight: 400`), màu `#1E293B`, line-height `1.5`.
* **Nhãn phụ / Ghi chú (Small/Caption):** `12px` – Regular / Medium, màu `#64748B`.
* **Tiêu đề cột bảng (Table Header):** `13px` – Semi-bold (`font-weight: 600`), màu `#475569`.



### 1.4. Khoảng cách & Hình khối (Spacing, Radius & Shadows)

* **Bán kính bo góc (Border Radius):**
* Nút bấm, ô nhập liệu (`input`, `select`): `6px` (`rounded-md`).
* Thẻ Card nội dung, Hộp thoại Modal: `10px` - `12px`.
* Huy hiệu trạng thái (`badge/pill`): `9999px` (bo tròn viên thuốc).


* **Độ bóng đổ (Box Shadows):**
* Thẻ Card tiêu chuẩn: `0 1px 3px 0 rgba(0, 0, 0, 0.05), 0 1px 2px 0 rgba(0, 0, 0, 0.03)`.
* Modal / Popover nổi: `0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)`.



---

## 2. Cấu trúc khung giao diện chuẩn (Global Layout Shell)

Toàn bộ các trang quản trị Web (gồm trang Quản lý Thực đơn và Quản lý Kho nguyên liệu) được đặt trong một cấu trúc layout đồng nhất 3 phần:

```
+------------------------------------------------------------------------------------+
|                                TOPBAR NAVIGATION                                   |
| [Brand Logo]       Breadcrumb: Trang chủ / Kho / Danh mục nguyên liệu  [User Info] |
+------------------+-----------------------------------------------------------------+
| SIDEBAR          | MAIN CONTENT CANVAS                                             |
| - Tổng quan      | +-------------------------------------------------------------+ |
| - Sơ đồ bàn      | | Page Header: [Tên Trang]                [+ Nút Thêm Mới]    | |
| - Thực đơn món * | +-------------------------------------------------------------+ |
| - Kho vật liệu * | | Filter Toolbar: [Ô tìm kiếm...] [Lọc danh mục] [Lọc tồn kho]| |
| - Đặt bàn        | +-------------------------------------------------------------+ |
| - Thu ngân / Bill| | Data Table Canvas (Sticky Header, Phân trang, Tác vụ dòng)  | |
| - Nhân sự        | |                                                             | |
| - Báo cáo        | +-------------------------------------------------------------+ |
+------------------+-----------------------------------------------------------------+

```

* **Sidebar điều hướng (Cố định chiều rộng: 260px):** Sử dụng tông màu tối `#2C7A3E`, các icon vector tối giản; phân định rõ phân hệ "Thực đơn món ăn" và "Kho nguyên liệu".


* **Page Header:** Thể hiện tiêu đề trang, mô tả tóm tắt nhiệm vụ và cụm nút hành động chính (nút Primary `+ Thêm mới`, `Xuất báo cáo`) ở góc trên bên phải.
* **Filter Toolbar:** Nằm ngay phía trên bảng dữ liệu, gồm: thanh tìm kiếm từ khóa kèm biểu tượng kính lúp, danh sách thả xuống chọn bộ lọc và nút đặt lại bộ lọc.

---

## 3. Quy chuẩn các thành phần dùng chung (Component Library)

### 3.1. Nút bấm (Buttons)

* **Primary Button:** Nền `#E11D48`, chữ trắng, hover `#BE123C`. Sử dụng cho: "Thêm món mới", "Thêm nguyên liệu", "Lưu thay đổi".
* **Secondary / Outline Button:** Viền `#CBD5E1`, nền trắng, chữ `#334155`. Sử dụng cho: "Hủy bỏ", "Quay lại danh sách", "Xuất file Excel".
* **Danger Button:** Nền `#EF4444`, chữ trắng, hover `#DC2626`. Sử dụng cho: "Xác nhận xóa", "Ngừng kinh doanh món".
* **Action Icon Group (Thao tác trên từng dòng bảng):**
* Nút Xem chi tiết: Icon mắt (Màu xanh dương `#0284C7`).
* Nút Chỉnh sửa: Icon bút chì (Màu vàng đất/amber `#D97706`).
* Nút Xóa / Vô hiệu hóa: Icon thùng rác (Màu đỏ `#EF4444`).



### 3.2. Huy hiệu trạng thái (Status Badges / Pills)

* **Đang phục vụ / Tồn kho an toàn:** Nền `#DEF7EC`, chữ `#03543F` (Chấm tròn xanh lá trước chữ).


* **Tạm hết món / Tồn kho chạm ngưỡng:** Nền `#FEF08A`, chữ `#713F12` (Chấm tròn vàng cảnh báo).


* **Ngừng kinh doanh / Hết hàng:** Nền `#FEE2E2`, chữ `#991B1B` (Chấm tròn đỏ nguy cơ).



### 3.3. Bảng dữ liệu (Data Table)

* Header bảng có nền xám nhẹ `#F1F5F9`, chữ in hoa nhẹ, cố định vị trí khi cuộn trang dài.
* Hàng dữ liệu có hiệu ứng hover `#F8FAFC`, phân cách bằng đường viền mỏng `1px solid #E2E8F0`.
* Cột tác vụ (Actions) luôn được canh lề phải để người dùng dễ thao tác.
* Khu vực chân bảng tích hợp thanh phân trang (Pagination) cùng chỉ báo: *"Hiển thị 1 - 10 trên tổng số X bản ghi"*.

---

## 4. Đặc tả giao diện: Phân hệ Quản lý Nguyên liệu (CRUD Ingredients)

### 4.1. Màn hình danh mục nguyên liệu (`Index.cshtml`)

* **Thẻ thống kê nhanh đầu trang (Summary Cards):**
1. *Tổng nguyên liệu:* Tổng số mặt hàng đang quản lý trong kho.


2. *Cảnh báo sắp hết:* Nền vàng nhạt, hiển thị số nguyên liệu có `SoLuongTon <= DinhMucToiThieu`.


3. *Nguyên liệu cạn kho:* Nền đỏ nhạt, số nguyên liệu có tồn kho bằng 0.




* **Thanh công cụ lọc (Toolbar):**
* Ô tìm kiếm: Tìm kiếm theo mã hoặc tên nguyên vật liệu.


* Dropdown: Lọc theo Đơn vị tính (Tất cả, kg, lốc, lít, lon, chai, gói...).


* Toggle Switch: "Chỉ hiển thị nguyên liệu chạm ngưỡng tối thiểu".




* **Cấu trúc bảng dữ liệu nguyên liệu:**
* Cột 1: Mã nguyên liệu (`MaNguyenLieu`, dạng `#NL001`).


* Cột 2: Tên nguyên liệu (Chữ đậm).


* Cột 3: Đơn vị tính (Huy hiệu bo xám).


* Cột 4: Đơn giá nhập hiện hành (Định dạng tiền tệ: `xx,xxx VNĐ`).


* Cột 5: Số lượng tồn kho (Số thực 2 chữ số thập phân; hiển thị cảnh báo đỏ khi chạm hoặc dưới ngưỡng).


* Cột 6: Định mức tồn tối thiểu.


* Cột 7: Trạng thái (Pill: "An toàn", "Cần nhập gấp", "Hết hàng").


* Cột 8: Hành động (Cụm nút: Sửa, Xóa).



### 4.2. Giao diện Thêm mới / Cập nhật nguyên liệu (`Create.cshtml` / `Edit.cshtml`)

* **Cấu trúc biểu mẫu nhập liệu (Form Grid 2 cột):**
* Tên nguyên liệu (`string`, bắt buộc, tự động kiểm tra trùng tên).


* Đơn vị tính (Dropdown chọn đơn vị có sẵn hoặc thêm mới: kg, lít, lon, gói...).


* Đơn giá nhập (`decimal`, tự động định dạng phân tách hàng nghìn).


* Số lượng tồn kho ban đầu (`decimal`, giá trị tối thiểu $\ge 0$).


* Định mức tồn tối thiểu (`decimal`, dùng kích hoạt cảnh báo nhập hàng).




* **Phản hồi lỗi (Validation):** Khung viền đỏ quanh trường nhập dữ liệu kèm thông báo lỗi chi tiết khi để trống trường bắt buộc hoặc nhập số âm.

### 4.3. Hộp thoại Modal xác nhận xóa nguyên liệu

* Hiển thị thông tin tên nguyên liệu chuẩn bị xóa.
* **Kiểm tra ràng buộc:** Nếu nguyên liệu đã được liên kết trong công thức định lượng của món ăn (`DinhMucMonAn`), hộp thoại hiển thị cảnh báo màu cam: *"Nguyên liệu này đang nằm trong công thức của món ăn đang kinh doanh. Không thể xóa!"* và vô hiệu hóa nút xác nhận xóa.



---

## 5. Đặc tả giao diện: Phân hệ Quản lý Món ăn (CRUD Dishes)

### 5.1. Màn hình danh sách món ăn (`Index.cshtml`)

* **Chế độ xem linh hoạt:** Nút chuyển đổi qua lại giữa **Dạng Bảng (Table View)** để đối soát nhanh và **Dạng Thẻ Lưới (Grid/Card View)** để xem hình ảnh trực quan món ăn.


* **Thanh công cụ lọc (Toolbar):**
* Ô tìm kiếm: Nhập tên món hoặc mã món ăn.


* Dropdown Danh mục: Tải động từ bảng `DanhMucMonAn` (Khai vị, Món chính, Món lẩu, Đồ uống...).


* Dropdown Trạng thái: "Đang phục vụ", "Tạm hết món", "Ngừng kinh doanh".




* **Quy chuẩn thẻ món ăn (Chế độ Grid View):**
* Khung ảnh đại diện món ăn (Tỷ lệ `16:9`, hiển thị ảnh placeholder nếu chưa có ảnh).


* Huy hiệu trạng thái nổi trên góc ảnh (Xanh: Còn món / Đỏ: Hết món).


* Tên món ăn (H2: `16px`, in đậm).


* Tên danh mục món (Huy hiệu nhỏ mờ).


* Giá bán niêm yết (Chữ to màu đỏ thương hiệu `#E11D48`, định dạng tiền tệ).


* Nút gạt nhanh (Toggle Switch): Bật/Tắt trạng thái phục vụ tức thời.


* Cụm nút hành động: Chỉnh sửa thông tin, Chỉnh sửa công thức định lượng (BOM), Xóa món.





### 5.2. Màn hình Thêm mới / Cập nhật Món ăn (`Create.cshtml` / `Edit.cshtml`)

Thiết kế bố cục chia thành **2 cột theo tỷ lệ 40% : 60%**:

* **Cột trái (Thông tin cơ bản & Ảnh món):**
* Khung tải ảnh: Hỗ trợ kéo thả (Drag & Drop) kèm ô xem trước hình ảnh (Image Preview), kiểm tra định dạng `.jpg`, `.png`.


* Tên món ăn (Ô nhập văn bản, bắt buộc).


* Danh mục món (Dropdown liên kết khóa ngoại `MaDanhMuc`).


* Giá bán niêm yết (Ô nhập số, kèm hậu tố "VNĐ").


* Mô tả món / Ghi chú dị ứng (Vùng văn bản nhiều dòng).


* Tình trạng phục vụ (Lựa chọn radio: Đang phục vụ / Tạm hết món / Ngừng kinh doanh).




* **Cột phải (Bộ thiết lập công thức định lượng - BOM / Recipe Builder):**
* Tiêu đề khối: *"Định mức nguyên vật liệu tiêu hao (Cho 1 phần ăn)"*.


* Thanh gán nguyên liệu: Dropdown chọn nguyên liệu từ kho + Ô nhập số lượng cần dùng + Nút `+ Thêm vào công thức`.


* Bảng danh sách nguyên liệu thành phần đã gán:
* Tên nguyên liệu | Số lượng tiêu hao | Đơn vị tính | Đơn giá vốn ước tính | Nút xóa dòng.




* Chân bảng tổng kết tự động:
* Tổng chi phí giá vốn ước tính (Food Cost): Tự động tính toán dựa trên tổng định lượng nhân đơn giá nhập.


* Tỷ suất lợi nhuận gộp ước tính:

$$\text{Tỷ suất lợi nhuận} = \frac{\text{Giá bán} - \text{Giá vốn}}{\text{Giá bán}} \times 100\%$$







### 5.3. Hộp thoại Modal xác nhận xóa / ngừng kinh doanh món ăn

* **Kiểm tra ràng buộc bàn đang mở:** Nếu món ăn đang có trong các phiếu gọi món chưa thanh toán tại các bàn ăn đang phục vụ (`HoaDon.TrangThai = 'ChuaThanhToan'`), hệ thống hiển thị cảnh báo: *"Món ăn đang được phục vụ tại [Danh sách bàn]. Không thể xóa hoặc ngừng phục vụ lúc này!"* và chặn thao tác.


* **Xác nhận hợp lệ:** Cho phép lựa chọn giữa *"Chuyển sang trạng thái Ngừng kinh doanh (Khuyến nghị để bảo toàn lịch sử hóa đơn)"* hoặc *"Xóa hoàn toàn"*.

---

## 6. Đặc tả giao diện: Portal Khách hàng (Đặt bàn & Đánh giá)

Tuân thủ chặt chẽ Design Tokens: Màu chủ đạo Rose (`#E11D48`), phụ trợ Amber (`#F59E0B`), nền Slate (`#F8FAFC`), viền `#E2E8F0`.

### 6.1. Màn hình Đặt bàn trực tuyến (`Views/DatBan/Index.cshtml`)
* **Bố cục 2 cột (Grid System 5 : 7 trên Desktop, 1 cột trên Mobile):**
  * **Cột trái - Chọn thời gian & không gian:**
    * Bộ chọn Ngày: Ô chọn ngày (mặc định hôm nay, tối thiểu là thời điểm hiện tại).
    * Bộ chọn Khung giờ: Nhóm nút pills (`btn-outline-danger`/`btn-check`) hiển thị các khung giờ chuẩn (11:00, 11:30, 12:00, 12:30, 18:00, 18:30, 19:00, 19:30, 20:00).
    * Bộ chọn Số lượng khách: Thanh điều khiển +/- hoặc nút pill nhanh (2 người, 4 người, 6 người, 8 người, nhóm đông). Validate từ 1 đến 50 khách.
    * Chọn Khu vực ưu tiên: Tầng trệt, Phòng máy lạnh, Sân vườn, Phòng VIP riêng.
  * **Cột phải - Thông tin khách hàng & Xác nhận:**
    * Họ và tên liên hệ (Bắt buộc).
    * Số điện thoại liên hệ (Bắt buộc, định dạng 10 chữ số chuẩn Việt Nam).
    * Địa chỉ Email (Để nhận thông tin xác nhận điện tử).
    * Ghi chú bổ sung (Yêu cầu trẻ em, ghế ăn dặm, trang trí sinh nhật, dị ứng thức ăn).
    * Nút CTA Đặt bàn: Nền `#E11D48`, chữ trắng in đậm, hiệu ứng chuyển màu khi hover sang `#BE123C`.

### 6.2. Màn hình Xác nhận Đặt bàn thành công (`Views/DatBan/Success.cshtml`)
* **Thẻ thành công (Success Receipt Card):**
  * Biểu tượng dấu tích xanh lục `#10B981` cỡ lớn kèm thông điệp chúc mừng.
  * **Mã đặt chỗ (Booking Code):** Kích thước chữ `28px`, phông monospace hoặc font đậm, dạng `BK-YYYYMMDD-XXXX`, có nút copy nhanh.
  * Tóm tắt chi tiết: Họ tên, Số điện thoại, Thời gian đến, Số lượng khách, Khu vực mong muốn.
  * Hướng dẫn đến nhà hàng: Giữ bàn trong vòng 15 phút, hotline hỗ trợ, nút in hoặc lưu biên nhận.
  * Nút điều hướng phụ: "Tra cứu lịch sử đặt bàn" và "Quay về trang chủ".

### 6.3. Màn hình Tra cứu Lịch sử đặt bàn (`Views/DatBan/LichSu.cshtml`)
* **Thanh tìm kiếm:** Ô nhập số điện thoại hoặc mã Booking Code kèm nút tra cứu tức thì.
* **Danh sách lịch sử dạng Card Timeline:**
  * Mỗi đơn đặt bàn là một Card độc lập với header hiển thị Booking Code, ngày giờ đến và Huy hiệu trạng thái (Chờ xác nhận, Đã xác nhận, Đang phục vụ, Hoàn tất, Đã hủy).
  * Thông tin bàn ăn đã được xếp (nếu có).
  * **Accordion Chi tiết hóa đơn món ăn:**
    * Khi đơn đã nhận bàn hoặc hoàn tất, khách hàng mở rộng để xem danh sách món ăn đã dùng, số lượng, đơn giá và tổng thanh toán.
  * **Nút hành động:**
    * Đơn "Hoàn tất" và chưa đánh giá: Hiển thị nút "★ Đánh giá dịch vụ" màu Amber `#F59E0B` nổi bật.
    * Đơn đã đánh giá: Hiển thị nhãn "Đã gửi đánh giá" màu xanh lá.

### 6.4. Màn hình Đánh giá chất lượng dịch vụ (`Views/DatBan/DanhGia.cshtml`)
* **Interactive 5-star Rating:**
  * Đánh giá chất lượng món ăn (1 - 5 sao vàng tương tác bằng icon sao).
  * Đánh giá chất lượng phục vụ & không gian (1 - 5 sao).
* **Vùng nhập phản hồi:** Textarea mở rộng với gợi ý (placeholder) về trải nghiệm món ăn và thái độ phục vụ.
* **Tải ảnh trải nghiệm:** Cho phép tải ảnh chụp thực tế món ăn/hóa đơn, xem trước thumbnail.
* Nút gửi đánh giá: Primary Button `#E11D48`.

---

## 7. Đặc tả giao diện: Phân hệ Quản lý Đặt bàn & Đánh giá (Admin/Lễ tân)

Tích hợp vào Layout Quản trị `_AdminLayout.cshtml`, màu nền tối Sidebar `#2C7A3E`.

### 7.1. Màn hình Tiếp nhận Đặt bàn (`Views/QuanLyDatBan/Index.cshtml`)
* **Hàng thẻ tóm tắt nhanh (Summary Cards):**
  1. *Chờ duyệt:* Số đơn mới đang chờ xếp bàn (`#F59E0B`).
  2. *Đã xác nhận:* Số đơn đã xếp bàn sắp đến (`#0284C7`).
  3. *Đang phục vụ:* Số bàn đang dùng bữa thực tế (`#10B981`).
  4. *Tổng lượt hôm nay:* Số lượt khách dự kiến trong ngày.
* **Tabs phân loại trạng thái:** Tab Chờ xác nhận, Tab Đã xác nhận, Tab Đang phục vụ, Tab Hoàn tất/Đã hủy.
* **Bảng dữ liệu tiếp nhận:**
  * Cột: Mã đặt bàn, Tên khách & SĐT, Giờ đến, Số khách, Khu vực/Bàn gán, Trạng thái, Thao tác.
  * Cột thao tác nhanh:
    * Đơn Chờ xác nhận: Nút "Xếp bàn" mở Modal chọn bàn còn trống không trùng lịch (±2 giờ).
    * Đơn Đã xác nhận: Nút "Check-in Nhận bàn" (chuyển bàn sang Đang phục vụ, mở Hóa đơn tự động).
    * Nút "Hủy đơn" kèm popup nhập lý do.

### 7.2. Màn hình Quản lý Đánh giá của khách (`Views/QuanLyDatBan/DanhGia.cshtml`)
* Bảng thống kê điểm hài lòng trung bình (Điểm sao trung bình, số lượt đánh giá).
* Danh sách nhận xét của khách: Hiển thị ngày giờ, mã đơn đặt, số sao, nội dung nhận xét của khách và ảnh đính kèm (nếu có).
* Khung phản hồi của Quản lý: Form nhập câu trả lời của nhà hàng và gửi phản hồi công khai đến khách hàng.

---

### 8. ĐẶC TẢ GIAO DIỆN PHÂN HỆ QUẢN LÝ KHO NGUYÊN LIỆU (WEEK 8)
Tuân thủ toàn bộ Design Tokens tại Mục 1: Primary Rose (#E11D48), Dark Navy (#0F172A), Amber (#F59E0B), Emerald (#10B981) và nền Slate (#F8FAFC).

#### 8.1. Màn hình Quản lý Nhà cung cấp (`Views/NhaCungCap/Index.cshtml`)
- **Summary Cards (Đầu trang):**
  + Card 1: Tổng số Nhà cung cấp đang quản lý.
  + Card 2: Nhà cung cấp đang hoạt động/giao dịch tích cực (Badge Emerald).
  + Card 3: Nhà cung cấp tạm ngưng hợp tác.
- **Toolbar & Bộ lọc:**
  + Ô tìm kiếm nhanh: Tìm theo Mã NCC, Tên NCC, SĐT hoặc Mã số thuế.
  + Dropdown trạng thái: "Tất cả", "Đang hợp tác", "Ngừng hợp tác".
  + Nút CTA chính: `+ Thêm Nhà Cung Cấp` (Nút Rose #E11D48, mở Modal).
- **Bảng dữ liệu Nhà cung cấp:**
  + Cột: Mã NCC (#NCC001), Tên đơn vị (In đậm), Người liên hệ, Điện thoại, Email, Địa chỉ, Trạng thái (Pill xanh/xám), Thao tác (Xem lịch sử nhập, Sửa, Đổi trạng thái).
- **Modal Thêm/Sửa Nhà Cung Cấp:**
  + Bố cục Form Grid 2 cột: Tên NCC (Required), Mã số thuế, Người liên hệ, Số điện thoại (Required, Regex Phone), Email, Địa chỉ kho/văn phòng, Ghi chú.

#### 8.2. Màn hình Quản lý Nhập kho (`Views/NhapKho/Index.cshtml` & `Create.cshtml`)
- **Danh sách phiếu nhập (`Index.cshtml`):**
  + Bảng phiếu nhập: Số phiếu (`PN-YYYYMMDD-XXXX`), Ngày nhập, Nhà cung cấp, Nhân viên lập phiếu, Tổng tiền hàng (Format VNĐ), Trạng thái ("Đã nhập kho" - Xanh, "Lưu tạm" - Vàng, "Đã hủy" - Đỏ), Thao tác (Xem chi tiết/In phiếu, Hủy phiếu).
  + Bộ lọc: Khoảng ngày nhập (Từ ngày - Đến ngày), lọc theo Nhà cung cấp.
  + Nút CTA: `+ Lập Phiếu Nhập Kho` dẫn sang trang `Create.cshtml`.
- **Giao diện Tạo phiếu nhập kho (`Create.cshtml` - Bố cục Master-Detail):**
  + *Khối Thông tin chung (Master - Phía trên hoặc Cột trái 35%):* Chọn Nhà cung cấp (Select2/Searchable dropdown), Ngày nhập, Số hóa đơn đỏ/chứng từ kèm theo, Ghi chú nhập hàng.
  + *Khối Danh sách hàng nhập (Detail - Bảng động bên phải/phía dưới):*
    - Thanh chọn nhanh: Dropdown chọn Nguyên liệu + Số lượng + Đơn vị tính + Đơn giá nhập + Hạn sử dụng (Date picker) + Nút "+ Thêm dòng".
    - Bảng chi tiết: STT, Tên nguyên liệu, ĐVT, Số lượng, Đơn giá nhập, Thành tiền, HSD, Nút xóa dòng.
    - Chân bảng: Tổng số lượng mặt hàng, **Tổng tiền thanh toán** (Chữ to màu đỏ #E11D48).
  + *Nút hành động cuối trang:* "Lưu tạm", "Hủy bỏ" (Outline button) và "Xác nhận Nhập kho & Tăng tồn" (Primary Rose button).

#### 8.3. Màn hình Quản lý Xuất kho phục vụ chế biến (`Views/XuatKho/Index.cshtml` & `Create.cshtml`)
- **Danh sách phiếu xuất (`Index.cshtml`):**
  + Bảng phiếu xuất: Số phiếu (`PX-YYYYMMDD-XXXX`), Ngày xuất, Bộ phận nhận (Bếp nóng, Bếp lạnh, Bar), Nhân viên xuất, Lý do xuất ("Phục vụ ca sáng", "Bổ sung đột xuất"), Trạng thái, Thao tác (Xem chi tiết/In phiếu).
- **Giao diện Lập phiếu xuất kho (`Create.cshtml`):**
  + Thông tin chung: Bộ phận/Người nhận hàng, Ngày xuất, Ca làm việc, Ghi chú.
  + Bảng chọn nguyên liệu xuất:
    - Hiển thị cột: Tên nguyên liệu | Tồn kho hiện tại | Số lượng xuất | ĐVT.
    - Ràng buộc trực quan: Ô nhập số lượng xuất tự động báo lỗi đỏ và chặn bấm lưu nếu số lượng xuất > số lượng tồn hiện có.

#### 8.4. Màn hình Theo dõi tồn kho & Cảnh báo sắp hết (`Views/Kho/TonKho.cshtml`)
- **Thẻ cảnh báo nổi bật (Stock Alert Banner):**
  + Thanh Alert màu cam/vàng: *"Hiện có X nguyên liệu đang dưới định mức an toàn và Y nguyên liệu đã cạn kho. Vui lòng tạo phiếu nhập hàng!"* kèm nút thao tác nhanh `Lập phiếu nhập ngay`.
- **Bảng dữ liệu Tồn kho chi tiết:**
  + Cột: Mã NL, Tên nguyên liệu, Danh mục, ĐVT, Tồn kho thực tế, Định mức tối thiểu, Giá trị vốn tồn kho (= Tồn * Giá nhập), Trạng thái (Pill: "An toàn" - Xanh lá, "Sắp hết" - Vàng, "Cạn kho" - Đỏ), Thao tác (Nhập thêm, Lịch sử thẻ kho).
- **Bộ lọc thông minh:**
  + Switch Toggle: "Chỉ hiển thị nguyên liệu cần cảnh báo nhập hàng".
  + Dropdown lọc theo Danh mục nguyên liệu (Thịt, Hải sản, Rau củ, Gia vị, Đồ khô...).

#### 8.5. Màn hình Quản lý Thanh lý / Hủy nguyên liệu (`Views/ThanhLy/Index.cshtml` & `Create.cshtml`)
- **Danh sách phiếu thanh lý (`Index.cshtml`):**
  + Hiển thị các đợt tiêu hủy hoặc thanh lý nguyên vật liệu hỏng/hết hạn.
  + Cột: Số phiếu (`TL-YYYYMMDD-XXXX`), Ngày lập, Nhân viên lập, Lý do (Hết hạn / Ẩm mốc hư hỏng / Đổ vỡ), Tổng giá trị thiệt hại (VNĐ), Thao tác (Xem chi tiết).
- **Giao diện Lập phiếu thanh lý (`Create.cshtml`):**
  + Form Master: Chọn Ngày thanh lý, Lý do chính (Dropdown: Hết hạn sử dụng, Hư hỏng/ẩm mốc, Rơi vỡ/sơ chế hỏng, Khác), Biên bản xác nhận.
  + Bảng Detail: Chọn nguyên liệu cần thanh lý, Số lượng hủy (kiểm tra <= Tồn kho), Đơn giá vốn, Thành tiền thiệt hại, Ghi chú tình trạng từng món.

#### 8.6. Màn hình Báo cáo Nhập - Xuất - Tồn (`Views/Kho/BaoCaoTonKho.cshtml`)
- **Bộ điều khiển báo cáo (Control Bar):**
  + Chọn khoảng thời gian: Dropdown (Hôm nay, Tuần này, Tháng này, Tháng trước, Tùy chọn Từ ngày - Đến ngày).
  + Nút chức năng: "Xem báo cáo", "Xuất file Excel" (Icon file-earmark-excel màu xanh lá), "In báo cáo" (Icon printer).
- **Thống kê chỉ số kỳ báo cáo (KPI Summary):**
  + 4 Thẻ chỉ số: [Giá trị tồn đầu kỳ] | [Tổng giá trị nhập] | [Tổng giá trị xuất chế biến] | [Giá trị hao hụt thanh lý] | [Giá trị tồn cuối kỳ].
- **Bảng tổng hợp Nhập - Xuất - Tồn chi tiết:**
  + Cột: Mã NL | Tên nguyên liệu | ĐVT | [Tồn đầu kỳ (SL & Tiền)] | [Nhập trong kỳ (SL & Tiền)] | [Xuất chế biến (SL & Tiền)] | [Thanh lý/Hủy (SL & Tiền)] | [Tồn cuối kỳ (SL & Tiền)].

---

### 9. ĐẶC TẢ GIAO DIỆN & UX CẢI TIẾN PHÂN HỆ MÓN ĂN - ĐỊNH MỨC - KHO (WEEK 8 REVISION)
Tuân thủ toàn bộ Design Tokens tại Mục 1 và Layout Shell tại Mục 2. Phiên bản cải tiến tập trung giải quyết triệt để trải nghiệm nhập liệu không gián đoạn thông qua kỹ thuật **In-Place AJAX Creation**, phân tách định mức đa kích cỡ (Multi-Size BOM), và khả năng thu gọn không gian làm việc (Collapsible Workspace).

#### 9.1. Thanh điều hướng thu gọn linh hoạt (Collapsible Admin Sidebar)
- **Cơ chế hoạt động:**
  + Trên thanh Topbar bên cạnh Logo, bổ sung nút chuyển đổi (Toggle button) dạng icon `bi-list` hoặc `bi-layout-sidebar-inset`.
  + Trạng thái bình thường: Sidebar rộng `260px`, hiển thị đầy đủ icon và nhãn chức năng.
  + Trạng thái thu gọn (`sidebar-collapsed`): Sidebar thu hẹp còn `72px`, chỉ hiển thị icon căn giữa kèm tooltip chú giải khi hover; Main Content Canvas tự động mở rộng 100% diện tích màn hình.
  + Trạng thái được lưu trong `localStorage.getItem('sidebar_collapsed')` để duy trì trải nghiệm khi chuyển qua lại các trang.

#### 9.2. Form Thêm/Sửa Món ăn tối ưu (Inline Group & Quick-Add Buttons)
- **Tối ưu kích thước trường Danh mục & Nguyên liệu:**
  + Thay vì sử dụng `<select>` chiếm toàn bộ chiều ngang dòng, bố trí dạng `input-group` gọn gàng:
    ```html
    <div class="input-group">
      <select class="form-select form-select-sm" id="cboDanhMuc">...</select>
      <button class="btn btn-outline-rose btn-sm" type="button" data-bs-toggle="modal" data-bs-target="#modalQuickDanhMuc" title="Thêm danh mục mới">
        <i class="bi bi-plus-lg"></i>
      </button>
    </div>
    ```
  + Kích thước dropdown được giới hạn cân đối (`max-width: 320px`), nhường không gian hiển thị cho các trường giá bán, hình ảnh và định mức.
- **Quy chuẩn Modal AJAX Thêm nhanh (Quick Create Modal):**
  + **Tiêu đề modal:** Badge nhỏ Rose `#E11D48` kèm icon, ví dụ: `+ Thêm Nhanh Danh Mục Món`.
  + **Form nhập liệu tối giản:** Chỉ chứa 1 - 2 trường bắt buộc (Tên, Ghi chú), không có trường rườm rà.
  + **Hành vi người dùng:**
    - Người dùng bấm nút `+` -> Modal mở đè lên form chính mà không làm mất bất kỳ ký tự nào đang gõ ở form thêm món ăn.
    - Điền tên -> Bấm "Lưu nhanh" (Nút Rose) -> Gửi AJAX POST -> Hiển thị spinner loading 0.3s.
    - Khi nhận phản hồi `200 OK`: Đóng modal, reset form trong modal, tự động thêm một `<option value="{id}" selected>{name}</option>` vào dropdown ở form chính và kích hoạt hiệu ứng highlight xanh nhẹ (Green pulse) trong 1 giây để người dùng nhận diện.

#### 9.3. Giao diện Định mức phân tách theo Size Món ăn (Multi-Size Recipe Builder)
- **Bố cục Tab Kích cỡ (Size Nav-Tabs):**
  + Phía trên bảng định mức nguyên liệu hiển thị danh sách các kích cỡ của món ăn:
    `[ Size Tiêu chuẩn / S ]` | `[ Size Vừa / M ]` | `[ Size Lớn / L ]` (Active tab có viền dưới Rose #E11D48, nền trắng).
  + Bên cạnh các tab có nút hành động phụ: `Sao chép định mức từ Size khác` (hỗ trợ nhân hệ số nhanh, ví dụ Size L = 1.5 x Size M).
- **Bảng định mức chi tiết cho từng Size:**
  + Cột: Tên nguyên liệu | ĐVT trong kho | Số lượng định mức cho 1 phần | Chi phí giá vốn ước tính | Nút xóa dòng (`bi-trash`).
  + Hàng chân bảng (Footer):
    - Tổng chi phí nguyên vật liệu của riêng size đó (Food Cost Size).
    - Tỷ suất lợi nhuận gộp tạm tính so với giá bán của size tương ứng.

#### 9.4. Form Quản lý Nguyên liệu & Nhà Cung Cấp Cung Ứng (Supplier-Supply View)
- **Khối Nhà cung cấp cũ đã từng cung ứng:**
  + Hiển thị danh sách các nhà cung cấp liên kết với nguyên liệu dạng danh sách thẻ nhỏ (Badges/Tags):
    - Mỗi badge hiển thị: `[Tên NCC - Đơn giá thỏa thuận]` kèm nút `x` gỡ liên kết nếu cần.
  + Dropdown chọn thêm NCC từ danh sách đã có trong hệ thống.
  + Nút `+ Thêm Nhà Cung Cấp Mới` mở Modal AJAX Quick-Add Nhà cung cấp (Tên NCC, Số điện thoại, Người liên hệ) để thêm tức thời và gán vào nguyên liệu đang chỉnh sửa.

#### 9.5. Hiển thị trực quan trạng thái Trừ tồn kho sau khi Nhận bàn
- **Tại màn hình Chi tiết Đơn hàng / Bàn ăn (`DonHang/Details` & `Bep/Index`):**
  + Khi khách vừa nhận bàn và xác nhận gọi món: Hiển thị thông báo Toast góc phải:
    `"Đã tự động xuất kho nguyên liệu cho X món theo định mức kích cỡ bàn [Bàn 5]."`
  + Nếu có nguyên liệu bị cạn kho hoặc không đủ định mức:
    - Hiển thị Banner cảnh báo màu vàng viền cam: *"Cảnh báo: Nguyên liệu [Thịt bò Wagyu] tồn kho chỉ còn 0.2kg, thiếu định mức phục vụ cho đơn hàng!"* kèm nút tắt hoặc lập phiếu nhập khẩn cấp.

