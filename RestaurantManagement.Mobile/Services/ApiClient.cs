using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantManagement.Shared.Mobile;

namespace RestaurantManagement.Mobile.Services;

// Lỗi nghiệp vụ do server trả về (đã là câu tiếng Việt hiển thị được cho người dùng).
public sealed class ApiException(string message, HttpStatusCode? status = null) : Exception(message)
{
    public HttpStatusCode? Status { get; } = status;
}

// Gọi REST API /api của website bằng bearer token. Một phiên đăng nhập dùng chung cho toàn app.
public sealed class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private HttpClient? http;

    public static ApiClient Current { get; } = new();
    public static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public UserProfileDto? User { get; private set; }
    public Uri? Server => http?.BaseAddress;

    public static string Money(decimal value) => value.ToString("N0", Vietnamese) + " đ";
    public static string Time(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(7)).ToString("HH:mm dd/MM", Vietnamese);

    public static Uri? ParseServer(string? text, out string? error)
    {
        error = null;
        if (!Uri.TryCreate(text?.Trim().TrimEnd('/'), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query))
        {
            error = "Nhập địa chỉ HTTPS của nhà hàng, ví dụ https://ten-tunnel.trycloudflare.com (không kèm đường dẫn).";
            return null;
        }
        return new Uri(uri.GetLeftPart(UriPartial.Authority));
    }

    public async Task<UserProfileDto> LoginAsync(Uri server, string email, string password)
    {
        Logout();
        var client = new HttpClient(CreateHandler(server)) { BaseAddress = server, Timeout = TimeSpan.FromSeconds(20) };
        try
        {
            using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email.Trim(), password), Json);
            var token = await Read<TokenResponse>(response);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            http = client;
            User = await GetAsync<UserProfileDto>("/api/auth/me");
            return User;
        }
        catch
        {
            http = null; User = null; client.Dispose();
            throw;
        }
    }

    public void Logout()
    {
        http?.Dispose();
        http = null;
        User = null;
    }

    // ----- Nhân viên -----
    public Task<List<TableDto>> TablesAsync() => GetAsync<List<TableDto>>("/api/staff/tables");
    public Task<MessageDto> FinishTableAsync(int id, string version) => PostAsync<MessageDto>($"/api/staff/tables/{id}/finish", new TableActionRequest(version));
    public Task<MessageDto> CleanTableAsync(int id, string version) => PostAsync<MessageDto>($"/api/staff/tables/{id}/clean", new TableActionRequest(version));
    public Task<MenuDto> MenuAsync() => GetAsync<MenuDto>("/api/staff/menu");
    public Task<List<OrderSummaryDto>> OpenOrdersAsync() => GetAsync<List<OrderSummaryDto>>("/api/staff/orders?open=true");
    public Task<OrderDetailDto> StaffOrderAsync(int id) => GetAsync<OrderDetailDto>($"/api/staff/orders/{id}");
    public Task<OrderCreatedDto> CreateOrderAsync(CreateOrderRequest request) => PostAsync<OrderCreatedDto>("/api/staff/orders", request);
    public Task<OrderCreatedDto> AddItemsAsync(int id, List<OrderItemRequest> items) => PostAsync<OrderCreatedDto>($"/api/staff/orders/{id}/items", new AddItemsRequest(items));
    public Task<MessageDto> UpdateLineAsync(int orderId, int lineId, string status) =>
        PostAsync<MessageDto>($"/api/staff/orders/{orderId}/lines/{lineId}", new UpdateLineRequest(status));
    public Task<PaymentResultDto> PayAsync(int id, PaymentRequest request) => PostAsync<PaymentResultDto>($"/api/staff/orders/{id}/pay", request);
    public Task<List<ReservationDto>> ReservationsAsync() => GetAsync<List<ReservationDto>>("/api/staff/reservations");
    public Task<MessageDto> AssignAsync(int id, int tableId, string version) =>
        PostAsync<MessageDto>($"/api/staff/reservations/{id}/assign", new AssignTableRequest(tableId, version));
    public Task<MessageDto> CheckInAsync(int id, string version) => PostAsync<MessageDto>($"/api/staff/reservations/{id}/checkin", new TableActionRequest(version));

    // ----- Khách hàng -----
    public Task<CustomerSummaryDto> CustomerSummaryAsync() => GetAsync<CustomerSummaryDto>("/api/customer/summary");
    public Task<List<OrderSummaryDto>> CustomerOrdersAsync() => GetAsync<List<OrderSummaryDto>>("/api/customer/orders");
    public Task<OrderDetailDto> CustomerOrderAsync(int id) => GetAsync<OrderDetailDto>($"/api/customer/orders/{id}");
    public Task<List<PointEntryDto>> PointsAsync() => GetAsync<List<PointEntryDto>>("/api/customer/points");

    private async Task<T> GetAsync<T>(string path)
    {
        var client = http ?? throw new ApiException("Bạn cần đăng nhập lại.", HttpStatusCode.Unauthorized);
        using var response = await Send(() => client.GetAsync(path));
        return await Read<T>(response);
    }

    private async Task<T> PostAsync<T>(string path, object body)
    {
        var client = http ?? throw new ApiException("Bạn cần đăng nhập lại.", HttpStatusCode.Unauthorized);
        using var response = await Send(() => client.PostAsync(path, JsonContent.Create(body, body.GetType(), options: Json)));
        return await Read<T>(response);
    }

    private static async Task<HttpResponseMessage> Send(Func<Task<HttpResponseMessage>> request)
    {
        try { return await request(); }
        catch (TaskCanceledException) { throw new ApiException("Máy chủ phản hồi quá lâu. Kiểm tra mạng rồi thử lại."); }
        catch (HttpRequestException) { throw new ApiException("Không kết nối được máy chủ nhà hàng. Kiểm tra địa chỉ, mạng và chứng chỉ HTTPS."); }
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<T>(Json) ?? throw new ApiException("Máy chủ trả về dữ liệu rỗng.");
        string? message = null;
        try { message = (await response.Content.ReadFromJsonAsync<ApiError>(Json))?.Message; } catch (JsonException) { } catch (NotSupportedException) { }
        throw new ApiException(message ?? response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
            HttpStatusCode.Forbidden => "Tài khoản của bạn không có quyền thực hiện thao tác này.",
            HttpStatusCode.NotFound => "Không tìm thấy dữ liệu.",
            _ => $"Máy chủ báo lỗi ({(int)response.StatusCode})."
        }, response.StatusCode);
    }

    private static HttpMessageHandler CreateHandler(Uri server)
    {
        var handler = new HttpClientHandler();
#if DEBUG
        // Chỉ bản Debug: chấp nhận chứng chỉ phát triển của máy chạy Web (localhost, hoặc 10.0.2.2 từ Android emulator).
        // Bản phát hành luôn kiểm tra chứng chỉ; khi demo trên điện thoại thật dùng URL Cloudflare có chứng chỉ hợp lệ.
        if (server.Host is "localhost" or "127.0.0.1" or "10.0.2.2")
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif
        return handler;
    }
}
