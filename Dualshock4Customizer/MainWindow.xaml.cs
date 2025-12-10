using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dualshock4Customizer.Services;
using Dualshock4Customizer.Models;
using Dualshock4Customizer.Windows;

namespace Dualshock4Customizer
{
    public partial class MainWindow : Window
    {
        private DS4ControllerManager _manager;
        private ProfileManager _profileManager;
        private Dictionary<string, DS4LedService> _ledServices = new Dictionary<string, DS4LedService>();
        private Dictionary<string, DS4BatteryService> _batteryServices = new Dictionary<string, DS4BatteryService>();
        private Dictionary<string, DS4ConnectionService> _connectionWrappers = new Dictionary<string, DS4ConnectionService>();
        private Dictionary<string, DS4BatteryWarningService> _batteryWarnings = new Dictionary<string, DS4BatteryWarningService>();
        private Dictionary<string, DS4LedEffectService> _effectServices = new Dictionary<string, DS4LedEffectService>();
        
        private SystemTrayService _systemTray;
        private GameDetectionService _gameDetection;
        private DispatcherTimer _timer;
        
        private ObservableCollection<DS4Controller> _controllerList = new ObservableCollection<DS4Controller>();
        private ObservableCollection<DS4Profile> _profileList = new ObservableCollection<DS4Profile>();
        private DS4Controller _selectedController;

        public MainWindow()
        {
            InitializeComponent();
            ControllerListBox.ItemsSource = _controllerList;
            ProfileListBox.ItemsSource = _profileList;
            
            _profileManager = new ProfileManager();
            _profileManager.CreateDefaultProfiles();
            _profileManager.ProfilesChanged += (s, e) => LoadProfileList();
            LoadProfileList();
            
            InitializeServices();
            InitializeManager();
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;
        }

        private void InitializeServices()
        {
            _systemTray = new SystemTrayService(this, OnTrayProfileSelected);
            _systemTray.OnQuickAction += OnSystemTrayQuickAction;
            _systemTray.UpdateContextMenu(_profileManager.Profiles.ToArray());
            _gameDetection = new GameDetectionService();
            _gameDetection.GameDetected += OnGameDetected;
        }

        private void OnTrayProfileSelected(DS4Profile profile)
        {
            if (_controllerList.Count == 0) return;
            Dispatcher.Invoke(() =>
            {
                foreach (var controller in _controllerList)
                {
                    profile.ApplyToController(controller);
                    ApplyProfileWithEffects(controller, profile);
                }
                _systemTray.ShowBalloonTip("Profil Yuklendi", $"'{profile.ProfileName}' uygulandi!", System.Windows.Forms.ToolTipIcon.Info);
            });
        }

