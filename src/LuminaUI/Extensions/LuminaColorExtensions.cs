using System;
using Avalonia.Media;

namespace LuminaUI.Extensions;

public static class LuminaColorExtensions
{
    public static Color WithAlpha(this Color color, byte alpha)
    {
        return new Color(alpha, color.R, color.G, color.B);
    }

    public static Color WithAlpha(this Color color, double alpha)
    {
        return color.WithAlpha((byte)Math.Clamp(alpha * 255, 0, 255));
    }

    public static float[] ToRgbFloatArray(this Color color)
    {
        return
        [
            color.R / 255f,
            color.G / 255f,
            color.B / 255f
        ];
    }

    public static void WriteRgbFloatArray(this Color color, Span<float> target)
    {
        if (target.Length < 3)
        {
            throw new ArgumentException("Target span must contain at least three items.", nameof(target));
        }

        target[0] = color.R / 255f;
        target[1] = color.G / 255f;
        target[2] = color.B / 255f;
    }
}
