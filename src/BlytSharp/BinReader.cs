using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace BlytSharp;

internal sealed class BinReader(byte[] data, bool big)
{
    public readonly byte[] Data = data;
    public readonly bool Big = big;
    public int Pos;

    public static BinReader ForHeader(byte[] data, int bomAt = 4) => new(data, data[bomAt] == 0xFE);

    public byte U8() => Data[Pos++];
    public sbyte S8() => (sbyte)Data[Pos++];
    public ushort U16() { var v = U16(Pos); Pos += 2; return v; }
    public short S16() => (short)U16();
    public uint U32() { var v = U32(Pos); Pos += 4; return v; }
    public int S32() => (int)U32();
    public float F32() => BitConverter.Int32BitsToSingle(S32());
    public Vector2 Vec2() => new(F32(), F32());
    public Vector3 Vec3() => new(F32(), F32(), F32());
    public byte[] Bytes(int n) { var b = Data.AsSpan(Pos, n).ToArray(); Pos += n; return b; }
    public Rgba8 Color() => new(U8(), U8(), U8(), U8());

    public ushort U16(int at) => Big ? BinaryPrimitives.ReadUInt16BigEndian(Data.AsSpan(at)) : BinaryPrimitives.ReadUInt16LittleEndian(Data.AsSpan(at));
    public uint U32(int at) => Big ? BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(at)) : BinaryPrimitives.ReadUInt32LittleEndian(Data.AsSpan(at));
    public float F32(int at) => BitConverter.Int32BitsToSingle((int)U32(at));

    public string Magic(int at) => Encoding.ASCII.GetString(Data, at, 4);

    public string FixedString(int n)
    {
        var s = CStr(Pos, Pos + n);
        Pos += n;
        return s;
    }

    public string CStr(int at, int limit = -1)
    {
        int end = limit < 0 ? Data.Length : Math.Min(limit, Data.Length);
        int e = Array.IndexOf(Data, (byte)0, at, end - at);
        return Encoding.UTF8.GetString(Data, at, (e < 0 ? end : e) - at);
    }

    public string Utf16(int at, int byteLen)
    {
        var s = (Big ? Encoding.BigEndianUnicode : Encoding.Unicode).GetString(Data, at, byteLen);
        int z = s.IndexOf('\0');
        return z < 0 ? s : s[..z];
    }
}

public readonly record struct Rgba8(byte R, byte G, byte B, byte A)
{
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}

public sealed record Section(string Magic, byte[] Data);
