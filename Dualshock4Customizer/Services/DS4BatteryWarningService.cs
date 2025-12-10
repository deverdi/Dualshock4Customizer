using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Düþük batarya uyarý sistemi
    /// </summary>
    public class DS4BatteryWarningService
    {
        private DS4Controller _controller;
        private DS4LedService _ledService;
        private int _lowBatteryThreshold = 20;
        private bool _warningActive = false;
        private bool _notificationShown = false;

        public bool EnableAutoColorChange { get; set; } = true;
        public bool EnableVibrationAlert { get; set; } = true;
        public bool EnableNotification { get; set; } = true;
        
        // Uyarý rengi
        public byte WarningColorR { get; set; } = 255;
        public byte WarningColorG { get; set; } = 0;
        public byte WarningColorB { get; set; } = 0;

        public DS4BatteryWarningService(DS4Controller controller, DS4LedService ledService, int threshold = 20)
        {
            _controller = controller;
            _ledService = ledService;
            _lowBatteryThreshold = threshold;
        }

        public void CheckBatteryLevel()
        {
            if (_controller.IsCharging)
            {
                ResetWarning();
                return;
            }

            if (_controller.BatteryPercent <= _lowBatteryThreshold)
            {
                if (!_warningActive)
                {
                    TriggerWarning();
                }
            }
            else
            {
                ResetWarning();
            }
        }

        private void TriggerWarning()
        {
            _warningActive = true;
            Debug.WriteLine($"?? Düþük batarya uyarýsý: {_controller.DisplayName} - %{_controller.BatteryPercent}");

            // Renk deðiþtir
            if (EnableAutoColorChange)
            {
                try
                {
                    _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0x00, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"? Uyarý LED hatasý: {ex.Message}");
                }
            }

            // Titreþim uyarýsý
            if (EnableVibrationAlert)
            {
                TriggerVibrationAlert();
            }

            // Sistem bildirimi
            if (EnableNotification && !_notificationShown)
            {
                ShowNotification();
                _notificationShown = true;
            }
        }

        private void TriggerVibrationAlert()
        {
            try
            {
                // Kýsa titreþim paterni (3 kýsa titreþim)
                for (int i = 0; i < 3; i++)
                {
                    _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0xFF, false);
                    System.Threading.Thread.Sleep(200);
                    _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0x00, false);
                    System.Threading.Thread.Sleep(200);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Titreþim uyarýsý hatasý: {ex.Message}");
            }
        }

        private void ShowNotification()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(
                    $"{_controller.DisplayName}\n\nBatarya seviyesi: %{_controller.BatteryPercent}\n\nLütfen kontrolcüyü þarj edin!",
                    "?? Düþük Batarya Uyarýsý",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }));
        }

        private void ResetWarning()
        {
            _warningActive = false;
            _notificationShown = false;
        }

        public void UpdateThreshold(int newThreshold)
        {
            _lowBatteryThreshold = newThreshold;
            ResetWarning();
        }
    }
}
