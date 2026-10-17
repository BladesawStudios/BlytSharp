using System.Numerics;

namespace BlytSharp;

public sealed class LayoutInfo
{
    public byte DrawFromCenter { get; set; }
    public byte[] Reserved { get; set; } = new byte[3];
    public float Width { get; set; }
    public float Height { get; set; }
    public float MaxPartsWidth { get; set; }
    public float MaxPartsHeight { get; set; }
    public string Name { get; set; } = "";
}

public enum UserDataType : byte { String, Int, Float, Struct }

public sealed class UserDataEntry
{
    public string Name { get; set; } = "";
    public UserDataType Type { get; set; }
    public byte Reserved { get; set; }
    public ushort Count { get; set; }
    public string? String { get; set; }
    public int[]? Ints { get; set; }
    public float[]? Floats { get; set; }
    /// <summary>For <see cref="UserDataType.Struct"/>: the entry's bytes, exactly as stored.</summary>
    public byte[]? Raw { get; set; }
    /// <summary>For <see cref="UserDataType.Struct"/> entries in the system format (named "ui2dsys"): the parsed block, when it parses.</summary>
    public SystemUserData? System { get; set; }
}

/// <summary>
/// The struct user data NintendoWare's layout editor writes as "ui2dsys": a u16 version, a u16 item
/// count and an offset table (from the block start), then items that each begin with a u32 type.
/// Only the type 0 item (a list of strings, e.g. a layout's animation names) has a known layout;
/// every other item keeps its bytes after the type in <see cref="SystemUserDataItem.Data"/>.
/// </summary>
public sealed class SystemUserData
{
    public ushort Version { get; set; }
    public List<SystemUserDataItem> Items { get; } = [];
}

public sealed class SystemUserDataItem
{
    public uint Type { get; set; }
    /// <summary>The item's bytes after its type, up to the next item or the end of the block.</summary>
    public byte[] Data { get; set; } = [];
    /// <summary>Type 0: u32 count, then u32 offsets (from the item start) to null-terminated strings.</summary>
    public List<string>? Strings { get; set; }
}

public sealed class TexMap
{
    public ushort TextureIndex { get; set; }
    public byte Flag1 { get; set; }
    public byte Flag2 { get; set; }
    public int WrapS => Flag1 & 3;
    public int MinFilter => (Flag1 >> 2) & 3;
    public int WrapT => Flag2 & 3;
    public int MagFilter => (Flag2 >> 2) & 3;
}

public sealed class TexSrt
{
    public Vector2 Translate { get; set; }
    public float Rotate { get; set; }
    public Vector2 Scale { get; set; }
}

public sealed class TexCoordGen
{
    public byte Matrix { get; set; }
    public byte Source { get; set; }
    public byte[] Reserved { get; set; } = [];
}

public sealed class TevStage
{
    public byte RgbMode { get; set; }
    public byte AlphaMode { get; set; }
    public ushort Reserved { get; set; }
}

public sealed class AlphaCompare
{
    public byte Function { get; set; }
    public byte[] Reserved { get; set; } = new byte[3];
    public float Reference { get; set; }
}

public sealed class BlendMode
{
    public byte Operation { get; set; }
    public byte Source { get; set; }
    public byte Destination { get; set; }
    public byte LogicOperation { get; set; }
}

public sealed class IndirectParameter
{
    public float Rotate { get; set; }
    public Vector2 Scale { get; set; }
}

public sealed class ProjectionTexGen
{
    public Vector2 Translate { get; set; }
    public Vector2 Scale { get; set; }
    public byte Flags { get; set; }
    public byte[] Reserved { get; set; } = new byte[3];
}

public sealed class FontShadowParameter
{
    public Rgba8 Black { get; set; }
    public Rgba8 White { get; set; }
}

