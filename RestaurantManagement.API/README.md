# RestaurantManagement.API — tầng dữ liệu dùng chung

Project này chứa mô hình dữ liệu và database dùng chung cho Web, REST API và bộ kiểm thử. REST API `/api` cho mobile **không** chạy ở đây: nó nằm trong `RestaurantManagement.Web/Api` để dùng chung service nghiệp vụ với website (xem [API_MOBILE.md](../API_MOBILE.md)).

| Thành phần | Nội dung |
| --- | --- |
| `Models/Entities.cs` | Entity và enum của toàn hệ thống (sửa ở đây khi đổi thiết kế) |
| `Data/RestaurantDbContext.cs` | Khóa, khóa ngoại, CHECK, chỉ mục, `RowVersion` chống ghi đè đồng thời; nguồn sinh migration |
| `Data/RestaurantDbContextFactory.cs` | Cho lệnh `dotnet ef` chạy khi chưa cấu hình ứng dụng |
| `Data/DbSeeder.cs` | Dữ liệu minh họa CRUD, chạy bằng `--seed` |
| `Migrations/` | Lịch sử migration từ `InitialSchema` (18/09) tới `PaymentLedgerAndAreaDeposit` (29/10) |
| `Scripts/` | `InitialSchema.sql` (schema gốc tới `SimplifySchema`), các script nạp dữ liệu `Populate*.sql`, `SeedPosKdsPaymentDemo.sql` và hai script nâng cấp thủ công |
| `Program.cs` | Chỉ có lệnh `--seed`; chạy không tham số sẽ in hướng dẫn rồi thoát |

## Cài đặt (mỗi máy một lần)

- .NET 10 SDK (`dotnet --list-sdks` có bản 10.x); Visual Studio 2026 nếu dùng IDE (bản 2022 không hỗ trợ .NET 10).
- SQL Server Express, instance `SQLEXPRESS` (máy dùng tên khác thì sửa chuỗi kết nối) và SSMS để xem database/vẽ sơ đồ.
- Công cụ EF: `dotnet tool install --global dotnet-ef`.

## Chuỗi kết nối

Web và project này cùng đọc `ConnectionStrings:DefaultConnection`:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=RestaurantDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Mỗi thành viên có thể dùng database local riêng nhưng phải dùng cùng migration; chỉ đổi tên instance/database, không tự đổi bảng/cột.

## Tạo và cập nhật database

Web tự áp dụng mọi migration còn thiếu khi khởi động, nên thường chỉ cần chạy Web. Muốn cập nhật database mà không chạy Web (từ thư mục gốc repository):

```powershell
dotnet ef database update --project RestaurantManagement.API --startup-project RestaurantManagement.API --context RestaurantDbContext
```

Khi đổi model, người phụ trách database tạo migration mới và kiểm tra không còn thay đổi treo:

```powershell
dotnet ef migrations add TenThayDoi --project RestaurantManagement.API --startup-project RestaurantManagement.API --context RestaurantDbContext
dotnet ef migrations has-pending-model-changes --project RestaurantManagement.API --startup-project RestaurantManagement.API
```

Nếu dựng database bằng SSMS từ `InitialSchema.sql`, script chỉ tạo schema gốc; lần chạy Web đầu tiên sẽ áp các migration sau đó. Nên dùng lệnh Web `--import-sql-data` hoặc `--init-website-demo` (xem [README](../README.md)) để nạp dữ liệu đúng thứ tự.

## Dữ liệu minh họa

```powershell
dotnet run --project RestaurantManagement.API -- --seed
```

Seed áp migration rồi tạo nhân viên, khu vực/bàn, danh mục/món + size (mỗi món có dòng “Mặc định”), nguyên liệu, nhà cung cấp, định mức, combo và phiếu tồn đầu kỳ. Chạy lại không tạo trùng và không ghi đè dữ liệu đã có. Seed không tạo tài khoản/mật khẩu hay giao dịch khách hàng. Giá, tên người, số điện thoại và tồn kho chỉ là dữ liệu tự soạn để kiểm thử, không phải số liệu khảo sát. Không chạy trên database dữ liệu thật.

## Quy tắc dùng chung

- Không dùng `EnsureCreated`; luôn dùng migration. Không sửa hay xóa migration đã phát hành, kể cả `InitialSchema`.
- Không sửa trực tiếp file migration hay `InitialSchema.sql` để đổi thiết kế; sửa model rồi tạo migration mới.
- Tồn kho tính từ phiếu nhập/xuất đã ghi sổ (tồn = tổng nhập − tổng xuất); không có cột tồn cho sửa tự do.
- Không xóa cứng dữ liệu lịch sử; dùng trạng thái ngừng sử dụng khi phù hợp.
- Trước khi lấy code mới, kiểm tra migration mới và cập nhật database local của mình.
