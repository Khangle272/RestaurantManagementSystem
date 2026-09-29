using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;

namespace RestaurantManagement.Web.Security;

public static class SampleDataImport
{
    public static async Task RunAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var scripts = FindScriptsDirectory();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            foreach (var name in new[] { "PopulateCrudData.sql", "PopulateRestaurantData.sql" })
            {
                var sql = await File.ReadAllTextAsync(Path.Combine(scripts, name));
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Đã nạp {name}.");
            }

            foreach (var table in new[] { "NhanVien", "DanhMuc", "MonAn", "MonAnSize", "BanAn", "KhachHang", "DatBan", "HoaDon", "TaiKhoan" })
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM dbo.[{table}]";
                Console.WriteLine($"{table}: {await command.ExecuteScalarAsync()}");
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static string FindScriptsDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "RestaurantManagement.API", "Scripts");
            if (File.Exists(Path.Combine(path, "PopulateCrudData.sql")) && File.Exists(Path.Combine(path, "PopulateRestaurantData.sql")))
                return path;
        }
        throw new DirectoryNotFoundException("Không tìm thấy RestaurantManagement.API/Scripts trong workspace.");
    }
}
