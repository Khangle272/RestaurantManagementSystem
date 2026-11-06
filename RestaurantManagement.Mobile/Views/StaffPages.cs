using RestaurantManagement.Mobile.Services;
using RestaurantManagement.Shared.Mobile;

namespace RestaurantManagement.Mobile.Views;

// Màn chính nhân viên: sơ đồ bàn, đơn đang mở, tiếp nhận đặt bàn (tiếp tân/quản lý).
public sealed class StaffHomePage : TabbedPage
{
    public StaffHomePage()
    {
        var user = ApiClient.Current.User!;
        Title = $"{user.HoTen} · {user.RoleLabel}";
        Children.Add(new TablesPage());
        Children.Add(new OpenOrdersPage());
        if (user.CanReceive) Children.Add(new ReservationsPage());
        ToolbarItems.Add(new ToolbarItem("Đăng xuất", null, () => AppNavigator.ShowLogin()));
    }
}

// Sơ đồ bàn theo khu vực: Trống / Đã đặt / Đang phục vụ / Chờ dọn.
public sealed class TablesPage : AppPage
{
    private readonly VerticalStackLayout list = new() { Padding = new Thickness(0, 8, 0, 24) };
    private readonly RefreshView refresh = new();

    public TablesPage() : base("Sơ đồ bàn")
    {
        refresh.Content = new ScrollView { Content = list };
        refresh.Command = new Command(async () => { await LoadAsync(); refresh.IsRefreshing = false; });
        SetBody(refresh);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        List<TableDto> tables = [];
        if (!await RunAsync(async () => tables = await ApiClient.Current.TablesAsync())) return;
        list.Children.Clear();
        var counts = tables.GroupBy(x => x.TrangThaiText).Select(g => $"{g.Key}: {g.Count()}");
        list.Children.Add(new Label { Text = string.Join("  ·  ", counts), Margin = new Thickness(14, 0, 14, 4), FontSize = 13 });
        foreach (var area in tables.GroupBy(x => (x.Tang, x.KhuVuc)))
        {
            list.Children.Add(new Label
            {
                Text = $"Tầng {area.Key.Tang} · {area.Key.KhuVuc}", FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(14, 12, 14, 2)
            });
            foreach (var table in area) list.Children.Add(TableCard(table));
        }
        if (tables.Count == 0) list.Children.Add(Muted("Chưa có bàn nào đang sử dụng."));
    }

    private View TableCard(TableDto t)
    {
        var detail = t.TrangThai switch
        {
            "DangPhucVu" => $"{t.KhachHang ?? "Khách"} · {t.SoKhach} khách" + (t.HoaDonId is null ? " · chưa gọi món"
                : $" · {t.SoMon} món · {ApiClient.Money(t.TongTien)}" + (t.MonSanSang > 0 ? $" · {t.MonSanSang} món chờ mang ra" : "")),
            "CanDon" => "Khách đã về, cần dọn bàn",
            _ => t.LichSapToi is null ? $"{t.SoCho} chỗ" : $"{t.SoCho} chỗ · lịch {t.LichSapToi}"
        };
        var card = Card(new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new VerticalStackLayout { Children = { new Label { Text = t.MaBan, FontSize = 17, FontAttributes = FontAttributes.Bold }, Muted(detail) } },
                new Label { Text = t.TrangThaiText, TextColor = StatusColor(t.TrangThai), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }
                    .Column(1)
            }
        });
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await OpenAsync(t)) });
        return card;
    }

    private async Task OpenAsync(TableDto t)
    {
        var user = ApiClient.Current.User!;
        var options = new List<string>();
        if (t.TrangThai == "DangPhucVu")
        {
            if (t.HoaDonId is not null) options.Add("Xem đơn hàng");
            options.Add(t.HoaDonId is null ? "Gọi món" : "Gọi thêm món");
            if (user.CanServe) options.Add("Kết thúc phục vụ");
        }
        else if (t.TrangThai == "CanDon" && user.CanServe) options.Add("Đã dọn xong");
        if (options.Count == 0) { await Info($"{t.MaBan}: {t.TrangThaiText}." + (t.LichSapToi is null ? "" : $" Lịch sắp tới {t.LichSapToi}.")); return; }

        var choice = await DisplayActionSheetAsync(t.MaBan, "Đóng", null, options.ToArray());
        switch (choice)
        {
            case "Xem đơn hàng": await Navigation.PushAsync(new OrderDetailPage(t.HoaDonId!.Value, staff: true)); break;
            case "Gọi món": await Navigation.PushAsync(new OrderEditorPage(t.Id, t.MaBan, null)); break;
            case "Gọi thêm món": await Navigation.PushAsync(new OrderEditorPage(t.Id, t.MaBan, t.HoaDonId)); break;
            case "Kết thúc phục vụ" or "Đã dọn xong":
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
    private readonly VerticalStackLayout list = new() { Padding = new Thickness(0, 8, 0, 24) };
    private readonly RefreshView refresh = new();

    public OpenOrdersPage() : base("Đơn hàng")
    {
        var takeaway = new Button { Text = "+ Đơn mang đi / giao hàng", Margin = new Thickness(12, 10, 12, 0) };
        takeaway.Clicked += async (_, _) => await Navigation.PushAsync(new OrderEditorPage(null, null, null));
        refresh.Content = new ScrollView { Content = list };
        refresh.Command = new Command(async () => { await LoadAsync(); refresh.IsRefreshing = false; });
        SetBody(new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }, Children = { takeaway, refresh.Row(1) } });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        List<OrderSummaryDto> orders = [];
        if (!await RunAsync(async () => orders = await ApiClient.Current.OpenOrdersAsync())) return;
        list.Children.Clear();
        if (orders.Count == 0) list.Children.Add(new Label { Text = "Không có đơn nào đang mở.", Margin = 16 });
        foreach (var order in orders) list.Children.Add(OrderCard(order, staff: true, Navigation));
    }

    public static View OrderCard(OrderSummaryDto o, bool staff, INavigation navigation)
    {
        var card = Card(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    Children =
                    {
                        new Label { Text = $"{o.Ban} · {o.LoaiDonText}", FontAttributes = FontAttributes.Bold, FontSize = 16 },
                        new Label { Text = o.TrangThaiText, TextColor = StatusColor(o.TrangThai), FontAttributes = FontAttributes.Bold }.Column(1)
                    }
                },
                Muted($"{o.MaHoaDon} · {ApiClient.Time(o.ThoiDiemLap)}" + (o.KhachHang is null ? "" : $" · {o.KhachHang}")),
                new Label { Text = $"{o.SoMon} món · {ApiClient.Money(o.TongTien)}" + (o.MonSanSang > 0 ? $" · {o.MonSanSang} món sẵn sàng" : "") }
            }
        });
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await navigation.PushAsync(new OrderDetailPage(o.Id, staff))) });
        return card;
    }
}

