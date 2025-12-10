using System;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Düþük pil uyarýlarýný yöneten servis
    /// </summary>
    public class DS4NotificationService
    {
        private readonly DS4LedService _ledService;

        public bool VibrationEnabled { get; set; }
        public bool FlashEnabled { get; set; }
        public int LowBatteryThreshold { get; set; } = 20; // %20

        public DS4NotificationService(DS4LedService ledService)
        {
            _ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        }

        /// <summary>
        /// Pil durumuna göre uyarý kontrolü yapar
        /// </summary>
        public void CheckAndNotify(BatteryStatus batteryStatus)
        {
            if (batteryStatus == null) return;

            // Düþük pil kontrolü
            if (batteryStatus.Percent < LowBatteryThreshold && !batteryStatus.IsCharging)
            {
                if (VibrationEnabled || FlashEnabled)
                {
                    // Kýrmýzý uyarý ver
                    byte rumble = VibrationEnabled ? (byte)0xFF : (byte)0x00;
                    _ledService.SetLedColor(255, 0, 0, rumble, FlashEnabled);
                }
            }
        }

        /// <summary>
        /// Uyarý ayarlarýný günceller
        /// </summary>
        public void UpdateSettings(bool vibration, bool flash)
        {
            VibrationEnabled = vibration;
            FlashEnabled = flash;
        }
    }
}
