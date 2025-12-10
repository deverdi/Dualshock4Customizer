using System;
using Dualshock4Customizer.Services;

namespace Dualshock4Customizer.Models
{
    /// <summary>
    /// Profil kategorileri
    /// </summary>
    public enum ProfileCategory
    {
        Genel,
        Oyun,
        Film,
        Muzik,
        Gece,
        Ozel
    }

    /// <summary>
    /// DS4 Controller profil modeli - Gelismis ozellikler
    /// </summary>
    public class DS4Profile
    {
        // ===== TEMEL BILGILER =====
        public string ProfileName { get; set; } = "Yeni Profil";
        public string Description { get; set; } = "";
        public string MacAddress { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUsedDate { get; set; } = DateTime.Now;
        
        // ===== ORGANIZASYON =====
        public ProfileCategory Category { get; set; } = ProfileCategory.Genel;
        public bool IsFavorite { get; set; } = false;
        public string IconEmoji { get; set; } = "??";  // Varsayilan ikon
        public int SortOrder { get; set; } = 0;
        
        // ===== LED RENK AYARLARI =====
        public byte LedR { get; set; } = 0;
        public byte LedG { get; set; } = 0;
        public byte LedB { get; set; } = 255;
        public string ColorName { get; set; } = "Mavi";
        public byte Brightness { get; set; } = 100;  // 0-100 arasi parlaklik
        
        // ===== LED EFEKT AYARLARI =====
        public LedEffectType EffectType { get; set; } = LedEffectType.None;
        public int EffectSpeed { get; set; } = 50;
        
        // ===== BATARYA UYARI AYARLARI =====
        public int LowBatteryThreshold { get; set; } = 20;
        public bool VibrationOnLowBattery { get; set; } = false;
        public bool PulseOnLowBattery { get; set; } = true;
        public bool EnableBatteryWarning { get; set; } = true;
        public bool EnableBatteryNotification { get; set; } = true;
        public bool AutoColorChangeOnLowBattery { get; set; } = true;
        
        // ===== OYUN ESLESTIRME =====
        public string LinkedGameProcess { get; set; } = "";
        public bool AutoLoadForGame { get; set; } = false;
        
        // ===== KULLANIM ISTATISTIKLERI =====
        public int UsageCount { get; set; } = 0;
        public TimeSpan TotalUsageTime { get; set; } = TimeSpan.Zero;
        
        public DS4Profile() { }
        
        /// <summary>
        /// Controller'dan profil olustur
        /// </summary>
        public static DS4Profile FromController(DS4Controller controller, string name, 
            LedEffectType activeEffect = LedEffectType.None, int effectSpeed = 50)
        {
            return new DS4Profile
            {
                ProfileName = name,
                MacAddress = controller.Id,
                LedR = controller.LedR,
                LedG = controller.LedG,
                LedB = controller.LedB,
                ColorName = controller.SelectedColor,
                VibrationOnLowBattery = controller.VibrationEnabled,
                PulseOnLowBattery = controller.PulseEnabled,
                EffectType = activeEffect,
                EffectSpeed = effectSpeed,
                CreatedDate = DateTime.Now,
                LastUsedDate = DateTime.Now,
                IconEmoji = GetDefaultIconForColor(controller.SelectedColor)
            };
        }
        
        /// <summary>
        /// Profili controller'a uygula
        /// </summary>
        public void ApplyToController(DS4Controller controller)
        {
            // Parlaklik ayarli renk
            float brightnessMultiplier = Brightness / 100f;
            controller.LedR = (byte)(LedR * brightnessMultiplier);
            controller.LedG = (byte)(LedG * brightnessMultiplier);
            controller.LedB = (byte)(LedB * brightnessMultiplier);
            controller.SelectedColor = ColorName;
            controller.VibrationEnabled = VibrationOnLowBattery;
            controller.PulseEnabled = PulseOnLowBattery;
            
            // Kullanim istatistikleri guncelle
            LastUsedDate = DateTime.Now;
            UsageCount++;
        }
        
        /// <summary>
        /// Renk icin varsayilan ikon
        /// </summary>
        private static string GetDefaultIconForColor(string colorName)
        {
            return colorName switch
            {
                "Kirmizi" => "??",
                "Yesil" => "??",
                "Mavi" => "??",
                "Turuncu" => "??",
                "Beyaz" => "?",
                "Rainbow" => "??",
                "Kapali" => "?",
                _ => "??"
            };
        }
        
        /// <summary>
        /// Profili kopyala
        /// </summary>
        public DS4Profile Clone()
        {
            return new DS4Profile
            {
                ProfileName = ProfileName + " (Kopya)",
                Description = Description,
                Category = Category,
                IsFavorite = false,
                IconEmoji = IconEmoji,
                LedR = LedR,
                LedG = LedG,
                LedB = LedB,
                ColorName = ColorName,
                Brightness = Brightness,
                EffectType = EffectType,
                EffectSpeed = EffectSpeed,
                LowBatteryThreshold = LowBatteryThreshold,
                VibrationOnLowBattery = VibrationOnLowBattery,
                PulseOnLowBattery = PulseOnLowBattery,
                EnableBatteryWarning = EnableBatteryWarning,
                EnableBatteryNotification = EnableBatteryNotification,
                AutoColorChangeOnLowBattery = AutoColorChangeOnLowBattery,
                LinkedGameProcess = LinkedGameProcess,
                AutoLoadForGame = AutoLoadForGame,
                CreatedDate = DateTime.Now,
                LastUsedDate = DateTime.Now
            };
        }
        
        /// <summary>
        /// Profil ozeti (liste gorunumu icin)
        /// </summary>
        public string DisplaySummary
        {
            get
            {
                string effect = EffectType != LedEffectType.None ? $" • {EffectType}" : "";
                string fav = IsFavorite ? " ?" : "";
                return $"{IconEmoji} {ProfileName}{fav}{effect}";
            }
        }
        
        /// <summary>
        /// Kategori etiketi
        /// </summary>
        public string CategoryLabel => Category switch
        {
            ProfileCategory.Oyun => "?? Oyun",
            ProfileCategory.Film => "?? Film",
            ProfileCategory.Muzik => "?? Muzik",
            ProfileCategory.Gece => "?? Gece",
            ProfileCategory.Ozel => "? Ozel",
            _ => "?? Genel"
        };
        
        public override string ToString()
        {
            string effectInfo = EffectType != LedEffectType.None ? $" [{EffectType}]" : "";
            return $"{IconEmoji} {ProfileName} (RGB: {LedR},{LedG},{LedB}{effectInfo})";
        }
    }
}
