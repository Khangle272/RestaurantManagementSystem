# Đưa RestaurantManagementSystem lên Azure miễn phí

Hướng dẫn theo dự án hiện tại, kiểm tra tài liệu Microsoft ngày 26/10/2026. Chưa tạo tài nguyên hoặc đưa dữ liệu lên tài khoản Azure nào trong lượt hướng dẫn này.

Kiến trúc: trình duyệt → Azure App Service Windows (.NET 10, F1 Free) → Azure SQL Database Free tier. Chỉ publish `RestaurantManagement.Web`; project API được Web tham chiếu nên assembly dữ liệu được đóng gói tự động. Không deploy solution MAUI lên App Service.

Gói ZIP đã chuẩn bị cục bộ ở `artifacts/azure/restaurant-web.zip`. File này không chứa mật khẩu Azure. Tạo lại sau khi sửa code bằng `./Prepare-AzurePackage.ps1`.

## 1. Chuẩn bị tài khoản và công cụ

- Tài khoản/subscription Azure tại https://portal.azure.com/. Azure SQL Free tier không hỗ trợ Azure for Students Starter; đây là gói khác với Azure for Students thông thường.
- Máy hiện tại đã có .NET 10 SDK. SSMS chỉ cần nếu chọn phương án sao chép nguyên database.
- Các ví dụ bên dưới dùng resource group `rg-restaurant-demo`, database `RestaurantDB`. Tên SQL server `sql-restaurant-leluu-demo` và Web App `restaurant-leluu-demo` chỉ là ví dụ, cần đổi nếu Azure báo trùng.
- Chọn cùng khu vực cho Web và DB, chẳng hạn Southeast Asia nếu portal cung cấp cả F1 và SQL Free ở đó. Nếu không có F1, tìm khu vực còn hỗ trợ F1 trước khi tạo; không tự chọn B1/S1 để thay thế.

## 2. Tạo resource group

Portal → Resource groups → Create → chọn Subscription → tên `rg-restaurant-demo` → Region → Review + create → Create. Dùng nhóm này cho cả Web và SQL để dễ tìm và quản lý chi phí.

## 3. Tạo Azure SQL Database đúng Free tier

Mở https://aka.ms/azuresqlhub, trong Create a database chọn **Start free**. Không chỉ tạo database Serverless thông thường vì Serverless không mặc nhiên miễn phí.

- Resource group: `rg-restaurant-demo`.
- Database name: `RestaurantDB`.
- Server: Create new. Đặt tên SQL server duy nhất và chọn khu vực. Chọn xác thực SQL (hoặc SQL + Microsoft Entra), tạo login riêng như `restaurantadmin` và lưu mật khẩu.
- Chọn database độc lập; không dùng elastic pool.
- Bảo đảm phần Free tier/Apply offer đang được bật, mức miễn phí hiển thị **100.000 vCore-giây, 32 GB dữ liệu/tháng**.
- Khi hết hạn mức: chọn **Auto-pause the database until next month**. Không chọn tiếp tục chạy có tính phí.
- Estimated cost phải là 0 cho cấu hình free; không bật thêm dịch vụ trả phí cho lần demo, chẳng hạn Defender/Log Analytics nếu portal gợi ý.
- Review + create → Create.

Free tier chỉ áp dụng khi tạo database mới. Không dùng luồng Restore/Copy/Import tạo database trả phí rồi mong chuyển nó về Free. Hạn mức được làm mới theo tháng; hết quota thì DB tạm ngừng đến tháng tiếp theo.

Nguồn: [Azure SQL Free tier](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-tier?view=azuresql), [FAQ](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-tier-faq?view=azuresql-db).

## 4. Cho máy của bạn kết nối SQL

Mở SQL server vừa tạo → **Networking**:

1. Bật public endpoint theo cấu hình Selected networks.
2. Trong Firewall rules, bấm **Add your client IPv4 address** rồi Save.
3. Khi IP Internet của máy thay đổi, cần cập nhật rule này.

