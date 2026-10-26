namespace RestaurantManagement.Mobile;

public partial class MainPage : ContentPage
{
    private Uri? server;
    public MainPage()
    {
        InitializeComponent();
        ServerEntry.Text = Preferences.Default.Get("RestaurantServer", "");
    }

    private void OnConnectClicked(object? sender, EventArgs e)
    {
        if (!Uri.TryCreate(ServerEntry.Text?.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            SetupError.Text = "Nhập địa chỉ máy chủ HTTPS, không kèm đường dẫn hay thông tin đăng nhập.";
            SetupError.IsVisible = true; return;
        }
        server = uri;
        Preferences.Default.Set("RestaurantServer", uri.GetLeftPart(UriPartial.Authority));
        SetupError.IsVisible = false; SetupPanel.IsVisible = false; CustomerWeb.IsVisible = true;
        CustomerWeb.Source = new Uri(server, "/Account/Login?returnUrl=%2FKhachHang%2FVouchers").AbsoluteUri;
    }
    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        // Leave the previous page before connecting to a different origin.
        CustomerWeb.Source = "about:blank"; CustomerWeb.IsVisible = false; SetupPanel.IsVisible = true;
    }
    private void OnBackClicked(object? sender, EventArgs e)
    {
        if (CustomerWeb.IsVisible && CustomerWeb.CanGoBack) CustomerWeb.GoBack();
    }
    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url == "about:blank") return;
        if (server is null || !Uri.TryCreate(e.Url, UriKind.Absolute, out var target)
            || target.Scheme != Uri.UriSchemeHttps || target.Authority != server.Authority)
            e.Cancel = true;
    }
    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Success) return;
        CustomerWeb.IsVisible = false; SetupPanel.IsVisible = true;
        SetupError.Text = "Không mở được website. Kiểm tra địa chỉ, mạng và chứng chỉ HTTPS của nhà hàng.";
        SetupError.IsVisible = true;
    }
}
