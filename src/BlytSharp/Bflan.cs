namespace BlytSharp;

public sealed class AnimTag
{
    public ushort Order { get; set; }
    public string Name { get; set; } = "";
    public List<string> Groups { get; } = [];
    public List<UserDataEntry>? UserData { get; set; }
    public ushort StartFrame { get; set; }
    public ushort EndFrame { get; set; }
    public byte ChildBinding { get; set; }
    public byte[] Reserved { get; set; } = new byte[3];
}

public enum CurveType : byte { Constant, Step, Hermite }

public readonly record struct Keyframe(float Frame, float Value, float Slope);

public sealed class AnimCurve
{
    public byte Index { get; set; }
    public byte Target { get; set; }
    public CurveType Type { get; set; }
    public byte Reserved { get; set; }
    public ushort KeyReserved { get; set; }
    public List<Keyframe> Keys { get; } = [];
    public string? Name { get; set; }
}

public sealed class AnimTarget
{
    public string Magic { get; set; } = "";
    public byte[] Reserved { get; set; } = new byte[3];
    public List<AnimCurve> Curves { get; } = [];
}

public sealed class AnimContent
{
    public string Name { get; set; } = "";
    public byte Type { get; set; }
    public ushort Reserved { get; set; }
    /// <summary>Type 2 (extended user data, FLEU targets): the name of the user data the curves animate, e.g. "__CUS_Float_0".</summary>
    public string? UserDataName { get; set; }
    /// <summary>Type 2: the 4 bytes stored before <see cref="UserDataName"/>, unnamed (seen: 04 00 00 00).</summary>
    public byte[] Extra { get; set; } = [];
    public List<AnimTarget> Targets { get; } = [];
}

public sealed class AnimInfo
{
    public ushort FrameSize { get; set; }
    public byte Loop { get; set; }
    public byte Reserved { get; set; }
    public List<string> Textures { get; } = [];
    public List<AnimContent> Contents { get; } = [];
}

public sealed class Bflan
{
    public bool BigEndian { get; set; }
    public uint Version { get; set; }
    public AnimTag? Tag { get; set; }
    public AnimInfo? Info { get; set; }
    public List<Section> UnknownSections { get; } = [];
    public List<string> SectionOrder { get; } = [];

    public int Major => (int)(Version >> 24);

    public static bool IsBflan(ReadOnlySpan<byte> d) => d.Length >= 0x14 && d[..4].SequenceEqual("FLAN"u8);

    public static Bflan FromBinary(ReadOnlySpan<byte> data)
    {
        if (!IsBflan(data)) throw new InvalidDataException("Not a BFLAN file.");
        var r = BinReader.ForHeader(data.ToArray());
        var f = new Bflan { BigEndian = r.Big, Version = r.U32(8) };
        int pos = r.U16(6), count = r.U16(0x10);
        for (int i = 0; i < count && pos + 8 <= r.Data.Length; i++)
        {
            string magic = r.Magic(pos);
            int size = (int)r.U32(pos + 4);
            f.SectionOrder.Add(magic);
            r.Pos = pos + 8;
            switch (magic)
            {
                case "pat1": f.Tag = ReadTag(r, pos, size, f.Major); break;
                case "pai1": f.Info = ReadInfo(r, pos, size); break;
                default: f.UnknownSections.Add(new Section(magic, r.Data.AsSpan(pos, size).ToArray())); break;
            }
            pos += size;
        }
        return f;
    }

    static AnimTag ReadTag(BinReader r, int pos, int size, int major)
    {
        var t = new AnimTag { Order = r.U16() };
        int groups = r.U16();
        int nameOff = (int)r.U32(), groupOff = (int)r.U32();
        int udOff = major >= 8 ? (int)r.U32() : 0;
        t.StartFrame = r.U16(); t.EndFrame = r.U16();
        t.ChildBinding = r.U8(); t.Reserved = r.Bytes(3);
        t.Name = r.CStr(pos + nameOff, pos + size);
        for (int k = 0; k < groups; k++) t.Groups.Add(r.CStr(pos + groupOff + k * 0x24, pos + groupOff + k * 0x24 + 0x24));
        if (udOff != 0) t.UserData = Bflyt.ReadUserData(r, pos + udOff);
        return t;
    }

    internal static AnimTarget ReadTarget(BinReader r, int tg, int limit)
    {
        var target = new AnimTarget { Magic = r.Magic(tg) };
        r.Pos = tg + 4;
        int entries = r.U8();
        target.Reserved = r.Bytes(3);
        for (int e = 0; e < entries; e++)
        {
            int en = tg + (int)r.U32(tg + 8 + e * 4);
            r.Pos = en;
            var cv = new AnimCurve { Index = r.U8(), Target = r.U8(), Type = (CurveType)r.U8(), Reserved = r.U8() };
            int keys = r.U16();
            cv.KeyReserved = r.U16();
            int keyOff = (int)r.U32();
            // a FLEU curve's name offset sits between its header and its keys; with keys at 0xC there is none
            if (target.Magic == "FLEU" && keyOff > 0xC) cv.Name = r.CStr(en + (int)r.U32(), limit);
            r.Pos = en + keyOff;
            for (int q = 0; q < keys; q++)
                cv.Keys.Add(cv.Type switch
                {
                    CurveType.Step => StepKey(r),
                    CurveType.Hermite => new Keyframe(r.F32(), r.F32(), r.F32()),
                    _ => new Keyframe(r.F32(), r.F32(), 0)
                });
            target.Curves.Add(cv);
        }
        return target;
    }

    static Keyframe StepKey(BinReader r)
    {
        float frame = r.F32();
        ushort value = r.U16();
        r.Pos += 2;
        return new Keyframe(frame, value, 0);
    }

    const byte ExtUserDataContent = 2;

    static AnimInfo ReadInfo(BinReader r, int pos, int size)
    {
        var a = new AnimInfo { FrameSize = r.U16(), Loop = r.U8(), Reserved = r.U8() };
        int texCount = r.U16(), contentCount = r.U16();
        int contentTable = pos + (int)r.U32();
        int texTable = r.Pos;
        for (int k = 0; k < texCount; k++) a.Textures.Add(r.CStr(texTable + (int)r.U32(texTable + k * 4), pos + size));
        for (int k = 0; k < contentCount; k++)
        {
            int c = pos + (int)r.U32(contentTable + k * 4);
            r.Pos = c;
            var ac = new AnimContent { Name = r.FixedString(0x1C) };
            int tags = r.U8();
            ac.Type = r.U8();
            ac.Reserved = r.U16();
            int table = c + 0x20;
            if (ac.Type == ExtUserDataContent)
            {
                // the target table is not inline: 0x20 holds its offset and 0x24 that of the user data name
                table = c + (int)r.U32(c + 0x20);
                int nameAt = c + (int)r.U32(c + 0x24);
                ac.Extra = r.Data.AsSpan(nameAt, 4).ToArray();
                ac.UserDataName = r.CStr(nameAt + 4, pos + size);
            }
            for (int j = 0; j < tags; j++)
            {
                ac.Targets.Add(ReadTarget(r, c + (int)r.U32(table + j * 4), pos + size));
            }
            a.Contents.Add(ac);
        }
        return a;
    }
}
