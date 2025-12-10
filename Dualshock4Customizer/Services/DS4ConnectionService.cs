using System;
using System.Linq;
using System.Diagnostics;
using HidLibrary;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// DS4 cihaz bağlantısını yöneten servis (HidLibrary)
    /// </summary>
    public class DS4ConnectionService : IDisposable
    {
        private const int VendorId = 0x054C;
        private const int ProductIdDS4USB = 0x05C4;
        private const int ProductIdDS4Bluetooth = 0x09CC;

        private IHidDevice _ds4Device;
        private bool _isBluetooth;
        private readonly object _deviceLock = new object();

        public bool IsConnected => _ds4Device != null && _ds4Device.IsConnected;
        public bool IsBluetooth => _isBluetooth;
        public IHidDevice Device => _ds4Device;

        // Events
        public event EventHandler DeviceConnected;
        public event EventHandler DeviceDisconnected;

        /// <summary>
        /// DS4 cihazını bulur ve bağlantı kurar
        /// </summary>
        public bool Connect()
        {
            try
            {
                lock (_deviceLock)
                {
                    // HidLibrary ile cihaz arama
                    var devices = HidDevices.Enumerate(VendorId, ProductIdDS4USB, ProductIdDS4Bluetooth).ToArray();

                    _ds4Device = devices.FirstOrDefault();

                    if (_ds4Device != null)
                    {
                        _ds4Device.OpenDevice();
                        
                        // Bluetooth kontrolü (InputReportByteLength ile)
                        _isBluetooth = _ds4Device.Capabilities.InputReportByteLength == 547;

                        Debug.WriteLine($"✓ DS4 Bağlandı: {(_isBluetooth ? "Bluetooth" : "USB")}");
                        Debug.WriteLine($"✓ Product ID: 0x{_ds4Device.Attributes.ProductId:X4}");
                        Debug.WriteLine($"✓ Input Report Length: {_ds4Device.Capabilities.InputReportByteLength}");
                        Debug.WriteLine($"✓ Output Report Length: {_ds4Device.Capabilities.OutputReportByteLength}");

                        DeviceConnected?.Invoke(this, EventArgs.Empty);
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Bağlantı hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bağlantı durumunu kontrol eder
        /// </summary>
        public bool CheckConnection()
        {
            lock (_deviceLock)
            {
                if (!IsConnected)
                {
                    DeviceDisconnected?.Invoke(this, EventArgs.Empty);
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Bağlantıyı kapatır
        /// </summary>
        public void Disconnect()
        {
            lock (_deviceLock)
            {
                _ds4Device?.CloseDevice();
                _ds4Device?.Dispose();
                _ds4Device = null;
            }
        }
        /// <summary>
        /// Mevcut bir cihaza bağlan (çoklu kontrolcü için)
        /// </summary>
        public void ConnectToExisting(IHidDevice device, bool isBluetooth)
        {
            lock (_deviceLock)
            {
                _ds4Device = device;
                _isBluetooth = isBluetooth;
                DeviceConnected?.Invoke(this, EventArgs.Empty);
            }
        }
        public void Dispose()
        {
            Disconnect();
        }
    }
}
