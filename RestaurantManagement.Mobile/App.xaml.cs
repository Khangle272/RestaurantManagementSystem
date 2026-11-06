using RestaurantManagement.Mobile.Views;

namespace RestaurantManagement.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Mở màn đăng nhập; sau khi đăng nhập AppNavigator chuyển sang màn nhân viên hoặc khách.
            return new Window(new NavigationPage(new LoginPage())) { Title = "Quản lý nhà hàng" };
        }
    }
}