using Microsoft.Maui.Controls.Shapes;
using RestaurantManagement.Mobile.Services;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Shared;

namespace RestaurantManagement.Mobile.Views;

// Màn chính nhân viên: sơ đồ bàn, đơn đang mở, tiếp nhận đặt bàn (tiếp tân/quản lý).
public sealed class StaffHomePage : HomeTabsPage
{
    public StaffHomePage() : base($"{ApiClient.Current.User!.HoTen} · {ApiClient.Current.User!.RoleLabel}")
    {
        AddTab(new TablesPage(), "tab_tables.png");
        AddTab(new OpenOrdersPage(), "tab_orders.png");
        if (ApiClient.Current.User!.CanReceive) AddTab(new ReservationsPage(), "tab_booking.png");
        AddTab(new AccountPage(), "tab_account.png");
    }
}

// Sơ đồ bàn theo khu vực: Trống / Đã đặt / Đang phục vụ / Chờ dọn. Lọc nhanh theo trạng thái bằng chip.
public sealed class TablesPage : AppPage
{
    private readonly VerticalStackLayout list = NewList();
    private List<TableDto> tables = [];
    private string? filter;

    public TablesPage() : base("Sơ đồ bàn")
    {
        SetBody(Refreshable(list, LoadAsync));
        AutoRefresh(TimeSpan.FromSeconds(30), LoadAsync);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        List<TableDto> loaded = [];
        if (!await RunAsync(async () => loaded = await ApiClient.Current.TablesAsync()) || !Changed(loaded)) return;
        tables = loaded;
        Render();
    }

