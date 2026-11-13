using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace RestaurantManagement.Web.Services;

// Thông báo lỗi kiểm tra dữ liệu bằng tiếng Việt cho các thuộc tính chưa ghi ErrorMessage riêng
// (mặc định của .NET là tiếng Anh, ví dụ "The CurrentPassword field is required.").
public sealed class VietnameseValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) || !string.IsNullOrEmpty(attribute.ErrorMessageResourceName)) continue;
            var message = attribute switch
            {
                RequiredAttribute => "Vui lòng nhập thông tin bắt buộc.",
                EmailAddressAttribute => "Email không hợp lệ.",
                PhoneAttribute => "Số điện thoại không hợp lệ.",
                StringLengthAttribute { MinimumLength: > 0 } => "Độ dài phải từ {2} đến {1} ký tự.",
                StringLengthAttribute => "Tối đa {1} ký tự.",
                MaxLengthAttribute => "Tối đa {1} ký tự.",
                MinLengthAttribute => "Tối thiểu {1} ký tự.",
                RangeAttribute => "Giá trị phải từ {1} đến {2}.",
                RegularExpressionAttribute => "Giá trị không đúng định dạng.",
                CompareAttribute => "Giá trị xác nhận không khớp.",
                UrlAttribute => "Địa chỉ web không hợp lệ.",
                _ => null
            };
            if (message is not null) attribute.ErrorMessage = message;
        }
    }

    public static void Configure(MvcOptions options)
    {
        options.ModelMetadataDetailsProviders.Add(new VietnameseValidationMetadataProvider());
        var messages = options.ModelBindingMessageProvider;
        messages.SetValueMustNotBeNullAccessor(_ => "Vui lòng nhập thông tin bắt buộc.");
        messages.SetMissingBindRequiredValueAccessor(_ => "Thiếu thông tin bắt buộc.");
        messages.SetMissingKeyOrValueAccessor(() => "Thiếu thông tin bắt buộc.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "Thiếu nội dung yêu cầu.");
        messages.SetAttemptedValueIsInvalidAccessor((value, _) => $"Giá trị '{value}' không hợp lệ.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
        messages.SetUnknownValueIsInvalidAccessor(_ => "Giá trị không hợp lệ.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị không hợp lệ.");
        messages.SetValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
        messages.SetValueMustBeANumberAccessor(_ => "Vui lòng nhập số hợp lệ.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "Vui lòng nhập số hợp lệ.");
    }
}

// Lỗi của ASP.NET Core Identity (đổi/đặt mật khẩu, đăng ký) bằng tiếng Việt.
public sealed class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
{
    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };

    public override IdentityError DefaultError() => Error(nameof(DefaultError), "Đã xảy ra lỗi. Vui lòng thử lại.");
    public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure), "Tài khoản vừa được thay đổi ở nơi khác. Vui lòng tải lại trang.");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "Mật khẩu hiện tại không đúng.");
    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "Mã xác thực không hợp lệ hoặc đã hết hạn.");
    public override IdentityError LoginAlreadyAssociated() => Error(nameof(LoginAlreadyAssociated), "Tài khoản đăng nhập này đã được liên kết.");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), $"Tên đăng nhập '{userName}' không hợp lệ.");
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), $"Email '{email}' không hợp lệ.");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), $"Email '{userName}' đã được sử dụng.");
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), $"Email '{email}' đã được sử dụng.");
    public override IdentityError InvalidRoleName(string? role) => Error(nameof(InvalidRoleName), $"Tên vai trò '{role}' không hợp lệ.");
    public override IdentityError DuplicateRoleName(string role) => Error(nameof(DuplicateRoleName), $"Vai trò '{role}' đã tồn tại.");
    public override IdentityError UserAlreadyHasPassword() => Error(nameof(UserAlreadyHasPassword), "Tài khoản đã có mật khẩu.");
    public override IdentityError UserLockoutNotEnabled() => Error(nameof(UserLockoutNotEnabled), "Tài khoản này không bật chức năng khóa.");
    public override IdentityError UserAlreadyInRole(string role) => Error(nameof(UserAlreadyInRole), $"Tài khoản đã có vai trò '{role}'.");
    public override IdentityError UserNotInRole(string role) => Error(nameof(UserNotInRole), $"Tài khoản không có vai trò '{role}'.");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"Mật khẩu phải có ít nhất {length} ký tự.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(nameof(PasswordRequiresUniqueChars), $"Mật khẩu phải có ít nhất {uniqueChars} ký tự khác nhau.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "Mật khẩu phải có ít nhất một ký tự đặc biệt.");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "Mật khẩu phải có ít nhất một chữ số (0-9).");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "Mật khẩu phải có ít nhất một chữ thường (a-z).");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "Mật khẩu phải có ít nhất một chữ hoa (A-Z).");
    public override IdentityError RecoveryCodeRedemptionFailed() => Error(nameof(RecoveryCodeRedemptionFailed), "Mã khôi phục không đúng.");
}
