namespace BlytSharp;

public readonly record struct CharWidth(sbyte Left, byte GlyphWidth, byte Width);

public sealed class FontInfo
{
    public byte FontType { get; set; }
    public byte Height { get; set; }
    public byte Width { get; set; }
    public byte Ascent { get; set; }
    public ushort LineFeed { get; set; }
    public ushort AlterCharIndex { get; set; }
    public CharWidth DefaultWidth { get; set; }
    public byte Encoding { get; set; }
}

public sealed class GlyphSheets
{
    public byte CellWidth { get; set; }
    public byte CellHeight { get; set; }
    public byte SheetCount { get; set; }
    public byte MaxCharWidth { get; set; }
    public uint SheetSize { get; set; }
    public ushort BaselinePos { get; set; }
    public ushort SheetFormat { get; set; }
    public ushort CellsPerRow { get; set; }
    public ushort CellsPerColumn { get; set; }
    public ushort SheetWidth { get; set; }
    public ushort SheetHeight { get; set; }
    public byte[] Data { get; set; } = [];

    public int Format => SheetFormat & 0x3FFF;
    public bool Compressed => (SheetFormat & 0x8000) != 0;
}

public sealed class WidthBlock
{
    public ushort First { get; set; }
    public ushort Last { get; set; }
    public List<CharWidth> Widths { get; } = [];
}

public enum MapMethod : ushort { Direct, Table, Scan }

public sealed class CodeMap
{
    public uint Begin { get; set; }
    public uint End { get; set; }
    public MapMethod Method { get; set; }
    public ushort Reserved { get; set; }
    public ushort DirectOffset { get; set; }
    public ushort[]? Table { get; set; }
    public List<(uint Code, ushort Index)>? Scan { get; set; }
}

public sealed class Bffnt
{
    public bool BigEndian { get; set; }
    public uint Version { get; set; }
    public FontInfo Info { get; set; } = new();
    public GlyphSheets? Glyphs { get; set; }
    public List<WidthBlock> Widths { get; } = [];
    public List<CodeMap> Maps { get; } = [];
    public SortedDictionary<(uint First, uint Second), short>? Kerning { get; set; }
    public List<Section> UnknownSections { get; } = [];
    public List<string> SectionOrder { get; } = [];

    public int Major => (int)(Version >> 24);

    public static bool IsBffnt(ReadOnlySpan<byte> d) => d.Length >= 0x14 && (d[..4].SequenceEqual("FFNT"u8) || d[..4].SequenceEqual("CFNT"u8));

