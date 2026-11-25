using System.Buffers.Binary;

namespace JobTot.Application.Ai;

public static class CareerImageValidation
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public static void Validate(CareerChatImage image)
    {
        if (image.MimeType is not ("image/png" or "image/jpeg") || string.IsNullOrWhiteSpace(image.Data)
            || image.Data.Length > ((MaxBytes + 2) / 3) * 4) throw Invalid();
        byte[] bytes;
        try { bytes = Convert.FromBase64String(image.Data); }
        catch (FormatException) { throw Invalid(); }
        if (bytes.Length is 0 or > MaxBytes) throw Invalid();
        var dimensions = image.MimeType == "image/png" ? Png(bytes) : Jpeg(bytes);
        if (dimensions.Width is < 1 or > 10000 || dimensions.Height is < 1 or > 10000
            || (long)dimensions.Width * dimensions.Height > 25000000) throw Invalid();
    }
    private static (int Width, int Height) Png(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 45 || !bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || !bytes.Slice(12, 4).SequenceEqual("IHDR"u8)
            || !bytes.Slice(bytes.Length - 8, 4).SequenceEqual("IEND"u8)) throw Invalid();
        return (BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(16, 4)), BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(20, 4)));
    }
    private static (int Width, int Height) Jpeg(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 255 || bytes[1] != 216 || bytes[^2] != 255 || bytes[^1] != 217) throw Invalid();
        var position = 2;
        while (position < bytes.Length - 1)
        {
            if (bytes[position++] != 255) throw Invalid();
            while (position < bytes.Length && bytes[position] == 255) position++;
            if (position >= bytes.Length) break;
            var marker = bytes[position++];
            if (marker is 0xD9 or 0xDA) break;
            if (marker is >= 0xD0 and <= 0xD7 or 0x01) continue;
            if (position + 2 > bytes.Length) break;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(position, 2));
            if (length < 2 || position + length > bytes.Length) break;
            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                if (length < 8) break;
                return (BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(position + 5, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(position + 3, 2)));
            }
            position += length;
        }
        throw Invalid();
    }
    private static CareerSearchException Invalid() => new("ai_invalid_image", "Vui lòng chọn ảnh CV JPG hoặc PNG hợp lệ, tối đa 5 MB và 25 triệu điểm ảnh.");
}
