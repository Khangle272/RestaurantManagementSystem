using RestaurantManagement.Mobile.Services;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Shared;

namespace RestaurantManagement.Mobile.Views;

// Màn chính khách hàng: đơn hàng (trạng thái, lịch sử mua) và lịch sử tích điểm.
public sealed class CustomerHomePage : TabbedPage
{
    public CustomerHomePage()
    {
        Title = ApiClient.Current.User!.HoTen;
        Children.Add(new CustomerOrdersPage());
        Children.Add(new PointsPage());
        ToolbarItems.Add(new ToolbarItem("Đăng xuất", null, () => AppNavigator.ShowLogin()));
    }
}

public sealed class CustomerOrdersPage : AppPage
{
    private readonly VerticalStackLayout list = new() { Padding = new Thickness(0, 8, 0, 24) };
    private readonly RefreshView refresh = new();

    public CustomerOrdersPage() : base("Đơn của tôi")
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
        CustomerSummaryDto? summary = null;
        List<OrderSummaryDto> orders = [];
        if (!await RunAsync(async () =>
            {
                summary = await ApiClient.Current.CustomerSummaryAsync();
                orders = await ApiClient.Current.CustomerOrdersAsync();
            })) return;
        list.Children.Clear();
        list.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = summary!.HoTen, FontSize = 18, FontAttributes = FontAttributes.Bold },
                Muted(summary.SoDienThoai),
                new Label { Text = $"Điểm hiện có: {summary.DiemHienCo:N0}", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C7A3E") },
                Muted($"{summary.SoDonHang} đơn · đã chi {ApiClient.Money(summary.TongChiTieu)}")
            }
        }));
        var open = orders.Where(x => x.TrangThai == "ChuaThanhToan").ToList();
        if (open.Count > 0)
        {
            list.Children.Add(new Label { Text = "Đơn đang phục vụ", FontAttributes = FontAttributes.Bold, Margin = new Thickness(14, 10, 14, 0) });
            foreach (var order in open) list.Children.Add(OpenOrdersPage.OrderCard(order, staff: false, Navigation));
        }
        list.Children.Add(new Label { Text = "Lịch sử mua hàng", FontAttributes = FontAttributes.Bold, Margin = new Thickness(14, 10, 14, 0) });
        var history = orders.Except(open).ToList();
        if (history.Count == 0) list.Children.Add(Muted("Chưa có đơn đã hoàn tất.").Margin(14, 4));
        foreach (var order in history) list.Children.Add(OpenOrdersPage.OrderCard(order, staff: false, Navigation));
    }
}

public sealed class PointsPage : AppPage
{
    private readonly VerticalStackLayout list = new() { Padding = new Thickness(0, 8, 0, 24) };
    private readonly RefreshView refresh = new();

    public PointsPage() : base("Tích điểm")
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
        List<PointEntryDto> points = [];
        if (!await RunAsync(async () => points = await ApiClient.Current.PointsAsync())) return;
        list.Children.Clear();
        list.Children.Add(Muted("Mỗi 10.000 đ thanh toán (sau ưu đãi) được 1 điểm. Đổi điểm lấy voucher trên website.").Margin(14, 0));
        if (points.Count == 0) list.Children.Add(new Label { Text = "Chưa có giao dịch điểm.", Margin = 16 });
        foreach (var p in points)
            list.Children.Add(Card(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                Children =
                {
                    new VerticalStackLayout { Children = { new Label { Text = p.NoiDung }, Muted(VnTime.NgayGio(p.ThoiDiem) + (p.MaHoaDon is null ? "" : $" · {p.MaHoaDon}")) } },
                    new Label
                    {
                        Text = (p.SoDiem > 0 ? "+" : "") + p.SoDiem.ToString("N0"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center,
                        TextColor = p.SoDiem >= 0 ? Color.FromArgb("#2C7A3E") : Color.FromArgb("#B91C1C")
                    }.Column(1)
                }
            }));
    }
}