public sealed class MaterialColor
{
    public bool IsFloat { get; set; }
    public Rgba8 Byte { get; set; }
    public Vector4 Float { get; set; }
    public override string ToString() => IsFloat ? $"({Float.X}, {Float.Y}, {Float.Z}, {Float.W})" : Byte.ToString();
}

public sealed class Material
{
    public string Name { get; set; } = "";
    public uint Flags { get; set; }
    public byte ColorTypeFlags { get; set; }
    public List<MaterialColor> Colors { get; } = [];
    public List<TexMap> TexMaps { get; } = [];
    public List<TexSrt> TexSrts { get; } = [];
    public List<TexCoordGen> TexCoordGens { get; } = [];
    public List<TevStage> TevStages { get; } = [];
    public AlphaCompare? AlphaCompare { get; set; }
    public BlendMode? ColorBlend { get; set; }
    public BlendMode? AlphaBlend { get; set; }
    public IndirectParameter? Indirect { get; set; }
    public List<ProjectionTexGen> ProjectionTexGens { get; } = [];
    public FontShadowParameter? FontShadow { get; set; }
    public byte[] Extra { get; set; } = [];

    public int TexMapCount => (int)(Flags & 3);
    public int TexSrtCount => (int)((Flags >> 2) & 3);
    public int TexCoordGenCount => (int)((Flags >> 4) & 3);
    public int TevStageCount => (int)((Flags >> 6) & 7);
    public bool HasAlphaCompare => (Flags & (1 << 9)) != 0;
    public bool HasColorBlend => (Flags & (1 << 10)) != 0;
    public bool TextureOnly => (Flags & (1 << 11)) != 0;
    public bool HasAlphaBlend => (Flags & (1 << 12)) != 0;
    public bool HasIndirect => (Flags & (1 << 14)) != 0;
    public int ProjectionTexGenCount => (int)((Flags >> 15) & 3);
    public bool HasFontShadow => (Flags & (1 << 17)) != 0;
    public bool ThresholdingAlphaInterpolation => (Flags & (1 << 18)) != 0;
}

