using System.Numerics;

namespace BlytSharp;

/// <summary>Plays a BFLAN on a BFLYT by writing the animated values for a frame into the layout's panes and materials.</summary>
/// <remarks>
/// It holds the file's values of every pane and material the animation touches: <c>Apply</c> puts them back before
/// writing a frame, so frames don't accumulate, and <c>Restore</c> leaves the layout as the file has it. To play several
/// animations on one layout, restore them all, then apply each in order without restoring: a later one wins where two
/// animate the same value. Curves are
/// constant, step or Hermite (value + slope keys). Targets, by animation group:
/// <list type="bullet">
/// <item>FLPA (pane SRT): 0-2 translate x/y/z, 3-5 rotate x/y/z, 6-7 scale x/y, 8-9 size w/h;</item>
/// <item>FLVI (visibility): 0;</item>
/// <item>FLVC (vertex colours): 0-15 top-left/top-right/bottom-left/bottom-right × RGBA, 16 pane alpha;</item>
/// <item>FLMC (material colours): 0-3 black RGBA, 4-7 white RGBA (0-255);</item>
/// <item>FLTP (texture pattern): the curve's index is the texture map, the value an index into the animation's texture list.</item>
/// </list>
/// Texture SRT (FLTS), indirect (FLIM) and the other groups are not applied.
/// </remarks>
public sealed class LayoutAnimator
{
    private readonly Bflyt _layout;
    private readonly Bflan _anim;
    private readonly Dictionary<string, Pane> _panes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Material> _materials = new(StringComparer.Ordinal);
    private readonly Dictionary<Pane, (Vector3 T, Vector3 R, Vector2 S, float W, float H, byte Flags, byte Alpha, Rgba8[]? Vc)> _paneOrig = [];
    private readonly Dictionary<Material, (MaterialColor? Black, MaterialColor? White, ushort[] Tex)> _matOrig = [];
    private readonly List<string>? _texturesOrig;

    public int FrameCount => Math.Max(1, (int)(_anim.Info?.FrameSize ?? 1));
    public bool Loops => (_anim.Info?.Loop ?? 0) != 0;
    public int AnimatedPanes { get; }
    public int AnimatedMaterials { get; }

    public LayoutAnimator(Bflyt layout, Bflan anim)
    {
        _layout = layout;
        _anim = anim;
        void Walk(Pane p) { _panes.TryAdd(p.Name, p); foreach (var c in p.Children) Walk(c); }
        if (layout.RootPane != null) Walk(layout.RootPane);
        foreach (var m in layout.Materials ?? []) _materials.TryAdd(m.Name, m);
        _texturesOrig = layout.Textures?.ToList();
        foreach (var content in anim.Info?.Contents ?? [])
        {
            if (_panes.TryGetValue(content.Name, out var p) && content.Targets.Any(t => t.Magic is "FLPA" or "FLVI" or "FLVC"))
                _paneOrig.TryAdd(p, (p.Translate, p.Rotate, p.Scale, p.Width, p.Height, p.Flags, p.Alpha, (p as PicturePane)?.VertexColors.ToArray()));
            if (_materials.TryGetValue(content.Name, out var m) && content.Targets.Any(t => t.Magic is "FLMC" or "FLTP"))
                _matOrig.TryAdd(m, (m.Colors.Count > 0 ? Copy(m.Colors[0]) : null, m.Colors.Count > 1 ? Copy(m.Colors[1]) : null, m.TexMaps.Select(t => t.TextureIndex).ToArray()));
        }
        AnimatedPanes = _paneOrig.Count;
        AnimatedMaterials = _matOrig.Count;
    }

    private static MaterialColor Copy(MaterialColor c) => new() { IsFloat = c.IsFloat, Byte = c.Byte, Float = c.Float };

    public void Restore()
    {
        foreach (var (p, o) in _paneOrig)
        {
            p.Translate = o.T; p.Rotate = o.R; p.Scale = o.S; p.Width = o.W; p.Height = o.H; p.Flags = o.Flags; p.Alpha = o.Alpha;
            if (p is PicturePane pic && o.Vc != null) pic.VertexColors = o.Vc.ToArray();
        }
        foreach (var (m, o) in _matOrig)
        {
            if (o.Black != null && m.Colors.Count > 0) m.Colors[0] = Copy(o.Black);
            if (o.White != null && m.Colors.Count > 1) m.Colors[1] = Copy(o.White);
            for (int i = 0; i < m.TexMaps.Count && i < o.Tex.Length; i++) m.TexMaps[i].TextureIndex = o.Tex[i];
        }
        if (_texturesOrig != null) _layout.Textures = _texturesOrig.ToList();
    }

