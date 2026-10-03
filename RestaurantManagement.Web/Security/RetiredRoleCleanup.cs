using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;

namespace RestaurantManagement.Web.Security;

// Explicit Development command only. Never delete accounts during normal startup.
public static class RetiredRoleCleanup
{
    public static async Task RunAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var roles = await db.Roles.Where(x => x.Name == "ThucDon" || x.Name == "DanhMucMon").ToListAsync();
        var roleIds = roles.Select(x => x.Id).ToList();
        var userIds = await db.UserRoles.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.UserId).Distinct().ToListAsync();
        var employees = await db.NhanVien.Where(x => x.ChucVu == "ThucDon" || x.ChucVu == "DanhMucMon"
            || (x.TaiKhoanId.HasValue && userIds.Contains(x.TaiKhoanId.Value))).ToListAsync();
        var employeeIds = employees.Select(x => x.Id).ToList();
        // Abort the whole batch if any identity is shared or any business history exists.
        if (await db.UserRoles.AnyAsync(x => userIds.Contains(x.UserId) && !roleIds.Contains(x.RoleId))
            || await db.KhachHang.AnyAsync(x => x.TaiKhoanId.HasValue && userIds.Contains(x.TaiKhoanId.Value))
            || employees.Any(x => x.TaiKhoanId.HasValue && !userIds.Contains(x.TaiKhoanId.Value))
            || await db.DatBan.AnyAsync(x => (x.NhanVienTiepNhanId.HasValue && employeeIds.Contains(x.NhanVienTiepNhanId.Value))
                || (x.NhanVienHuyId.HasValue && employeeIds.Contains(x.NhanVienHuyId.Value)))
            || await db.HoaDon.AnyAsync(x => employeeIds.Contains(x.NhanVienId))
            || await db.PhieuNhap.AnyAsync(x => employeeIds.Contains(x.NhanVienId))
            || await db.PhieuXuat.AnyAsync(x => employeeIds.Contains(x.NhanVienId)))
            throw new InvalidOperationException("Không xóa: vai trò mẫu cũ có tài khoản dùng chung hoặc lịch sử nghiệp vụ. Không dữ liệu nào bị xóa. Admin hãy phân công lại hồ sơ thay vì xóa lịch sử.");

        var users = await db.Users.Where(x => userIds.Contains(x.Id)).ToListAsync();
        Console.WriteLine($"Database: {db.Database.GetDbConnection().Database}; kiểm tra {roles.Count} vai trò, {users.Count} tài khoản, {employees.Count} hồ sơ; không có lịch sử liên quan.");
        db.Set<IdentityUserClaim<int>>().RemoveRange(await db.Set<IdentityUserClaim<int>>().Where(x => userIds.Contains(x.UserId)).ToListAsync());
        db.Set<IdentityUserLogin<int>>().RemoveRange(await db.Set<IdentityUserLogin<int>>().Where(x => userIds.Contains(x.UserId)).ToListAsync());
        db.Set<IdentityUserToken<int>>().RemoveRange(await db.Set<IdentityUserToken<int>>().Where(x => userIds.Contains(x.UserId)).ToListAsync());
        db.UserRoles.RemoveRange(await db.UserRoles.Where(x => userIds.Contains(x.UserId)).ToListAsync());
        db.Set<IdentityRoleClaim<int>>().RemoveRange(await db.Set<IdentityRoleClaim<int>>().Where(x => roleIds.Contains(x.RoleId)).ToListAsync());
        db.NhanVien.RemoveRange(employees);
        db.Users.RemoveRange(users);
        db.Roles.RemoveRange(roles);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine("Đã xóa các vai trò/tài khoản/hồ sơ mẫu cũ đã kiểm tra. Chạy lại không xóa thêm dữ liệu khác.");
    }
}