public class Pane
{
    public string Magic { get; set; } = "pan1";
    public byte Flags { get; set; }
    public byte BasePosition { get; set; }
    public byte Alpha { get; set; }
    public byte PartsScale { get; set; }
    public string Name { get; set; } = "";
    public string UserInfo { get; set; } = "";
    public Vector3 Translate { get; set; }
    public Vector3 Rotate { get; set; }
    public Vector2 Scale { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public List<UserDataEntry>? UserData { get; set; }
    public List<Pane> Children { get; } = [];
    public byte[] Extra { get; set; } = [];

    public bool Visible => (Flags & 1) != 0;
    public bool InfluencedAlpha => (Flags & 2) != 0;
    public bool LocationAdjust => (Flags & 4) != 0;
}

public sealed class TexCoord
{
    public Vector2 TopLeft { get; set; }
    public Vector2 TopRight { get; set; }
    public Vector2 BottomLeft { get; set; }
    public Vector2 BottomRight { get; set; }
}

public sealed class PicturePane : Pane
{
    public Rgba8[] VertexColors { get; set; } = new Rgba8[4];
    public ushort MaterialIndex { get; set; }
    public byte PictureFlags { get; set; }
    public List<TexCoord> TexCoords { get; } = [];
}

public sealed class TextPane : Pane
{
    public ushort TextBufferBytes { get; set; }
    public ushort TextStringBytes { get; set; }
    public ushort MaterialIndex { get; set; }
    public ushort FontIndex { get; set; }
    public byte TextPosition { get; set; }
    public byte TextAlignment { get; set; }
    public byte TextBoxFlags { get; set; }
    public byte Reserved { get; set; }
    public float ItalicRatio { get; set; }
    public string? Text { get; set; }
    public Rgba8 TopColor { get; set; }
    public Rgba8 BottomColor { get; set; }
    public Vector2 FontSize { get; set; }
    public float CharSpace { get; set; }
    public float LineSpace { get; set; }
    public string? TextId { get; set; }
    public Vector2 ShadowOffset { get; set; }
    public Vector2 ShadowScale { get; set; }
    public Rgba8 ShadowTopColor { get; set; }
    public Rgba8 ShadowBottomColor { get; set; }
    public float ShadowItalicRatio { get; set; }
    public byte[]? LineWidthOffsets { get; set; }
    public PerCharacterTransform? PerCharacterTransform { get; set; }
}

public sealed class PerCharacterTransform
{
    public float EvalTimeOffset { get; set; }
    public float EvalTimeWidth { get; set; }
    public byte LoopType { get; set; }
    public byte OriginV { get; set; }
    public byte HasAnimInfo { get; set; }
    public byte Reserved { get; set; }
    /// <summary>
    /// Version 9+: 0x14 more bytes between the header and the animation, unnamed (seen: u32 0, f32, u32 1, u32 0, u32 0).
    /// Empty for older versions.
    /// </summary>
    public byte[] Extra { get; set; } = [];
    public AnimTarget? Anim { get; set; }
}

public sealed class WindowFrame
{
    public ushort MaterialIndex { get; set; }
    public byte TextureFlip { get; set; }
    public byte Reserved { get; set; }
}

public sealed class WindowPane : Pane
{
    public short[] Inflation { get; set; } = new short[4];
    public ushort[] FrameSize { get; set; } = new ushort[4];
    public byte WindowFlags { get; set; }
    public ushort Reserved { get; set; }
    public Rgba8[] ContentVertexColors { get; set; } = new Rgba8[4];
    public ushort ContentMaterialIndex { get; set; }
    public byte ContentReserved { get; set; }
    public List<TexCoord> ContentTexCoords { get; } = [];
    public List<WindowFrame> Frames { get; } = [];
}

public sealed class PartsBasicInfo
{
    public string UserName { get; set; } = "";
    public Vector3 Translate { get; set; }
    public Vector3 Rotate { get; set; }
    public Vector2 Scale { get; set; }
    public Vector2 Size { get; set; }
    public byte Alpha { get; set; }
    public byte[] Reserved { get; set; } = new byte[3];
}

public sealed class PartsProperty
{
    public string Name { get; set; } = "";
    public byte UsageFlags { get; set; }
    public byte BasicUsageFlags { get; set; }
    public byte MaterialUsageFlags { get; set; }
    public byte Reserved { get; set; }
    public Pane? Pane { get; set; }
    public List<UserDataEntry>? UserData { get; set; }
    public PartsBasicInfo? BasicInfo { get; set; }
}

public sealed class PartsPane : Pane
{
    public Vector2 Magnify { get; set; }
    public List<PartsProperty> Properties { get; } = [];
    public string PartsName { get; set; } = "";
}

public sealed class Group
{
    public string Name { get; set; } = "";
    public List<string> Panes { get; } = [];
    public List<Group> Children { get; } = [];
}

public sealed class ControlSource
{
    public string UserName { get; set; } = "";
    public string ControlName { get; set; } = "";
    public List<string> FunctionalPanes { get; } = [];
    public List<string> FunctionalAnimations { get; } = [];
    public List<string> PaneParameterNames { get; } = [];
    public List<string> AnimParameterNames { get; } = [];
}

public sealed class Bflyt
{
    public bool BigEndian { get; set; }
    public uint Version { get; set; }
    public LayoutInfo? Info { get; set; }
    public List<UserDataEntry>? UserData { get; set; }
    public List<string>? Textures { get; set; }
    public List<string>? Fonts { get; set; }
    public List<Material>? Materials { get; set; }
    public Pane? RootPane { get; set; }
    public Group? RootGroup { get; set; }
    public List<ControlSource> Controls { get; } = [];
    public List<Section> UnknownSections { get; } = [];
    public List<string> SectionOrder { get; } = [];

    public int Major => (int)(Version >> 24);

