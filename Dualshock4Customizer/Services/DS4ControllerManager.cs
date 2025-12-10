using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using HidLibrary;

namespace Dualshock4Customizer.Services
{
    public class DS4Controller : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string MacAddress { get; set; }
        public IHidDevice BluetoothDevice { get; set; }
        public IHidDevice UsbDevice { get; set; }
        
        public IHidDevice Device => BluetoothDevice ?? UsbDevice;
        public bool IsBluetooth => BluetoothDevice != null;
        public bool IsUsbConnected => UsbDevice != null;
        
        public string DisplayName { get; set; }
        
        private int _batteryPercent = 100;
        public int BatteryPercent
        {
            get => _batteryPercent;
            set { if (_batteryPercent != value) { _batteryPercent = value; OnPropertyChanged(nameof(BatteryPercent)); } }
        }

        private bool _isCharging;
        public bool IsCharging
        {
            get => _isCharging;
            set { if (_isCharging != value) { _isCharging = value; OnPropertyChanged(nameof(IsCharging)); OnPropertyChanged(nameof(ConnectionStatus)); } }
        }

        public string ConnectionStatus
        {
            get
            {
                if (IsBluetooth && IsUsbConnected)
                    return IsCharging ? "Bluetooth + USB (Þarj)" : "Bluetooth + USB";
                if (IsBluetooth)
                    return "Bluetooth";
                if (IsUsbConnected)
                    return IsCharging ? "USB (Þarj)" : "USB";
                return "Baðlantý kesildi";
            }
        }

        private byte _ledR = 0;
        public byte LedR { get => _ledR; set { if (_ledR != value) { _ledR = value; OnPropertyChanged(nameof(LedR)); } } }
        
        private byte _ledG = 0;
        public byte LedG { get => _ledG; set { if (_ledG != value) { _ledG = value; OnPropertyChanged(nameof(LedG)); } } }
        
        private byte _ledB = 255;
        public byte LedB { get => _ledB; set { if (_ledB != value) { _ledB = value; OnPropertyChanged(nameof(LedB)); } } }

        public bool VibrationEnabled { get; set; } = false;
        public bool PulseEnabled { get; set; } = true;

        private string _activeProfileName = "";
        public string ActiveProfileName { get => _activeProfileName; set { if (_activeProfileName != value) { _activeProfileName = value; OnPropertyChanged(nameof(ActiveProfileName)); } } }

        private string _selectedColor = "Mavi";
        public string SelectedColor { get => _selectedColor; set { if (_selectedColor != value) { _selectedColor = value; OnPropertyChanged(nameof(SelectedColor)); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class DS4ControllerManager : IDisposable
    {
        private const int VendorId = 0x054C;
        private const int ProductIdDS4USB = 0x05C4;
        private const int ProductIdDS4Bluetooth = 0x09CC;

        private readonly Dictionary<string, DS4Controller> _controllers = new();
        private readonly object _lockObj = new();

        public IReadOnlyList<DS4Controller> Controllers
        {
            get { lock (_lockObj) { return _controllers.Values.ToList(); } }
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
                        try
                        {
                            device.OpenDevice();
                            bool isBT = device.Capabilities.InputReportByteLength == 547;
                            string devicePath = device.DevicePath;
                            
                            // Basit ID - DevicePath kullan (MAC okuma sorunlu)
                            var existingController = _controllers.Values.FirstOrDefault(c => 
                                (c.BluetoothDevice?.DevicePath == devicePath) || 
                                (c.UsbDevice?.DevicePath == devicePath));

                            if (existingController != null)
                            {
                                Debug.WriteLine($"?? Cihaz zaten kayýtlý: {devicePath}");
                                continue;
                            }

                            // Yeni kontrolcü
                            string controllerId = devicePath;
                            var controller = new DS4Controller
                            {
                                Id = controllerId,
                                MacAddress = controllerId,
                                DisplayName = $"DS4 #{_controllers.Count + 1}",
                                BatteryPercent = 100,
                                SelectedColor = "Mavi",
                                ActiveProfileName = ""
                            };

                            if (isBT)
                                controller.BluetoothDevice = device;
                            else
                                controller.UsbDevice = device;

                            _controllers.Add(controllerId, controller);
                            Debug.WriteLine($"? {controller.DisplayName} baðlandý ({controller.ConnectionStatus})");
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
            lock (_lockObj) { return _controllers.TryGetValue(id, out var controller) ? controller : null; }
        }

        public DS4Controller GetFirstController()
        {
            lock (_lockObj) { return _controllers.Values.FirstOrDefault(); }
        }

        public void CleanupDisconnected()
        {
            lock (_lockObj)
            {
                var toRemove = new List<string>();

                foreach (var kvp in _controllers)
                {
                    var controller = kvp.Value;
                    bool bluetoothDisconnected = controller.BluetoothDevice != null && !controller.BluetoothDevice.IsConnected;
                    bool usbDisconnected = controller.UsbDevice != null && !controller.UsbDevice.IsConnected;

                    if (bluetoothDisconnected)
                    {
                        Debug.WriteLine($"?? Bluetooth kesildi: {controller.DisplayName}");
                        controller.BluetoothDevice?.CloseDevice();
                        controller.BluetoothDevice = null;
                        controller.OnPropertyChanged(nameof(controller.ConnectionStatus));
                    }

                    if (usbDisconnected)
                    {
                        Debug.WriteLine($"?? USB kesildi: {controller.DisplayName}");
                        controller.UsbDevice?.CloseDevice();
                        controller.UsbDevice = null;
                        controller.OnPropertyChanged(nameof(controller.ConnectionStatus));
                    }

                    if (controller.BluetoothDevice == null && controller.UsbDevice == null)
                    {
                        Debug.WriteLine($"?? {controller.DisplayName} tamamen kesildi");
                        toRemove.Add(kvp.Key);
                        ControllerDisconnected?.Invoke(this, new ControllerEventArgs(controller));
                    }
                }

                foreach (var key in toRemove)
                    _controllers.Remove(key);
            }
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                foreach (var controller in _controllers.Values)
                {
                    controller.BluetoothDevice?.CloseDevice();
                    controller.BluetoothDevice?.Dispose();
                    controller.UsbDevice?.CloseDevice();
                    controller.UsbDevice?.Dispose();
                }
                _controllers.Clear();
            }
        }
    }

    public class ControllerEventArgs : EventArgs
    {
        public DS4Controller Controller { get; }
        public ControllerEventArgs(DS4Controller controller) { Controller = controller; }
    }
}
