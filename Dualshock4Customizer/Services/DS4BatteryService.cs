using System;
using System.Diagnostics;
using System.Linq;
using HidLibrary;

namespace Dualshock4Customizer.Services
{
    public class DS4BatteryService
    {
        private readonly DS4ConnectionService _connectionService;
        public event EventHandler<BatteryStatusEventArgs> BatteryStatusChanged;
        
        private int _lastValidBattery = -1;
        private bool _foundCorrectByte = false;
        private int _correctByteIndex = -1;

        public DS4BatteryService(DS4ConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        }

        public BatteryStatus ReadBatteryStatus()
        {
            if (!_connectionService.CheckConnection())
            {
                return null;
            }

            try
            {
                var device = _connectionService.Device;

                HidDeviceData data = device.Read(1000);

                if (data.Status != HidDeviceData.ReadStatus.Success || data.Data == null || data.Data.Length < 32)
                {
                    return null;
                }

                int batteryPercent = 0;
                bool isCharging = false;

                // Eðer doðru byte'ý bulduysan, direkt onu kullan
                if (_foundCorrectByte && _correctByteIndex > 0)
                {
                    byte correctByte = data.Data[_correctByteIndex];
                    int level = correctByte & 0x0F;
                    isCharging = (correctByte & 0x10) != 0;
                    batteryPercent = Math.Min(100, level * 10);
                    
                    Debug.WriteLine($"?? Kayýtlý byte[{_correctByteIndex}]=0x{correctByte:X2} ? %{batteryPercent}, Þarj={isCharging}");
                }
                else
                {
                    // ÝLK OKUMA: Tüm aday lokasyonlarý tara
                    Debug.WriteLine("?? TÜM ADAYLAR:");
                    
                    // Tüm olasý pil lokasyonlarý
                    int[] candidates = { 30, 31, 32, 12, 13, 11, 29 };
                    
                    foreach (int index in candidates)
                    {
                        if (data.Data.Length > index)
                        {
                            byte b = data.Data[index];
                            int lv = b & 0x0F;
                            bool ch = (b & 0x10) != 0;
                            
                            Debug.WriteLine($"  byte[{index}]=0x{b:X2} ? Level={lv}, Charging={ch}");
                            
                            // Geçerli aday (0-10 arasý seviye)
                            if (lv >= 1 && lv <= 10)
                            {
                                batteryPercent = lv * 10;
                                isCharging = ch;
                                _correctByteIndex = index;
                                _foundCorrectByte = true;
                                
                                Debug.WriteLine($"? DOÐRU BYTE BULUNDU: byte[{index}]");
                                break;
                            }
                        }
                    }
                    
                    // Hâlâ bulamadýysan önbellek kullan
                    if (batteryPercent == 0 && _lastValidBattery > 0)
                    {
                        batteryPercent = _lastValidBattery;
                        Debug.WriteLine($"? Önbellek: %{batteryPercent}");
                    }
                }

                // Geçerli deðeri sakla
                if (batteryPercent > 0)
                {
                    _lastValidBattery = batteryPercent;
                }

                var status = new BatteryStatus
                {
                    Percent = batteryPercent,
                    IsCharging = isCharging
                };

                BatteryStatusChanged?.Invoke(this, new BatteryStatusEventArgs(status));
                return status;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Hata: {ex.Message}");
                return null;
            }
        }
    }

    public class BatteryStatus
    {
        public int Percent { get; set; }
        public bool IsCharging { get; set; }

        public string GetStatusText()
        {
            if (IsCharging)
            {
                if (Percent >= 95) return "Þarj Tamamlandý";
                return "Þarj Ediliyor";
            }

            if (Percent >= 80) return "Yüksek";
            if (Percent >= 50) return "Orta";
            if (Percent >= 20) return "Düþük";
            if (Percent >= 10) return "Çok Düþük";
            return "Kritik!";
        }

        public string GetBatteryBar()
        {
            int bars = Percent / 10;
            return $"[{new string('?', bars)}{new string('?', 10 - bars)}]";
        }
    }

    public class BatteryStatusEventArgs : EventArgs
    {
        public BatteryStatus Status { get; }

        public BatteryStatusEventArgs(BatteryStatus status)
        {
            Status = status;
        }
    }
}
