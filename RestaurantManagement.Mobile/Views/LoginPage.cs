using RestaurantManagement.Mobile.Services;

namespace RestaurantManagement.Mobile.Views;

// Đăng nhập bằng tài khoản website: nhân viên phục vụ hoặc khách hàng.
public sealed class LoginPage : AppPage
{
    private readonly Entry server = new() { Placeholder = "https://ten-tunnel.trycloudflare.com", Keyboard = Keyboard.Url };
    private readonly Entry email = new() { Placeholder = "Email", Keyboard = Keyboard.Email };
    private readonly Entry password = new() { Placeholder = "Mật khẩu", IsPassword = true };
    private readonly Label error = new() { TextColor = Color.FromArgb("#B91C1C"), IsVisible = false };

    public LoginPage(string? message = null) : base("Đăng nhập")
    {
        server.Text = Preferences.Default.Get("RestaurantServer", "");
        email.Text = Preferences.Default.Get("RestaurantEmail", "");
        if (message is not null) { error.Text = message; error.IsVisible = true; }
        var login = new Button { Text = "Đăng nhập", Margin = new Thickness(0, 8, 0, 0) };
        login.Clicked += async (_, _) => await LoginAsync();
        password.Completed += async (_, _) => await LoginAsync();

        SetBody(new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 32), Spacing = 12,
                Children =
                {
                    new Label { Text = "Quản lý nhà hàng", FontSize = 28, FontAttributes = FontAttributes.Bold },
                    Muted("Nhân viên: xem sơ đồ bàn, gọi món, phục vụ, thanh toán, tiếp nhận đặt bàn. Khách hàng: theo dõi đơn, lịch sử mua và điểm tích lũy."),
                    new Label { Text = "Địa chỉ máy chủ", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    server,
                    Muted("Dùng URL HTTPS của website (Cloudflare Tunnel khi demo). Android emulator: https://10.0.2.2:7167."),
                    new Label { Text = "Tài khoản", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    email, password, error, login
                }
            }
        });
    }

    private async Task LoginAsync()
    {
        error.IsVisible = false;
        var uri = ApiClient.ParseServer(server.Text, out var invalid);
        if (uri is null) { error.Text = invalid; error.IsVisible = true; return; }
        if (string.IsNullOrWhiteSpace(email.Text) || string.IsNullOrEmpty(password.Text))
        {
            error.Text = "Nhập email và mật khẩu.";
            error.IsVisible = true;
            return;
        }
        var ok = await RunAsync(async () =>
        {
            await ApiClient.Current.LoginAsync(uri, email.Text, password.Text);
            Preferences.Default.Set("RestaurantServer", uri.AbsoluteUri.TrimEnd('/'));
            Preferences.Default.Set("RestaurantEmail", email.Text.Trim());
        });
        if (ok) { password.Text = ""; AppNavigator.ShowHome(); }
    }
}
