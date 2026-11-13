using Microsoft.Maui.Controls.Shapes;
using RestaurantManagement.Mobile.Services;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Shared;

namespace RestaurantManagement.Mobile.Views;

// Màn chính khách hàng: đơn hàng (trạng thái, lịch sử mua) và lịch sử tích điểm.
public sealed class CustomerHomePage : HomeTabsPage
{
    public CustomerHomePage() : base(ApiClient.Current.User!.HoTen)
    {
        AddTab(new CustomerOrdersPage(), "tab_orders.png");
        AddTab(new PointsPage(), "tab_points.png");
        AddTab(new AccountPage(), "tab_account.png");
    }
}

public sealed class CustomerOrdersPage : AppPage
{
    private readonly VerticalStackLayout list = NewList();

    public CustomerOrdersPage() : base("Đơn của tôi")
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
        CustomerSummaryDto? summary = null;
        List<OrderSummaryDto> orders = [];
        if (!await RunAsync(async () =>
            {
                summary = await ApiClient.Current.CustomerSummaryAsync();
                orders = await ApiClient.Current.CustomerOrdersAsync();
            }) || !Changed(new object[] { summary!, orders })) return;
        list.Children.Clear();
        list.Children.Add(ProfileCard(summary!));

        var open = orders.Where(x => x.TrangThai == "ChuaThanhToan").ToList();
        if (open.Count > 0)
        {
            list.Children.Add(Ui.Section("Đơn đang phục vụ", $"{open.Count} đơn"));
            foreach (var order in open) list.Children.Add(OpenOrdersPage.OrderCard(order, staff: false, Navigation));
        }
        var history = orders.Except(open).ToList();
        list.Children.Add(Ui.Section("Lịch sử mua hàng", history.Count == 0 ? null : $"{history.Count} đơn"));
        if (history.Count == 0) list.Children.Add(Ui.Empty("🧾", "Chưa có đơn đã hoàn tất", "Đơn đã thanh toán sẽ hiện ở đây."));
        foreach (var order in history) list.Children.Add(OpenOrdersPage.OrderCard(order, staff: false, Navigation));
        list.Children.Add(Stamp);
    }

    // Thẻ thành viên: tên, điểm hiện có nổi bật, số đơn và tổng chi tiêu.
    private static View ProfileCard(CustomerSummaryDto s) => new Border
    {
        Margin = new Thickness(12, 12, 12, 4), Padding = new Thickness(18, 16), StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Background = new LinearGradientBrush(
            [new GradientStop(Color.FromArgb("#2C7A3E"), 0f), new GradientStop(Color.FromArgb("#1F5A2D"), 1f)], new Point(0, 0), new Point(1, 1)),
        Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.12f, Radius = 10, Offset = new Point(0, 4) },
        Content = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "THÀNH VIÊN", FontSize = 11, CharacterSpacing = 1, TextColor = Color.FromRgba(255, 255, 255, 0.75) },
                new Label { Text = s.HoTen, FontSize = 20, FontAttributes = FontAttributes.Bold, FontFamily = "OpenSansSemibold", TextColor = Colors.White },
                new Label { Text = s.SoDienThoai, FontSize = 13, TextColor = Color.FromRgba(255, 255, 255, 0.8), IsVisible = !string.IsNullOrWhiteSpace(s.SoDienThoai) },
                new HorizontalStackLayout
                {
                    Spacing = 6, Margin = new Thickness(0, 12, 0, 0),
                    Children =
                    {
                        new Label { Text = s.DiemHienCo.ToString("N0", ApiClient.Vietnamese), FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        new Label { Text = "điểm", FontSize = 15, TextColor = Colors.White, VerticalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 0, 7) }
                    }
                },
                new Label { Text = $"{s.SoDonHang} đơn · đã chi {ApiClient.Money(s.TongChiTieu)}", FontSize = 13, TextColor = Color.FromRgba(255, 255, 255, 0.85) }
            }
        }
    };
}

public sealed class PointsPage : AppPage
{
    private readonly VerticalStackLayout list = NewList();

    public PointsPage() : base("Tích điểm")
    {
        SetBody(Refreshable(list, LoadAsync));
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
        list.Children.Add(Ui.Banner("💡 Mỗi 10.000 đ thanh toán (sau ưu đãi) được 1 điểm. Đổi điểm lấy voucher trên website.", Ui.Brand, Ui.BrandSoft));
        if (points.Count == 0) { list.Children.Add(Ui.Empty("⭐", "Chưa có giao dịch điểm", "Điểm được cộng sau mỗi lần thanh toán.")); return; }
        list.Children.Add(Ui.Section("Lịch sử điểm", $"{points.Count} giao dịch"));
        foreach (var p in points)
        {
            var plus = p.SoDiem >= 0;
            list.Children.Add(Ui.Card(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 12,
                Children =
                {
                    // Biểu tượng tròn: xanh khi cộng, đỏ khi trừ điểm.
                    new Border
                    {
                        WidthRequest = 36, HeightRequest = 36, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 18 }, VerticalOptions = LayoutOptions.Center,
                        Content = new Label { Text = plus ? "+" : "−", FontSize = 18, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                            .Fg(plus ? Ui.Success : Ui.Danger)
                    }.Bg(plus ? Ui.SuccessSoft : Ui.DangerSoft),
                    new VerticalStackLayout
                    {
                        Spacing = 2, VerticalOptions = LayoutOptions.Center,
                        Children = { Ui.Body(p.NoiDung), Ui.Muted(VnTime.NgayGio(p.ThoiDiem) + (p.MaHoaDon is null ? "" : $" · {p.MaHoaDon}"), 12) }
                    }.Column(1),
                    new Label
                    {
                        Text = (plus ? "+" : "") + p.SoDiem.ToString("N0", ApiClient.Vietnamese), FontSize = 16, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center
                    }.Fg(plus ? Ui.Success : Ui.Danger).Column(2)
                }
            }));
        }
    }
}
