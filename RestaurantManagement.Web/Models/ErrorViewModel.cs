namespace RestaurantManagement.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    /// <summary>Nội dung trang báo lỗi thân thiện (404, 403, 500...).</summary>
    public class StatusPageViewModel
    {
        public int? Code { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Icon { get; set; } = "bi-question-circle";
        public string Tone { get; set; } = "";
        public string? RequestId { get; set; }

        public static StatusPageViewModel ForCode(int code, string? requestId = null) => code switch
        {
            404 => new() { Code = 404, Icon = "bi-signpost-split", Title = "Không tìm thấy trang",
                Message = "Trang bạn tìm có thể đã bị xóa, đổi địa chỉ hoặc dữ liệu không còn tồn tại. Vui lòng kiểm tra lại đường dẫn." },
            403 => Denied(),
            400 => new() { Code = 400, Icon = "bi-exclamation-octagon", Tone = "is-warning", Title = "Yêu cầu không hợp lệ",
                Message = "Phiên làm việc của biểu mẫu có thể đã hết hạn. Hãy tải lại trang rồi thử lại." },
            _ when code >= 500 => new() { Code = code, Icon = "bi-cloud-slash", Tone = "is-danger", Title = "Hệ thống đang gặp sự cố",
                Message = "Đã có lỗi xảy ra khi xử lý yêu cầu. Dữ liệu của bạn chưa bị thay đổi; vui lòng thử lại sau ít phút.", RequestId = requestId },
            _ => new() { Code = code, Icon = "bi-exclamation-circle", Tone = "is-warning", Title = "Không thể xử lý yêu cầu",
                Message = "Yêu cầu không thực hiện được. Vui lòng quay lại và thử lại." }
        };

        public static StatusPageViewModel Denied() => new()
        {
            Code = 403, Icon = "bi-shield-lock", Tone = "is-warning", Title = "Bạn không có quyền truy cập",
            Message = "Chức năng này không thuộc quyền của tài khoản đang đăng nhập. Nếu cần sử dụng, hãy liên hệ quản lý để được phân quyền."
        };
    }
}
