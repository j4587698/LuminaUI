using System;
using Avalonia.Animation.Easings;

namespace LuminaUI.Controls;

public sealed class LuminaSpringEase : Easing
{
    public double Damping { get; set; } = 8.0;

    public double Stiffness { get; set; } = 44.0;

    public override double Ease(double progress)
    {
        if (progress <= 0.0)
        {
            return 0.0;
        }
        if (progress >= 1.0)
        {
            return 1.0;
        }
        double raw = Spring(progress);
        double end = Spring(1.0);
        return (Math.Abs(end) < 0.001) ? progress : (raw / end);
    }

    private double Spring(double progress)
    {
        return 1.0 - Math.Exp((0.0 - Damping) * progress) * Math.Cos(Stiffness * progress * 0.12);
    }
}
