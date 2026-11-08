using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using RestaurantManagement.Mobile.Services;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Shared;

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
    private bool delivery;
    private string? category;
    private bool cartExpanded = true;
    private bool leaving;

    private readonly Entry recipient = new() { Placeholder = "Tên khách / người nhận" };
    private readonly Entry phone = new() { Placeholder = "Số điện thoại", Keyboard = Keyboard.Telephone };
    private readonly Entry address = new() { Placeholder = "Số nhà, đường, phường/xã..." };
    private readonly Entry note = new() { Placeholder = "Không bắt buộc" };
    private readonly View addressField;
    private readonly HorizontalStackLayout typeChips = new() { Spacing = 8 };
    private readonly SearchBar search = new() { Placeholder = "Tìm món theo tên" };
    private readonly ContentView categoryChips = new();
    private readonly VerticalStackLayout menuList = new() { Padding = new Thickness(0, 0, 0, 12) };
    private readonly VerticalStackLayout cartList = new() { Spacing = 10 };
    private readonly ScrollView cartScroll;
    private readonly Label cartToggle = new() { FontSize = 13, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
    private readonly Label cartCount = new() { FontSize = 13 };
    private readonly Label cartTotal = new() { FontSize = 18, FontAttributes = FontAttributes.Bold, FontFamily = "OpenSansSemibold" };
    private readonly Button submit;

    public OrderEditorPage(int? tableId, string? tableLabel, int? existingOrderId)
        : base(existingOrderId is not null ? $"Gọi thêm · {tableLabel}" : tableId is not null ? $"Gọi món · {tableLabel}" : "Đơn mang đi / giao hàng")
    {
        this.tableId = tableId;
        this.existingOrderId = existingOrderId;
        var takeaway = tableId is null && existingOrderId is null;
        search.TextChanged += (_, _) => RenderMenu();
        submit = Ui.Primary("Gửi bếp", SubmitAsync);
        cartScroll = new ScrollView { Content = cartList, MaximumHeightRequest = 210 };
        cartToggle.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { cartExpanded = !cartExpanded; RenderCart(); }) });
        addressField = Ui.Field("Địa chỉ giao hàng *", address);
        recipient.ReturnType = ReturnType.Next;
        phone.ReturnType = ReturnType.Next;
        recipient.Completed += (_, _) => phone.Focus();
        phone.Completed += (_, _) => { if (delivery) address.Focus(); else note.Focus(); };

        // Thông tin đơn mang đi / giao hàng trong thẻ đầu trang.
        var header = Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Title("Thông tin đơn", 16), typeChips,
                Ui.Field("Khách / người nhận *", recipient), Ui.Field("Số điện thoại *", phone), addressField, Ui.Field("Ghi chú đơn", note)
            }
        }, margin: new Thickness(12, 12, 12, 4));
        header.IsVisible = takeaway;
        RenderType();

        var content = new VerticalStackLayout
        {
            Children = { header, Ui.Section("Thực đơn"), new ContentView { Content = search, Padding = new Thickness(8, 0) }, categoryChips, menuList }
        };

        var footer = new Border
        {
            Padding = new Thickness(14, 10, 14, 12), StrokeThickness = 0,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.12f, Radius = 12, Offset = new Point(0, -2) },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    cartToggle, cartScroll,
                    new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12,
                        Children =
                        {
                            new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { cartCount.Fg(Ui.MutedText), cartTotal.Fg(Ui.Text) } },
                            submit.Column(1)
                        }
                    }
                }
            }
        }.Bg(Ui.Surface);

        SetBody(new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Children = { new ScrollView { Content = content }, footer.Row(1) }
        });
        RenderCart();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (menu.Mon.Count > 0) return;
        if (!await RunAsync(async () => menu = await ApiClient.Current.MenuAsync())) return;
        BuildCategoryChips();
        RenderMenu();
    }

    // Nút quay lại Android: hỏi trước khi bỏ các món đã chọn.
    protected override bool OnBackButtonPressed()
    {
        if (cart.Count == 0 || leaving) return base.OnBackButtonPressed();
        Dispatcher.Dispatch(async () =>
        {
            if (!await DisplayAlertAsync("Bỏ các món đã chọn?", $"Giỏ đang có {cart.Sum(x => x.Quantity)} món chưa gửi bếp.", "Bỏ món", "Ở lại")) return;
            leaving = true;
            await Navigation.PopAsync();
        });
        return true;
    }

    private void RenderType()
    {
        typeChips.Children.Clear();
        typeChips.Add(Ui.Chip("🛍  Mang đi", !delivery, () => { delivery = false; RenderType(); }));
        typeChips.Add(Ui.Chip("🛵  Giao hàng", delivery, () => { delivery = true; RenderType(); }));
        addressField.IsVisible = delivery;
    }

    // Chip danh mục tạo một lần sau khi tải thực đơn; chọn chip chỉ đổi màu, không dựng lại hàng chip (giữ vị trí cuộn ngang).
    private void BuildCategoryChips()
    {
        var names = new List<string?> { null };
        names.AddRange(menu.DanhMuc.Where(c => menu.Mon.Any(d => d.DanhMucId == c.Id)).Select(c => c.Ten));
        var chips = new List<(string? Name, Border Chip)>();
        foreach (var name in names)
        {
            Border chip = null!;
            chip = Ui.Chip(name ?? "Tất cả", name == category, () =>
            {
                category = name;
                foreach (var (n, c) in chips) Ui.SetChipSelected(c, n == category);
                RenderMenu();
            });
            chips.Add((name, chip));
        }
        categoryChips.Content = Ui.ChipRow(chips.Select(x => (View)x.Chip));
    }

    private void RenderMenu()
    {
        menuList.Children.Clear();
        var text = search.Text?.Trim() ?? "";
        var dishes = menu.Mon.Where(d => (category is null || d.DanhMuc == category)
            && (text.Length == 0 || d.TenMon.Contains(text, StringComparison.CurrentCultureIgnoreCase))).ToList();
        if (menu.Mon.Count > 0 && dishes.Count == 0) menuList.Children.Add(Ui.Empty("🔍", "Không có món phù hợp", "Thử từ khóa khác hoặc chọn \"Tất cả\"."));
        foreach (var dish in dishes)
        {
            var price = dish.Sizes.Count == 1 ? ApiClient.Money(dish.Sizes[0].GiaBan) : "từ " + ApiClient.Money(dish.Sizes.Min(s => s.GiaBan));
            var inCart = cart.Where(x => x.Dish.Id == dish.Id).Sum(x => x.Quantity);
            var right = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
            if (inCart > 0) right.Add(Ui.Pill($"×{inCart}", Ui.Brand, Ui.BrandSoft));
            right.Add(Ui.Round("+", () => _ = AddAsync(dish), filled: true));
            var card = Ui.Card(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12,
                Children =
                {
                    Ui.Thumb(dish.TenMon, dish.HinhAnh),
                    new VerticalStackLayout
                    {
                        Spacing = 2, VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label { Text = dish.TenMon, FontSize = 15, FontAttributes = FontAttributes.Bold }.Fg(Ui.Text),
                            new Label { Text = dish.MoTa ?? "", FontSize = 12, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation, IsVisible = !string.IsNullOrWhiteSpace(dish.MoTa) }.Fg(Ui.MutedText),
                            new Label { Text = price + (dish.Sizes.Count > 1 ? $" · {dish.Sizes.Count} size" : ""), FontSize = 14, FontAttributes = FontAttributes.Bold }.Fg(Ui.Brand)
                        }
                    }.Column(1),
                    right.Column(2)
                }
            }, padding: new Thickness(10));
            menuList.Children.Add(Tappable(card, () => AddAsync(dish)));
        }
    }

    private async Task AddAsync(MenuDishDto dish)
    {
        var size = dish.Sizes[0];
        if (dish.Sizes.Count > 1)
        {
            var labels = dish.Sizes.Select(s => $"{s.TenSize} · {ApiClient.Money(s.GiaBan)}").ToArray();
            var choice = await DisplayActionSheetAsync($"{dish.TenMon} – chọn size", "Hủy", null, labels);
            var index = Array.IndexOf(labels, choice);
            if (index < 0) return;
            size = dish.Sizes[index];
        }
        var line = cart.FirstOrDefault(x => x.Size.Id == size.Id && x.Note is null);
        if (line is null) cart.Add(new CartLine(dish, size)); else line.Quantity = Math.Min(99, line.Quantity + 1);
        RenderCart();
        RenderMenu();
    }

    private void RenderCart()
    {
        cartList.Children.Clear();
        foreach (var line in cart.ToList())
        {
            var noteLink = new Label { Text = line.Note is null ? "✎ Thêm ghi chú" : $"✎ {line.Note}", FontSize = 12, LineBreakMode = LineBreakMode.TailTruncation }
                .Fg(line.Note is null ? Ui.Brand : Ui.Warning);
            noteLink.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    var value = await DisplayPromptAsync(line.Dish.TenMon, "Yêu cầu chế biến (ít cay, không hành...)", "Lưu", "Hủy", maxLength: 500, initialValue: line.Note ?? "");
                    if (value is null) return;
                    line.Note = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                    RenderCart();
                })
            });
            var stepper = new HorizontalStackLayout
            {
                Spacing = 8, VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    Ui.Round("−", () => { if (--line.Quantity <= 0) cart.Remove(line); RenderCart(); RenderMenu(); }),
                    new Label { Text = line.Quantity.ToString(), FontSize = 15, FontAttributes = FontAttributes.Bold, WidthRequest = 22, HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center }.Fg(Ui.Text),
                    Ui.Round("+", () => { line.Quantity = Math.Min(99, line.Quantity + 1); RenderCart(); RenderMenu(); })
                }
            };
            cartList.Children.Add(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8,
                Children =
                {
                    new VerticalStackLayout
                    {
                        Spacing = 1,
                        Children =
                        {
                            new Label { Text = line.Dish.TenMon + (line.Dish.Sizes.Count > 1 ? $" ({line.Size.TenSize})" : ""), FontSize = 14, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation }.Fg(Ui.Text),
                            Ui.Muted(ApiClient.Money(line.Quantity * line.Size.GiaBan), 12),
                            noteLink
                        }
                    },
                    stepper.Column(1)
                }
            });
        }
        var count = cart.Sum(x => x.Quantity);
        cartToggle.Text = count == 0 ? "Chạm vào món để thêm vào giỏ" : cartExpanded ? $"Giỏ món ({cart.Count} dòng)  ▾ thu gọn" : $"Giỏ món ({cart.Count} dòng)  ▸ xem";
        cartToggle.Fg(count == 0 ? Ui.MutedText : Ui.Brand);
        cartScroll.IsVisible = count > 0 && cartExpanded;
        cartCount.Text = count == 0 ? "Chưa chọn món" : $"{count} món";
        cartTotal.Text = ApiClient.Money(cart.Sum(x => x.Quantity * x.Size.GiaBan));
        submit.IsEnabled = count > 0;
    }

    private async Task SubmitAsync()
    {
        // Đơn mang đi / giao hàng: kiểm tra đủ thông tin trước khi gửi (cùng quy tắc với website), báo một lần mọi mục còn thiếu.
        if (tableId is null && existingOrderId is null)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(recipient.Text)) missing.Add("tên khách / người nhận");
            var digits = new string((phone.Text ?? "").Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(phone.Text)) missing.Add("số điện thoại");
            else if (digits.Length is < 9 or > 11) missing.Add("số điện thoại hợp lệ (9–11 chữ số)");
            if (delivery && string.IsNullOrWhiteSpace(address.Text)) missing.Add("địa chỉ giao hàng");
            if (missing.Count > 0)
            {
                await Info("Vui lòng nhập " + string.Join(", ", missing) + ".");
                (string.IsNullOrWhiteSpace(recipient.Text) ? recipient : missing.Any(m => m.StartsWith("số")) ? phone : address).Focus();
                return;
            }
        }
        var items = cart.Select(x => new OrderItemRequest(x.Dish.Id, x.Size.Id, x.Quantity, x.Note)).ToList();
        OrderCreatedDto? created = null;
        var ok = await RunAsync(async () => created = existingOrderId is int orderId
            ? await ApiClient.Current.AddItemsAsync(orderId, items)
            : await ApiClient.Current.CreateOrderAsync(new CreateOrderRequest(
                tableId is not null ? "TaiBan" : delivery ? "GiaoHang" : "MangDi", tableId,
                recipient.Text, phone.Text, delivery ? address.Text : null, note.Text, items)));
        if (!ok) return;
        leaving = true;
        await Info(created!.Message);
        // Gọi thêm từ trang chi tiết đơn: quay lại trang đó (tự tải lại); còn lại mở chi tiết đơn thay cho trang gọi món.
        var stack = Navigation.NavigationStack;
        if (stack.Count >= 2 && stack[^2] is OrderDetailPage detail && detail.OrderId == created.HoaDonId)
            await Navigation.PopAsync();
        else
        {
            await Navigation.PushAsync(new OrderDetailPage(created.HoaDonId, staff: true));
            Navigation.RemovePage(this);
        }
    }
}