    private void Render()
    {
        list.Children.Clear();
        if (tables.Count == 0) { list.Children.Add(Ui.Empty("🪑", "Chưa có bàn nào", "Quản lý thêm bàn trên website.")); return; }
        // Sau khi tự làm mới, trạng thái đang lọc có thể không còn bàn nào (ví dụ bàn chờ dọn vừa dọn xong): quay về Tất cả.
        if (filter is not null && tables.All(x => x.TrangThai != filter)) filter = null;

        // Chip lọc: Tất cả + từng trạng thái kèm số lượng.
        var chips = new List<View> { Ui.Chip($"Tất cả · {tables.Count}", filter is null, () => { filter = null; Render(); }) };
        foreach (var g in tables.GroupBy(x => (x.TrangThai, x.TrangThaiText)))
            chips.Add(Ui.Chip($"{g.Key.TrangThaiText} · {g.Count()}", filter == g.Key.TrangThai, () => { filter = g.Key.TrangThai; Render(); }));
        list.Children.Add(Ui.ChipRow(chips));

        // Nhắc món đã nấu xong đang chờ mang ra.
        var ready = tables.Where(x => x.MonSanSang > 0).ToList();
        if (ready.Count > 0)
        {
            // Chạm thanh nhắc: một bàn thì mở thẳng đơn của bàn đó, nhiều bàn thì lọc các bàn đang phục vụ.
            var banner = Ui.Banner($"🔔 {ready.Sum(x => x.MonSanSang)} món sẵn sàng chờ mang ra: {string.Join(", ", ready.Select(x => x.MaBan))} ›", Ui.Info, Ui.InfoSoft);
            banner.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    if (ready.Count == 1 && ready[0].HoaDonId is int orderId) { await Navigation.PushAsync(new OrderDetailPage(orderId, staff: true)); return; }
                    filter = "DangPhucVu";
                    Render();
                })
            });
            list.Children.Add(banner);
        }

        var shown = tables.Where(x => filter is null || x.TrangThai == filter).ToList();
        foreach (var area in shown.GroupBy(x => (x.Tang, x.KhuVuc)))
        {
            list.Children.Add(Ui.Section($"Tầng {area.Key.Tang} · {area.Key.KhuVuc}", $"{area.Count()} bàn"));
            // Lưới 2 cột ô bàn.
            var grid = new Grid { Padding = new Thickness(8, 0), ColumnSpacing = 0, RowSpacing = 0, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
            var items = area.ToList();
            for (var i = 0; i < items.Count; i++)
            {
                if (i % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                grid.Add(TableTile(items[i]), i % 2, i / 2);
            }
            list.Children.Add(grid);
        }
        list.Children.Add(Stamp);
    }

    private View TableTile(TableDto t)
    {
        var lines = new VerticalStackLayout { Spacing = 2 };
        switch (t.TrangThai)
        {
            case "DangPhucVu":
                lines.Add(Ui.Body((t.KhachHang ?? "Khách lẻ") + (t.SoKhach > 0 ? $" · {t.SoKhach} khách" : ""), 13));
                lines.Add(t.HoaDonId is null
                    ? Ui.Muted("Chưa gọi món")
                    : new Label { Text = $"{t.SoMon} món · {ApiClient.Money(t.TongTien)}", FontSize = 13, FontAttributes = FontAttributes.Bold }.Fg(Ui.Text));
                if (t.MonSanSang > 0) lines.Add(new Label { Text = $"🔔 {t.MonSanSang} món chờ mang ra", FontSize = 12, FontAttributes = FontAttributes.Bold }.Fg(Ui.Info));
                break;
            case "CanDon":
                lines.Add(Ui.Muted("Khách đã về, cần dọn"));
                break;
            default:
                lines.Add(Ui.Muted($"{t.SoCho} chỗ ngồi"));
                if (t.LichSapToi is not null) lines.Add(new Label { Text = $"Lịch {t.LichSapToi}", FontSize = 12, FontAttributes = FontAttributes.Bold }.Fg(Ui.Info));
                break;
        }
        var (accent, _) = Ui.StatusTone(t.TrangThai);
        var tile = Ui.Card(new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(4)), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children =
            {
                // Vạch màu trạng thái bên trái.
                new BoxView { CornerRadius = 2 }.Bg(accent),
                new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { Ui.Title(t.MaBan, 18), Ui.Badge(t.TrangThai, t.TrangThaiText).Start(), lines }
                }.Column(1)
            }
        }, margin: new Thickness(4), padding: new Thickness(10, 12, 12, 12));
        return Tappable(tile, () => OpenAsync(t));
    }

    private async Task OpenAsync(TableDto t)
    {
        var user = ApiClient.Current.User!;
        var options = new List<string>();
        if (t.TrangThai == "DangPhucVu")
        {
            if (t.HoaDonId is not null) options.Add("Xem đơn hàng");
            // Gọi món tại bàn: phục vụ / quản lý (CanServe). Tiếp tân chỉ tạo đơn mang đi / giao hàng.
            if (user.CanOrder && user.CanServe) options.Add(t.HoaDonId is null ? "Gọi món" : "Gọi thêm món");
            if (user.CanServe) options.Add("Kết thúc phục vụ");
        }
        else if (t.TrangThai == "CanDon" && user.CanServe) options.Add("Đã dọn xong");
        if (options.Count == 0)
        {
            // Giải thích vì sao bàn chưa có thao tác và bước tiếp theo, thay vì chỉ nhắc lại trạng thái.
            var next = t.TrangThai switch
            {
                "Trong" => $"Bàn {t.SoCho} chỗ đang trống. Khách đến trực tiếp: tiếp tân nhận khách trên website (Đặt bàn cho khách), sau đó gọi món tại đây.",
                "DaDat" => "Bàn đã được giữ cho lịch đặt. Khi khách đến, nhận bàn ở tab Đặt bàn (tiếp tân) hoặc trên website.",
                "CanDon" => "Bàn đang chờ dọn. Bồi bàn hoặc quản lý báo đã dọn xong để bàn sẵn sàng.",
                _ => $"Trạng thái: {t.TrangThaiText}."
            };
            await DisplayAlertAsync($"Bàn {t.MaBan} · {t.TrangThaiText}", next + (t.LichSapToi is null ? "" : $"\nLịch sắp tới: {t.LichSapToi}."), "Đóng");
            return;
        }

        var choice = await DisplayActionSheetAsync($"Bàn {t.MaBan} · {t.TrangThaiText}", "Đóng", null, options.ToArray());
        switch (choice)
        {
            case "Xem đơn hàng": await Navigation.PushAsync(new OrderDetailPage(t.HoaDonId!.Value, staff: true)); break;
            case "Gọi món": await Navigation.PushAsync(new OrderEditorPage(t.Id, t.MaBan, null)); break;
            case "Gọi thêm món": await Navigation.PushAsync(new OrderEditorPage(t.Id, t.MaBan, t.HoaDonId)); break;
            case "Kết thúc phục vụ" or "Đã dọn xong":
                // Thao tác đổi trạng thái bàn: hỏi lại trước khi gửi.
                var question = choice == "Đã dọn xong" ? $"Xác nhận bàn {t.MaBan} đã dọn xong và sẵn sàng đón khách?" : $"Kết thúc phục vụ bàn {t.MaBan}?";
                if (!await DisplayAlertAsync(choice, question, "Xác nhận", "Không")) return;
                MessageDto? result = null;
                if (await RunAsync(async () => result = choice == "Đã dọn xong"
                    ? await ApiClient.Current.CleanTableAsync(t.Id, t.RowVersion)
                    : await ApiClient.Current.FinishTableAsync(t.Id, t.RowVersion)))
                    await Info(result!.Message);
                await LoadAsync();
                break;
        }
    }
}

