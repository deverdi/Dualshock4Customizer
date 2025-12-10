using System;
using System.Windows;
using System.Diagnostics;
using Dualshock4Customizer.Models;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Basit sistem tepsisi yönetim servisi (sadece minimize/restore)
    /// </summary>
    public class SystemTrayService : IDisposable
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private Window _mainWindow;

        public bool IsMinimizedToTray { get; private set; } = false;

        public SystemTrayService(Window mainWindow, Action<DS4Profile> onProfileSelected = null)
        {
            _mainWindow = mainWindow;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = false,
                    Text = "DS4 Customizer"
                };

                _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
                Debug.WriteLine("Sistem tepsisi basariyla baslatildi");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sistem tepsisi hatasi: {ex.Message}");
            }
        }

        public void UpdateContextMenu(DS4Profile[] profiles = null)
        {
            // Basit versiyon - context menu yok
        }

        public void MinimizeToTray()
        {
            try
            {
                _mainWindow.Hide();
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = true;
                }
                IsMinimizedToTray = true;
                ShowBalloonTip("DS4 Customizer", "Uygulama sistem tepsisinde calismaya devam ediyor.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MinimizeToTray hatasi: {ex.Message}");
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
                Debug.WriteLine($"ShowMainWindow hatasi: {ex.Message}");
            }
        }

        public void ShowBalloonTip(string title, string message, System.Windows.Forms.ToolTipIcon icon = System.Windows.Forms.ToolTipIcon.Info, int timeout = 3000)
        {
            try
            {
                if (_notifyIcon != null && _notifyIcon.Visible)
                {
                    _notifyIcon.ShowBalloonTip(timeout, title, message, icon);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowBalloonTip hatasi: {ex.Message}");
            }
        }

        public void UpdateTooltip(string text)
        {
            try
            {
                if (_notifyIcon != null && text.Length <= 63)
                {
                    _notifyIcon.Text = text;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateTooltip hatasi: {ex.Message}");
            }
        }

        public void NotifyControllerStatus(string controllerName, int batteryPercent, bool isCharging)
        {
            string chargeStatus = isCharging ? "Sarj oluyor" : "Batarya";
            string message = $"{controllerName}\n{chargeStatus}: %{batteryPercent}";
            ShowBalloonTip("Kontrolcu Durumu", message);
        }

        public event EventHandler<QuickActionEventArgs> OnQuickAction;

        public void Dispose()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SystemTrayService.Dispose hatasi: {ex.Message}");
            }
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
