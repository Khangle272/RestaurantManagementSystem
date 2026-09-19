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
| **Secondary** | `brand-secondary` | `#0F172A` (Slate 900) | Nền thanh điều hướng bên (Sidebar), tiêu đề trang chính, màu chữ đậm |
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

* **Sidebar điều hướng (Cố định chiều rộng: 260px):** Sử dụng tông màu tối `#0F172A`, các icon vector tối giản; phân định rõ phân hệ "Thực đơn món ăn" và "Kho nguyên liệu".


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


* Dropdown: Lọc theo Đơn vị tính (Tất cả, kg, gram, lít, lon, chai, gói...).


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