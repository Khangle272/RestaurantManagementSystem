using System.Globalization;
using RestaurantManagement.Mobile.Services;
using RestaurantManagement.Shared.Mobile;

namespace RestaurantManagement.Mobile.Views;

// Ghi nhận gọi món: tại bàn đang phục vụ, gọi thêm vào đơn đang mở, hoặc đơn mang đi/giao hàng.
public sealed class OrderEditorPage : AppPage
{
    private sealed class CartLine(MenuDishDto dish, MenuSizeDto size)
    {
        public MenuDishDto Dish { get; } = dish;
        public MenuSizeDto Size { get; } = size;
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }

    private readonly int? tableId;
    private readonly int? existingOrderId;
    private readonly List<CartLine> cart = [];
    private MenuDto menu = new([], []);

    private readonly Picker orderType = new() { Title = "Loại đơn", ItemsSource = new[] { "Mang đi", "Giao hàng" }, SelectedIndex = 0 };
    private readonly Entry recipient = new() { Placeholder = "Tên khách / người nhận" };
    private readonly Entry phone = new() { Placeholder = "Số điện thoại", Keyboard = Keyboard.Telephone };
    private readonly Entry address = new() { Placeholder = "Địa chỉ giao hàng", IsVisible = false };
    private readonly Entry note = new() { Placeholder = "Ghi chú đơn (không bắt buộc)" };
    private readonly SearchBar search = new() { Placeholder = "Tìm món" };
    private readonly Picker category = new() { Title = "Danh mục" };
    private readonly VerticalStackLayout menuList = new() { Padding = new Thickness(0, 4, 0, 8) };
    private readonly VerticalStackLayout cartList = new() { Spacing = 4 };
    private readonly Label cartSummary = new() { FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
    private readonly Button submit = new() { Text = "Gửi bếp" };

    public OrderEditorPage(int? tableId, string? tableLabel, int? existingOrderId)
        : base(existingOrderId is not null ? $"Gọi thêm · {tableLabel}" : tableId is not null ? $"Gọi món · {tableLabel}" : "Đơn mang đi / giao hàng")
    {
        this.tableId = tableId;
        this.existingOrderId = existingOrderId;
        var takeaway = tableId is null && existingOrderId is null;
        orderType.SelectedIndexChanged += (_, _) => address.IsVisible = orderType.SelectedIndex == 1;
        search.TextChanged += (_, _) => RenderMenu();
        category.SelectedIndexChanged += (_, _) => RenderMenu();
        submit.Clicked += async (_, _) => await SubmitAsync();

        var header = new VerticalStackLayout
        {
            Padding = new Thickness(12, 8), Spacing = 6,
            Children = { orderType, recipient, phone, address, note },
            IsVisible = takeaway
        };
        var filters = new Grid
        {
            Padding = new Thickness(8, 0), ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(140)) },
            Children = { search, category.Column(1) }
        };
        var footer = new VerticalStackLayout
        {
            Padding = new Thickness(12, 8), Spacing = 6, BackgroundColor = Color.FromArgb("#F1F7F2"),
            Children =
            {
                new ScrollView { Content = cartList, MaximumHeightRequest = 170 },
                new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Children = { cartSummary, submit.Column(1) } }
            }
        };
        SetBody(new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Children = { header, filters.Row(1), new ScrollView { Content = menuList }.Row(2), footer.Row(3) }
        });
        RenderCart();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (menu.Mon.Count > 0) return;
        if (!await RunAsync(async () => menu = await ApiClient.Current.MenuAsync())) return;
        category.ItemsSource = new[] { "Tất cả" }.Concat(menu.DanhMuc.Where(c => menu.Mon.Any(d => d.DanhMucId == c.Id)).Select(c => c.Ten)).ToList();
        category.SelectedIndex = 0;
        RenderMenu();
    }

    private void RenderMenu()
    {
        menuList.Children.Clear();
        var text = search.Text?.Trim() ?? "";
        var selected = category.SelectedIndex > 0 ? category.SelectedItem as string : null;
        var dishes = menu.Mon.Where(d => (selected is null || d.DanhMuc == selected)
            && (text.Length == 0 || d.TenMon.Contains(text, StringComparison.CurrentCultureIgnoreCase))).ToList();
        if (dishes.Count == 0) menuList.Children.Add(new Label { Text = menu.Mon.Count == 0 ? "" : "Không có món phù hợp.", Margin = 16 });
        foreach (var dish in dishes)
        {
            var price = dish.Sizes.Count == 1 ? ApiClient.Money(dish.Sizes[0].GiaBan) : "từ " + ApiClient.Money(dish.Sizes.Min(s => s.GiaBan));
            var card = Card(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                Children =
                {
                    new VerticalStackLayout { Children = { new Label { Text = dish.TenMon, FontAttributes = FontAttributes.Bold }, Muted($"{dish.DanhMuc} · {price}") } },
                    new Label { Text = "+ Thêm", TextColor = Color.FromArgb("#2C7A3E"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }.Column(1)
                }
            });
            card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await AddAsync(dish)) });
            menuList.Children.Add(card);
        }
    }

    private async Task AddAsync(MenuDishDto dish)
    {
        var size = dish.Sizes[0];
        if (dish.Sizes.Count > 1)
        {
            var labels = dish.Sizes.Select(s => $"{s.TenSize} · {ApiClient.Money(s.GiaBan)}").ToArray();
            var choice = await DisplayActionSheetAsync(dish.TenMon, "Hủy", null, labels);
            var index = Array.IndexOf(labels, choice);
            if (index < 0) return;
            size = dish.Sizes[index];
        }
        var line = cart.FirstOrDefault(x => x.Size.Id == size.Id && x.Note is null);
        if (line is null) cart.Add(new CartLine(dish, size)); else line.Quantity = Math.Min(99, line.Quantity + 1);
        RenderCart();
    }

    private void RenderCart()
    {
        cartList.Children.Clear();
        foreach (var line in cart.ToList())
        {
            var minus = new Button { Text = "−", WidthRequest = 40, Padding = 0 };
            minus.Clicked += (_, _) => { if (--line.Quantity <= 0) cart.Remove(line); RenderCart(); };
            var noteButton = new Button { Text = line.Note is null ? "Ghi chú" : "Sửa", Padding = new Thickness(8, 0) };
            noteButton.Clicked += async (_, _) =>
            {
                var value = await DisplayPromptAsync(line.Dish.TenMon, "Yêu cầu chế biến (ít cay, không hành...)", "Lưu", "Hủy", maxLength: 500, initialValue: line.Note ?? "");
                if (value is null) return;
                line.Note = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                RenderCart();
            };
            cartList.Children.Add(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 6,
                Children =
                {
                    new VerticalStackLayout
                    {
                        Children =
                        {
                            new Label { Text = $"{line.Quantity} × {line.Dish.TenMon} ({line.Size.TenSize})" },
                            Muted(line.Note is null ? ApiClient.Money(line.Quantity * line.Size.GiaBan) : $"{ApiClient.Money(line.Quantity * line.Size.GiaBan)} · {line.Note}")
                        }
                    },
                    noteButton.Column(1), minus.Column(2)
                }
            });
        }
        var count = cart.Sum(x => x.Quantity);
        cartSummary.Text = count == 0 ? "Chưa chọn món" : $"{count} món · {ApiClient.Money(cart.Sum(x => x.Quantity * x.Size.GiaBan))}";
        submit.IsEnabled = count > 0;
    }

    private async Task SubmitAsync()
    {
        var items = cart.Select(x => new OrderItemRequest(x.Dish.Id, x.Size.Id, x.Quantity, x.Note)).ToList();
        OrderCreatedDto? created = null;
        var ok = await RunAsync(async () => created = existingOrderId is int orderId
            ? await ApiClient.Current.AddItemsAsync(orderId, items)
            : await ApiClient.Current.CreateOrderAsync(new CreateOrderRequest(
                tableId is not null ? "TaiBan" : orderType.SelectedIndex == 1 ? "GiaoHang" : "MangDi", tableId,
                recipient.Text, phone.Text, orderType.SelectedIndex == 1 ? address.Text : null, note.Text, items)));
        if (!ok) return;
        await Info(created!.Message);
        await Navigation.PushAsync(new OrderDetailPage(created.HoaDonId, staff: true));
        Navigation.RemovePage(this);
    }
}