Ở trang database → Connection strings → chọn **ADO.NET / SQL authentication**. Chuỗi có dạng:

```text
Server=tcp:sql-restaurant-leluu-demo.database.windows.net,1433;Initial Catalog=RestaurantDB;Persist Security Info=False;User ID=restaurantadmin;Password=<MAT_KHAU_SQL>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;
```

Thay tên server, login và mật khẩu bằng giá trị thật. Giữ `Encrypt=True`, `TrustServerCertificate=False`. Không dùng `Trusted_Connection=True`, `Integrated Security=True` hoặc `.\SQLEXPRESS` cho Azure. Nếu mật khẩu chứa dấu chấm phẩy, phải quote giá trị theo cú pháp connection string của SqlClient. Không commit chuỗi có mật khẩu vào Git.

Nguồn firewall: [Azure SQL networking](https://learn.microsoft.com/en-us/azure/azure-sql/database/firewall-configure?view=azuresql).

## 5. Nạp database — chọn một trong hai cách

### Cách A: tạo lại đúng bộ demo của dự án, dễ nhất

Cách này tạo schema và bộ demo trên database Azure mới; không tự sao chép những sửa đổi tay ngoài bộ demo ở database local. Database local giữ nguyên. Mở **một cửa sổ PowerShell mới** trên máy và chạy:

```powershell
Set-Location 'D:\RestaurantManagementSystem'
$azureSecret = Read-Host 'Dán connection string Azure SQL đã điền mật khẩu' -AsSecureString
$env:ConnectionStrings__DefaultConnection = [System.Net.NetworkCredential]::new('', $azureSecret).Password

if ($env:ConnectionStrings__DefaultConnection -notmatch '\.database\.windows\.net') {
    throw 'Chuỗi kết nối phải trỏ tới Azure SQL, không phải database local.'
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AuthBootstrap__DemoPassword = 'Demo@2026!'
$env:Logging__LogLevel__Default = 'Warning'

try {
    dotnet run --project RestaurantManagement.Web --no-launch-profile -- --init-website-demo
    if ($LASTEXITCODE -ne 0) { throw 'Nạp demo chưa thành công. Đọc lỗi SQL phía trên trước khi tiếp tục.' }
}
finally {
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    Remove-Item Env:AuthBootstrap__DemoPassword -ErrorAction SilentlyContinue
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    Remove-Item Env:Logging__LogLevel__Default -ErrorAction SilentlyContinue
    $azureSecret = $null
}
```

Đóng cửa sổ này khi xong. Trong lúc chạy, máy bạn phải có Internet và IP đã được mở firewall. Lệnh sử dụng `DefaultConnection` để chạy migration, tạo tài khoản và nạp dữ liệu qua mạng thẳng vào Azure. `Development` chỉ đặt tạm cho lệnh local, không đặt lên website công khai. Nếu lệnh thất bại, xem phần xử lý lỗi; không tiếp tục với DB nạp dang dở.

Đợi thông báo đã chuẩn bị `WEB-yyyyMMdd`. Không cần chạy thêm `InitialSchema.sql` hoặc import file `.bak`. Mã và lịch được tạo theo ngày/tháng chạy thực tế.

### Cách B: giữ nguyên toàn bộ database local hiện có

Chỉ dùng cách B nếu cần giữ cả dữ liệu bạn đã sửa tay. Database Azure phải còn trống: chưa chạy cách A và chưa khởi động bản Web có tự migration.

Trong SSMS kết nối `.\SQLEXPRESS` → chuột phải `RestaurantDB` → **Tasks → Generate Scripts**. Chọn **Select specific database objects → Tables → Select all tables**, gồm `__EFMigrationsHistory` và toàn bộ bảng tài khoản/quyền, không chọn server logins/users. Save thành một file Unicode `.sql`, rồi mở **Advanced**:

| Tùy chọn | Giá trị |
|---|---|
| Types of data to script | Schema and data |
| Script for database engine type | Microsoft Azure SQL Database |
| Script USE DATABASE | False |
| Script DROP and CREATE | Script CREATE |
| Script Indexes / Primary keys / Unique keys | True |
| Script Foreign Keys / Check Constraints | True |
| Script Logins / Object-Level Permissions | False |

Generate → mở file. Kết nối SSMS tới `<server>.database.windows.net`, SQL Server Authentication, chọn **RestaurantDB** ở Connection Properties/Connect to database. Mở file trong kết nối Azure này và Execute. Kiểm tra hoàn tất không có dòng lỗi màu đỏ. Script này chỉ chạy một lần trên đích trống; không dùng DROP để xử lý lỗi trên DB đã có dữ liệu.

Chọn engine Azure và chỉ chọn Tables giúp tránh tạo lại database hoặc các tùy chọn cấp SQL Server không được Azure SQL hỗ trợ. Bảng lịch sử migration cần được chuyển để ứng dụng không tạo lại bảng đã có. Không có câu lệnh `USE master`, `CREATE DATABASE`, `BACKUP` hay `RESTORE` nào cần chạy trên đích.

Nguồn: [Generate Scripts Wizard](https://learn.microsoft.com/en-us/ssms/scripting/generate-and-publish-scripts-wizard).

Với cả hai cách, xác nhận trong SSMS trên Azure:

```sql
SELECT DB_NAME() AS DatabaseDangMo;
SELECT COUNT(*) AS SoMon FROM dbo.MonAn;
SELECT COUNT(*) AS SoHoaDon FROM dbo.HoaDon;
SELECT COUNT(*) AS SoDongDiem FROM dbo.LichSuTichDiem;
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
```

Bộ demo hiện tại có 39 hóa đơn và 29 dòng điểm ở lần nạp ngày 26/10; số đếm có thể đổi khi demo hoặc chạy vào ngày khác. Không cần đúng ID bản ghi như local.

## 6. Tạo Web App Windows F1

Portal → **App Services → Create → Web App**. Tránh mẫu **Web App + Database** nếu mẫu đó tự chọn DB/gói có phí.

| Trường | Chọn |
|---|---|
| Resource group | rg-restaurant-demo |
| Name | Tên Web App duy nhất |
| Publish | Code |
| Runtime stack | .NET 10 / .NET 10 (LTS) |
| Operating System | Windows |
| Region | Cùng khu vực DB nếu có thể |
| Windows Plan | Tạo App Service Plan riêng cho demo |
| Pricing plan | **Free F1**, xác nhận giá 0 |
| Application Insights | Không bật cho lần demo miễn phí này |

Nếu portal chỉ hiện .NET Framework 4.8 thì chưa chọn đúng runtime cho Web này. Nếu chưa có .NET 10 trong khu vực được chọn, kiểm tra lại runtime/khu vực trước khi publish; không tự hạ dự án xuống .NET 8.

Sau Create, vào Overview và sao chép **Default domain** đúng như Azure hiển thị; hostname có thể kèm chuỗi định danh/khu vực, không tự đoán theo tên app. URL công khai là `https://<Default-domain>`.

Nguồn: [ASP.NET quickstart](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore), [App Service Windows F1](https://azure.microsoft.com/en-gb/pricing/details/app-service/windows/).

## 7. Cấu hình Web kết nối DB

Web App → **Settings → Environment variables → App settings → Add**:

| Name | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | Chuỗi Azure SQL đã điền đủ password ở bước 4 |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `SCM_DO_BUILD_DURING_DEPLOYMENT` | `false` |

Apply/Save. Tên `ConnectionStrings__DefaultConnection` có **hai dấu gạch dưới**, vì code đọc `GetConnectionString("DefaultConnection")`. Không đặt thành `RestaurantDb` hoặc `AZURE_SQL_CONNECTIONSTRING`. Không cấu hình trùng một connection string khác tên `DefaultConnection` ở tab Connection strings; chỉ dùng một nơi để tránh nhầm giá trị.

Nếu đã có `WEBSITE_RUN_FROM_PACKAGE=1`, xóa setting đó và Apply. Gói hiện tại cần giải nén vào thư mục Web có quyền ghi: ảnh món nằm trong `wwwroot/uploads/monan`, QR đặt bàn nằm trong `App_Data/payment-qr`. Run-from-package sẽ làm `wwwroot` read-only và gây lỗi upload. Không cần đổi `Cookie.SecurePolicy`, giữ đăng nhập chỉ qua HTTPS.

Web App → Configuration/General settings (hoặc TLS/SSL settings tùy giao diện) → **HTTPS Only: On**. App này không cần startup command riêng trên Windows; IIS dùng `web.config` từ `dotnet publish`.

Để Web App truy cập SQL, quay lại SQL server → Networking. Cho demo, có thể bật **Allow Azure services and resources to access this server** rồi Save. Tùy chọn này cho phép kết nối từ mạng Azure rộng hơn riêng Web App; xác thực SQL vẫn bắt buộc. Nếu muốn giới hạn chặt hơn, lấy danh sách Outbound/Possible outbound IP của Web App trong Properties và thêm từng IP vào firewall thay cho tùy chọn này.

Nguồn: [Environment variables](https://learn.microsoft.com/en-us/azure/app-service/configure-common), [Run-from-package chỉ đọc](https://learn.microsoft.com/en-us/azure/app-service/deploy-run-package).

## 8. Publish và upload đúng gói ZIP

Gói đã chuẩn bị: `D:\RestaurantManagementSystem\artifacts\azure\restaurant-web.zip`.

Tạo lại nếu bạn có sửa code:

```powershell
Set-Location 'D:\RestaurantManagementSystem'
.\Prepare-AzurePackage.ps1
```

Script chỉ publish project Web ở Release rồi nén nội dung thư mục publish; không tạo hoặc cập nhật tài nguyên Azure. ZIP phải có `web.config`, `RestaurantManagement.Web.dll` và `wwwroot` ngay ở gốc, không nằm trong một thư mục `publish` lồng bên trong.

Cách không cần CLI: Web App → **Development Tools → Advanced Tools → Go** → Kudu → **Tools → Zip Push Deploy** → kéo file ZIP lên → đợi **Deployment successful**. Với Windows, đây là ZIP deploy giải nén, phù hợp cách lưu ảnh hiện tại. Nếu cập nhật bằng công cụ khác, giữ file upload/QR đã có thay vì chọn Remove additional files.

Nếu không thấy Zip Push Deploy, dùng [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows) cài trên máy:

```powershell
az login
# Chọn đúng subscription nếu tài khoản có nhiều subscription.
az account show --query '{name:name,id:id}' --output table
az webapp deploy --resource-group rg-restaurant-demo --name <TEN_WEB_APP> --src-path 'D:\RestaurantManagementSystem\artifacts\azure\restaurant-web.zip' --type zip --clean false
```

Thay `<TEN_WEB_APP>` trước khi chạy; không gõ nguyên dấu `< >`. CLI dùng quyền đăng nhập Azure để deploy, không cần gửi publish profile hoặc mật khẩu tài khoản Azure vào chat. Restart Web App nếu portal chưa tự restart.

Nguồn: [ZIP deployment](https://learn.microsoft.com/en-us/azure/app-service/deploy-zip).

## 9. Kiểm tra website online

Mở Default domain bằng HTTPS. Chờ lần đầu nếu DB đang khởi động lại sau auto-pause. Kiểm tra:

- `/`: thực đơn có dữ liệu.
- `/admin`: `admin.demo@example.test`, mật khẩu `Demo@2026!`.
- `/Account/Login`: `khach.demo@example.test`, cùng mật khẩu demo.
- `/KhuyenMai`: chương trình demo có bốn mã.
- `/KhachHang`: lịch sử đơn và điểm; `/KhachHang/Vouchers?wallet=true`: hai vé lưu sẵn.
- `/SoDoBan`, `/DonHang`, `/Bep`, `/HoaDon`: bàn/đơn/KDS/thu tiền.
- `/Kho/TonKho`, `/BaoCao`, `/ThongKeDatBan`: tồn kho, biểu đồ và dự trù.
- Thử upload một ảnh món hoặc QR để xác nhận deploy có quyền ghi. Nếu chuyển DB bằng cách B và dữ liệu có đường dẫn ảnh upload, copy cả `wwwroot/uploads/monan` từ máy lên thư mục tương ứng; cấu hình QR đã upload cũng cần copy `App_Data/payment-qr`. SQL không chứa nội dung các file này.

Danh sách đủ bảy tài khoản và kịch bản demo ở `DEMO_WEBSITE.md`. Chỉ đưa dữ liệu mô phỏng lên bản demo dùng mật khẩu công khai này.

## 10. Lỗi hay gặp ở chính dự án này

| Hiện tượng | Kiểm tra |
|---|---|
| Web hiện trang chào mừng Azure | ZIP chưa deploy đúng app hoặc bị lồng thư mục; kiểm tra gốc `web.config` |
| HTTP 500.30 / Application failed to start | Runtime .NET 10, SQL connection string, firewall và startup migration. Mở Log stream/Kudu log để đọc lỗi cụ thể |
| Không kết nối SQL / lỗi 40615 | Add client IP khi nạp local; thêm IP Web/bật rule Azure services khi chạy online |
| Login failed for user | Sai SQL login/password, đang dùng Windows Authentication hoặc server chỉ bật Entra-only |
| Cannot open database requested by login | Sai `Initial Catalog`; phải trỏ database đã tạo/nạp |
| Invalid object name | Nạp DB chưa hoàn tất hoặc đang kết nối nhầm DB; kiểm tra `__EFMigrationsHistory` |
| Mất phiên/không đăng nhập được | Dùng URL HTTPS và kiểm tra dữ liệu tài khoản đã nạp; không sửa cookie sang HTTP để chữa tạm |
| Ảnh/QR không lưu được | Xóa `WEBSITE_RUN_FROM_PACKAGE`; kiểm tra quyền ghi và thư mục dữ liệu |
| Database unavailable sau khi dùng nhiều | Xem Free amount remaining; có thể hết quota tháng và đã AutoPause |
| Website chậm ở lần đầu | F1 không Always On; Web/DB có thể cần khởi động lại sau khi nghỉ |

Trong Portal mở Web App → **Monitoring → Log stream**. Nếu lỗi khởi động không hiện, xem **Diagnose and solve problems** hoặc Kudu `LogFiles`. Không chuyển website công khai sang Development để hiển thị stack trace chứa cấu hình.

## 11. Giữ chi phí và quota đúng mục tiêu demo

- App Service Plan phải vẫn là **F1**. Giới hạn 60 phút CPU/ngày không có nghĩa là 60 phút truy cập theo đồng hồ; hết quota có thể tạm ngừng.
- Azure SQL phải giữ **Free tier + AutoPause khi hết quota**; không đổi sang BillOverUsage. Sau auto-pause do hết quota, DB không dùng được đến khi quota tháng sau làm mới.
- Dự án có `TableExpiryWorker` truy vấn DB **mỗi phút khi Web đang chạy**, nên không thể giả định DB luôn ngủ và không tốn vCore-giây khi không có khách. Khi không demo, Stop Web App, đóng kết nối SSMS/Object Explorer và theo dõi Free amount remaining; DB sẽ tự pause khi đủ điều kiện idle. Không cần thay luật hết hạn cọc để tiết kiệm quota.
- F1 không bảo đảm worker chạy 24/7 khi Web idle. Phù hợp demo; không coi đây là hạ tầng vận hành nhà hàng thật liên tục.
- Kiểm tra Cost Management định kỳ; không thêm tài nguyên có phí ngoài hướng dẫn. Có thể đặt budget alert, nhưng alert không phải cơ chế tự chặn mọi khoản phí.

Tài liệu chính thức: [F1](https://azure.microsoft.com/en-gb/pricing/details/app-service/windows/), [Free SQL và theo dõi quota](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-tier?view=azuresql), [SQL FAQ về kết nối ngăn pause](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-tier-faq?view=azuresql-db).
