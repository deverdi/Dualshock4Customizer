using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// DS4 LED kontrolünü yöneten servis (HidLibrary) - PS4 Pulse + Kalp Ritmi
    /// </summary>
    public class DS4LedService
    {
        private readonly DS4ConnectionService _connectionService;
        private readonly object _ledLock = new object();
        
        private CancellationTokenSource _heartbeatCts;
        private bool _isHeartbeatActive;

        public DS4LedService(DS4ConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        }

        /// <summary>
        /// LED rengini ayarlar (PS4 Pulse + Kalp Ritmi)
        /// </summary>
        public bool SetLedColor(byte r, byte g, byte b, byte rumble = 0, bool flash = false)
        {
            lock (_ledLock)
            {
                if (!_connectionService.CheckConnection())
                {
                    Debug.WriteLine("? Cihaz baðlý deðil!");
                    return false;
                }

                // Kalp ritmi kontrolü
                if (rumble > 0 && !_isHeartbeatActive)
                {
                    StartHeartbeat(r, g, b, flash);
                    return true;
                }
                else if (rumble == 0 && _isHeartbeatActive)
                {
                    StopHeartbeat();
                }

                // Normal LED ayarý
                return SendLedCommand(r, g, b, 0, flash);
            }
        }

        /// <summary>
        /// Kalp ritmi baþlat (1.2 saniyede bir minimal nabýz)
        /// </summary>
        private void StartHeartbeat(byte r, byte g, byte b, bool flash)
        {
            _isHeartbeatActive = true;
            _heartbeatCts = new CancellationTokenSource();
            var token = _heartbeatCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // NABIZ (minimal güç)
                        SendLedCommand(r, g, b, 0x08, flash);
                        await Task.Delay(120, token);
                        
                        // DURAKLAMA
                        SendLedCommand(r, g, b, 0x00, flash);
                        await Task.Delay(1080, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
                
                _isHeartbeatActive = false;
            }, token);

            Debug.WriteLine("?? Kalp ritmi baþlatýldý (1.2s, 0x08 güç)");
        }

        /// <summary>
        /// Kalp ritmini durdur
        /// </summary>
        private void StopHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _isHeartbeatActive = false;
            Debug.WriteLine("?? Kalp ritmi durduruldu");
        }

        /// <summary>
        /// LED komutunu gönder (HidLibrary)
        /// </summary>
        private bool SendLedCommand(byte r, byte g, byte b, byte rumble, bool flash)
        {
            var device = _connectionService.Device;
            if (device == null || !device.IsConnected)
            {
                return false;
            }

            try
            {
                byte[] report;
                bool isBluetooth = _connectionService.IsBluetooth;

                if (isBluetooth)
                {
                    report = CreateBluetoothReport(r, g, b, rumble, flash);
                }
                else
                {
                    report = CreateUsbReport(r, g, b, rumble, flash);
                }

                // HidLibrary Write metodu
                bool success = device.Write(report);
                
                if (!success)
                {
                    Debug.WriteLine("? Write baþarýsýz, WriteFeatureData deneniyor...");
                    success = device.WriteFeatureData(report);
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? LED yazma hatasý: {ex}");
                return false;
            }
        }

        /// <summary>
        /// LED'i kapatýr
        /// </summary>
        public void TurnOffLed()
        {
            StopHeartbeat();
            SendLedCommand(0, 0, 0, 0, false);
        }

        private byte[] CreateBluetoothReport(byte r, byte g, byte b, byte rumble, bool flash)
        {
            byte[] report = new byte[78];
            report[0] = 0x11;
            report[1] = 0xC0;
            report[2] = 0x20;
            report[3] = 0xF3;
            report[4] = 0x04;

            // Minimal titreþim
            report[6] = 0x00;
            report[7] = rumble;

            // RGB
            report[8] = r;
            report[9] = g;
            report[10] = b;

            // PS4 Pulse
            if (flash)
            {
                report[11] = 0xFF;
                report[12] = 0xFF;
            }

            // CRC32
            uint crc = CalculateCRC32(report, 0, 74);
            report[74] = (byte)(crc & 0xFF);
            report[75] = (byte)((crc >> 8) & 0xFF);
            report[76] = (byte)((crc >> 16) & 0xFF);
            report[77] = (byte)((crc >> 24) & 0xFF);

            return report;
        }

        private byte[] CreateUsbReport(byte r, byte g, byte b, byte rumble, bool flash)
        {
            byte[] report = new byte[32];
            report[0] = 0x05;
            report[1] = 0xFF;

            // Minimal titreþim
            report[4] = rumble;
            report[5] = 0x00;

            // RGB
            report[6] = r;
            report[7] = g;
            report[8] = b;

            // PS4 Pulse
            if (flash)
            {
                report[9] = 0xFF;
                report[10] = 0xFF;
            }

            return report;
        }

        private uint CalculateCRC32(byte[] data, int offset, int length)
        {
            uint[] table = new uint[256];
            uint crc = 0xFFFFFFFF;

            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                        c = 0xEDB88320 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                table[i] = c;
            }

            for (int i = offset; i < offset + length; i++)
            {
                crc = table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }

            return crc;
        }
    }
}
