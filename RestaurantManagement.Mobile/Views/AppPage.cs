using System.Net;
using RestaurantManagement.Mobile.Services;

namespace RestaurantManagement.Mobile.Views;

// Điều hướng gốc: màn đăng nhập hoặc màn chính theo vai trò.
public static class AppNavigator
{
    private static Window? MainWindow => Application.Current?.Windows.FirstOrDefault();

    public static void ShowLogin(string? message = null)
    {
        ApiClient.Current.Logout();
        if (MainWindow is { } window) window.Page = new NavigationPage(new LoginPage(message));
    }

    public static void ShowHome()
    {
        var user = ApiClient.Current.User;
        if (user is null || MainWindow is not { } window) { ShowLogin(); return; }
        window.Page = new NavigationPage(user.IsCustomer ? new CustomerHomePage() : new StaffHomePage());
    }
}

// Lớp cơ sở các màn hình: chạy thao tác mạng, hiện vòng chờ và thông báo lỗi tiếng Việt từ server.
public abstract class AppPage : ContentPage
{
    private readonly ActivityIndicator busy = new() { IsRunning = false, IsVisible = false, Color = Color.FromArgb("#2C7A3E") };
    private bool running;

    protected AppPage(string title)
    {
        Title = title;
        Padding = 0;
    }

    // Đặt nội dung kèm vòng chờ phủ trên cùng.
    protected void SetBody(View body)
    {
        var grid = new Grid();
        grid.Add(body);
        grid.Add(new Grid { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Children = { busy } });
        Content = grid;
    }

    protected async Task<bool> RunAsync(Func<Task> action)
    {
        if (running) return false;
        running = true; busy.IsVisible = busy.IsRunning = true;
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
        catch (ApiException ex)
        {
            await DisplayAlertAsync("Không thực hiện được", ex.Message, "Đóng");
            return false;
        }
        finally
        {
            running = false; busy.IsVisible = busy.IsRunning = false;
        }
    }

    protected Task Info(string message) => DisplayAlertAsync("Thông báo", message, "OK");

    protected static Label Heading(string text) => new() { Text = text, FontSize = 20, FontAttributes = FontAttributes.Bold };
    protected static Label Muted(string text) => new() { Text = text, FontSize = 13, TextColor = Colors.Gray };

    public static Color StatusColor(string code) => code switch
    {
        "DangPhucVu" or "DangCheBien" => Color.FromArgb("#D97706"),
        "CanDon" or "ChoCheBien" or "ChoXacNhan" or "ChoCoc" => Color.FromArgb("#B91C1C"),
        "DaDat" or "SanSang" or "DaXacNhan" => Color.FromArgb("#1D4ED8"),
        "Trong" or "DaPhucVu" or "DaThanhToan" => Color.FromArgb("#2C7A3E"),
        _ => Colors.Gray
    };

    // Thẻ bo góc dùng cho danh sách.
    protected static Border Card(View content) => new()
    {
        Padding = new Thickness(14, 10), Margin = new Thickness(12, 5), StrokeThickness = 1, Stroke = Color.FromArgb("#D9E5DC"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, Content = content
    };
}
