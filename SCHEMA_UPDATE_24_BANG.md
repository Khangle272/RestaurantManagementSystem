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

## Chi tiết các bảng được gộp hoặc thay thế

| Bảng cũ | Cách xử lý mới | Lý do |
|---|---|---|
| `DonHang`, `DonHangBan`, `ChiTietDonHang` | Dùng `HoaDon` và `ChiTietHoaDon`. Bàn của đoàn đã quản lý tại `ChiTietDatBan`. | Tránh đồng thời có đơn hàng và hóa đơn cùng lưu một nghiệp vụ bán hàng, gây trùng dữ liệu. |
| `GiaoDichThanhToan`, `DoiTruCoc` | Cọc lưu tại `DatBan`; tổng tiền, tiền giảm, tiền cọc được trừ và phương thức thanh toán lưu tại `HoaDon`. | Đồ án không cần mô hình kế toán nhiều giao dịch/hoàn tiền riêng. |
| `DonViTinh`, `QuyDoiNguyenLieu` | Lưu `DonViTinh` trực tiếp trong `NguyenLieu`. | Không triển khai quy đổi phức tạp giữa kg, g, thùng, chai trong phạm vi hiện tại. |
| `SuDungVoucher`, `ApDungKhuyenMai` | `HoaDon` lưu voucher áp dụng và tổng tiền giảm; `KhuyenMaiMon` vẫn xác định khuyến mãi theo món. | Không cần nhật ký áp dụng nhiều tầng cho mỗi khuyến mãi/voucher ở giai đoạn này. |
| `ThanhPhanSet` | Đổi tên thành `ChiTietCombo`. | Tên phản ánh đúng nghiệp vụ combo gồm món thành phần và số lượng. |
| `ChiTietDonHang` | Thay bằng `ChiTietHoaDon`. | Đúng góp ý: Hóa đơn phải liên kết Món ăn thông qua Chi tiết hóa đơn. |
| Không có ở schema cũ | Thêm `MonAnSize`. | Một món có thể có nhiều size và giá bán theo size; món không có size dùng size “Mặc định”. |

## Thành viên khác cần làm

1. Pull nhánh `thanhvien1-3.1-database` sau khi nhóm thống nhất merge.
2. Không tự tạo hoặc đổi tên bảng/cột trong database local.
3. Cập nhật database local bằng migration hoặc chạy script `RestaurantManagementSchema.sql` khi cần tạo database mới.
4. Nếu database local đang có dữ liệu cần giữ, sao lưu trước khi chạy migration vì schema cũ có bảng đã được gộp/bỏ.

Không dùng `InitialSchema.sql` cũ để tạo database mới sau cập nhật này; file đó chỉ giữ để đối chiếu lịch sử migration.