// Chi tiết đơn: tiến độ từng món, phục vụ, thanh toán (nhân viên) hoặc chỉ xem và tự cập nhật (khách).
public sealed class OrderDetailPage : AppPage
{
    private readonly int id;
    private readonly bool staff;
    private readonly VerticalStackLayout body = new() { Padding = new Thickness(0, 8, 0, 24), Spacing = 2 };
    private readonly RefreshView refresh = new();
    private IDispatcherTimer? timer;

    public OrderDetailPage(int id, bool staff) : base("Đơn hàng")
    {
        this.id = id;
        this.staff = staff;
        refresh.Content = new ScrollView { Content = body };
        refresh.Command = new Command(async () => { await LoadAsync(); refresh.IsRefreshing = false; });
        SetBody(refresh);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
        // Khách xem tiến độ món: tự làm mới mỗi 15 giây như trang web.
        if (!staff && timer is null)
        {
            timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(15);
            timer.Tick += async (_, _) => await LoadAsync();
        }
        timer?.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        timer?.Stop();
    }

    private async Task LoadAsync()
    {
        OrderDetailDto? order = null;
        if (!await RunAsync(async () => order = staff ? await ApiClient.Current.StaffOrderAsync(id) : await ApiClient.Current.CustomerOrderAsync(id))) return;
        Render(order!);
    }

