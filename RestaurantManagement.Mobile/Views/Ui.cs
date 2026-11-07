using Microsoft.Maui.Controls.Shapes;

namespace RestaurantManagement.Mobile.Views;

// Bộ màu và thành phần giao diện dùng chung cho mọi màn hình (tự đổi theo giao diện sáng/tối của máy).
public static class Ui
{
    public readonly record struct Tone(Color Light, Color Dark);

    private static Tone T(string light, string dark) => new(Color.FromArgb(light), Color.FromArgb(dark));

    // Màu thương hiệu xanh lá giống website.
    public static readonly Tone Brand = T("#2C7A3E", "#7CC48D");
    public static readonly Tone BrandSoft = T("#E3F1E6", "#1D3324");
    public static readonly Tone Background = T("#F3F6F4", "#0F1411");
    public static readonly Tone Surface = T("#FFFFFF", "#1A211C");
    public static readonly Tone Text = T("#1E293B", "#E5E7EB");
    public static readonly Tone MutedText = T("#64748B", "#9CA3AF");
    public static readonly Tone Line = T("#E2E8F0", "#2A332D");
    public static readonly Tone Danger = T("#B91C1C", "#FCA5A5");
    public static readonly Tone DangerSoft = T("#FEE2E2", "#3B1414");
    public static readonly Tone Warning = T("#B45309", "#FCD34D");
    public static readonly Tone WarningSoft = T("#FEF3C7", "#3A2E0F");
    public static readonly Tone Info = T("#1D4ED8", "#93C5FD");
    public static readonly Tone InfoSoft = T("#DBEAFE", "#172554");
    public static readonly Tone Success = T("#15803D", "#86EFAC");
    public static readonly Tone SuccessSoft = T("#DCFCE7", "#0F2E1A");
    public static readonly Tone Neutral = T("#475569", "#CBD5E1");
    public static readonly Tone NeutralSoft = T("#F1F5F9", "#1F2937");

    // ----- Gán màu theo giao diện sáng/tối -----
    public static T Fg<T>(this T label, Tone tone) where T : Label { label.SetAppThemeColor(Label.TextColorProperty, tone.Light, tone.Dark); return label; }
    public static T Bg<T>(this T view, Tone tone) where T : VisualElement { view.SetAppThemeColor(VisualElement.BackgroundColorProperty, tone.Light, tone.Dark); return view; }
    public static T StrokeTone<T>(this T border, Tone tone) where T : Border { border.SetAppThemeColor(Border.StrokeProperty, tone.Light, tone.Dark); return border; }

    // Cặp màu (chữ, nền) của nhãn trạng thái theo mã trạng thái phía server.
    public static (Tone Fg, Tone Bg) StatusTone(string code) => code switch
    {
        "DangPhucVu" or "DangCheBien" or "ChoCoc" => (Warning, WarningSoft),
        "CanDon" or "ChoCheBien" or "ChoXacNhan" or "ChuaThanhToan" => (Danger, DangerSoft),
        "DaDat" or "SanSang" or "DaXacNhan" => (Info, InfoSoft),
        "Trong" or "DaPhucVu" or "DaThanhToan" or "DaNhanBan" => (Success, SuccessSoft),
        _ => (Neutral, NeutralSoft)
    };

    // ----- Chữ -----
    public static Label Title(string text, double size = 18) =>
        new Label { Text = text, FontSize = size, FontFamily = "OpenSansSemibold", FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation }.Fg(Text);

    public static Label Body(string text, double size = 14) => new Label { Text = text, FontSize = size }.Fg(Text);

    public static Label Muted(string text, double size = 13) => new Label { Text = text, FontSize = size, IsVisible = !string.IsNullOrEmpty(text) }.Fg(MutedText);

    // Tiêu đề nhóm trong danh sách, có thể kèm chữ phụ bên phải.
    public static View Section(string title, string? trailing = null) => new Grid
    {
        Margin = new Thickness(16, 16, 16, 6),
        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        Children =
        {
            new Label { Text = title.ToUpperInvariant(), FontSize = 12, FontFamily = "OpenSansSemibold", FontAttributes = FontAttributes.Bold, CharacterSpacing = 0.6 }.Fg(MutedText),
            Muted(trailing ?? "", 12).Column(1)
        }
    };