    public static bool IsBflyt(ReadOnlySpan<byte> d) => d.Length >= 0x14 && d[..4].SequenceEqual("FLYT"u8);

    const int PerCharacterExtraSize = 0x14;

    public static Bflyt FromBinary(ReadOnlySpan<byte> data)
    {
        if (!IsBflyt(data)) throw new InvalidDataException("Not a BFLYT file.");
        var r = BinReader.ForHeader(data.ToArray());
        var f = new Bflyt { BigEndian = r.Big, Version = r.U32(8) };
        int pos = r.U16(6), count = r.U16(0x10);
        var paneStack = new Stack<Pane>();
        var groupStack = new Stack<Group>();
        Pane? lastPane = null;
        Group? lastGroup = null;
        bool afterLayout = false;
        for (int i = 0; i < count && pos + 8 <= r.Data.Length; i++)
        {
            string magic = r.Magic(pos);
            int size = (int)r.U32(pos + 4);
            f.SectionOrder.Add(magic);
            r.Pos = pos + 8;
            bool wasLayout = afterLayout;
            afterLayout = false;
            switch (magic)
            {
                case "lyt1":
                    f.Info = new LayoutInfo
                    {
                        DrawFromCenter = r.U8(), Reserved = r.Bytes(3), Width = r.F32(), Height = r.F32(),
                        MaxPartsWidth = r.F32(), MaxPartsHeight = r.F32()
                    };
                    f.Info.Name = r.CStr(r.Pos, pos + size);
                    afterLayout = true;
                    break;
                case "txl1": f.Textures = StringTable(r, pos + size); break;
                case "fnl1": f.Fonts = StringTable(r, pos + size); break;
                case "mat1":
                {
                    f.Materials = [];
                    int n = r.U16();
                    r.Pos += 2;
                    var offs = new int[n];
                    for (int k = 0; k < n; k++) offs[k] = (int)r.U32();
                    for (int k = 0; k < n; k++)
                    {
                        int start = pos + offs[k], end = k + 1 < n ? pos + offs[k + 1] : pos + size;
                        f.Materials.Add(ReadMaterial(r, start, end, f.Major));
                    }
                    break;
                }
                case "pan1" or "pic1" or "txt1" or "wnd1" or "prt1" or "bnd1" or "scr1" or "ali1":
                {
                    var p = ReadPane(r, pos, size, f.Major);
                    if (paneStack.Count > 0) paneStack.Peek().Children.Add(p);
                    else if (f.RootPane is null) f.RootPane = p;
                    else throw new InvalidDataException("Multiple root panes.");
                    lastPane = p;
                    break;
                }
                case "pas1":
                    if (lastPane is null) throw new InvalidDataException("pas1 without a pane.");
                    paneStack.Push(lastPane);
                    break;
                case "pae1":
                    lastPane = paneStack.Pop();
                    break;
                case "grp1":
                {
                    var g = new Group { Name = r.FixedString(0x22) };
                    int n = r.U16();
                    for (int k = 0; k < n; k++) g.Panes.Add(r.FixedString(24));
                    if (groupStack.Count > 0) groupStack.Peek().Children.Add(g);
                    else if (f.RootGroup is null) f.RootGroup = g;
                    else throw new InvalidDataException("Multiple root groups.");
                    lastGroup = g;
                    break;
                }
                case "grs1":
                    if (lastGroup is null) throw new InvalidDataException("grs1 without a group.");
                    groupStack.Push(lastGroup);
                    break;
                case "gre1":
                    lastGroup = groupStack.Pop();
                    break;
                case "usd1":
                {
                    var ud = ReadUserData(r, pos);
                    if (wasLayout) f.UserData = ud;
                    else if (lastPane is not null) lastPane.UserData = ud;
                    else throw new InvalidDataException("usd1 without an owner.");
                    break;
                }
                case "cnt1": f.Controls.Add(ReadControl(r, pos, size)); break;
                default: f.UnknownSections.Add(new Section(magic, r.Data.AsSpan(pos, size).ToArray())); break;
            }
            pos += size;
        }
        if (paneStack.Count != 0 || groupStack.Count != 0) throw new InvalidDataException("Unbalanced pane or group hierarchy.");
        return f;
    }

