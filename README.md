# BlytSharp
A library for working with Nintendo layout files!

Confirmed Support:
- Breath of the Wild
- Tears of the Kingdom
- Pokemon: Legends ZA

## Playing animations

`LayoutAnimator` plays a BFLAN on a BFLYT: `Apply(frame)` writes the animated pane SRT, visibility, vertex colours,
material colours and texture patterns into the layout's panes and materials, and `Restore()` puts the file's values back.

```csharp
var layout = Bflyt.FromBinary(File.ReadAllBytes("Title.bflyt"));
var anim = new LayoutAnimator(layout, Bflan.FromBinary(File.ReadAllBytes("Title_In.bflan")));
for (int f = 0; f < anim.FrameCount; f++)
{
    anim.Apply(f);
    // draw layout
}
```