    // ----- Khối -----
    public static Border Card(View content, Thickness? margin = null, Thickness? padding = null)
    {
        var card = new Border
        {
            Padding = padding ?? new Thickness(14, 12), Margin = margin ?? new Thickness(12, 5), StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = content,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.06f, Radius = 6, Offset = new Point(0, 2) }
        };
        return card.Bg(Surface).StrokeTone(Line);
    }

    // Nhãn trạng thái dạng viên thuốc.
    public static Border Badge(string code, string text) { var (fg, bg) = StatusTone(code); return Pill(text, fg, bg); }

    public static Border Pill(string text, Tone fg, Tone bg, double size = 12) => new Border
    {
        Padding = new Thickness(10, 3), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 999 },
        HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center,
        Content = new Label { Text = text, FontSize = size, FontFamily = "OpenSansSemibold", FontAttributes = FontAttributes.Bold }.Fg(fg)
    }.Bg(bg);

    // Màn hình trống: biểu tượng, câu chính, gợi ý.
    public static View Empty(string icon, string title, string? hint = null) => new VerticalStackLayout
    {
        Padding = new Thickness(32, 48), Spacing = 6, HorizontalOptions = LayoutOptions.Center,
        Children =
        {
            new Label { Text = icon, FontSize = 44, HorizontalTextAlignment = TextAlignment.Center },
            new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }.Fg(Text),
            new Label { Text = hint ?? "", FontSize = 13, HorizontalTextAlignment = TextAlignment.Center, IsVisible = hint is not null }.Fg(MutedText)
        }
    };

    // Thanh thông báo nổi bật (ví dụ: món đã sẵn sàng chờ mang ra).
    public static Border Banner(string text, Tone fg, Tone bg) => new Border
    {
        Margin = new Thickness(12, 8, 12, 0), Padding = new Thickness(14, 10), StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 10 },
        Content = new Label { Text = text, FontSize = 13, FontAttributes = FontAttributes.Bold }.Fg(fg)
    }.Bg(bg);

    // ----- Nút -----
    public static Button Primary(string text, Func<Task>? action = null)
    {
        var button = new Button { Text = text, MinimumHeightRequest = 48 };
        if (action is not null) button.Clicked += async (_, _) => await action();
        return button;
    }

    public static Button Outline(string text, Func<Task>? action = null, Tone? tone = null)
    {
        var t = tone ?? Brand;
        var button = new Button { Text = text, BorderWidth = 1.5, MinimumHeightRequest = 48 };
        button.SetAppThemeColor(Button.TextColorProperty, t.Light, t.Dark);
        button.SetAppThemeColor(Button.BorderColorProperty, t.Light, t.Dark);
        button.SetAppThemeColor(VisualElement.BackgroundColorProperty, Surface.Light, Surface.Dark);
        if (action is not null) button.Clicked += async (_, _) => await action();
        return button;
    }

    // Nút nhỏ trong thẻ (Đã mang ra, Hủy món...).
    public static Button Small(string text, Tone fg, Tone bg, Func<Task> action)
    {
        var button = new Button { Text = text, FontSize = 13, Padding = new Thickness(12, 0), MinimumHeightRequest = 36, HeightRequest = 36, CornerRadius = 18 };
        button.SetAppThemeColor(Button.TextColorProperty, fg.Light, fg.Dark);
        button.SetAppThemeColor(VisualElement.BackgroundColorProperty, bg.Light, bg.Dark);
        button.Clicked += async (_, _) => await action();
        return button;
    }

    // Nút tròn (+ / −) cho bộ đếm số lượng.
    public static Button Round(string text, Action action, bool filled = false)
    {
        var button = new Button
        {
            Text = text, FontSize = 18, Padding = 0, WidthRequest = 34, HeightRequest = 34, MinimumWidthRequest = 34, MinimumHeightRequest = 34,
            CornerRadius = 17, BorderWidth = filled ? 0 : 1
        };
        button.SetAppThemeColor(Button.TextColorProperty, filled ? Colors.White : Brand.Light, filled ? Colors.Black : Brand.Dark);
        button.SetAppThemeColor(VisualElement.BackgroundColorProperty, filled ? Brand.Light : Surface.Light, filled ? Brand.Dark : Surface.Dark);
        button.SetAppThemeColor(Button.BorderColorProperty, Brand.Light, Brand.Dark);
        button.Clicked += (_, _) => action();
        return button;
    }

    // Chip lọc chọn được (danh mục món, trạng thái bàn...).
    public static Border Chip(string text, bool selected, Action onTap)
    {
        var chip = new Border
        {
            Padding = new Thickness(14, 7), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Content = new Label { Text = text, FontSize = 13, FontFamily = "OpenSansSemibold" }
        };
        SetChipSelected(chip, selected);
        chip.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(onTap) });
        return chip;
    }

    public static void SetChipSelected(Border chip, bool selected)
    {
        var label = (Label)chip.Content!;
        label.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        if (selected)
        {
            label.SetAppThemeColor(Label.TextColorProperty, Colors.White, Color.FromArgb("#0F1411"));
            chip.Bg(Brand).StrokeTone(Brand);
        }
        else { label.Fg(Text); chip.Bg(Surface).StrokeTone(Line); }
    }

    // Ảnh món từ website; chưa có ảnh thì hiện ô chữ cái đầu tên món.
    public static View Thumb(string name, string? image, double size = 52)
    {
        var box = new Border
        {
            WidthRequest = size, HeightRequest = size, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 12 },
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = name.Trim().Length > 0 ? name.Trim()[..1].ToUpperInvariant() : "?", FontSize = size * 0.4, FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            }.Fg(Brand)
        }.Bg(BrandSoft);
        if (ImageUrl(image) is { } url)
        {
            var picture = new Image { Source = new UriImageSource { Uri = url, CacheValidity = TimeSpan.FromDays(3) }, Aspect = Aspect.AspectFill };
            box.Content = new Grid { Children = { box.Content, picture } };
        }
        return box;
    }

    private static Uri? ImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) && absolute.Scheme is "https" or "http") return absolute;
        return Services.ApiClient.Current.Server is { } server && Uri.TryCreate(server, path.TrimStart('~'), out var relative) ? relative : null;
    }

    // Hàng chip cuộn ngang.
    public static ScrollView ChipRow(IEnumerable<View> chips) => new()
    {
        Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
        Content = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(12, 8) }.WithChildren(chips)
    };

    // Ô nhập có nhãn phía trên và khung bo góc.
    public static View Field(string label, View input) => new VerticalStackLayout
    {
        Spacing = 4,
        Children =
        {
            new Label { Text = label, FontSize = 13, FontFamily = "OpenSansSemibold", FontAttributes = FontAttributes.Bold }.Fg(Text),
            Box(input)
        }
    };

    public static Border Box(View input) => new Border
    {
        Padding = new Thickness(10, 0), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = input
    }.Bg(Surface).StrokeTone(Line);

    // Dòng nhãn – giá trị (tổng tiền, thông tin đơn).
    public static Grid KeyValue(string key, string value, bool strong = false, Tone? valueTone = null) => new()
    {
        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        Children =
        {
            (strong ? Title(key, 16) : Muted(key, 14)),
            new Label
            {
                Text = value, FontSize = strong ? 18 : 14, FontAttributes = strong ? FontAttributes.Bold : FontAttributes.None,
                FontFamily = strong ? "OpenSansSemibold" : null, HorizontalTextAlignment = TextAlignment.End
            }.Fg(valueTone ?? Text).Column(1)
        }
    };

    public static BoxView Divider() => new BoxView { HeightRequest = 1, Margin = new Thickness(0, 4) }.Bg(Line);

    public static T WithChildren<T>(this T layout, IEnumerable<View> children) where T : Layout
    {
        foreach (var child in children) layout.Children.Add(child);
        return layout;
    }
}

internal static class ViewExtensions
{
    public static T Column<T>(this T view, int column) where T : BindableObject { Grid.SetColumn(view, column); return view; }
    public static T Row<T>(this T view, int row) where T : BindableObject { Grid.SetRow(view, row); return view; }
    public static T ColumnSpan<T>(this T view, int span) where T : BindableObject { Grid.SetColumnSpan(view, span); return view; }
    public static T Start<T>(this T view) where T : View { view.HorizontalOptions = LayoutOptions.Start; return view; }
    public static T Margin<T>(this T view, double horizontal, double vertical) where T : View { view.Margin = new Thickness(horizontal, vertical); return view; }
}