    static List<string> StringTable(BinReader r, int end)
    {
        int n = r.U16();
        r.Pos += 2;
        int baseAt = r.Pos;
        var res = new List<string>(n);
        for (int k = 0; k < n; k++) res.Add(r.CStr(baseAt + (int)r.U32(baseAt + k * 4), end));
        return res;
    }

    internal static List<UserDataEntry> ReadUserData(BinReader r, int pos)
    {
        r.Pos = pos + 8;
        int n = r.U16();
        r.Pos += 2;
        int blockEnd = Math.Min(r.Data.Length, pos + (int)r.U32(pos + 4));
        // every name and data position in the block, to bound a struct entry (it has no stored size)
        var marks = new List<int>(n * 2);
        for (int k = 0; k < n; k++)
        {
            int e = pos + 0xC + k * 0xC;
            int nameOff = (int)r.U32(e), dataOff = (int)r.U32(e + 4);
            if (nameOff != 0) marks.Add(e + nameOff);
            if (dataOff != 0) marks.Add(e + dataOff);
        }
        var res = new List<UserDataEntry>(n);
        for (int k = 0; k < n; k++)
        {
            int e = pos + 0xC + k * 0xC;
            int nameOff = (int)r.U32(e), dataOff = (int)r.U32(e + 4);
            var u = new UserDataEntry
            {
                Name = nameOff == 0 ? "" : r.CStr(e + nameOff), Count = r.U16(e + 8), Type = (UserDataType)r.Data[e + 10], Reserved = r.Data[e + 11]
            };
            int d = e + dataOff;
            switch (u.Type)
            {
                case UserDataType.String: u.String = System.Text.Encoding.UTF8.GetString(r.Data, d, u.Count).TrimEnd('\0'); break;
                case UserDataType.Int: u.Ints = Enumerable.Range(0, u.Count).Select(j => (int)r.U32(d + j * 4)).ToArray(); break;
                case UserDataType.Float: u.Floats = Enumerable.Range(0, u.Count).Select(j => r.F32(d + j * 4)).ToArray(); break;
                case UserDataType.Struct:
                {
                    int end = marks.Where(m => m > d && m <= blockEnd).DefaultIfEmpty(blockEnd).Min();
                    u.Raw = d < end ? r.Data.AsSpan(d, end - d).ToArray() : [];
                    u.System = ReadSystemUserData(r, u.Raw);
                    break;
                }
                default: throw new InvalidDataException($"Unsupported user data type {u.Type}.");
            }
            res.Add(u);
        }
        return res;
    }

    /// <summary>Parses a "ui2dsys" block; null when its header or offsets don't fit.</summary>
    static SystemUserData? ReadSystemUserData(BinReader r, byte[] raw)
    {
        var b = new BinReader(raw, r.Big);
        if (raw.Length < 4) return null;
        int count = b.U16(2);
        if (4 + count * 4 > raw.Length) return null;
        var offs = new int[count];
        for (int i = 0; i < count; i++)
        {
            offs[i] = (int)b.U32(4 + i * 4);
            if (offs[i] < 4 + count * 4 || offs[i] + 4 > raw.Length || (i > 0 && offs[i] <= offs[i - 1])) return null;
        }
        var sys = new SystemUserData { Version = b.U16(0) };
        for (int i = 0; i < count; i++)
        {
            int start = offs[i], end = i + 1 < count ? offs[i + 1] : raw.Length;
            var item = new SystemUserDataItem { Type = b.U32(start), Data = raw.AsSpan(start + 4, end - start - 4).ToArray() };
            if (item.Type == 0 && end - start >= 8)
            {
                int n = (int)b.U32(start + 4);
                if (8 + n * 4 <= end - start)
                {
                    var strings = new List<string>(n);
                    for (int k = 0; k < n; k++)
                    {
                        int at = start + (int)b.U32(start + 8 + k * 4);
                        if (at < start || at >= end) { strings = null; break; }
                        strings.Add(b.CStr(at, end));
                    }
                    item.Strings = strings;
                }
            }
            sys.Items.Add(item);
        }
        return sys;
    }

