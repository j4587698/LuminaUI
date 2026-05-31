using System;
using Avalonia.Animation.Easings;

namespace LuminaUI.Controls;

public sealed class LuminaBackEaseOut : Easing
{
    public LuminaMotionEasingIntensity Intensity { get; set; } = LuminaMotionEasingIntensity.Normal;

    public override double Ease(double progress)
    {
        LuminaMotionEasingIntensity intensity = Intensity;
        double c1 = intensity switch
        {
            LuminaMotionEasingIntensity.Soft => 0.9, 
            LuminaMotionEasingIntensity.Strong => 1.5, 
            _ => 1.15, 
        };
        double c3 = c1 + 1.0;
        return 1.0 + c3 * Math.Pow(progress - 1.0, 3.0) + c1 * Math.Pow(progress - 1.0, 2.0);
    }
}
