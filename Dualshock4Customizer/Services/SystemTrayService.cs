using System;
using System.Windows;
using System.Diagnostics;
using Dualshock4Customizer.Models;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Simplified system tray service without Windows.Forms dependency
    /// </summary>
    public class SystemTrayService : IDisposable
    {
        private Window _mainWindow;

        public bool IsMinimizedToTray { get; private set; } = false;

        public SystemTrayService(Window mainWindow, Action<DS4Profile> onProfileSelected = null)
        {
            _mainWindow = mainWindow;
            Debug.WriteLine("System tray service initialized (simplified mode)");
        }

        public void UpdateContextMenu(DS4Profile[] profiles = null)
        {
            // Simplified version - no context menu
        }

        public void MinimizeToTray()
        {
            try
            {
                _mainWindow.WindowState = WindowState.Minimized;
                IsMinimizedToTray = true;
                Debug.WriteLine("Window minimized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MinimizeToTray error: {ex.Message}");
            }
        }

        public void ShowMainWindow()
        {
            try
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                IsMinimizedToTray = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowMainWindow error: {ex.Message}");
            }
        }

        public void ShowBalloonTip(string title, string message, int timeout = 3000)
        {
            // Simplified - just log to debug
            Debug.WriteLine($"Notification: {title} - {message}");
        }

        public void UpdateTooltip(string text)
        {
            // Simplified - no action
        }

        public void NotifyControllerStatus(string controllerName, int batteryPercent, bool isCharging)
        {
            string chargeStatus = isCharging ? "Charging" : "Battery";
            Debug.WriteLine($"Controller Status: {controllerName} - {chargeStatus}: {batteryPercent}%");
        }

        public event EventHandler<QuickActionEventArgs> OnQuickAction;

        public void Dispose()
        {
            // Nothing to dispose in simplified mode
        }
    }

    public enum QuickActionType
    {
        TurnOffAllLeds,
        RainbowEffect,
        ApplyDefaultProfile
    }

    public class QuickActionEventArgs : EventArgs
    {
        public QuickActionType ActionType { get; }

        public QuickActionEventArgs(QuickActionType actionType)
        {
            ActionType = actionType;
        }
    }
}
