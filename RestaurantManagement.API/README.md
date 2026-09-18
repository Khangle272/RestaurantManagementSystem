# RestaurantManagement.API — database chung

Schema SQL Server được sinh từ `Models/Entities.cs` và `Data/RestaurantDbContext.cs` bằng EF Core migration. Không sửa trực tiếp file migration hoặc `Scripts/InitialSchema.sql` để đổi thiết kế; sửa model rồi tạo migration mới.

## Chuẩn bị

- .NET SDK 10
- SQL Server Express/Developer và SSMS
- Chuỗi kết nối đặt ở biến môi trường, không commit mật khẩu:

```powershell
$env:ConnectionStrings__RestaurantDb='Server=.\SQLEXPRESS;Database=RestaurantManagement;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Mỗi thành viên có thể dùng database local riêng nhưng phải dùng cùng migration. Chỉ đổi tên database trong chuỗi kết nối, không tự đổi bảng/cột.

## Tạo và cập nhật database

Chạy từ thư mục `RestaurantManagementSystem`:

```powershell
dotnet restore RestaurantManagement.API/RestaurantManagement.API.csproj
dotnet build RestaurantManagement.API/RestaurantManagement.API.csproj
dotnet ef database update --project RestaurantManagement.API --startup-project RestaurantManagement.API --context RestaurantDbContext
```

Khi model thay đổi, chỉ người phụ trách database tạo migration mới và gửi cho nhóm:

```powershell
dotnet ef migrations add TenThayDoi --project RestaurantManagement.API --startup-project RestaurantManagement.API --context RestaurantDbContext
dotnet ef migrations script --idempotent --project RestaurantManagement.API --startup-project RestaurantManagement.API --context RestaurantDbContext --output RestaurantManagement.API/Scripts/Database.sql
```

## Dữ liệu minh họa dùng chung

```powershell
dotnet run --project RestaurantManagement.API -- --seed
```

Seed tạo nhân viên, khu vực/bàn, danh mục/món, nguyên liệu/đơn vị/quy đổi, nhà cung cấp, định lượng và phiếu tồn đầu kỳ. Lệnh chạy lại không tạo trùng và không ghi đè dữ liệu đã có. Seed không tạo tài khoản/mật khẩu hay giao dịch khách hàng.

Các mức giá, tên người, số điện thoại và số lượng tồn chỉ là dữ liệu minh họa để kiểm thử CRUD, không phải số liệu khảo sát.

## Quy tắc dùng chung

- Không dùng `EnsureCreated`; luôn dùng migration.
- Không tự sửa `SoLuongTon`; tồn được tính từ phiếu nhập/xuất `DaGhiSo` sau quy đổi về đơn vị cơ sở.
- Không xóa cứng dữ liệu lịch sử; dùng trạng thái ngừng sử dụng khi phù hợp.
- Trước khi lấy code mới, kiểm tra migration mới và chạy `database update` trên database local của mình.
- Sơ đồ database chính thức được tạo từ database đã áp migration trong SSMS.
