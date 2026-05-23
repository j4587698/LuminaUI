using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.ColorPicker;

public sealed class LuminaColorPalette : IColorPalette
{
    private static readonly uint[,] DefaultColorValues =
    {
        { 4294898413u, 4294893010u, 4294817701u, 4294676600u, 4294600268u, 4294523168u, 4292158741u, 4289860620u, 4287498245u, 4285137155u },
        { 4294831343u, 4294692824u, 4294353077u, 4294079382u, 4293740667u, 4293467747u, 4291105622u, 4288809800u, 4286448954u, 4284088619u },
        { 4294437367u, 4293905136u, 4292713440u, 4291391441u, 4290005442u, 4288555187u, 4287045278u, 4285601418u, 4284223349u, 4282976865u },
        { 4294176249u, 4293054964u, 4291078121u, 4289101789u, 4287126482u, 4285151943u, 4283903923u, 4282787230u, 4281736330u, 4280816757u },
        { 4293718008u, 4291942640u, 4289180641u, 4286615763u, 4284379076u, 4282339765u, 4281549473u, 4280824972u, 4280232056u, 4279704931u },
        { 4293588479u, 4291553278u, 4288204285u, 4284855036u, 4281505275u, 4278215930u, 4278215382u, 4278210483u, 4278205839u, 4278201451u },
        { 4293523453u, 4291423484u, 4288010488u, 4284662773u, 4281380081u, 4278228462u, 4278221770u, 4278215591u, 4278209411u, 4278203743u },
        { 4293261304u, 4290965488u, 4287290850u, 4284009427u, 4281120965u, 4278559926u, 4278421144u, 4278282617u, 4278209883u, 4278202941u },
        { 4293195764u, 4290834664u, 4287094995u, 4283748801u, 4280795824u, 4278236065u, 4278228361u, 4278220655u, 4278212949u, 4278205498u },
        { 4293720044u, 4291883217u, 4288995495u, 4286435714u, 4284138082u, 4282102598u, 4281374011u, 4280645423u, 4279982372u, 4279319576u },
        { 4294179052u, 4293128400u, 4291355301u, 4289581950u, 4287874395u, 4286297660u, 4284782640u, 4283332902u, 4281948955u, 4280630546u },
        { 4294114022u, 4293129925u, 4291554702u, 4290241371u, 4289190444u, 4288401664u, 4286492160u, 4284713728u, 4282935296u, 4281288192u },
        { 4294966762u, 4294900683u, 4294833048u, 4294764645u, 4294695474u, 4294625280u, 4291865088u, 4289170176u, 4286409216u, 4283648000u },
        { 4294900715u, 4294768078u, 4294568094u, 4294367343u, 4294166081u, 4293964052u, 4291332623u, 4288701962u, 4286072326u, 4283443971u },
        { 4294965482u, 4294897356u, 4294891928u, 4294820197u, 4294813235u, 4294739968u, 4291979008u, 4289219072u, 4286460160u, 4283702528u },
        { 4294572537u, 4293322986u, 4291218125u, 4289178544u, 4287139218u, 4285231221u, 4283784033u, 4282467916u, 4281217592u, 4280033059u },
        { 4294503935u, 4294105855u, 4293113343u, 4291924479u, 4290604287u, 4289087487u, 4286985947u, 4285081784u, 4283309204u, 4281733744u }
    };

    private string? _colorValues;
    private int _shadeCount = DefaultColorValues.GetLength(1);

    public AvaloniaList<Color> BaseColors { get; } = new();

    public string? ColorValues
    {
        get => _colorValues;
        set
        {
            _colorValues = value;
            BaseColors.Clear();

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                BaseColors.Add(Color.Parse(item));
            }
        }
    }

    public int ColorCount => BaseColors.Count > 0 ? BaseColors.Count : DefaultColorValues.GetLength(0);

    public int ShadeCount
    {
        get => BaseColors.Count > 0 ? Math.Max(1, _shadeCount) : DefaultColorValues.GetLength(1);
        set => _shadeCount = Math.Max(1, value);
    }

    public Color GetColor(int colorIndex, int shadeIndex)
    {
        colorIndex = Math.Clamp(colorIndex, 0, ColorCount - 1);
        shadeIndex = Math.Clamp(shadeIndex, 0, ShadeCount - 1);

        return BaseColors.Count == 0
            ? Color.FromUInt32(DefaultColorValues[colorIndex, shadeIndex])
            : CreateShade(BaseColors[colorIndex], shadeIndex, ShadeCount);
    }

    private static Color CreateShade(Color source, int shadeIndex, int shadeCount)
    {
        if (shadeCount <= 1)
        {
            return source;
        }

        var offset = (double)shadeIndex / (shadeCount - 1);
        if (offset < 0.56)
        {
            return Mix(source, Colors.White, (0.56 - offset) / 0.56 * 0.92);
        }

        return Mix(source, Colors.Black, (offset - 0.56) / 0.44 * 0.68);
    }

    private static Color Mix(Color source, Color target, double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);
        var sourceAmount = 1d - amount;

        return Color.FromArgb(
            source.A,
            (byte)Math.Round(source.R * sourceAmount + target.R * amount),
            (byte)Math.Round(source.G * sourceAmount + target.G * amount),
            (byte)Math.Round(source.B * sourceAmount + target.B * amount));
    }
}