    static ControlSource ReadControl(BinReader r, int pos, int size)
    {
        var c = new ControlSource();
        int nameOff = (int)r.U32(pos + 8), paneNames = (int)r.U32(pos + 0xC);
        int paneCount = r.U16(pos + 0x10), animCount = r.U16(pos + 0x12);
        int paneParams = (int)r.U32(pos + 0x14), animParams = (int)r.U32(pos + 0x18);
        c.UserName = r.CStr(pos + 0x1C, pos + size);
        c.ControlName = r.CStr(pos + nameOff, pos + size);
        for (int k = 0; k < paneCount; k++) c.FunctionalPanes.Add(r.CStr(pos + paneNames + k * 24, pos + paneNames + k * 24 + 24));
        int animTable = pos + paneNames + paneCount * 24;
        for (int k = 0; k < animCount; k++) c.FunctionalAnimations.Add(r.CStr(animTable + (int)r.U32(animTable + k * 4), pos + size));
        for (int k = 0; k < paneCount; k++) c.PaneParameterNames.Add(r.CStr(pos + paneParams + (int)r.U32(pos + paneParams + k * 4), pos + size));
        for (int k = 0; k < animCount; k++) c.AnimParameterNames.Add(r.CStr(pos + animParams + (int)r.U32(pos + animParams + k * 4), pos + size));
        return c;
    }

    static Material ReadMaterial(BinReader r, int start, int end, int major)
    {
        r.Pos = start;
        var m = new Material { Name = r.FixedString(0x1C) };
        if (major >= 8)
        {
            m.Flags = r.U32();
            m.ColorTypeFlags = r.U8();
            int n = r.U8();
            int hdr = r.Pos - 2;
            var offs = r.Bytes(n);
            for (int k = 0; k < n; k++)
            {
                r.Pos = hdr + offs[k];
                bool isFloat = (m.ColorTypeFlags & (1 << k)) != 0;
                m.Colors.Add(isFloat ? new MaterialColor { IsFloat = true, Float = new Vector4(r.F32(), r.F32(), r.F32(), r.F32()) } : new MaterialColor { Byte = r.Color() });
            }
            if (n == 0) r.Pos = (hdr + 2 + 3) & ~3;
        }
        else
        {
            m.Colors.Add(new MaterialColor { Byte = r.Color() });
            m.Colors.Add(new MaterialColor { Byte = r.Color() });
            m.Flags = r.U32();
        }
        for (int k = 0; k < m.TexMapCount; k++) m.TexMaps.Add(new TexMap { TextureIndex = r.U16(), Flag1 = r.U8(), Flag2 = r.U8() });
        for (int k = 0; k < m.TexSrtCount; k++) m.TexSrts.Add(new TexSrt { Translate = r.Vec2(), Rotate = r.F32(), Scale = r.Vec2() });
        for (int k = 0; k < m.TexCoordGenCount; k++)
            m.TexCoordGens.Add(new TexCoordGen { Matrix = r.U8(), Source = r.U8(), Reserved = r.Bytes(major >= 8 ? 14 : 6) });
        for (int k = 0; k < m.TevStageCount; k++) m.TevStages.Add(new TevStage { RgbMode = r.U8(), AlphaMode = r.U8(), Reserved = r.U16() });
        if (m.HasAlphaCompare) m.AlphaCompare = new AlphaCompare { Function = r.U8(), Reserved = r.Bytes(3), Reference = r.F32() };
        if (m.HasColorBlend) m.ColorBlend = Blend(r);
        if (m.HasAlphaBlend) m.AlphaBlend = Blend(r);
        if (m.HasIndirect) m.Indirect = new IndirectParameter { Rotate = r.F32(), Scale = r.Vec2() };
        for (int k = 0; k < m.ProjectionTexGenCount; k++)
            m.ProjectionTexGens.Add(new ProjectionTexGen { Translate = r.Vec2(), Scale = r.Vec2(), Flags = r.U8(), Reserved = r.Bytes(3) });
        if (m.HasFontShadow) m.FontShadow = new FontShadowParameter { Black = r.Color(), White = r.Color() };
        if (r.Pos > end) throw new InvalidDataException($"Material {m.Name} overruns its entry (flags 0x{m.Flags:X}).");
        m.Extra = r.Bytes(end - r.Pos);
        return m;
    }

