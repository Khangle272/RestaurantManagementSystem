using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace RestaurantManagement.Web.Services;

public sealed class PaymentQrStore(IWebHostEnvironment environment, IConfiguration configuration)
{
    // shortcut: single-host QR files; add shared storage and locking if deploying multiple hosts.
    private readonly string directory = configuration["PaymentQr:Directory"]
        ?? Path.Combine(environment.ContentRootPath, "App_Data", "payment-qr");
    private readonly object gate = new();
    public string CurrentVersion
    {
        get { lock (gate) return File.Exists(Path.Combine(directory, "current")) ? File.ReadAllText(Path.Combine(directory, "current")).Trim() : ""; }
    }
    public static string? ContentType(byte[] data) => data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
        ? "image/png" : data.Length >= 3 && data[0] == 255 && data[1] == 216 && data[2] == 255 ? "image/jpeg" : null;
    public byte[]? Read(string? version)
    {
        if (version is null || !Regex.IsMatch(version, "\\A[a-f0-9]{64}\\z")) return null;
        lock (gate) return File.Exists(Path.Combine(directory, version)) ? File.ReadAllBytes(Path.Combine(directory, version)) : null;
    }
    public bool Save(byte[] data, string? expectedVersion)
    {
        lock (gate)
        {
            if (CurrentVersion != (expectedVersion ?? "")) return false;
            Directory.CreateDirectory(directory);
            var version = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            var image = Path.Combine(directory, version);
            var pointer = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                if (!File.Exists(image)) { File.WriteAllBytes(pointer, data); File.Move(pointer, image); }
                File.WriteAllText(pointer, version);
                File.Move(pointer, Path.Combine(directory, "current"), overwrite: true);
            }
            finally { if (File.Exists(pointer)) File.Delete(pointer); }
            return true;
        }
    }
}