    // Without restore, only this animation's curves are written over what is there, so several can be applied in turn.
    public void Apply(float frame, bool restore = true)
    {
        if (restore) Restore();
        foreach (var content in _anim.Info?.Contents ?? [])
            foreach (var target in content.Targets)
                foreach (var curve in target.Curves)
                {
                    if (curve.Keys.Count == 0) continue;
                    float v = Evaluate(curve, frame);
                    switch (target.Magic)
                    {
                        case "FLPA" when _panes.TryGetValue(content.Name, out var p): PaneSrt(p, curve.Target, v); break;
                        case "FLVI" when _panes.TryGetValue(content.Name, out var p):
                            p.Flags = (byte)(v >= 0.5f ? p.Flags | 1 : p.Flags & ~1);
                            break;
                        case "FLVC" when _panes.TryGetValue(content.Name, out var p): VertexColor(p, curve.Target, v); break;
                        case "FLMC" when _materials.TryGetValue(content.Name, out var m): MaterialColour(m, curve.Target, v); break;
                        case "FLTP" when _materials.TryGetValue(content.Name, out var m): TexturePattern(m, curve.Index, (int)MathF.Round(v)); break;
                    }
                }
    }

    private static void PaneSrt(Pane p, int target, float v)
    {
        switch (target)
        {
            case 0: p.Translate = p.Translate with { X = v }; break;
            case 1: p.Translate = p.Translate with { Y = v }; break;
            case 2: p.Translate = p.Translate with { Z = v }; break;
            case 3: p.Rotate = p.Rotate with { X = v }; break;
            case 4: p.Rotate = p.Rotate with { Y = v }; break;
            case 5: p.Rotate = p.Rotate with { Z = v }; break;
            case 6: p.Scale = p.Scale with { X = v }; break;
            case 7: p.Scale = p.Scale with { Y = v }; break;
            case 8: p.Width = v; break;
            case 9: p.Height = v; break;
        }
    }

    private static byte B(float v) => (byte)Math.Clamp((int)MathF.Round(v), 0, 255);

    private static void VertexColor(Pane p, int target, float v)
    {
        if (target == 16) { p.Alpha = B(v); return; }
        if (p is not PicturePane pic || target > 15) return;
        var c = pic.VertexColors[target / 4];
        pic.VertexColors[target / 4] = (target % 4) switch
        {
            0 => c with { R = B(v) }, 1 => c with { G = B(v) }, 2 => c with { B = B(v) }, _ => c with { A = B(v) },
        };
    }

    private static void MaterialColour(Material m, int target, float v)
    {
        int which = target / 4, channel = target % 4;
        if (which > 1 || which >= m.Colors.Count) return;
        var c = m.Colors[which];
        if (c.IsFloat)
        {
            var f = c.Float;
            float x = v / 255f;
            c.Float = channel switch { 0 => f with { X = x }, 1 => f with { Y = x }, 2 => f with { Z = x }, _ => f with { W = x } };
        }
        else
        {
            var b = c.Byte;
            c.Byte = channel switch { 0 => b with { R = B(v) }, 1 => b with { G = B(v) }, 2 => b with { B = B(v) }, _ => b with { A = B(v) } };
        }
    }

    private void TexturePattern(Material m, int texMap, int index)
    {
        var names = _anim.Info?.Textures;
        if (names == null || index < 0 || index >= names.Count || texMap >= m.TexMaps.Count) return;
        _layout.Textures ??= [];
        int i = _layout.Textures.IndexOf(names[index]);
        if (i < 0) { _layout.Textures.Add(names[index]); i = _layout.Textures.Count - 1; }
        m.TexMaps[texMap].TextureIndex = (ushort)i;
    }

    // constant and step hold the last key at or before the frame; Hermite interpolates with the keys' slopes
    public static float Evaluate(AnimCurve c, float frame)
    {
        var k = c.Keys;
        if (frame <= k[0].Frame) return k[0].Value;
        if (frame >= k[^1].Frame) return k[^1].Value;
        int i = 0;
        while (i + 1 < k.Count && k[i + 1].Frame <= frame) i++;
        var (a, b) = (k[i], k[i + 1]);
        if (c.Type != CurveType.Hermite) return a.Value;
        float span = b.Frame - a.Frame;
        if (span <= 0) return b.Value;
        float t = (frame - a.Frame) / span, t2 = t * t, t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * a.Value + (t3 - 2 * t2 + t) * a.Slope * span + (-2 * t3 + 3 * t2) * b.Value + (t3 - t2) * b.Slope * span;
    }
}