    static BlendMode Blend(BinReader r) => new() { Operation = r.U8(), Source = r.U8(), Destination = r.U8(), LogicOperation = r.U8() };

    static TexCoord ReadTexCoord(BinReader r) => new() { TopLeft = r.Vec2(), TopRight = r.Vec2(), BottomLeft = r.Vec2(), BottomRight = r.Vec2() };

    internal static Pane ReadPane(BinReader r, int pos, int size, int major)
    {
        string magic = r.Magic(pos);
        Pane p = magic switch
        {
            "pic1" => new PicturePane(),
            "txt1" => new TextPane(),
            "wnd1" => new WindowPane(),
            "prt1" => new PartsPane(),
            _ => new Pane()
        };
        p.Magic = magic;
        r.Pos = pos + 8;
        p.Flags = r.U8(); p.BasePosition = r.U8(); p.Alpha = r.U8(); p.PartsScale = r.U8();
        p.Name = r.FixedString(24);
        p.UserInfo = r.FixedString(8);
        p.Translate = r.Vec3(); p.Rotate = r.Vec3(); p.Scale = r.Vec2();
        p.Width = r.F32(); p.Height = r.F32();
        int end = pos + size;
        switch (p)
        {
            case PicturePane pic:
                for (int k = 0; k < 4; k++) pic.VertexColors[k] = r.Color();
                pic.MaterialIndex = r.U16();
                int ptc = r.U8();
                pic.PictureFlags = r.U8();
                for (int k = 0; k < ptc; k++) pic.TexCoords.Add(ReadTexCoord(r));
                break;
            case TextPane t:
            {
                t.TextBufferBytes = r.U16(); t.TextStringBytes = r.U16();
                t.MaterialIndex = r.U16(); t.FontIndex = r.U16();
                t.TextPosition = r.U8(); t.TextAlignment = r.U8(); t.TextBoxFlags = r.U8(); t.Reserved = r.U8();
                t.ItalicRatio = r.F32();
                int textOff = (int)r.U32();
                t.TopColor = r.Color(); t.BottomColor = r.Color();
                t.FontSize = r.Vec2(); t.CharSpace = r.F32(); t.LineSpace = r.F32();
                int idOff = (int)r.U32();
                t.ShadowOffset = r.Vec2(); t.ShadowScale = r.Vec2();
                t.ShadowTopColor = r.Color(); t.ShadowBottomColor = r.Color();
                t.ShadowItalicRatio = r.F32();
                int lineOff = major >= 8 ? (int)r.U32() : 0;
                int charOff = (int)r.U32();
                if (textOff != 0 && pos + textOff != r.Pos) throw new InvalidDataException($"Text pane {p.Name} has an unexpected header size.");
                if (textOff != 0 && t.TextStringBytes > 0) t.Text = r.Utf16(pos + textOff, t.TextStringBytes);
                if (idOff != 0) t.TextId = r.CStr(pos + idOff, end);
                if (lineOff != 0) t.LineWidthOffsets = Tail(r, pos + lineOff, end, pos, textOff, idOff, charOff);
                if (charOff != 0)
                {
                    r.Pos = pos + charOff;
                    var pc = new PerCharacterTransform
                    {
                        EvalTimeOffset = r.F32(), EvalTimeWidth = r.F32(), LoopType = r.U8(), OriginV = r.U8(), HasAnimInfo = r.U8(), Reserved = r.U8()
                    };
                    if (major >= 9) pc.Extra = r.Bytes(PerCharacterExtraSize);
                    if (pc.HasAnimInfo != 0) pc.Anim = Bflan.ReadTarget(r, r.Pos, end);
                    t.PerCharacterTransform = pc;
                }
                r.Pos = end;
                break;
            }
            case WindowPane w:
            {
                for (int k = 0; k < 4; k++) w.Inflation[k] = r.S16();
                for (int k = 0; k < 4; k++) w.FrameSize[k] = r.U16();
                int frames = r.U8();
                w.WindowFlags = r.U8();
                w.Reserved = r.U16();
                int contentOff = (int)r.U32(), frameTable = (int)r.U32();
                if (pos + contentOff != r.Pos) throw new InvalidDataException($"Window pane {p.Name} has an unexpected header size.");
                r.Pos = pos + contentOff;
                for (int k = 0; k < 4; k++) w.ContentVertexColors[k] = r.Color();
                w.ContentMaterialIndex = r.U16();
                int tc = r.U8();
                w.ContentReserved = r.U8();
                for (int k = 0; k < tc; k++) w.ContentTexCoords.Add(ReadTexCoord(r));
                for (int k = 0; k < frames; k++)
                {
                    r.Pos = pos + (int)r.U32(pos + frameTable + k * 4);
                    w.Frames.Add(new WindowFrame { MaterialIndex = r.U16(), TextureFlip = r.U8(), Reserved = r.U8() });
                }
                r.Pos = end;
                break;
            }
            case PartsPane pp:
            {
                int n = (int)r.U32();
                pp.Magnify = r.Vec2();
                int props = r.Pos;
                for (int k = 0; k < n; k++)
                {
                    r.Pos = props + k * 0x28;
                    var pr = new PartsProperty { Name = r.FixedString(24), UsageFlags = r.U8(), BasicUsageFlags = r.U8(), MaterialUsageFlags = r.U8(), Reserved = r.U8() };
                    int paneOff = (int)r.U32(), udOff = (int)r.U32(), basicOff = (int)r.U32();
                    if (paneOff != 0) pr.Pane = ReadPane(r, pos + paneOff, (int)r.U32(pos + paneOff + 4), major);
                    // TotK's GetWindow_00 stores 1 here for a property with no user data; that points into the
                    // section header, so only an offset that lands on a usd1 block is read
                    if (udOff != 0 && pos + udOff + 4 <= r.Data.Length && r.Magic(pos + udOff) == "usd1")
                        pr.UserData = ReadUserData(r, pos + udOff);
                    if (basicOff != 0)
                    {
                        r.Pos = pos + basicOff;
                        pr.BasicInfo = new PartsBasicInfo
                        {
                            UserName = r.FixedString(8), Translate = r.Vec3(), Rotate = r.Vec3(), Scale = r.Vec2(), Size = r.Vec2(),
                            Alpha = r.U8(), Reserved = r.Bytes(3)
                        };
                    }
                    pp.Properties.Add(pr);
                }
                pp.PartsName = r.CStr(props + n * 0x28, end);
                r.Pos = end;
                break;
            }
        }
        if (r.Pos > end) throw new InvalidDataException($"Pane {p.Name} overruns its section.");
        p.Extra = r.Bytes(end - r.Pos);
        return p;
    }

    static byte[] Tail(BinReader r, int at, int end, int pos, params int[] others)
    {
        int stop = end;
        foreach (var o in others)
            if (o != 0 && pos + o > at && pos + o < stop) stop = pos + o;
        return r.Data.AsSpan(at, stop - at).ToArray();
    }
}