        private void OnSystemTrayQuickAction(object sender, QuickActionEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.ActionType)
                {
                    case QuickActionType.TurnOffAllLeds: TurnOffAllLeds(); break;
                    case QuickActionType.RainbowEffect: ApplyRainbowToAll(); break;
                }
            });
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && MinimizeToTrayCheckbox != null && MinimizeToTrayCheckbox.IsChecked == true)
                _systemTray.MinimizeToTray();
        }

        private void OnGameDetected(object sender, GameDetectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var profile = _profileManager.GetProfileForGame(e.ProcessName);
                if (profile != null && _controllerList.Count > 0)
                {
                    foreach (var controller in _controllerList)
                    {
                        profile.ApplyToController(controller);
                        ApplyProfileWithEffects(controller, profile);
                    }
                    _systemTray.ShowBalloonTip("Oyun Algilandi", $"{e.ProcessName} - '{profile.ProfileName}'", System.Windows.Forms.ToolTipIcon.Info);
                }
            });
        }

        private void LoadProfileList(string searchText = null)
        {
            _profileList.Clear();
            IEnumerable<DS4Profile> profiles = string.IsNullOrWhiteSpace(searchText) 
                ? _profileManager.GetSortedProfiles(ProfileSortType.Favorites) 
                : _profileManager.SearchProfiles(searchText);
            foreach (var profile in profiles) _profileList.Add(profile);
            _systemTray?.UpdateContextMenu(_profileManager.Profiles.ToArray());
        }

        private void InitializeManager()
        {
            _manager = new DS4ControllerManager();
            _manager.ControllerConnected += OnControllerConnected;
            _manager.ControllerDisconnected += OnControllerDisconnected;
            int count = _manager.ScanAndConnect();
            if (count > 0)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
            else
            {
                SelectedControllerTitle.Text = "Kontrolcu bulunamadi";
                SelectedControllerStatus.Text = "DS4Windows kapali olmali!";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _manager.ScanAndConnect();
            _manager.CleanupDisconnected();
            ReadAllBatteries();
            CheckBatteryWarnings();
        }

        private void CheckBatteryWarnings() { foreach (var kvp in _batteryWarnings) kvp.Value.CheckBatteryLevel(); }

        private void OnControllerConnected(object sender, ControllerEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var controller = e.Controller;
                var connectionWrapper = new DS4ConnectionService();
                connectionWrapper.ConnectToExisting(controller.Device, controller.IsBluetooth);
                var ledService = new DS4LedService(connectionWrapper);
                var batteryService = new DS4BatteryService(connectionWrapper);
                var batteryWarning = new DS4BatteryWarningService(controller, ledService, 20);
                var effectService = new DS4LedEffectService(controller, ledService);
                batteryWarning.EnableVibrationAlert = false;
                batteryWarning.EnableAutoColorChange = true;
                batteryService.BatteryStatusChanged += (s, args) => { controller.BatteryPercent = args.Status.Percent; controller.IsCharging = args.Status.IsCharging; };
                _connectionWrappers[controller.Id] = connectionWrapper;
                _ledServices[controller.Id] = ledService;
                _batteryServices[controller.Id] = batteryService;
                _batteryWarnings[controller.Id] = batteryWarning;
                _effectServices[controller.Id] = effectService;
                _controllerList.Add(controller);
                AutoLoadProfile(controller);
                _systemTray.ShowBalloonTip("Kontrolcu Baglandi", controller.DisplayName, System.Windows.Forms.ToolTipIcon.Info);
            });
        }

        private void AutoLoadProfile(DS4Controller controller)
        {
            var profile = _profileManager.GetProfileByMacAddress(controller.Id);
            if (profile != null)
            {
                profile.ApplyToController(controller);
                ApplyProfileWithEffects(controller, profile);
                _profileManager.SaveProfiles();
                controller.ActiveProfileName = profile.ProfileName;
            }
            else ApplyColorToController(controller);
        }

        private void ApplyProfileWithEffects(DS4Controller controller, DS4Profile profile)
        {
            ApplyColorToController(controller);
            if (_effectServices.TryGetValue(controller.Id, out var effectService))
            {
                // Efekt hizini ayarla
                effectService.EffectSpeed = profile.EffectSpeed;
                
                if (profile.EffectType == LedEffectType.Breathing)
                    effectService.StartBreathingWithColor(profile.LedR, profile.LedG, profile.LedB);
                else if (profile.EffectType != LedEffectType.None)
                    effectService.StartEffect(profile.EffectType);
                else 
                    effectService.StopEffect();
            }
        }

        private void OnControllerDisconnected(object sender, ControllerEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_effectServices.TryGetValue(e.Controller.Id, out var effectService)) { effectService.Dispose(); _effectServices.Remove(e.Controller.Id); }
                _connectionWrappers.Remove(e.Controller.Id);
                _ledServices.Remove(e.Controller.Id);
                _batteryServices.Remove(e.Controller.Id);
                _batteryWarnings.Remove(e.Controller.Id);
                _controllerList.Remove(e.Controller);
                if (_selectedController == e.Controller) { _selectedController = null; SettingsPanel.IsEnabled = false; SelectedControllerTitle.Text = "Kontrolcu kesildi"; }
            });
        }

        private void ReadAllBatteries() { foreach (var kvp in _batteryServices.ToList()) try { kvp.Value.ReadBatteryStatus(); } catch { } }

        private void ControllerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedController = ControllerListBox.SelectedItem as DS4Controller;
            if (_selectedController == null) { SettingsPanel.IsEnabled = false; SelectedControllerTitle.Text = "Kontrolcu secin"; SelectedControllerStatus.Text = ""; return; }
            SettingsPanel.IsEnabled = true;
            SelectedControllerTitle.Text = _selectedController.DisplayName;
            SelectedControllerStatus.Text = $"Pil: {_selectedController.BatteryPercent}%\n{(_selectedController.IsBluetooth ? "Bluetooth" : "USB")}";
            LoadControllerSettings(_selectedController);
        }

        private void LoadControllerSettings(DS4Controller controller)
        {
            string colorTag = controller.SelectedColor;
            if (colorTag == "OzelRenk")
                UpdateCustomColorDisplay(controller.LedR, controller.LedG, controller.LedB);
            else
            {
                ResetCustomColorDisplay();
                foreach (ComboBoxItem item in ColorSelector.Items) 
                    if (item.Tag.ToString() == colorTag) { ColorSelector.SelectedItem = item; break; }
            }
            
            if (_batteryWarnings.TryGetValue(controller.Id, out var warningService)) 
            { 
                VibrationCheckbox.IsChecked = warningService.EnableVibrationAlert; 
                FlashCheckbox.IsChecked = warningService.EnableAutoColorChange; 
            }
            else { VibrationCheckbox.IsChecked = false; FlashCheckbox.IsChecked = true; }
            UpdateNotificationText();
        }

        private void UpdateCustomColorDisplay(byte r, byte g, byte b)
        {
            var lastItem = ColorSelector.Items[ColorSelector.Items.Count - 1] as ComboBoxItem;
            if (lastItem != null && lastItem.Tag.ToString() == "OzelRenk")
            {
                lastItem.Content = $"?? Ozel Renk ({r}, {g}, {b})";
                ColorSelector.SelectedItem = lastItem;
            }
        }

        private void ResetCustomColorDisplay()
        {
            var lastItem = ColorSelector.Items[ColorSelector.Items.Count - 1] as ComboBoxItem;
            if (lastItem != null && lastItem.Tag.ToString() == "OzelRenk")
                lastItem.Content = "Ozel Renk Sec...";
        }

        private void ColorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedController == null) return;
            ComboBoxItem selectedItem = ColorSelector.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;
            string colorName = selectedItem.Tag.ToString();
            _selectedController.SelectedColor = colorName;
            
            if (colorName != "Rainbow" && _effectServices.TryGetValue(_selectedController.Id, out var effectService))
                if (effectService.CurrentEffect == LedEffectType.Rainbow) effectService.StopEffect();
            
            if (colorName != "OzelRenk") ResetCustomColorDisplay();
            
            switch (colorName)
            {
                case "Kirmizi": _selectedController.LedR = 255; _selectedController.LedG = 0; _selectedController.LedB = 0; ApplyColorToController(_selectedController); break;
                case "Yesil": _selectedController.LedR = 0; _selectedController.LedG = 255; _selectedController.LedB = 0; ApplyColorToController(_selectedController); break;
                case "Mavi": _selectedController.LedR = 0; _selectedController.LedG = 0; _selectedController.LedB = 255; ApplyColorToController(_selectedController); break;
                case "Turuncu": _selectedController.LedR = 255; _selectedController.LedG = 165; _selectedController.LedB = 0; ApplyColorToController(_selectedController); break;
                case "Beyaz": _selectedController.LedR = 255; _selectedController.LedG = 255; _selectedController.LedB = 255; ApplyColorToController(_selectedController); break;
                case "Kapali": _selectedController.LedR = 0; _selectedController.LedG = 0; _selectedController.LedB = 0; ApplyColorToController(_selectedController); break;
                case "Rainbow": if (_effectServices.TryGetValue(_selectedController.Id, out var rs)) rs.StartEffect(LedEffectType.Rainbow); break;
                case "OzelRenk": OpenColorPicker(); return;
            }
        }

        private void OpenColorPicker()
        {
            if (_selectedController == null) return;
            var colorPicker = new ColorPickerWindow(_selectedController.LedR, _selectedController.LedG, _selectedController.LedB);
            if (colorPicker.ShowDialog() == true)
            {
                _selectedController.LedR = colorPicker.SelectedR;
                _selectedController.LedG = colorPicker.SelectedG;
                _selectedController.LedB = colorPicker.SelectedB;
                _selectedController.SelectedColor = "OzelRenk";
                UpdateCustomColorDisplay(colorPicker.SelectedR, colorPicker.SelectedG, colorPicker.SelectedB);
                ApplyColorToController(_selectedController);
            }
        }

        private void NotificationCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectedController == null) return;
            if (_batteryWarnings.TryGetValue(_selectedController.Id, out var warningService))
            {
                warningService.EnableVibrationAlert = VibrationCheckbox.IsChecked ?? false;
                warningService.EnableAutoColorChange = FlashCheckbox.IsChecked ?? false;
            }
            UpdateNotificationText();
        }

        private void ApplyColorToController(DS4Controller controller)
        {
            if (!_ledServices.TryGetValue(controller.Id, out var ledService)) return;
            try { ledService.SetLedColor(controller.LedR, controller.LedG, controller.LedB, 0x00, false); } catch { }
        }

        private void RgbColorPickerButton_Click(object sender, RoutedEventArgs e) { OpenColorPicker(); }

        private void EffectBreathingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedController == null) return;
            if (_effectServices.TryGetValue(_selectedController.Id, out var effectService))
            {
                byte r = _selectedController.LedR, g = _selectedController.LedG, b = _selectedController.LedB;
                if (r == 0 && g == 0 && b == 0) { r = 0; g = 0; b = 255; }
                effectService.StartBreathingWithColor(r, g, b);
            }
        }

        private void EffectStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedController == null) return;
            if (_effectServices.TryGetValue(_selectedController.Id, out var effectService))
            {
                effectService.StopEffect();
                ApplyColorToController(_selectedController);
            }
        }

        private void ProfileSearchBox_TextChanged(object sender, TextChangedEventArgs e) { LoadProfileList(ProfileSearchBox.Text); }

        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ProfileEditorWindow();
            editor.Owner = this;
            SetupLivePreviewCallbacks(editor);
            
            if (editor.ShowDialog() == true && editor.IsSaved)
            {
                _profileManager.AddProfile(editor.Profile);
                LoadProfileList();
                ApplyProfileToMatchingControllers(editor.Profile);
                MessageBox.Show($"'{editor.Profile.ProfileName}' olusturuldu!", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfile = ProfileListBox.SelectedItem as DS4Profile;
            if (selectedProfile == null) { MessageBox.Show("Profil secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            
            var editor = new ProfileEditorWindow(selectedProfile);
            editor.Owner = this;
            SetupLivePreviewCallbacks(editor);
            
            if (editor.ShowDialog() == true && editor.IsSaved)
            {
                _profileManager.UpdateProfile(editor.Profile);
                LoadProfileList();
                ApplyProfileToMatchingControllers(editor.Profile);
                MessageBox.Show($"'{editor.Profile.ProfileName}' guncellendi!", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ===== CANLI ONIZLEME CALLBACK'LERINI AYARLA =====
        private void SetupLivePreviewCallbacks(ProfileEditorWindow editor)
        {
            var targetController = _selectedController ?? _controllerList.FirstOrDefault();
            if (targetController == null) return;
            
            // Renk callback
            Action<byte, byte, byte> colorCallback = (r, g, b) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_ledServices.TryGetValue(targetController.Id, out var ledService))
                        try { ledService.SetLedColor(r, g, b, 0x00, false); } catch { }
                });
            };
            
            // Efekt callback - hiz ile birlikte
            Action<LedEffectType> effectCallback = (effectType) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_effectServices.TryGetValue(targetController.Id, out var effectService))
                    {
                        // Editor'daki profil uzerinden hizi al
                        int speed = editor.Profile?.EffectSpeed ?? 50;
                        if (speed < 10) speed = 50;
                        effectService.EffectSpeed = speed;
                        effectService.StartEffect(effectType);
                    }
                });
            };
            
            // Stop callback
            Action stopEffectCallback = () =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_effectServices.TryGetValue(targetController.Id, out var effectService))
                        effectService.StopEffect();
                });
            };
            
            editor.SetLivePreviewCallbacks(colorCallback, effectCallback, stopEffectCallback);
        }

        private void ApplyProfileToMatchingControllers(DS4Profile profile)
        {
            int appliedCount = 0;
            if (!string.IsNullOrWhiteSpace(profile.MacAddress))
            {
                var matchingController = _controllerList.FirstOrDefault(c => c.Id.Equals(profile.MacAddress, StringComparison.OrdinalIgnoreCase));
                if (matchingController != null)
                {
                    profile.ApplyToController(matchingController);
                    ApplyProfileWithEffects(matchingController, profile);
                    matchingController.ActiveProfileName = profile.ProfileName;
                    if (matchingController == _selectedController) LoadControllerSettings(matchingController);
                    appliedCount++;
                }
            }
            if (_selectedController != null && appliedCount == 0)
            {
                var selectedProfileInList = ProfileListBox.SelectedItem as DS4Profile;
                if (selectedProfileInList != null && selectedProfileInList.ProfileName == profile.ProfileName)
                {
                    profile.ApplyToController(_selectedController);
                    ApplyProfileWithEffects(_selectedController, profile);
                    _selectedController.ActiveProfileName = profile.ProfileName;
                    LoadControllerSettings(_selectedController);
                }
            }
        }

        private void DuplicateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfile = ProfileListBox.SelectedItem as DS4Profile;
            if (selectedProfile == null) { MessageBox.Show("Profil secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var copy = _profileManager.DuplicateProfile(selectedProfile.ProfileName);
            if (copy != null) { LoadProfileList(); MessageBox.Show($"'{copy.ProfileName}' olusturuldu!", "Kopyalandi", MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private void ToggleFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfile = ProfileListBox.SelectedItem as DS4Profile;
            if (selectedProfile == null) { MessageBox.Show("Profil secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _profileManager.ToggleFavorite(selectedProfile.ProfileName);
            LoadProfileList();
            string msg = selectedProfile.IsFavorite ? "favorilerden cikarildi" : "favorilere eklendi";
            MessageBox.Show($"'{selectedProfile.ProfileName}' {msg}!", "Favori", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedController == null) { MessageBox.Show("Kontrolcu secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var selectedProfile = ProfileListBox.SelectedItem as DS4Profile;
            if (selectedProfile == null) { MessageBox.Show("Profil secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            selectedProfile.ApplyToController(_selectedController);
            LoadControllerSettings(_selectedController);
            ApplyProfileWithEffects(_selectedController, selectedProfile);
            _selectedController.ActiveProfileName = selectedProfile.ProfileName;
            _profileManager.SaveProfiles();
            string effectMsg = selectedProfile.EffectType != LedEffectType.None ? $" (Efekt: {selectedProfile.EffectType}, Hiz: {selectedProfile.EffectSpeed})" : "";
            MessageBox.Show($"'{selectedProfile.ProfileName}' yuklendi!{effectMsg}", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfile = ProfileListBox.SelectedItem as DS4Profile;
            if (selectedProfile == null) { MessageBox.Show("Profil secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var result = MessageBox.Show($"'{selectedProfile.ProfileName}' silinsin mi?", "Profil Sil", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) { _profileManager.DeleteProfile(selectedProfile.ProfileName); LoadProfileList(); }
        }

        private void LinkProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedController == null) { MessageBox.Show("Kontrolcu secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var selectedProfile = ProfileListBox.SelectedItem as DS4Profile;
            if (selectedProfile == null) { MessageBox.Show("Profil secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            selectedProfile.MacAddress = _selectedController.Id;
            _profileManager.SaveProfiles();
            selectedProfile.ApplyToController(_selectedController);
            ApplyProfileWithEffects(_selectedController, selectedProfile);
            _selectedController.ActiveProfileName = selectedProfile.ProfileName;
            LoadControllerSettings(_selectedController);
            MessageBox.Show($"'{selectedProfile.ProfileName}' eslestirildi ve uygulandi!", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfile = ProfileListBox.SelectedItem as DS4Profile;
            if (selectedProfile == null) { MessageBox.Show("Profil secin!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (ProfileImportExportService.ExportProfile(selectedProfile)) MessageBox.Show("Disa aktarildi!", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profile = ProfileImportExportService.ImportProfile(out bool success);
            if (success && profile != null) 
            { 
                _profileManager.UpdateProfile(profile); 
                LoadProfileList(); 
                ApplyProfileToMatchingControllers(profile);
                MessageBox.Show($"'{profile.ProfileName}' ice aktarildi!", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information); 
            }
        }

        private void ToggleGameDetectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gameDetection.IsEnabled) { _gameDetection.Stop(); GameDetectionStatusText.Text = "Oyun algilama: Kapali"; }
            else { _gameDetection.Start(); GameDetectionStatusText.Text = "Oyun algilama: Acik"; }
        }

        private void TurnOffAllLeds() { foreach (var controller in _controllerList) { controller.LedR = 0; controller.LedG = 0; controller.LedB = 0; ApplyColorToController(controller); } }
        private void ApplyRainbowToAll() { foreach (var controller in _controllerList) if (_effectServices.TryGetValue(controller.Id, out var es)) es.StartEffect(LedEffectType.Rainbow); }

        private void UpdateNotificationText()
        {
            if (_selectedController == null) return;
            if (_batteryWarnings.TryGetValue(_selectedController.Id, out var warningService))
            {
                bool vib = warningService.EnableVibrationAlert;
                bool flash = warningService.EnableAutoColorChange;
                NotificationStatusText.Text = $"{(vib ? "Titresim aktif" : "Titresim kapali")}\n{(flash ? "Renk uyarisi aktif" : "Renk uyarisi kapali")}";
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _timer?.Stop();
            _gameDetection?.Dispose();
            foreach (var effectService in _effectServices.Values) try { effectService?.Dispose(); } catch { }
            foreach (var ledService in _ledServices.Values) try { ledService.TurnOffLed(); } catch { }
            System.Threading.Thread.Sleep(100);
            _manager?.Dispose();
            foreach (var wrapper in _connectionWrappers.Values) wrapper?.Dispose();
            _systemTray?.Dispose();
        }
    }

    public class ProfileNameDialog : Window
    {
        public string ProfileName { get; private set; }
        private TextBox _nameTextBox;
        public ProfileNameDialog()
        {
            Title = "Profil Adi"; Width = 350; Height = 150; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "Profil Adi:", Margin = new Thickness(0, 0, 0, 5) });
            _nameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 15) };
            panel.Children.Add(_nameTextBox);
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okBtn = new Button { Content = "Tamam", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
            var cancelBtn = new Button { Content = "Iptal", Width = 75 };
            okBtn.Click += (s, e) => { ProfileName = _nameTextBox.Text.Trim(); DialogResult = true; };
            cancelBtn.Click += (s, e) => { DialogResult = false; };
            btnPanel.Children.Add(okBtn); btnPanel.Children.Add(cancelBtn);
            panel.Children.Add(btnPanel);
            Content = panel; _nameTextBox.Focus();
        }
    }
}
