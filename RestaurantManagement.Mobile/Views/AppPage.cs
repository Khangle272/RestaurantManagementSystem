using System.Net;
using Microsoft.Maui.Controls.Shapes;
using RestaurantManagement.Mobile.Services;

namespace RestaurantManagement.Mobile.Views;

// Điều hướng gốc: màn đăng nhập hoặc màn chính theo vai trò.
public static class AppNavigator
{
    private static Window? MainWindow => Application.Current?.Windows.FirstOrDefault();

    // keepSession: không xóa phiên đã lưu (khi chỉ lỗi mạng lúc khởi động).
    public static void ShowLogin(string? message = null, bool keepSession = false)
    {
        if (!keepSession) ApiClient.Current.Logout();
        if (MainWindow is { } window) window.Page = new NavigationPage(new LoginPage(message));
    }

    public static void ShowHome()
    {
        var user = ApiClient.Current.User;
        if (user is null || MainWindow is not { } window) { ShowLogin(); return; }
        window.Page = new NavigationPage(user.IsCustomer ? new CustomerHomePage() : new StaffHomePage());
    }
}

// Màn chính có thanh tab: trên Android đặt tab ở đáy màn hình kèm biểu tượng; tab cuối là Tài khoản (đăng xuất).
public abstract class HomeTabsPage : TabbedPage
{
    protected HomeTabsPage(string title)
    {
        Title = title;
        Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);
    }

    protected void AddTab(Page page, string icon)
    {
        page.IconImageSource = icon;
        Children.Add(page);
    }
}

// Lớp cơ sở các màn hình: chạy thao tác mạng, hiện lớp chờ và thông báo lỗi tiếng Việt từ server.
public abstract class AppPage : ContentPage
{
    private readonly Grid overlay;
    private readonly RefreshView refresh = new();
    private bool running;
    private bool background;
    private int busyTicket;
    private IDispatcherTimer? timer;

    protected AppPage(string title)
    {
        Title = title;
        Padding = 0;
        this.Bg(Ui.Background);
        refresh.SetAppThemeColor(RefreshView.RefreshColorProperty, Ui.Brand.Light, Ui.Brand.Dark);
        // Lớp chờ mờ phủ toàn màn, chặn chạm trong lúc gửi yêu cầu.
        overlay = new Grid
        {
            IsVisible = false, InputTransparent = false, BackgroundColor = Color.FromRgba(0, 0, 0, 0.18),
            Children =
            {
                new Border
                {
                    Padding = 20, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                    Content = new ActivityIndicator { IsRunning = true, WidthRequest = 36, HeightRequest = 36 }
                }.Bg(Ui.Surface)
            }
        };
    }

    // Đặt nội dung kèm lớp chờ phủ trên cùng.
    protected void SetBody(View body)
    {
        var grid = new Grid();
        grid.Add(body);
        grid.Add(overlay);
        Content = grid;
    }

    // Danh sách cuộn có kéo để làm mới; trả về RefreshView để đặt vào bố cục.
    protected RefreshView Refreshable(View content, Func<Task> load)
    {
        refresh.Content = new ScrollView { Content = content };
        refresh.Command = new Command(async () => { await load(); refresh.IsRefreshing = false; });
        return refresh;
    }

    protected static VerticalStackLayout NewList() => new() { Padding = new Thickness(0, 4, 0, 32) };

    // Tự làm mới ngầm theo chu kỳ khi màn đang hiển thị (dừng khi rời màn hoặc chuyển tab).
    protected void AutoRefresh(TimeSpan interval, Func<Task> load)
    {
        timer = Dispatcher.CreateTimer();
        timer.Interval = interval;
        timer.Tick += async (_, _) =>
        {
            if (running) return;
            // Cờ chỉ bật trong lúc gọi đồng bộ tới RunAsync đầu tiên của load(), nên thao tác người dùng xen giữa không bị coi là ngầm.
            Task task;
            background = true;
            try { task = load(); } finally { background = false; }
            await task;
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        timer?.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        timer?.Stop();
    }

    // onError: hiện lỗi ngay trên màn (ví dụ form đăng nhập) thay cho hộp thoại.
    // Lần làm mới ngầm không hiện lớp chờ và không bật hộp thoại lỗi (chỉ phiên hết hạn mới về màn đăng nhập).
    protected async Task<bool> RunAsync(Func<Task> action, Action<string>? onError = null)
    {
        var quiet = background;
        if (quiet && running) return false;
        // Thao tác của người dùng chờ yêu cầu đang chạy xong thay vì bị bỏ qua.
        while (running) await Task.Delay(100);
        running = true;
        // Chỉ hiện lớp chờ khi yêu cầu lâu hơn 250 ms và người dùng không đang kéo làm mới, tránh nhấp nháy.
        var ticket = ++busyTicket;
        _ = Task.Delay(250).ContinueWith(_ => Dispatcher.Dispatch(() =>
        {
            if (running && !quiet && ticket == busyTicket && !refresh.IsRefreshing) overlay.IsVisible = true;
        }));
        try
        {
            await action();
            return true;
        }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.Unauthorized && ApiClient.Current.User is not null)
        {
            AppNavigator.ShowLogin("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
            return false;
        }
        catch (Exception ex)
        {
            overlay.IsVisible = false;
            var message = ex is ApiException ? ex.Message : "Đã xảy ra lỗi ngoài dự kiến. Vui lòng thử lại.";
            if (quiet) return false;
            if (onError is not null) onError(message);
            else await DisplayAlertAsync("Không thực hiện được", message, "Đóng");
            return false;
        }
        finally
        {
            running = false;
            overlay.IsVisible = false;
        }
    }

    protected Task Info(string message) => DisplayAlertAsync("Thông báo", message, "OK");

    protected static Label Muted(string text) => Ui.Muted(text);

    // Thẻ bo góc dùng cho danh sách, chạm để mở.
    protected static Border Card(View content) => Ui.Card(content);

    protected static Border Tappable(Border card, Func<Task> onTap)
    {
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await onTap()) });
        return card;
    }

    protected static string UpdatedAt() => "Cập nhật " + DateTime.Now.ToString("HH:mm");

    private string? lastData;

    // Dòng "Cập nhật HH:mm" cuối danh sách (cùng một nhãn, thêm lại sau mỗi lần vẽ).
    protected readonly Label Stamp = new Label { FontSize = 12, Margin = new Thickness(16, 12) }.Fg(Ui.MutedText);

    // Ghi nhận lần tải; trả về false khi dữ liệu không đổi để khỏi vẽ lại (giữ vị trí cuộn khi tự làm mới).
    protected bool Changed(object data)
    {
        Stamp.Text = UpdatedAt() + " · tự làm mới, kéo xuống để làm ngay";
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        if (json == lastData) return false;
        lastData = json;
        return true;
    }
}
