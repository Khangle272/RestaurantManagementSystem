using Microsoft.Maui.Controls.Shapes;
using RestaurantManagement.Mobile.Services;

namespace RestaurantManagement.Mobile.Views;

// Đăng nhập bằng tài khoản website: nhân viên phục vụ hoặc khách hàng.
public sealed class LoginPage : AppPage
{
    private readonly Entry server = new() { Placeholder = "https://ten-tunnel.trycloudflare.com", Keyboard = Keyboard.Url };
    private readonly Entry email = new() { Placeholder = "ten@email.com", Keyboard = Keyboard.Email, ReturnType = ReturnType.Next };
    private readonly Entry password = new() { Placeholder = "Nhập mật khẩu", IsPassword = true, ReturnType = ReturnType.Go };
    private readonly Label errorText = new() { FontSize = 13 };
    private readonly Border error;
    private readonly Button login;
    private readonly View serverEditor;
    private readonly Grid serverSummary;
    private readonly Label serverHost = new() { FontSize = 13, LineBreakMode = LineBreakMode.MiddleTruncation };

    public LoginPage(string? message = null) : base("Đăng nhập")
    {
        NavigationPage.SetHasNavigationBar(this, false);
        server.Text = Preferences.Default.Get("RestaurantServer", "");
        email.Text = Preferences.Default.Get("RestaurantEmail", "");

        error = new Border
        {
            IsVisible = false, Padding = new Thickness(12, 10), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = errorText.Fg(Ui.Danger)
        }.Bg(Ui.DangerSoft);
        if (message is not null) ShowError(message);

        login = Ui.Primary("Đăng nhập", LoginAsync);
        login.Margin = new Thickness(0, 6, 0, 0);
        email.Completed += (_, _) => password.Focus();
        password.Completed += async (_, _) => await LoginAsync();

        // Ô mật khẩu có nút Hiện/Ẩn.
        var toggle = new Label { Text = "Hiện", FontSize = 13, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, Padding = new Thickness(8, 0, 2, 0) }.Fg(Ui.Brand);
        toggle.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { password.IsPassword = !password.IsPassword; toggle.Text = password.IsPassword ? "Hiện" : "Ẩn"; })
        });
        var passwordRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Children = { password, toggle.Column(1) } };

        // Máy chủ: đã lưu thì thu gọn thành một dòng, chạm "Đổi" để sửa.
        serverEditor = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Ui.Field("Địa chỉ máy chủ", server),
                Ui.Muted("URL HTTPS của website (Cloudflare Tunnel khi demo). Android emulator: https://10.0.2.2:7167.", 12)
            }
        };
        var change = new Label { Text = "Đổi", FontSize = 13, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }.Fg(Ui.Brand);
        change.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => ShowServerEditor(true)) });
        serverSummary = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 6,
            Children = { Ui.Muted("Máy chủ:", 13), serverHost.Fg(Ui.Text).Column(1), change.Column(2) }
        };
        ShowServerEditor(string.IsNullOrWhiteSpace(server.Text));

        var hero = new VerticalStackLayout
        {
            Padding = new Thickness(24, 56, 24, 64), Spacing = 6,
            Background = new LinearGradientBrush(
                [new GradientStop(Color.FromArgb("#2C7A3E"), 0f), new GradientStop(Color.FromArgb("#1F5A2D"), 1f)], new Point(0, 0), new Point(1, 1)),
            Children =
            {
                new Border
                {
                    WidthRequest = 72, HeightRequest = 72, StrokeThickness = 0, BackgroundColor = Color.FromRgba(255, 255, 255, 0.18),
                    StrokeShape = new RoundRectangle { CornerRadius = 36 }, HorizontalOptions = LayoutOptions.Start,
                    Content = new Label { Text = "🍽", FontSize = 34, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                },
                new Label { Text = "Quản lý nhà hàng", FontSize = 28, FontFamily = "OpenSansSemibold", FontAttributes = FontAttributes.Bold, TextColor = Colors.White, Margin = new Thickness(0, 10, 0, 0) },
                new Label { Text = "Nhân viên: sơ đồ bàn, gọi món, phục vụ, thanh toán, đặt bàn.\nKhách hàng: theo dõi đơn, lịch sử mua và điểm tích lũy.", FontSize = 13, TextColor = Color.FromRgba(255, 255, 255, 0.85) }
            }
        };

        var form = Ui.Card(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Ui.Title("Đăng nhập", 20),
                Ui.Muted("Dùng tài khoản đã đăng ký trên website nhà hàng."),
                Ui.Field("Email", email),
                Ui.Field("Mật khẩu", passwordRow),
                error, login,
                Ui.Divider(),
                serverSummary, serverEditor
            }
        }, margin: new Thickness(16, -40, 16, 24), padding: new Thickness(18, 20));

        SetBody(new ScrollView { Content = new VerticalStackLayout { Children = { hero, form } } });
    }

    private void ShowServerEditor(bool editing)
    {
        serverHost.Text = Uri.TryCreate(server.Text, UriKind.Absolute, out var uri) ? uri.Authority : server.Text;
        serverEditor.IsVisible = editing;
        serverSummary.IsVisible = !editing;
        if (editing && !string.IsNullOrEmpty(server.Text)) server.Focus();
    }

    private void ShowError(string text)
    {
        errorText.Text = text;
        error.IsVisible = true;
    }

    private async Task LoginAsync()
    {
        error.IsVisible = false;
        var uri = ApiClient.ParseServer(server.Text, out var invalid);
        if (uri is null) { ShowServerEditor(true); ShowError(invalid ?? "Địa chỉ máy chủ không hợp lệ."); return; }
        if (string.IsNullOrWhiteSpace(email.Text) || string.IsNullOrEmpty(password.Text))
        {
            ShowError("Nhập email và mật khẩu.");
            return;
        }
        login.Text = "Đang đăng nhập…";
        login.IsEnabled = false;
        var ok = await RunAsync(async () =>
        {
            await ApiClient.Current.LoginAsync(uri, email.Text, password.Text);
            Preferences.Default.Set("RestaurantServer", uri.AbsoluteUri.TrimEnd('/'));
            Preferences.Default.Set("RestaurantEmail", email.Text.Trim());
        }, ShowError);
        login.Text = "Đăng nhập";
        login.IsEnabled = true;
        if (ok) { password.Text = ""; AppNavigator.ShowHome(); }
    }
}