    public static Bffnt FromBinary(ReadOnlySpan<byte> data)
    {
        if (!IsBffnt(data)) throw new InvalidDataException("Not a BFFNT file.");
        var r = BinReader.ForHeader(data.ToArray());
        var f = new Bffnt { BigEndian = r.Big, Version = r.U32(8) };
        bool wide = f.Major >= 4;
        int pos = r.U16(6), count = r.U16(0x10);
        for (int i = 0; i < count && pos + 8 <= r.Data.Length; i++)
        {
            string magic = r.Magic(pos);
            int size = (int)r.U32(pos + 4);
            f.SectionOrder.Add(magic);
            r.Pos = pos + 8;
            switch (magic)
            {
                case "FINF":
                    f.Info = new FontInfo
                    {
                        FontType = r.U8(), Height = r.U8(), Width = r.U8(), Ascent = r.U8(), LineFeed = r.U16(), AlterCharIndex = r.U16(),
                        DefaultWidth = new CharWidth(r.S8(), r.U8(), r.U8()), Encoding = r.U8()
                    };
                    break;
                case "TGLP":
                {
                    var g = new GlyphSheets
                    {
                        CellWidth = r.U8(), CellHeight = r.U8(), SheetCount = r.U8(), MaxCharWidth = r.U8(), SheetSize = r.U32(),
                        BaselinePos = r.U16(), SheetFormat = r.U16(), CellsPerRow = r.U16(), CellsPerColumn = r.U16(),
                        SheetWidth = r.U16(), SheetHeight = r.U16()
                    };
                    int dataOff = (int)r.U32();
                    long len = wide ? pos + size - dataOff : (long)g.SheetSize * g.SheetCount;
                    g.Data = r.Data.AsSpan(dataOff, (int)Math.Min(len, r.Data.Length - dataOff)).ToArray();
                    f.Glyphs = g;
                    break;
                }
                case "CWDH":
                {
                    var w = new WidthBlock { First = r.U16(), Last = r.U16() };
                    r.Pos += 4;
                    for (int k = w.First; k <= w.Last; k++) w.Widths.Add(new CharWidth(r.S8(), r.U8(), r.U8()));
                    f.Widths.Add(w);
                    break;
                }
                case "CMAP":
                {
                    var m = new CodeMap();
                    if (wide) { m.Begin = r.U32(); m.End = r.U32(); }
                    else { m.Begin = r.U16(); m.End = r.U16(); }
                    m.Method = (MapMethod)r.U16();
                    m.Reserved = r.U16();
                    r.Pos += 4;
                    switch (m.Method)
                    {
                        case MapMethod.Direct: m.DirectOffset = r.U16(); break;
                        case MapMethod.Table:
                            m.Table = new ushort[m.End - m.Begin + 1];
                            for (int k = 0; k < m.Table.Length; k++) m.Table[k] = r.U16();
                            break;
                        case MapMethod.Scan:
                        {
                            int n = r.U16();
                            m.Scan = new List<(uint, ushort)>(n);
                            if (wide)
                            {
                                r.Pos += 2;
                                for (int k = 0; k < n; k++) { uint c = r.U32(); ushort ix = r.U16(); r.Pos += 2; m.Scan.Add((c, ix)); }
                            }
                            else for (int k = 0; k < n; k++) m.Scan.Add((r.U16(), r.U16()));
                            break;
                        }
                        default: throw new InvalidDataException($"Unknown CMAP method {m.Method}.");
                    }
                    f.Maps.Add(m);
                    break;
                }
                case "KRNG": f.Kerning = ReadKerning(r, pos + 8, wide); break;
                default: f.UnknownSections.Add(new Section(magic, r.Data.AsSpan(pos, size).ToArray())); break;
            }
            pos += size;
        }
        return f;
    }

    static SortedDictionary<(uint, uint), short> ReadKerning(BinReader r, int b, bool wide)
    {
        var res = new SortedDictionary<(uint, uint), short>();
        r.Pos = b;
        int n = wide ? (int)r.U32() : r.U16();
        for (int i = 0; i < n; i++)
        {
            uint first;
            int table;
            if (wide) { first = r.U32(); table = b + (int)r.U32(); }
            else { first = r.U16(); table = b + r.U16() * 2; }
            int save = r.Pos;
            r.Pos = table;
            int m = wide ? (int)r.U32() : r.U16();
            for (int j = 0; j < m; j++)
            {
                uint second = wide ? r.U32() : r.U16();
                short v = r.S16();
                if (wide) r.Pos += 2;
                res[(first, second)] = v;
            }
            r.Pos = save;
        }
        return res;
    }

    public Dictionary<uint, int> GlyphIndices()
    {
        var res = new Dictionary<uint, int>();
        foreach (var m in Maps)
            switch (m.Method)
            {
                case MapMethod.Direct:
                    for (uint c = m.Begin; c <= m.End; c++) res[c] = (int)(c - m.Begin + m.DirectOffset);
                    break;
                case MapMethod.Table:
                    for (int k = 0; k < m.Table!.Length; k++) if (m.Table[k] != 0xFFFF) res[m.Begin + (uint)k] = m.Table[k];
                    break;
                case MapMethod.Scan:
                    foreach (var (c, ix) in m.Scan!) if (ix != 0xFFFF) res[c] = ix;
                    break;
            }
        return res;
    }

    public CharWidth WidthOf(int glyph)
    {
        foreach (var w in Widths)
            if (glyph >= w.First && glyph <= w.Last) return w.Widths[glyph - w.First];
        return Info.DefaultWidth;
    }
}