// Chi tiết đơn: tiến độ từng món, phục vụ, thanh toán (nhân viên) hoặc chỉ xem và tự cập nhật (khách).
public sealed class OrderDetailPage : AppPage
{
    private readonly int id;
    private readonly bool staff;
    private readonly VerticalStackLayout body = NewList();
    private readonly Grid actionBar = new() { ColumnSpacing = 10, Padding = new Thickness(12, 10, 12, 12), IsVisible = false };

    public int OrderId => id;

    public OrderDetailPage(int id, bool staff) : base("Đơn hàng")
    {
        this.id = id;
        this.staff = staff;
        actionBar.Bg(Ui.Surface);
        actionBar.Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.12f, Radius = 12, Offset = new Point(0, -2) };
        SetBody(new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Children = { Refreshable(body, LoadAsync), actionBar.Row(1) }
        });
        // Theo dõi tiến độ món: tự làm mới (khách 15 giây như trang web, nhân viên 30 giây).
        AutoRefresh(TimeSpan.FromSeconds(staff ? 30 : 15), LoadAsync);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        OrderDetailDto? order = null;
        if (!await RunAsync(async () => order = staff ? await ApiClient.Current.StaffOrderAsync(id) : await ApiClient.Current.CustomerOrderAsync(id)) || !Changed(order!)) return;
        Render(order!);
    }

    private void Render(OrderDetailDto o)
    {
        var user = ApiClient.Current.User!;
        Title = o.MaHoaDon;
        body.Children.Clear();

        // Thẻ đầu: bàn/loại đơn, trạng thái, khách, tiến độ phục vụ.
        var active = o.Lines.Where(x => x.TrangThai != "DaHuy").ToList();
        var served = active.Count(x => x.TrangThai == "DaPhucVu");
        var head = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8,
                    Children = { Ui.Title(Ui.OrderHeading(o.Ban, o.LoaiDon, o.LoaiDonText), 18), Ui.Badge(o.TrangThai, o.TrangThaiText).Column(1) }
                },
                Ui.Muted(o.MaHoaDon + (o.PhuongThucDaTra is null ? "" : $" · {o.PhuongThucDaTra}")),
                Ui.Body(string.Join(" · ", new[] { o.KhachHang, o.SoDienThoai }.Where(x => !string.IsNullOrWhiteSpace(x))) is { Length: > 0 } who ? "👤 " + who : "", 13),
                Ui.Body(o.DiaChiGiaoHang is null ? "" : "📍 " + o.DiaChiGiaoHang, 13),
                Ui.Body(o.GhiChu is null ? "" : "📝 " + o.GhiChu, 13)
            }
        };
        foreach (var label in head.Children.OfType<Label>()) label.IsVisible = !string.IsNullOrEmpty(label.Text);
        if (active.Count > 0 && o.TrangThai == "ChuaThanhToan")
        {
            var progress = new ProgressBar { Progress = (double)served / active.Count, HeightRequest = 6 };
            progress.SetAppThemeColor(ProgressBar.ProgressColorProperty, Ui.Brand.Light, Ui.Brand.Dark);
            head.Add(new Grid
            {
                Margin = new Thickness(0, 6, 0, 0), RowSpacing = 4,
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                Children = { Ui.Muted($"Đã phục vụ {served}/{active.Count} món", 12), progress.Row(1) }
            });
        }
        body.Children.Add(Ui.Card(head, margin: new Thickness(12, 12, 12, 4)));

        body.Children.Add(Ui.Section("Món đã gọi", $"{active.Sum(x => x.SoLuong)} món"));
        foreach (var line in o.Lines) body.Children.Add(LineCard(o, line, user.CanServe));

        // Tổng tiền: chỉ hiện dòng ưu đãi / tiền cọc khi có.
        var totals = new VerticalStackLayout { Spacing = 6, Children = { Ui.KeyValue("Tiền món", ApiClient.Money(o.TongTienHang)) } };
        if (o.TienGiam > 0) totals.Add(Ui.KeyValue("Ưu đãi", "− " + ApiClient.Money(o.TienGiam), valueTone: Ui.Success));
        if (o.TienCocTru > 0) totals.Add(Ui.KeyValue("Trừ tiền cọc", "− " + ApiClient.Money(o.TienCocTru), valueTone: Ui.Success));
        totals.Add(Ui.Divider());
        var open = o.TrangThai == "ChuaThanhToan";
        totals.Add(Ui.KeyValue(open ? "Còn phải thu" : "Đã thanh toán",
            ApiClient.Money(open ? o.PhaiThu : Math.Max(0, o.TongTienHang - o.TienGiam - o.TienCocTru)), strong: true, valueTone: Ui.Brand));
        body.Children.Add(Ui.Section("Thanh toán"));
        body.Children.Add(Ui.Card(totals));

        if (!staff && open)
            body.Children.Add(Ui.Muted("Thanh toán tại quầy thu ngân.", 12).Margin(16, 10));
        body.Children.Add(Stamp);

        // Thanh hành động cố định ở đáy màn (nhân viên).
        actionBar.Children.Clear();
        actionBar.ColumnDefinitions.Clear();
        var buttons = new List<Button>();
        if (staff && o.CoTheGoiThem) buttons.Add(Ui.Outline("Gọi thêm món", () => Navigation.PushAsync(new OrderEditorPage(null, o.Ban, o.Id))));
        if (staff && o.CoTheThanhToan && user.CanPay) buttons.Add(Ui.Primary($"Thanh toán {ApiClient.Money(o.PhaiThu)}", () => PayAsync(o)));
        for (var i = 0; i < buttons.Count; i++)
        {
            actionBar.ColumnDefinitions.Add(new ColumnDefinition(i == buttons.Count - 1 && buttons.Count > 1 ? new GridLength(1.4, GridUnitType.Star) : GridLength.Star));
            actionBar.Add(buttons[i], i);
        }
        actionBar.IsVisible = buttons.Count > 0;
    }

    private View LineCard(OrderDetailDto o, OrderLineDto line, bool canServe)
    {
        var cancelled = line.TrangThai == "DaHuy";
        var info = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = line.TenMon + (string.IsNullOrWhiteSpace(line.TenSize) || line.TenSize == "Mặc định" ? "" : $" ({line.TenSize})"), FontSize = 15, FontAttributes = FontAttributes.Bold,
                    TextDecorations = cancelled ? TextDecorations.Strikethrough : TextDecorations.None
                }.Fg(cancelled ? Ui.MutedText : Ui.Text),
                Ui.Muted($"{ApiClient.Money(line.SoLuong * line.DonGia)} · gọi lúc {Ui.WhenShort(line.ThoiDiemGoi)}", 12),
                new Label { Text = line.GhiChu is null ? "" : "✎ " + line.GhiChu, FontSize = 12, FontAttributes = FontAttributes.Italic, IsVisible = line.GhiChu is not null }.Fg(Ui.Warning)
            }
        };
        var actions = new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(0, 6, 0, 0) };
        if (line.CoTheMangRa && canServe && staff)
            actions.Add(Ui.Small("✓ Đã mang ra", Ui.Success, Ui.SuccessSoft, () => UpdateLineAsync(o, line, "DaPhucVu")));
        if (line.CoTheHuy && canServe && staff)
            actions.Add(Ui.Small("Hủy món", Ui.Danger, Ui.DangerSoft, async () =>
            {
                if (await DisplayAlertAsync("Hủy món", $"Hủy {line.SoLuong} × {line.TenMon}?", "Hủy món", "Không"))
                    await UpdateLineAsync(o, line, "DaHuy");
            }));
        if (actions.Children.Count > 0) info.Add(actions);

        var qty = new Border
        {
            WidthRequest = 40, HeightRequest = 40, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 10 }, VerticalOptions = LayoutOptions.Start,
            Content = new Label { Text = $"{line.SoLuong}×", FontSize = 14, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }.Fg(Ui.Brand)
        }.Bg(Ui.BrandSoft);

        return Ui.Card(new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            Children = { qty, info.Column(1), Ui.Badge(line.TrangThai, line.TrangThaiText).Column(2) }
        });
    }

    private async Task UpdateLineAsync(OrderDetailDto o, OrderLineDto line, string status)
    {
        if (await RunAsync(() => ApiClient.Current.UpdateLineAsync(o.Id, line.Id, status))) await LoadAsync();
    }

    private async Task PayAsync(OrderDetailDto o)
    {
        var method = await DisplayActionSheetAsync($"Thu {ApiClient.Money(o.PhaiThu)} · {o.MaHoaDon}", "Hủy", null, "💵 Tiền mặt", "🏦 Chuyển khoản");
        PaymentRequest request;
        if (method == "💵 Tiền mặt")
        {
            decimal amount;
            var initial = o.PhaiThu.ToString("0", CultureInfo.InvariantCulture);
            // Hỏi lại tới khi số tiền hợp lệ và đủ, giữ số vừa nhập để sửa; nhận cả kiểu viết tắt "500k".
            while (true)
            {
                var given = await DisplayPromptAsync("Tiền mặt", $"Phải thu {ApiClient.Money(o.PhaiThu)}. Nhập số tiền khách đưa (có thể viết 500k):",
                    "Xác nhận", "Hủy", keyboard: Keyboard.Text, initialValue: initial);
                if (given is null) return;
                initial = given;
                var text = given.Trim().ToLowerInvariant().Replace(".", "").Replace(",", "").Replace(" ", "").Replace("đ", "");
                var thousands = text.EndsWith('k');
                if (thousands) text = text[..^1];
                if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) || amount <= 0)
                {
                    await Info("Số tiền không hợp lệ. Nhập số, ví dụ 500000 hoặc 500k.");
                    continue;
                }
                if (thousands) amount *= 1000;
                if (amount < o.PhaiThu)
                {
                    await Info($"Khách đưa {ApiClient.Money(amount)}, còn thiếu {ApiClient.Money(o.PhaiThu - amount)}.");
                    continue;
                }
                break;
            }
            request = new PaymentRequest("TienMat", amount, o.PhaiThu, null, o.RowVersion, o.BookingRowVersion);
        }
        else if (method == "🏦 Chuyển khoản")
        {
            var page = new TransferPaymentPage(o.PhaiThu, o.MaHoaDon);
            await Navigation.PushModalAsync(page);
            if (!await page.Accepted) return;
            // Hệ thống chưa đối soát tự động: mã hóa đơn làm nội dung chuyển khoản và cũng là mã giao dịch ghi nhận.
            request = new PaymentRequest("ChuyenKhoan", o.PhaiThu, o.PhaiThu, o.MaHoaDon, o.RowVersion, o.BookingRowVersion);
        }
        else return;

        PaymentResultDto? result = null;
        if (await RunAsync(async () => result = await ApiClient.Current.PayAsync(o.Id, request)))
            await Info(result!.Message + (result.TienThoiLai > 0 ? $" Tiền thối lại: {ApiClient.Money(result.TienThoiLai)}." : ""));
        await LoadAsync();
    }
}

