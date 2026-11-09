namespace RaceDay.Api.Services;

/// <summary>Validates uploaded images by size, extension and file signature (magic bytes), not just the client-supplied type.</summary>
public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif"
    };

    public static async Task<(string? Error, string Extension, string ContentType)> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return ("No file was uploaded.", string.Empty, string.Empty);
        }

        if (file.Length > MaxBytes)
        {
            return ("The image must be 5 MB or smaller.", string.Empty, string.Empty);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            return ("Only JPG, PNG, WEBP or GIF images are allowed.", string.Empty, string.Empty);
        }

        var header = new byte[12];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        }

        if (!MatchesSignature(header, read, contentType))
        {
            return ("The file content is not a valid image.", string.Empty, string.Empty);
        }

        return (null, extension.ToLowerInvariant(), contentType);
    }

    private static bool MatchesSignature(byte[] h, int length, string contentType)
    {
        switch (contentType)
        {
            case "image/jpeg":
                return length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
            case "image/png":
                return length >= 4 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;
            case "image/gif":
                return length >= 4 && h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38;
            case "image/webp":
                return length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                    && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;
            default:
                return false;
        }
    }
}
