using System;
using System.Collections.Generic;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Tek bir controller için tüm servisleri yöneten helper sýnýf
    /// Dictionary karmaþasýný azaltýr
    /// </summary>
    public class ControllerServiceManager : IDisposable
    {
        public string ControllerId { get; }
        public DS4ConnectionService ConnectionService { get; }
        public DS4LedService LedService { get; }
        public DS4BatteryService BatteryService { get; }
        public DS4BatteryWarningService BatteryWarning { get; }
        public DS4LedEffectService EffectService { get; }

        public ControllerServiceManager(
            string controllerId,
            DS4ConnectionService connectionService,
            DS4LedService ledService,
            DS4BatteryService batteryService,
            DS4BatteryWarningService batteryWarning,
            DS4LedEffectService effectService)
        {
            ControllerId = controllerId;
            ConnectionService = connectionService;
            LedService = ledService;
            BatteryService = batteryService;
            BatteryWarning = batteryWarning;
            EffectService = effectService;
        }

        public void Dispose()
        {
            EffectService?.Dispose();
            BatteryWarning?.Dispose();
            LedService?.TurnOffLed();
            ConnectionService?.Dispose();
        }
    }

    /// <summary>
    /// Tüm controller servislerini yöneten yönetici
    /// MainWindow'daki 5 Dictionary yerine 1 tane
    /// </summary>
    public class ControllerServicesCollection
    {
        private readonly Dictionary<string, ControllerServiceManager> _services = new();

        public void Add(ControllerServiceManager manager)
        {
            _services[manager.ControllerId] = manager;
        }

        public bool Remove(string controllerId)
        {
            if (_services.TryGetValue(controllerId, out var manager))
            {
                manager.Dispose();
                _services.Remove(controllerId);
                return true;
            }
            return false;
        }

        public bool TryGet(string controllerId, out ControllerServiceManager manager)
        {
            return _services.TryGetValue(controllerId, out manager);
        }

        public IEnumerable<ControllerServiceManager> GetAll() => _services.Values;

        public void Clear()
        {
            foreach (var manager in _services.Values)
                manager.Dispose();
            _services.Clear();
        }
    }
}
