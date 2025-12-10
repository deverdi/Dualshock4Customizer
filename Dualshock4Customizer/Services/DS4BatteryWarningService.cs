using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dualshock4Customizer.Services
{
    public class DS4BatteryWarningService
    {
        private DS4Controller _controller;
        private DS4LedService _ledService;
        private SystemTrayService _systemTray;
        private int _lowBatteryThreshold = 20;
        private bool _warningActive = false;
        private bool _notificationShown = false;
        
        // 2 dakikada bir titreþim
        private CancellationTokenSource _vibrationTimerCts;
        private Task _vibrationTimerTask;

        public bool EnableAutoColorChange { get; set; } = true;
        public bool EnableVibrationAlert { get; set; } = true; // VARSAYILAN AÇIK!
        
        public byte WarningColorR { get; set; } = 255;
        public byte WarningColorG { get; set; } = 0;
        public byte WarningColorB { get; set; } = 0;

        public DS4BatteryWarningService(DS4Controller controller, DS4LedService ledService, SystemTrayService systemTray, int threshold = 20)
        {
            _controller = controller;
            _ledService = ledService;
            _systemTray = systemTray;
            _lowBatteryThreshold = threshold;
            Debug.WriteLine($"[BatteryWarning] Servis olusturuldu - Esik: {threshold}%, Vibration: {EnableVibrationAlert}");
        }

        public void CheckBatteryLevel()
        {
            Debug.WriteLine($"[BatteryWarning] >>> CHECK: {_controller.DisplayName}");
            Debug.WriteLine($"[BatteryWarning]     Pil: {_controller.BatteryPercent}%");
            Debug.WriteLine($"[BatteryWarning]     Esik: {_lowBatteryThreshold}%");
            Debug.WriteLine($"[BatteryWarning]     Sarj: {_controller.IsCharging}");
            Debug.WriteLine($"[BatteryWarning]     Uyari Aktif: {_warningActive}");
            Debug.WriteLine($"[BatteryWarning]     EnableVibrationAlert: {EnableVibrationAlert}");
            
            if (_controller.IsCharging)
            {
                Debug.WriteLine($"[BatteryWarning] SARJDA - uyari kapatiliyor");
                ResetWarning();
                return;
            }

            if (_controller.BatteryPercent <= _lowBatteryThreshold)
            {
                Debug.WriteLine($"[BatteryWarning] !!! DUSUK PIL TESPIT EDILDI !!! {_controller.BatteryPercent}% <= {_lowBatteryThreshold}%");
                
                if (!_warningActive)
                {
                    Debug.WriteLine($"[BatteryWarning] ==> UYARI TETIKLENIYOR!");
                    TriggerWarning();
                }
            }
            else
            {
                Debug.WriteLine($"[BatteryWarning] Pil seviyesi normal: {_controller.BatteryPercent}% > {_lowBatteryThreshold}%");
                if (_warningActive)
                {
                    Debug.WriteLine($"[BatteryWarning] Uyari sifirlaniyor");
                    ResetWarning();
                }
            }
        }

        private void TriggerWarning()
        {
            _warningActive = true;
            Debug.WriteLine($"[BatteryWarning] *** UYARI TETIKLENDI ***");
            Debug.WriteLine($"[BatteryWarning] Kontrolcu: {_controller.DisplayName}");
            Debug.WriteLine($"[BatteryWarning] Pil: %{_controller.BatteryPercent}");

            // Renk deðiþtir
            if (EnableAutoColorChange)
            {
                Debug.WriteLine($"[BatteryWarning] LED rengi degistiriliyor: RGB({WarningColorR}, {WarningColorG}, {WarningColorB})");
                try
                {
                    _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0x00, true);
                    Debug.WriteLine($"[BatteryWarning] LED renk degisimi BASARILI!");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BatteryWarning] LED renk degisimi HATASI: {ex.Message}");
                }
            }

            // Ýlk titreþim + 2 dakikada bir timer baþlat
            if (EnableVibrationAlert)
            {
                Debug.WriteLine($"[BatteryWarning] Titresim uyarisi basladi");
                TriggerInitialVibration();
                Start2MinuteVibrationTimer();
            }

            // SystemTray bildirimi
            if (!_notificationShown)
            {
                Debug.WriteLine($"[BatteryWarning] SystemTray bildirimi gosteriliyor");
                _systemTray?.ShowBalloonTip(
                    "?? Düþük Batarya",
                    $"{_controller.DisplayName} - %{_controller.BatteryPercent}\nLütfen þarj edin!");
                _notificationShown = true;
            }
        }

        private void TriggerInitialVibration()
        {
            Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine($"[BatteryWarning] Ýlk titreþim burst (3 kez)");
                    for (int i = 0; i < 3; i++)
                    {
                        _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0xFF, false);
                        await Task.Delay(200);
                        _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0x00, false);
                        await Task.Delay(200);
                    }
                    Debug.WriteLine($"[BatteryWarning] Ýlk titreþim tamamlandý");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BatteryWarning] Ýlk titreþim HATASI: {ex.Message}");
                }
            });
        }

        private void Start2MinuteVibrationTimer()
        {
            // Önceki timer'ý durdur
            Stop2MinuteVibrationTimer();

            _vibrationTimerCts = new CancellationTokenSource();
            var token = _vibrationTimerCts.Token;

            _vibrationTimerTask = Task.Run(async () =>
            {
                Debug.WriteLine($"[BatteryWarning] ?? 2 dakikalýk titreþim timer'ý baþlatýldý");
                
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // 2 dakika bekle
                        await Task.Delay(TimeSpan.FromMinutes(2), token);
                        
                        if (!token.IsCancellationRequested && _warningActive && EnableVibrationAlert)
                        {
                            Debug.WriteLine($"[BatteryWarning] ? 2 dakika doldu - titreþim gönderiliyor");
                            
                            // 2 titreþim
                            for (int i = 0; i < 2; i++)
                            {
                                _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0xFF, false);
                                await Task.Delay(300);
                                _ledService.SetLedColor(WarningColorR, WarningColorG, WarningColorB, 0x00, false);
                                await Task.Delay(300);
                            }
                            
                            Debug.WriteLine($"[BatteryWarning] ? 2 dakikalýk titreþim tamamlandý");
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        Debug.WriteLine($"[BatteryWarning] Timer iptal edildi");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[BatteryWarning] Timer hatasý: {ex.Message}");
                    }
                }
                
                Debug.WriteLine($"[BatteryWarning] ?? 2 dakikalýk timer durdu");
            }, token);
        }

        private void Stop2MinuteVibrationTimer()
        {
            if (_vibrationTimerCts != null)
            {
                Debug.WriteLine($"[BatteryWarning] 2 dakikalýk timer durduruluyor...");
                _vibrationTimerCts.Cancel();
                try { _vibrationTimerTask?.Wait(1000); } catch { }
                _vibrationTimerCts?.Dispose();
                _vibrationTimerCts = null;
                _vibrationTimerTask = null;
            }
        }

        private void ResetWarning()
        {
            if (_warningActive)
            {
                Debug.WriteLine($"[BatteryWarning] Uyari sifirlandi");
                Stop2MinuteVibrationTimer();
            }
            
            _warningActive = false;
            _notificationShown = false;
        }

        public void UpdateThreshold(int newThreshold)
        {
            Debug.WriteLine($"[BatteryWarning] Esik guncellendi: {_lowBatteryThreshold}% -> {newThreshold}%");
            _lowBatteryThreshold = newThreshold;
            ResetWarning();
        }

        public void Dispose()
        {
            Stop2MinuteVibrationTimer();
        }
    }
}

