using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public sealed class TableExpiryWorker(IServiceScopeFactory scopes, ILogger<TableExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                List<int> ids;
                List<int> expiredHolds;
                using (var scope = scopes.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                    var cutoff = DateTimeOffset.UtcNow.AddHours(-TableService.MaxDiningHours);
                    ids = await db.DatBan.AsNoTracking().Where(x => x.TrangThai == TrangThaiDatBan.DaNhanBan
                        && x.ThoiDiemKetThuc == null && (x.ThoiDiemNhanBan ?? x.GioDen) <= cutoff)
                        .Select(x => x.Id).ToListAsync(stoppingToken);
                    expiredHolds = await db.DatBan.AsNoTracking().Where(x => x.CocTuDong && x.TrangThai == TrangThaiDatBan.ChoCoc
                        && x.HanThanhToanCoc <= DateTimeOffset.UtcNow && x.ThoiDiemBaoChuyenKhoan == null && x.TienCocDaNop == 0)
                        .Select(x => x.Id).ToListAsync(stoppingToken);
                }
                foreach (var id in ids)
                {
                    if (stoppingToken.IsCancellationRequested) return;
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<TableService>().CloseOverdue(id);
                }
                foreach (var id in expiredHolds)
                {
                    if (stoppingToken.IsCancellationRequested) return;
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<TableService>().ExpireUnpaidHold(id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception error) { logger.LogError(error, "Không kiểm tra được thời hạn giữ chỗ/phục vụ bàn; sẽ thử lại sau một phút."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
