# Cập nhật schema 24 bảng nghiệp vụ

Ngày 19/09/2026, nhánh `thanhvien1-3.1-database` đã cập nhật schema theo góp ý giảng viên.

## Thay đổi chính

- Schema nghiệp vụ được rút từ 31 xuống 24 bảng; các bảng Identity và bảng lịch sử migration vẫn được giữ riêng.
- Thêm `MonAnSize` để quản lý giá bán theo size.
- Thêm `ChiTietHoaDon`; hóa đơn liên kết món ăn qua bảng này.
- Đổi `ThanhPhanSet` thành `ChiTietCombo` và `DinhLuongMon` thành `DinhMucMon`.
- Gộp các bảng chi tiết thanh toán, đối trừ cọc, quy đổi đơn vị và nhật ký áp dụng khuyến mãi vào các bảng nghiệp vụ chính, phù hợp phạm vi đồ án.
- Có migration mới: `20260919024114_SimplifySchema`.
- Script dựng database mới: `RestaurantManagement.API/Scripts/InitialSchema.sql`.

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

## Quy ước dữ liệu đã chốt

- Bỏ `MonAn.GiaBan`; giá hiện hành nằm duy nhất tại `MonAnSize.GiaBan`.
- `DatBan` lưu tiền cọc, thời điểm cọc và trạng thái cọc.
- `HoaDon` lưu `DatBanId` và `KhachHangId` (đều có thể rỗng), nhân viên lập hóa đơn, trạng thái, tổng tiền hàng, tiền giảm, tiền cọc đã trừ, tổng thanh toán, voucher, phương thức và thời điểm thanh toán.
- `PhieuXuat.HoaDonId` cho phép rỗng: chỉ gắn hóa đơn khi xuất phục vụ bán hàng; xuất hủy, hao hụt hoặc điều chỉnh không gắn hóa đơn và phân biệt bằng lý do xuất.
- `ChiTietCombo` dùng các cột `ComboId`, `MonAnId`, `SoLuong`; cả `ComboId` và `MonAnId` đều tham chiếu `MonAn`.
- `DanhGia` dùng thang điểm từ 1 đến 5.

## Trạng thái sau cập nhật

- Database theo schema mới có 24 bảng nghiệp vụ, 7 bảng Identity, bảng lịch sử EF Core và bảng `sysdiagrams` của SSMS.
- Migration `SimplifySchema` phải được chạy sau migration `InitialSchema`; không sửa trực tiếp script `InitialSchema.sql` để đổi thiết kế.
- Database mới nên được dựng bằng `RestaurantManagement.API/Scripts/InitialSchema.sql` hoặc chạy toàn bộ migration của EF Core.

## Thành viên khác cần làm

1. Pull nhánh `thanhvien1-3.1-database` sau khi nhóm thống nhất merge.
2. Không tự tạo hoặc đổi tên bảng/cột trong database local.
3. Cập nhật database local bằng migration hoặc chạy script `InitialSchema.sql` khi cần tạo database mới.
4. Nếu database local đang có dữ liệu cần giữ, sao lưu trước khi chạy migration vì schema cũ có bảng đã được gộp/bỏ.

`InitialSchema.sql` là script duy nhất để dựng database mới theo schema 24 bảng nghiệp vụ. Không dùng script cũ hoặc tự sửa trực tiếp script để thay đổi thiết kế.
