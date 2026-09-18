# RestaurantManagement.API — database chung

## Nhanh da up gi (doc 1 phut la hieu)
- `Scripts/InitialSchema.sql` (script tao san 39 bang — chi can mo bang SSMS roi Execute)
- `Migrations/` (3 file — lich su sinh script, EF dung de nang cap DB sau nay)
- `Models/Entities.cs` (32 lop C# mo ta bang — sua day khi doi thiet ke)
- `Data/RestaurantDbContext.cs` (dinh nghia khoa/FK/CHECK — nguon sinh migration)
- `Data/RestaurantDbContextFactory.cs` (giup lenh EF chay duoc khi chua co SQL)
- `Data/DbSeeder.cs` (nap data mau CRUD — chay bang `--seed`)
- `Program.cs`, `.csproj` (dang ky DB + thu vien EF can thiet)

## Cai gi truoc (moi may lam 1 lan)
- Visual Studio 2026 Community (ban 2022 bao loi .NET 10) + workload ASP.NET
- .NET 10 SDK (`dotnet --list-sdks` thay 10.x)
- SQL Server Express, instance `SQLEXPRESS` (may ai ten khac thi sua chuoi ket noi)
- SSMS 22 (xem DB + ve so do)
- Tool EF: `dotnet tool install --global dotnet-ef`

Schema SQL Server được sinh từ `Models/Entities.cs` và `Data/RestaurantDbContext.cs` bằng EF Core migration. Không sửa trực tiếp file migration hoặc `Scripts/InitialSchema.sql` để đổi thiết kế; sửa model rồi tạo migration mới.

## Chuỗi kết nối (mỗi máy theo instance của mình)

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