    private void Render(OrderDetailDto o)
    {
        var user = ApiClient.Current.User!;
        Title = o.MaHoaDon;
        body.Children.Clear();
        body.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = $"{o.Ban} · {o.LoaiDonText}", FontSize = 18, FontAttributes = FontAttributes.Bold },
                new Label { Text = o.TrangThaiText + (o.PhuongThucDaTra is null ? "" : $" · {o.PhuongThucDaTra}"), TextColor = StatusColor(o.TrangThai), FontAttributes = FontAttributes.Bold },
                Muted(string.Join(" · ", new[] { o.KhachHang, o.SoDienThoai }.Where(x => !string.IsNullOrWhiteSpace(x)))),
                Muted(o.DiaChiGiaoHang is null ? "" : "Giao đến: " + o.DiaChiGiaoHang),
                Muted(o.GhiChu is null ? "" : "Ghi chú: " + o.GhiChu)
            }
        }));

        body.Children.Add(new Label { Text = "Món đã gọi", FontAttributes = FontAttributes.Bold, Margin = new Thickness(14, 10, 14, 0) });
        foreach (var line in o.Lines)
        {
            var actions = new HorizontalStackLayout { Spacing = 8 };
            if (line.CoTheMangRa && user.CanServe)
                actions.Add(ActionButton("Đã mang ra", async () => await UpdateLineAsync(o, line, "DaPhucVu")));
            if (line.CoTheHuy && user.CanServe)
                actions.Add(ActionButton("Hủy món", async () =>
                {
                    if (await DisplayAlertAsync("Hủy món", $"Hủy {line.SoLuong} × {line.TenMon}?", "Hủy món", "Không"))
                        await UpdateLineAsync(o, line, "DaHuy");
                }));
            body.Children.Add(Card(new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        Children =
                        {
                            new Label { Text = $"{line.SoLuong} × {line.TenMon}" + (string.IsNullOrWhiteSpace(line.TenSize) ? "" : $" ({line.TenSize})"), FontAttributes = FontAttributes.Bold },
                            new Label { Text = line.TrangThaiText, TextColor = StatusColor(line.TrangThai) }.Column(1)
                        }
                    },
                    Muted($"{ApiClient.Money(line.SoLuong * line.DonGia)} · gọi lúc {ApiClient.Time(line.ThoiDiemGoi)}" + (line.GhiChu is null ? "" : $" · {line.GhiChu}")),
                    actions
                }
            }));
        }

        body.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Row("Tiền món", ApiClient.Money(o.TongTienHang)),
                Row("Ưu đãi", "− " + ApiClient.Money(o.TienGiam)),
                Row("Trừ tiền cọc", "− " + ApiClient.Money(o.TienCocTru)),
                Row(o.TrangThai == "ChuaThanhToan" ? "Còn phải thu" : "Đã thanh toán",
                    ApiClient.Money(o.TrangThai == "ChuaThanhToan" ? o.PhaiThu : Math.Max(0, o.TongTienHang - o.TienGiam - o.TienCocTru)), bold: true)
            }
        }));

        if (staff)
        {
            var buttons = new VerticalStackLayout { Padding = new Thickness(12, 6), Spacing = 8 };
            if (o.CoTheGoiThem)
                buttons.Add(ActionButton("Gọi thêm món", async () => await Navigation.PushAsync(new OrderEditorPage(null, o.Ban, o.Id))));
            if (o.CoTheThanhToan && user.CanPay)
                buttons.Add(ActionButton($"Thanh toán {ApiClient.Money(o.PhaiThu)}", async () => await PayAsync(o)));
            body.Children.Add(buttons);
        }
        else if (o.TrangThai == "ChuaThanhToan")
            body.Children.Add(Muted("Trang tự cập nhật trạng thái món mỗi 15 giây. Thanh toán tại quầy thu ngân.").Margin(14, 6));
    }

    private async Task UpdateLineAsync(OrderDetailDto o, OrderLineDto line, string status)
    {
        if (await RunAsync(() => ApiClient.Current.UpdateLineAsync(o.Id, line.Id, status))) await LoadAsync();
    }

    private async Task PayAsync(OrderDetailDto o)
    {
        var method = await DisplayActionSheetAsync($"Thu {ApiClient.Money(o.PhaiThu)}", "Hủy", null, "Tiền mặt", "Chuyển khoản");
        PaymentRequest request;
        if (method == "Tiền mặt")
        {
            var given = await DisplayPromptAsync("Tiền mặt", $"Phải thu {ApiClient.Money(o.PhaiThu)}. Nhập số tiền khách đưa:",
                "Xác nhận", "Hủy", keyboard: Keyboard.Numeric, initialValue: o.PhaiThu.ToString("0", CultureInfo.InvariantCulture));
            if (given is null) return;
            if (!decimal.TryParse(given.Replace(".", "").Replace(",", "").Replace(" ", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                await Info("Số tiền không hợp lệ.");
                return;
            }
            request = new PaymentRequest("TienMat", amount, o.PhaiThu, null, o.RowVersion, o.BookingRowVersion);
        }
        else if (method == "Chuyển khoản")
        {
            var reference = await DisplayPromptAsync("Chuyển khoản",
                $"Đối chiếu tài khoản nhận đủ {ApiClient.Money(o.PhaiThu)}, nội dung {o.MaHoaDon}. Nhập mã giao dịch ngân hàng:", "Xác nhận", "Hủy", maxLength: 100);
            if (string.IsNullOrWhiteSpace(reference)) return;
            request = new PaymentRequest("ChuyenKhoan", o.PhaiThu, o.PhaiThu, reference.Trim(), o.RowVersion, o.BookingRowVersion);
        }
        else return;

        PaymentResultDto? result = null;
        if (await RunAsync(async () => result = await ApiClient.Current.PayAsync(o.Id, request)))
            await Info(result!.Message + (result.TienThoiLai > 0 ? $" Tiền thối lại: {ApiClient.Money(result.TienThoiLai)}." : ""));
        await LoadAsync();
    }

    private static Grid Row(string label, string value, bool bold = false) => new()
    {
        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        Children =
        {
            new Label { Text = label, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None },
            new Label { Text = value, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, FontSize = bold ? 17 : 14 }.Column(1)
        }
    };

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, Padding = new Thickness(12, 4) };
        button.Clicked += async (_, _) => await action();
        return button;
    }
}