// Đơn chưa thanh toán: tại bàn, mang đi, giao hàng. Tạo đơn mang đi/giao hàng mới.
public sealed class OpenOrdersPage : AppPage
{
    private readonly VerticalStackLayout list = NewList();
    private List<OrderSummaryDto> orders = [];
    private string? filter;

    public OpenOrdersPage() : base("Đơn hàng")
    {
        var takeaway = Ui.Primary("+  Tạo đơn mang đi / giao hàng", () => Navigation.PushAsync(new OrderEditorPage(null, null, null)));
        takeaway.Margin = new Thickness(12, 12, 12, 0);
        // Thu ngân không gọi món: chỉ xem đơn và thanh toán.
        takeaway.IsVisible = ApiClient.Current.User!.CanOrder;
        SetBody(new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children = { takeaway, Refreshable(list, LoadAsync).Row(1) }
        });
        AutoRefresh(TimeSpan.FromSeconds(30), LoadAsync);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        List<OrderSummaryDto> loaded = [];
        if (!await RunAsync(async () => loaded = await ApiClient.Current.OpenOrdersAsync()) || !Changed(loaded)) return;
        orders = loaded;
        Render();
    }

    private void Render()
    {
        list.Children.Clear();
        if (orders.Count == 0) { list.Children.Add(Ui.Empty("🧾", "Không có đơn nào đang mở", "Đơn tại bàn được tạo khi gọi món từ sơ đồ bàn.")); list.Children.Add(Stamp); return; }
        if (filter is not null && orders.All(x => x.LoaiDon != filter)) filter = null;
        var chips = new List<View> { Ui.Chip($"Tất cả · {orders.Count}", filter is null, () => { filter = null; Render(); }) };
        foreach (var g in orders.GroupBy(x => (x.LoaiDon, x.LoaiDonText)))
            chips.Add(Ui.Chip($"{g.Key.LoaiDonText} · {g.Count()}", filter == g.Key.LoaiDon, () => { filter = g.Key.LoaiDon; Render(); }));
        list.Children.Add(Ui.ChipRow(chips));
        // Đơn mới nhất lên đầu; đơn có món sẵn sàng chờ mang ra được ưu tiên.
        foreach (var order in orders.Where(x => filter is null || x.LoaiDon == filter).OrderByDescending(x => x.MonSanSang > 0).ThenByDescending(x => x.ThoiDiemLap))
            list.Children.Add(OrderCard(order, staff: true, Navigation));
        list.Children.Add(Stamp);
    }

    public static View OrderCard(OrderSummaryDto o, bool staff, INavigation navigation)
    {
        var bottom = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 8, VerticalOptions = LayoutOptions.Center,
                    Children = { Ui.Body($"{o.SoMon} món", 13) }
                },
                new Label { Text = ApiClient.Money(o.TongTien), FontSize = 16, FontAttributes = FontAttributes.Bold, FontFamily = "OpenSansSemibold" }.Fg(Ui.Brand).Column(1)
            }
        };
        if (o.MonSanSang > 0) ((HorizontalStackLayout)bottom.Children[0]).Add(Ui.Pill($"🔔 {o.MonSanSang} sẵn sàng", Ui.Info, Ui.InfoSoft, 11));

        var card = Ui.Card(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    ColumnSpacing = 8,
                    Children = { Ui.Title(Ui.OrderHeading(o.Ban, o.LoaiDon, o.LoaiDonText), 16), Ui.Badge(o.TrangThai, o.TrangThaiText).Column(1) }
                },
                Ui.Muted($"{o.MaHoaDon} · {VnTime.NgayGio(o.ThoiDiemLap)}" + (o.KhachHang is null ? "" : $" · {o.KhachHang}")),
                Ui.Divider(),
                bottom
            }
        });
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await navigation.PushAsync(new OrderDetailPage(o.Id, staff))) });
        return card;
    }
}

// Tiếp tân: xác nhận yêu cầu bằng bàn gợi ý, nhận bàn khi khách đến.
public sealed class ReservationsPage : AppPage
{
    private readonly VerticalStackLayout list = NewList();

