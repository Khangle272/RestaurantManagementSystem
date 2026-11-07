using RestaurantManagement.Mobile.Views;

namespace RestaurantManagement.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheming.Saved;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Màn khởi động khôi phục phiên đã lưu; không có phiên thì mở màn đăng nhập.
            // Sau khi đăng nhập AppNavigator chuyển sang màn nhân viên hoặc khách.
            return new Window(new NavigationPage(new StartupPage())) { Title = "Quản lý nhà hàng" };
        }
    }
}