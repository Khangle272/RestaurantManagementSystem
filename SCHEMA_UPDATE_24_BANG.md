# Cập nhật schema 24 bảng nghiệp vụ

Ngày 19/09/2026, nhánh `thanhvien1-3.1-database` đã cập nhật schema theo góp ý giảng viên.

## Thay đổi chính

- Schema nghiệp vụ được rút từ 31 xuống 24 bảng; các bảng Identity và bảng lịch sử migration vẫn được giữ riêng.
- Thêm `MonAnSize` để quản lý giá bán theo size.
- Thêm `ChiTietHoaDon`; hóa đơn liên kết món ăn qua bảng này.
- Đổi `ThanhPhanSet` thành `ChiTietCombo` và `DinhLuongMon` thành `DinhMucMon`.
- Gộp các bảng chi tiết thanh toán, đối trừ cọc, quy đổi đơn vị và nhật ký áp dụng khuyến mãi vào các bảng nghiệp vụ chính, phù hợp phạm vi đồ án.
- Có migration mới: `20260919024114_SimplifySchema`.
- Script dựng database mới: `RestaurantManagement.API/Scripts/RestaurantManagementSchema.sql`.

## Thành viên khác cần làm

1. Pull nhánh `thanhvien1-3.1-database` sau khi nhóm thống nhất merge.
2. Không tự tạo hoặc đổi tên bảng/cột trong database local.
3. Cập nhật database local bằng migration hoặc chạy script `RestaurantManagementSchema.sql` khi cần tạo database mới.
4. Nếu database local đang có dữ liệu cần giữ, sao lưu trước khi chạy migration vì schema cũ có bảng đã được gộp/bỏ.

Không dùng `InitialSchema.sql` cũ để tạo database mới sau cập nhật này; file đó chỉ giữ để đối chiếu lịch sử migration.