    public ReservationsPage() : base("Đặt bàn")
    {
        SetBody(Refreshable(list, LoadAsync));
        AutoRefresh(TimeSpan.FromSeconds(60), LoadAsync);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        List<ReservationDto> bookings = [];
        if (!await RunAsync(async () => bookings = await ApiClient.Current.ReservationsAsync()) || !Changed(bookings)) return;
        list.Children.Clear();
        list.Children.Add(Ui.Section("Cần xử lý", $"{bookings.Count} lịch"));
        if (bookings.Count == 0)
            list.Children.Add(Ui.Empty("📅", "Không có lịch cần xử lý", "Yêu cầu đặt bàn mới và lịch hôm nay sẽ hiện ở đây."));
        else
            list.Children.Add(Ui.Muted("Yêu cầu chờ xác nhận và lịch hôm nay chưa nhận bàn. Chạm vào lịch để xếp bàn hoặc nhận bàn.").Margin(16, 0));
        foreach (var b in bookings.OrderBy(x => x.GioDen)) list.Children.Add(Tappable(BookingCard(b), () => OpenAsync(b)));
        list.Children.Add(Stamp);
    }

    private static Border BookingCard(ReservationDto b)
    {
        // Cột giờ đến bên trái, thông tin khách bên phải.
        var time = new Border
        {
            WidthRequest = 64, Padding = new Thickness(4, 8), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 10 }, VerticalOptions = LayoutOptions.Start,
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = VnTime.Gio(b.GioDen), FontSize = 17, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }.Fg(Ui.Brand),
                    new Label { Text = VnTime.Local(b.GioDen).ToString("dd/MM"), FontSize = 12, HorizontalTextAlignment = TextAlignment.Center }.Fg(Ui.MutedText)
                }
            }
        }.Bg(Ui.BrandSoft);

        var tags = new HorizontalStackLayout { Spacing = 6, Children = { Ui.Pill($"👥 {b.SoKhach} khách", Ui.Neutral, Ui.NeutralSoft, 11) } };
        if (b.YeuCauVip) tags.Add(Ui.Pill("VIP", Ui.Warning, Ui.WarningSoft, 11));
        tags.Add(b.Ban is null ? Ui.Pill("Chưa có bàn", Ui.Danger, Ui.DangerSoft, 11) : Ui.Pill($"Bàn {b.Ban}", Ui.Success, Ui.SuccessSoft, 11));

        return Ui.Card(new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12,
            Children =
            {
                time,
                new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Grid
                        {
                            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 6,
                            Children = { Ui.Title(b.HoTen, 16), Ui.Badge(b.TrangThai, b.TrangThaiText).Column(1) }
                        },
                        Ui.Muted(b.MaDatBan + (b.SoDienThoai is null ? "" : $" · {b.SoDienThoai}")),
                        tags,
                        Ui.Muted(string.IsNullOrWhiteSpace(b.YeuCau) ? "" : $"“{b.YeuCau}”")
                    }
                }.Column(1)
            }
        });
    }

    private async Task OpenAsync(ReservationDto b)
    {
        var options = new List<string>();
        if (b.CoTheNhanBan) options.Add("Nhận bàn (khách đã đến)");
        // Lịch đã có bàn: "Đổi sang bàn ..."; lịch chưa có bàn: "Chọn bàn ...".
        var verb = b.Ban is null ? "Chọn bàn" : "Đổi sang bàn";
        options.AddRange(b.GoiYBan.Select(t => $"{verb} {t.MaBan} · {t.KhuVuc} · {t.SoCho} chỗ"));
        if (options.Count == 0)
        {
            await Info(b.Ban is null ? "Không còn bàn trống phù hợp. Đổi giờ hoặc chọn bàn trên website." : "Lịch đã có bàn; nhận bàn khi khách đến trong ngày, đổi bàn trên website nếu cần.");
            return;
        }
        var choice = await DisplayActionSheetAsync($"{b.MaDatBan} · {b.HoTen} · {VnTime.Gio(b.GioDen)}", "Đóng", null, options.ToArray());
        MessageDto? result = null;
        bool ok;
        if (choice == "Nhận bàn (khách đã đến)")
            ok = await RunAsync(async () => result = await ApiClient.Current.CheckInAsync(b.Id, b.RowVersion));
        else if (b.GoiYBan.FirstOrDefault(t => choice == $"{verb} {t.MaBan} · {t.KhuVuc} · {t.SoCho} chỗ") is { } table)
            ok = await RunAsync(async () => result = await ApiClient.Current.AssignAsync(b.Id, table.Id, b.RowVersion));
        else return;
        if (ok) await Info(result!.Message);
        await LoadAsync();
    }
}
