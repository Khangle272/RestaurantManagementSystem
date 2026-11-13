using Microsoft.Maui.Controls.Shapes;
using RestaurantManagement.Mobile.Services;

namespace RestaurantManagement.Mobile.Views;

// Màn khởi động: khôi phục phiên đã lưu rồi vào thẳng màn chính, không có phiên thì mở màn đăng nhập.
public sealed class StartupPage : AppPage
{
    private readonly Label status = new() { Text = "Đang mở phiên làm việc…", FontSize = 14, TextColor = Color.FromRgba(255, 255, 255, 0.85), HorizontalTextAlignment = TextAlignment.Center };
    private bool started;

    public StartupPage() : base("Quản lý nhà hàng")
    {
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#2C7A3E");
        SetBody(new VerticalStackLayout
        {
            Spacing = 14, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Border
                {
                    WidthRequest = 96, HeightRequest = 96, StrokeThickness = 0, BackgroundColor = Color.FromRgba(255, 255, 255, 0.18),
                    StrokeShape = new RoundRectangle { CornerRadius = 48 }, HorizontalOptions = LayoutOptions.Center,
                    Content = new Label { Text = "🍽", FontSize = 44, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                },
                new Label { Text = "Quản lý nhà hàng", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                new ActivityIndicator { IsRunning = true, Color = Colors.White, WidthRequest = 28, HeightRequest = 28 },
                status
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (started) return;
        started = true;
        try
        {
            if (await ApiClient.Current.RestoreAsync() is not null) { AppNavigator.ShowHome(); return; }
            AppNavigator.ShowLogin();
        }
        catch (ApiException ex)
        {
            // Mất mạng hoặc server tắt: về màn đăng nhập kèm lý do, phiên đã lưu vẫn giữ để lần sau thử lại.
            AppNavigator.ShowLogin(ex.Message, keepSession: true);
        }
    }
}

// Tài khoản: thông tin người dùng, quyền trên app, máy chủ, giao diện sáng/tối, phiên bản, đăng xuất.
public sealed class AccountPage : AppPage
{
    private readonly HorizontalStackLayout themeChips = new() { Spacing = 8 };

    public AccountPage() : base("Tài khoản")
    {
        var user = ApiClient.Current.User!;
        // Bỏ các từ không bắt đầu bằng chữ cái (ví dụ "(demo)") để ảnh đại diện không thành "A(".
        var initials = string.Concat(user.HoTen.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => char.IsLetter(w[0])).TakeLast(2).Select(w => char.ToUpperInvariant(w[0])));
        if (initials.Length == 0) initials = "?";

        var profile = Ui.Card(new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 14,
            Children =
            {
                new Border
                {
                    WidthRequest = 60, HeightRequest = 60, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 30 },
                    Content = new Label { Text = initials, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                }.Bg(Ui.Brand),
                new VerticalStackLayout
                {
                    Spacing = 4, VerticalOptions = LayoutOptions.Center,
                    Children = { Ui.Title(user.HoTen, 18), Ui.Muted(user.Email), Ui.Pill(user.RoleLabel, Ui.Brand, Ui.BrandSoft).Start() }
                }.Column(1)
            }
        }, margin: new Thickness(12, 12, 12, 4), padding: new Thickness(16));

        // Quyền lấy từ /api/auth/me, giống phân quyền trên website.
        var rights = user.IsCustomer
            ? new[] { ("Theo dõi đơn và tiến độ món", true), ("Lịch sử mua hàng", true), ("Lịch sử tích điểm", true) }
            : new[]
            {
                ("Tạo đơn mang đi / giao hàng", user.CanOrder),
                ("Gọi món tại bàn", user.CanOrder && user.CanServe),
                ("Mang món ra, hủy món, kết thúc phục vụ, dọn bàn", user.CanServe),
                ("Thanh toán tiền mặt / chuyển khoản", user.CanPay),
                ("Xếp bàn và nhận bàn cho lịch đặt", user.CanReceive)
            };
        var rightsList = new VerticalStackLayout { Spacing = 10 };
        foreach (var (text, allowed) in rights)
            rightsList.Add(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(new GridLength(26)), new ColumnDefinition(GridLength.Star) },
                Children =
                {
                    new Label { Text = allowed ? "✓" : "—", FontSize = 15, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }.Fg(allowed ? Ui.Success : Ui.MutedText),
                    new Label { Text = text, FontSize = 14, VerticalOptions = LayoutOptions.Center }.Fg(allowed ? Ui.Text : Ui.MutedText).Column(1)
                }
            });

        var session = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Ui.KeyValue("Máy chủ", ApiClient.Current.Server?.Authority ?? "—"),
                Ui.KeyValue("Phiên hết hạn", ApiClient.Current.ExpiresAt is { } exp ? exp.ToLocalTime().ToString("HH:mm dd/MM/yyyy") : "—"),
                Ui.Muted("Phiên được ghi nhớ trên máy; mở lại app không cần đăng nhập cho tới khi hết hạn hoặc đăng xuất.", 12)
            }
        };

        RenderTheme();
        var app = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Body("Giao diện", 14), themeChips,
                Ui.Divider(),
                Ui.KeyValue("Phiên bản", $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})"),
                Ui.KeyValue("Nền tảng", $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}")
            }
        };

        var logout = Ui.Outline("Đăng xuất", async () =>
        {
            if (await DisplayAlertAsync("Đăng xuất", "Bạn muốn đăng xuất khỏi ứng dụng?", "Đăng xuất", "Ở lại"))
                AppNavigator.ShowLogin();
        }, Ui.Danger);
        logout.Margin = new Thickness(12, 16, 12, 8);

        SetBody(new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(0, 0, 0, 24),
                Children =
                {
                    profile,
                    Ui.Section("Quyền trên ứng dụng"), Ui.Card(rightsList),
                    Ui.Section("Kết nối"), Ui.Card(session),
                    Ui.Section("Ứng dụng"), Ui.Card(app),
                    logout,
                    Ui.Muted("Đồ án hệ thống quản lý nhà hàng · ứng dụng .NET MAUI dùng chung dữ liệu với website.", 12).Margin(16, 4)
                }
            }
        });
    }

    private void RenderTheme()
    {
        themeChips.Children.Clear();
        var current = AppTheming.Saved;
        foreach (var (theme, label) in new[] { (AppTheme.Unspecified, "Theo máy"), (AppTheme.Light, "☀ Sáng"), (AppTheme.Dark, "☾ Tối") })
            themeChips.Add(Ui.Chip(label, current == theme, () => { AppTheming.Apply(theme); RenderTheme(); }));
    }
}

// Lưu lựa chọn giao diện sáng/tối của người dùng.
public static class AppTheming
{
    private const string Key = "RestaurantTheme";

    public static AppTheme Saved => (AppTheme)Preferences.Default.Get(Key, (int)AppTheme.Unspecified);

    public static void Apply(AppTheme theme)
    {
        Preferences.Default.Set(Key, (int)theme);
        if (Application.Current is { } app) app.UserAppTheme = theme;
    }
}
