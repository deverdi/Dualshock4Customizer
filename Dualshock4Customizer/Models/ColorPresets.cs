using System;
using Dualshock4Customizer.Services;

namespace Dualshock4Customizer.Models
{
    /// <summary>
    /// Renk presetleri - switch-case kalabalýðýný ortadan kaldýrýr
    /// </summary>
    public static class ColorPresets
    {
        public static (byte R, byte G, byte B) GetColor(string colorName)
        {
            return colorName switch
            {
                "Kirmizi" => (255, 0, 0),
                "Yesil" => (0, 255, 0),
                "Mavi" => (0, 0, 255),
                "Turuncu" => (255, 165, 0),
                "Beyaz" => (255, 255, 255),
                "Sari" => (255, 255, 0),
                "Mor" => (128, 0, 128),
                "Pembe" => (255, 192, 203),
                "Kapali" => (0, 0, 0),
                _ => (0, 0, 255) // Varsayýlan: Mavi
            };
        }

        public static void ApplyColorToController(DS4Controller controller, string colorName)
        {
            var (r, g, b) = GetColor(colorName);
            controller.LedR = r;
            controller.LedG = g;
            controller.LedB = b;
            controller.SelectedColor = colorName;
        }
    }
}
