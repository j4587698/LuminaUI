using System;

namespace LuminaUI.Demo;

public static class DemoPlatformServices
{
    public static Action? ExitApplication { get; set; }

    public static bool TryExitApplication()
    {
        Action? exitApplication = ExitApplication;
        if (exitApplication == null)
        {
            return false;
        }

        exitApplication();
        return true;
    }
}