// Xác nhận chuyển khoản: hiện QR của nhà hàng và nội dung tự sinh (mã hóa đơn) như trang thanh toán trên web.
public sealed class TransferPaymentPage : AppPage
{
    private readonly TaskCompletionSource<bool> accepted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Image qr = new() { Aspect = Aspect.AspectFit, HeightRequest = 250, IsVisible = false };
    private readonly Label qrNote = Ui.Muted("Đang tải ảnh QR…", 12);
    private bool confirmed;

    public TransferPaymentPage(decimal amount, string content) : base("Chuyển khoản")
    {
        var info = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Border
                {
                    BackgroundColor = Colors.White, Padding = 10, StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }, HorizontalOptions = LayoutOptions.Center, Content = qr
                },
                qrNote,
                Ui.KeyValue("Số tiền", ApiClient.Money(amount), strong: true, valueTone: Ui.Brand),
                Ui.KeyValue("Nội dung", content)
            }
        };
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 16, 16, 24),
            Spacing = 12,
            Children =
            {
                Ui.Title("Quét mã để chuyển khoản"),
                Ui.Card(info, padding: new Thickness(14)),
                Ui.Muted("Nội dung chuyển khoản được tạo tự động, giữ nguyên mã hóa đơn khi chuyển.", 12),
                Ui.Primary("Tôi đã chuyển khoản", ConfirmAsync),
                Ui.Outline("Hủy", CancelAsync, Ui.MutedText)
            }
        };
        SetBody(new ScrollView { Content = body });
    }

    // Kết quả sau khi hộp thoại đóng: true khi nhân viên bấm "Tôi đã chuyển khoản".
    public Task<bool> Accepted => accepted.Task;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        byte[]? bytes = null;
        // Lỗi tải QR không chặn thanh toán: chỉ hiện cảnh báo để nhân viên tự đối chiếu.
        await RunAsync(async () => bytes = await ApiClient.Current.PaymentQrAsync(), _ => { });
        if (bytes is null || bytes.Length == 0)
        {
            qrNote.Text = "Chưa tải được ảnh QR nhận chuyển khoản. Vui lòng đối chiếu tài khoản ngân hàng của nhà hàng.";
            return;
        }
        qr.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        qr.IsVisible = true;
        qrNote.Text = "Ảnh QR không tự xác nhận thanh toán; thu ngân đối chiếu tiền thực nhận.";
    }

    private async Task ConfirmAsync() => await CloseAsync(true);

    private async Task CancelAsync() => await CloseAsync(false);

    private async Task CloseAsync(bool result)
    {
        if (accepted.Task.IsCompleted) return;
        confirmed = result;
        await Navigation.PopModalAsync();
    }

    // Đóng bằng nút back của máy cũng được tính là hủy.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        accepted.TrySetResult(confirmed);
    }
}
