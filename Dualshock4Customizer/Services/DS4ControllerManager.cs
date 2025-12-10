using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using HidLibrary;
using System.ComponentModel;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Tek bir DS4 kontrolcüsünü temsil eder (Bireysel ayarlarla)
    /// </summary>
    public class DS4Controller : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public IHidDevice Device { get; set; }
        public bool IsBluetooth { get; set; }
        public string DisplayName { get; set; }
        
        private int _batteryPercent = 100;
        public int BatteryPercent
        {
            get => _batteryPercent;
            set
            {
                if (_batteryPercent != value)
                {
                    _batteryPercent = value;
                    OnPropertyChanged(nameof(BatteryPercent));
                }
            }
        }

        private bool _isCharging;
        public bool IsCharging
        {
            get => _isCharging;
            set
            {
                if (_isCharging != value)
                {
                    _isCharging = value;
                    OnPropertyChanged(nameof(IsCharging));
                }
            }
        }

        // Bireysel LED ayarlarý
        private byte _ledR = 0;
        public byte LedR
        {
            get => _ledR;
            set
            {
                if (_ledR != value)
                {
                    _ledR = value;
                    OnPropertyChanged(nameof(LedR));
                }
            }
        }

        private byte _ledG = 0;
        public byte LedG
        {
            get => _ledG;
            set
            {
                if (_ledG != value)
                {
                    _ledG = value;
                    OnPropertyChanged(nameof(LedG));
                }
            }
        }

        private byte _ledB = 255;
        public byte LedB
        {
            get => _ledB;
            set
            {
                if (_ledB != value)
                {
                    _ledB = value;
                    OnPropertyChanged(nameof(LedB));
                }
            }
        }

        // Bireysel titreþim/pulse ayarlarý
        private bool _vibrationEnabled = false;
        public bool VibrationEnabled
        {
            get => _vibrationEnabled;
            set
            {
                if (_vibrationEnabled != value)
                {
                    _vibrationEnabled = value;
                    OnPropertyChanged(nameof(VibrationEnabled));
                }
            }
        }

        private bool _pulseEnabled = true;
        public bool PulseEnabled
        {
            get => _pulseEnabled;
            set
            {
                if (_pulseEnabled != value)
                {
                    _pulseEnabled = value;
                    OnPropertyChanged(nameof(PulseEnabled));
                }
            }
        }
        // Aktif profil adý
        public string ActiveProfileName { get; set; } = "";



        // Seçili renk adý (UI için)
        private string _selectedColor = "Mavi";
        public string SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    OnPropertyChanged(nameof(SelectedColor));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Birden fazla DS4 kontrolcüsünü yöneten servis
    /// </summary>
    public class DS4ControllerManager : IDisposable
    {
        private const int VendorId = 0x054C;
        private const int ProductIdDS4USB = 0x05C4;
        private const int ProductIdDS4Bluetooth = 0x09CC;

        private Dictionary<string, DS4Controller> _controllers = new Dictionary<string, DS4Controller>();
        private readonly object _lockObj = new object();

        public IReadOnlyList<DS4Controller> Controllers
        {
            get
            {
                lock (_lockObj)
                {
                    return _controllers.Values.ToList();
                }
            }
        }

        public event EventHandler<ControllerEventArgs> ControllerConnected;
        public event EventHandler<ControllerEventArgs> ControllerDisconnected;

        public int ScanAndConnect()
        {
            try
            {
                lock (_lockObj)
                {
                    var devices = HidDevices.Enumerate(VendorId, ProductIdDS4USB, ProductIdDS4Bluetooth).ToArray();
                    
                    Debug.WriteLine($"?? {devices.Length} DS4 cihaz bulundu");

                    foreach (var device in devices)
                    {
                        string deviceId = device.DevicePath;

                        if (_controllers.ContainsKey(deviceId))
                            continue;

                        try
                        {
                            device.OpenDevice();
                            bool isBT = device.Capabilities.InputReportByteLength == 547;

                            var controller = new DS4Controller
                            {
                                Id = deviceId,
                                Device = device,
                                IsBluetooth = isBT,
                                DisplayName = $"DS4 #{_controllers.Count + 1} ({(isBT ? "BT" : "USB")})",
                                BatteryPercent = 100,
                                SelectedColor = "Mavi"
                            };

                            _controllers.Add(deviceId, controller);
                            
                            Debug.WriteLine($"? {controller.DisplayName} baðlandý");
                            ControllerConnected?.Invoke(this, new ControllerEventArgs(controller));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"? Cihaz açýlamadý: {ex.Message}");
                        }
                    }

                    return _controllers.Count;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Tarama hatasý: {ex.Message}");
                return 0;
            }
        }

        public DS4Controller GetController(string id)
        {
            lock (_lockObj)
            {
                return _controllers.TryGetValue(id, out var controller) ? controller : null;
            }
        }

        public DS4Controller GetFirstController()
        {
            lock (_lockObj)
            {
                return _controllers.Values.FirstOrDefault();
            }
        }

        public void CleanupDisconnected()
        {
            lock (_lockObj)
            {
                var disconnected = _controllers.Where(kvp => !kvp.Value.Device.IsConnected).ToList();

                foreach (var kvp in disconnected)
                {
                    Debug.WriteLine($"?? {kvp.Value.DisplayName} baðlantýsý kesildi");
                    kvp.Value.Device?.CloseDevice();
                    _controllers.Remove(kvp.Key);
                    ControllerDisconnected?.Invoke(this, new ControllerEventArgs(kvp.Value));
                }
            }
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                foreach (var controller in _controllers.Values)
                {
                    controller.Device?.CloseDevice();
                    controller.Device?.Dispose();
                }
                _controllers.Clear();
            }
        }
    }

    public class ControllerEventArgs : EventArgs
    {
        public DS4Controller Controller { get; }

        public ControllerEventArgs(DS4Controller controller)
        {
            Controller = controller;
        }
    }
}
