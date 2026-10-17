namespace BlytSharp;

public enum FlimFormat : byte
{
    L8, A8, LA4, LA8, HiLo8, Rgb565, Rgb8, Rgb5A1, Rgba4, Rgba8, Etc1, Etc1A4,
    Bc1, Bc2, Bc3, Bc4L, Bc4A, Bc5, L4, A4, Rgba8Srgb, Bc1Srgb, Bc2Srgb, Bc3Srgb, Rgb10A2, Rgb565Indirect
}

public sealed class Bflim
{
    public bool BigEndian { get; set; }
    public uint Version { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort Alignment { get; set; }
    public FlimFormat Format { get; set; }
    public byte TileMode { get; set; }
    public byte Swizzle { get; set; }
    public uint ImageSize { get; set; }
    public byte[] ImageData { get; set; } = [];

    public const int FooterSize = 0x28;

    public static bool IsBflim(ReadOnlySpan<byte> d) => d.Length >= FooterSize && d[^FooterSize..][..4].SequenceEqual("FLIM"u8);

    public static Bflim FromBinary(ReadOnlySpan<byte> data)
    {
        if (!IsBflim(data)) throw new InvalidDataException("Not a BFLIM file.");
        var bytes = data.ToArray();
        int h = bytes.Length - FooterSize;
        var r = new BinReader(bytes, bytes[h + 4] == 0xFE);
        int headerSize = r.U16(h + 6);
        int imag = h + headerSize;
        if (r.Magic(imag) != "imag") throw new InvalidDataException("BFLIM is missing its imag block.");
        r.Pos = imag + 8;
        var f = new Bflim { BigEndian = r.Big, Version = r.U32(h + 8), Width = r.U16(), Height = r.U16(), Alignment = r.U16(), Format = (FlimFormat)r.U8() };
        byte ts = r.U8();
        f.TileMode = (byte)(ts & 0x1F);
        f.Swizzle = (byte)(ts >> 5);
        f.ImageSize = r.U32();
        f.ImageData = bytes.AsSpan(0, (int)Math.Min(f.ImageSize, (uint)h)).ToArray();
        return f;
    }

    public uint Gx2Format => Format switch
    {
        FlimFormat.L8 or FlimFormat.A8 => 0x01,
        FlimFormat.LA4 or FlimFormat.L4 or FlimFormat.A4 => 0x02,
        FlimFormat.LA8 or FlimFormat.HiLo8 => 0x07,
        FlimFormat.Rgb565 or FlimFormat.Rgb565Indirect => 0x08,
        FlimFormat.Rgb5A1 => 0x0A,
        FlimFormat.Rgba4 => 0x0B,
        FlimFormat.Rgb10A2 => 0x19,
        FlimFormat.Rgb8 or FlimFormat.Rgba8 => 0x1A,
        FlimFormat.Rgba8Srgb => 0x41A,
        FlimFormat.Bc1 => 0x31,
        FlimFormat.Bc2 => 0x32,
        FlimFormat.Bc3 => 0x33,
        FlimFormat.Bc4L or FlimFormat.Bc4A => 0x34,
        FlimFormat.Bc5 => 0x35,
        FlimFormat.Bc1Srgb => 0x431,
        FlimFormat.Bc2Srgb => 0x432,
        FlimFormat.Bc3Srgb => 0x433,
        _ => throw new NotSupportedException($"FLIM format {Format} has no GX2 equivalent.")
    };

    public byte[] Gx2ComponentSelector => Format switch
    {
        FlimFormat.L8 or FlimFormat.L4 or FlimFormat.Bc4L => [0, 0, 0, 5],
        FlimFormat.A8 or FlimFormat.A4 or FlimFormat.Bc4A => [5, 5, 5, 0],
        FlimFormat.LA4 or FlimFormat.LA8 => [0, 0, 0, 1],
        FlimFormat.Bc5 => [0, 0, 0, 1],
        FlimFormat.HiLo8 => [0, 1, 4, 5],
        FlimFormat.Rgb565 or FlimFormat.Rgb8 or FlimFormat.Rgb565Indirect => [0, 1, 2, 5],
        _ => [0, 1, 2, 3]
    };

    public uint Gx2Swizzle => (uint)Swizzle << 8;
}