// Tiếp tân: xác nhận yêu cầu bằng bàn gợi ý, nhận bàn khi khách đến.
public sealed class ReservationsPage : AppPage
{
    private readonly VerticalStackLayout list = new() { Padding = new Thickness(0, 8, 0, 24) };
    private readonly RefreshView refresh = new();

    public ReservationsPage() : base("Đặt bàn")
    {
        refresh.Content = new ScrollView { Content = list };
        refresh.Command = new Command(async () => { await LoadAsync(); refresh.IsRefreshing = false; });
        SetBody(refresh);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        List<ReservationDto> bookings = [];
        if (!await RunAsync(async () => bookings = await ApiClient.Current.ReservationsAsync())) return;
        list.Children.Clear();
        list.Children.Add(Muted("Yêu cầu chờ xác nhận và lịch hôm nay chưa nhận bàn.").Margin(14, 0));
        if (bookings.Count == 0) list.Children.Add(new Label { Text = "Không có lịch cần xử lý.", Margin = 16 });
        foreach (var b in bookings)
        {
            var card = Card(new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        Children =
                        {
                            new Label { Text = $"{ApiClient.Time(b.GioDen)} · {b.HoTen}", FontAttributes = FontAttributes.Bold, FontSize = 16 },
                            new Label { Text = b.TrangThaiText, TextColor = StatusColor(b.TrangThai), FontAttributes = FontAttributes.Bold }.Column(1)
                        }
                    },
                    Muted($"{b.MaDatBan} · {b.SoKhach} khách" + (b.YeuCauVip ? " · VIP" : "") + (b.SoDienThoai is null ? "" : $" · {b.SoDienThoai}")),
                    new Label { Text = b.Ban is null ? "Chưa xếp bàn" : $"Bàn {b.Ban}" },
                    Muted(b.YeuCau ?? "")
                }
            });
            card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await OpenAsync(b)) });
            list.Children.Add(card);
        }
    }

    private async Task OpenAsync(ReservationDto b)
    {
        var options = new List<string>();
        if (b.CoTheNhanBan) options.Add("Nhận bàn (khách đã đến)");
        options.AddRange(b.GoiYBan.Select(t => $"Xếp bàn {t.MaBan} · {t.KhuVuc} · {t.SoCho} chỗ"));
        if (options.Count == 0)
        {
            await Info(b.Ban is null ? "Không còn bàn trống phù hợp. Đổi giờ hoặc xếp bàn trên website." : "Lịch đã xếp bàn; nhận bàn khi khách đến trong ngày.");
            return;
        }
        var choice = await DisplayActionSheetAsync($"{b.MaDatBan} · {b.HoTen}", "Đóng", null, options.ToArray());
        MessageDto? result = null;
        bool ok;
        if (choice == "Nhận bàn (khách đã đến)")
            ok = await RunAsync(async () => result = await ApiClient.Current.CheckInAsync(b.Id, b.RowVersion));
        else if (b.GoiYBan.FirstOrDefault(t => choice == $"Xếp bàn {t.MaBan} · {t.KhuVuc} · {t.SoCho} chỗ") is { } table)
            ok = await RunAsync(async () => result = await ApiClient.Current.AssignAsync(b.Id, table.Id, b.RowVersion));
        else return;
        if (ok) await Info(result!.Message);
        await LoadAsync();
    }
}

internal static class ViewExtensions
{
    public static T Column<T>(this T view, int column) where T : BindableObject { Grid.SetColumn(view, column); return view; }
    public static T Row<T>(this T view, int row) where T : BindableObject { Grid.SetRow(view, row); return view; }
    public static T Margin<T>(this T view, double horizontal, double vertical) where T : View { view.Margin = new Thickness(horizontal, vertical); return view; }
}
