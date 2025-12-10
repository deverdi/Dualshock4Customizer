using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dualshock4Customizer.Models;
using Dualshock4Customizer.Services;

namespace Dualshock4Customizer.Windows
{
    public partial class ProfileEditorWindow : Window
    {
        public DS4Profile Profile { get; private set; }
        public bool IsSaved { get; private set; } = false;
        
        private bool _isNewProfile;
        private bool _isLoading = true;
        
        private Action<byte, byte, byte> _livePreviewCallback;
        private Action<LedEffectType> _effectPreviewCallback;
        private Action _stopEffectCallback;

        public ProfileEditorWindow(DS4Profile profile = null)
        {
            _isNewProfile = profile == null;
            Profile = profile ?? new DS4Profile();
            
            InitializeUI();
            LoadProfileData();
            _isLoading = false;
            UpdateControlStates();
        }

        public void SetLivePreviewCallbacks(
            Action<byte, byte, byte> colorCallback, 
            Action<LedEffectType> effectCallback,
            Action stopEffectCallback)
        {
            _livePreviewCallback = colorCallback;
            _effectPreviewCallback = effectCallback;
            _stopEffectCallback = stopEffectCallback;
        }

        private void InitializeUI()
        {
            Title = _isNewProfile ? "Yeni Profil Olustur" : "Profil Duzenle";
            Width = 500;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var contentPanel = new StackPanel { Margin = new Thickness(20) };

            // ===== TEMEL BILGILER =====
            AddSectionHeader(contentPanel, "Temel Bilgiler");
            
            AddLabeledControl(contentPanel, "Profil Adi:", _txtProfileName = new TextBox { Margin = new Thickness(0, 0, 0, 10) });
            AddLabeledControl(contentPanel, "Aciklama:", _txtDescription = new TextBox { Margin = new Thickness(0, 0, 0, 10), Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true });
            
            var iconPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            iconPanel.Children.Add(new TextBlock { Text = "Ikon:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
            _cmbIcon = new ComboBox { Width = 80, FontSize = 18 };
            var icons = new[] { "??", "??", "??", "??", "??", "?", "??", "??", "?", "??", "??", "??", "??", "??", "??", "??" };
            foreach (var icon in icons) _cmbIcon.Items.Add(icon);
            iconPanel.Children.Add(_cmbIcon);
            contentPanel.Children.Add(iconPanel);

            var categoryPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            categoryPanel.Children.Add(new TextBlock { Text = "Kategori:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
            _cmbCategory = new ComboBox { Width = 150 };
            _cmbCategory.Items.Add("?? Genel");
            _cmbCategory.Items.Add("?? Oyun");
            _cmbCategory.Items.Add("?? Film");
            _cmbCategory.Items.Add("?? Muzik");
            _cmbCategory.Items.Add("?? Gece");
            _cmbCategory.Items.Add("? Ozel");
            categoryPanel.Children.Add(_cmbCategory);
            contentPanel.Children.Add(categoryPanel);

            _chkFavorite = new CheckBox { Content = "? Favorilere Ekle", Margin = new Thickness(0, 0, 0, 15) };
            contentPanel.Children.Add(_chkFavorite);

            // ===== LED AYARLARI =====
            AddSectionHeader(contentPanel, "LED Ayarlari");

            _colorSectionPanel = new StackPanel();
            
            var colorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            colorPanel.Children.Add(new TextBlock { Text = "Renk:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
            _cmbColor = new ComboBox { Width = 120 };
            _cmbColor.Items.Add("Kirmizi");
            _cmbColor.Items.Add("Yesil");
            _cmbColor.Items.Add("Mavi");
            _cmbColor.Items.Add("Turuncu");
            _cmbColor.Items.Add("Beyaz");
            _cmbColor.Items.Add("Kapali");
            _cmbColor.Items.Add("Ozel Renk");
            _cmbColor.SelectionChanged += ColorSelector_Changed;
            colorPanel.Children.Add(_cmbColor);
            
            _colorPreview = new Border { Width = 25, Height = 25, CornerRadius = new CornerRadius(12), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(8, 0, 0, 0) };
            colorPanel.Children.Add(_colorPreview);
            
            _chkLivePreview = new CheckBox 
            { 
                Content = "?? Canli", 
                IsChecked = true, 
                Margin = new Thickness(15, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Degisiklikler aninda kontrolcuye gonderilir"
            };
            colorPanel.Children.Add(_chkLivePreview);
            
            _colorSectionPanel.Children.Add(colorPanel);

            _rgbSliderPanel = new StackPanel();
            
            AddLabeledSliderToPanel(_rgbSliderPanel, "Kirmizi (R):", _sliderR = new Slider { Minimum = 0, Maximum = 255, Value = 0 });
            AddLabeledSliderToPanel(_rgbSliderPanel, "Yesil (G):", _sliderG = new Slider { Minimum = 0, Maximum = 255, Value = 0 });
            AddLabeledSliderToPanel(_rgbSliderPanel, "Mavi (B):", _sliderB = new Slider { Minimum = 0, Maximum = 255, Value = 255 });
            
            _sliderR.ValueChanged += RgbSlider_Changed;
            _sliderG.ValueChanged += RgbSlider_Changed;
            _sliderB.ValueChanged += RgbSlider_Changed;

            _colorSectionPanel.Children.Add(_rgbSliderPanel);

            AddLabeledSliderToPanel(_colorSectionPanel, "Parlaklik:", _sliderBrightness = new Slider { Minimum = 0, Maximum = 100, Value = 100 });
            _sliderBrightness.ValueChanged += RgbSlider_Changed;

            contentPanel.Children.Add(_colorSectionPanel);

            // ===== EFEKT AYARLARI =====
            AddSectionHeader(contentPanel, "Efekt Ayarlari");

            var effectPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            effectPanel.Children.Add(new TextBlock { Text = "Efekt:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
            _cmbEffect = new ComboBox { Width = 120 };
            _cmbEffect.Items.Add("Yok");
            _cmbEffect.Items.Add("Rainbow");
            _cmbEffect.Items.Add("Breathing");
            _cmbEffect.Items.Add("Pulse");
            _cmbEffect.Items.Add("Strobe");
            _cmbEffect.SelectionChanged += EffectSelector_Changed;
            effectPanel.Children.Add(_cmbEffect);
            
            _lblRainbowWarning = new TextBlock 
            { 
                Text = "?? Renk ayarlari devre disi", 
                FontSize = 10, 
                Foreground = Brushes.OrangeRed,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            effectPanel.Children.Add(_lblRainbowWarning);
            contentPanel.Children.Add(effectPanel);

            // ===== OYUN ESLESTIRME =====
            AddSectionHeader(contentPanel, "Oyun Eslestirme");

            AddLabeledControl(contentPanel, "Oyun Process:", _txtGameProcess = new TextBox { Margin = new Thickness(0, 0, 0, 5) });
            contentPanel.Children.Add(new TextBlock { Text = "Ornek: GTA5.exe, csgo.exe", FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(80, 0, 0, 10) });
            _chkAutoLoad = new CheckBox { Content = "Oyun acildiginda otomatik yukle", Margin = new Thickness(0, 0, 0, 15) };
            contentPanel.Children.Add(_chkAutoLoad);

            // ===== DÜÞÜK PÝL AYARLARI =====
            AddSectionHeader(contentPanel, "Dusuk Pil Uyarilari");

            var thresholdPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            thresholdPanel.Children.Add(new TextBlock { Text = "Esik (%):", Width = 80, VerticalAlignment = VerticalAlignment.Center });
            _sliderThreshold = new Slider { Minimum = 5, Maximum = 95, Value = 50, Width = 150 };
            thresholdPanel.Children.Add(_sliderThreshold);
            _lblThreshold = new TextBlock { Text = "50%", Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            _sliderThreshold.ValueChanged += (s, e) => _lblThreshold.Text = $"{(int)_sliderThreshold.Value}%";
            thresholdPanel.Children.Add(_lblThreshold);
            contentPanel.Children.Add(thresholdPanel);

            _chkVibration = new CheckBox { Content = "? 2 dakikada bir titresim uyarisi", IsChecked = true, Margin = new Thickness(0, 0, 0, 5) };
            _chkColorChange = new CheckBox { Content = "?? Renk degisimi (Kirmizi + Flash)", IsChecked = true, Margin = new Thickness(0, 0, 0, 15) };
            contentPanel.Children.Add(_chkVibration);
            contentPanel.Children.Add(_chkColorChange);

            scrollViewer.Content = contentPanel;
            Grid.SetRow(scrollViewer, 0);
            mainGrid.Children.Add(scrollViewer);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(20) };
            
            var btnCancel = new Button { Content = "Iptal", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0) };
            btnCancel.Click += (s, e) => { IsSaved = false; DialogResult = false; };
            
            var btnSave = new Button { Content = "Kaydet", Width = 80, Height = 30, Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)), Foreground = Brushes.White };
            btnSave.Click += BtnSave_Click;
            
            buttonPanel.Children.Add(btnCancel);
            buttonPanel.Children.Add(btnSave);
            
            Grid.SetRow(buttonPanel, 1);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void UpdateControlStates()
        {
            if (_cmbEffect == null || _cmbColor == null) return;
            
            var effectType = (LedEffectType)_cmbEffect.SelectedIndex;
            string selectedColor = _cmbColor.SelectedItem?.ToString() ?? "";
            bool isRainbow = effectType == LedEffectType.Rainbow;
            bool isCustomColor = selectedColor == "Ozel Renk";
            
            _cmbColor.IsEnabled = !isRainbow;
            _colorPreview.Opacity = isRainbow ? 0.5 : 1.0;
            _lblRainbowWarning.Visibility = isRainbow ? Visibility.Visible : Visibility.Collapsed;
            
            bool slidersEnabled = !isRainbow && isCustomColor;
            _sliderR.IsEnabled = slidersEnabled;
            _sliderG.IsEnabled = slidersEnabled;
            _sliderB.IsEnabled = slidersEnabled;
            _rgbSliderPanel.Opacity = slidersEnabled ? 1.0 : 0.5;
            
            _sliderBrightness.IsEnabled = !isRainbow;
        }

        private void LoadProfileData()
        {
            _txtProfileName.Text = Profile.ProfileName;
            _txtDescription.Text = Profile.Description;
            
            int iconIndex = _cmbIcon.Items.IndexOf(Profile.IconEmoji);
            _cmbIcon.SelectedIndex = iconIndex >= 0 ? iconIndex : 0;
            
            _cmbCategory.SelectedIndex = (int)Profile.Category;
            _chkFavorite.IsChecked = Profile.IsFavorite;
            
            _sliderR.Value = Profile.LedR;
            _sliderG.Value = Profile.LedG;
            _sliderB.Value = Profile.LedB;
            _sliderBrightness.Value = Profile.Brightness;
            
            string colorName = Profile.ColorName;
            if (colorName == "Rainbow") colorName = "Mavi";
            int colorIndex = _cmbColor.Items.IndexOf(colorName);
            _cmbColor.SelectedIndex = colorIndex >= 0 ? colorIndex : 2;
            
            _cmbEffect.SelectedIndex = (int)Profile.EffectType;
            
            _txtGameProcess.Text = Profile.LinkedGameProcess;
            _chkAutoLoad.IsChecked = Profile.AutoLoadForGame;
            
            _sliderThreshold.Value = Profile.LowBatteryThreshold;
            _chkVibration.IsChecked = Profile.VibrationOnLowBattery;
            _chkColorChange.IsChecked = Profile.AutoColorChangeOnLowBattery;
            
            UpdateColorPreview();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtProfileName.Text))
            {
                MessageBox.Show("Profil adi bos olamaz!", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Profile.ProfileName = _txtProfileName.Text.Trim();
            Profile.Description = _txtDescription.Text;
            Profile.IconEmoji = _cmbIcon.SelectedItem?.ToString() ?? "??";
            Profile.Category = (ProfileCategory)_cmbCategory.SelectedIndex;
            Profile.IsFavorite = _chkFavorite.IsChecked ?? false;
            
            Profile.LedR = (byte)_sliderR.Value;
            Profile.LedG = (byte)_sliderG.Value;
            Profile.LedB = (byte)_sliderB.Value;
            Profile.Brightness = (byte)_sliderBrightness.Value;
            
            var effectType = (LedEffectType)_cmbEffect.SelectedIndex;
            Profile.ColorName = effectType == LedEffectType.Rainbow ? "Rainbow" : _cmbColor.SelectedItem?.ToString() ?? "Mavi";
            
            Profile.EffectType = effectType;
            Profile.EffectSpeed = 50;
            
            Profile.LinkedGameProcess = _txtGameProcess.Text.Trim();
            Profile.AutoLoadForGame = _chkAutoLoad.IsChecked ?? false;
            
            Profile.LowBatteryThreshold = (int)_sliderThreshold.Value;
            Profile.VibrationOnLowBattery = _chkVibration.IsChecked ?? false;
            Profile.AutoColorChangeOnLowBattery = _chkColorChange.IsChecked ?? false;

            IsSaved = true;
            DialogResult = true;
        }

        private void ColorSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            
            string color = _cmbColor.SelectedItem?.ToString();
            UpdateControlStates();
            _stopEffectCallback?.Invoke();
            
            switch (color)
            {
                case "Kirmizi": _sliderR.Value = 255; _sliderG.Value = 0; _sliderB.Value = 0; break;
                case "Yesil": _sliderR.Value = 0; _sliderG.Value = 255; _sliderB.Value = 0; break;
                case "Mavi": _sliderR.Value = 0; _sliderG.Value = 0; _sliderB.Value = 255; break;
                case "Turuncu": _sliderR.Value = 255; _sliderG.Value = 165; _sliderB.Value = 0; break;
                case "Beyaz": _sliderR.Value = 255; _sliderG.Value = 255; _sliderB.Value = 255; break;
                case "Kapali": _sliderR.Value = 0; _sliderG.Value = 0; _sliderB.Value = 0; break;
            }
            
            UpdateColorPreview();
            if (_chkLivePreview.IsChecked == true) ApplyLivePreview();
        }

        private void EffectSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            UpdateControlStates();
            if (_chkLivePreview.IsChecked != true) return;
            
            var effectType = (LedEffectType)_cmbEffect.SelectedIndex;
            
            if (effectType == LedEffectType.None)
            {
                _stopEffectCallback?.Invoke();
                ApplyLivePreview();
            }
            else
            {
                _effectPreviewCallback?.Invoke(effectType);
            }
        }

        private void RgbSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateColorPreview();
            
            if (!_isLoading && _chkLivePreview?.IsChecked == true)
            {
                var effectType = (LedEffectType)(_cmbEffect?.SelectedIndex ?? 0);
                if (effectType != LedEffectType.None)
                {
                    _stopEffectCallback?.Invoke();
                    _cmbEffect.SelectedIndex = 0;
                    UpdateControlStates();
                }
                ApplyLivePreview();
            }
        }

        private void ApplyLivePreview()
        {
            if (_livePreviewCallback == null) return;
            if (_sliderR == null || _sliderG == null || _sliderB == null || _sliderBrightness == null) return;
            
            float brightness = (float)_sliderBrightness.Value / 100f;
            byte r = (byte)(_sliderR.Value * brightness);
            byte g = (byte)(_sliderG.Value * brightness);
            byte b = (byte)(_sliderB.Value * brightness);
            
            _livePreviewCallback(r, g, b);
        }

        private void UpdateColorPreview()
        {
            if (_colorPreview == null || _sliderR == null) return;
            
            float brightness = (float)(_sliderBrightness?.Value ?? 100) / 100f;
            byte r = (byte)(_sliderR.Value * brightness);
            byte g = (byte)(_sliderG.Value * brightness);
            byte b = (byte)(_sliderB.Value * brightness);
            _colorPreview.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        #region Helper Methods

        private void AddSectionHeader(StackPanel panel, string text)
        {
            panel.Children.Add(new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 10, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 100, 180))
            });
        }

        private void AddLabeledControl(StackPanel panel, string label, Control control)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            sp.Children.Add(new TextBlock { Text = label, Width = 80, VerticalAlignment = VerticalAlignment.Center });
            control.Width = 300;
            sp.Children.Add(control);
            panel.Children.Add(sp);
        }

        private void AddLabeledSliderToPanel(StackPanel panel, string label, Slider slider)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(new TextBlock { Text = label, Width = 80, VerticalAlignment = VerticalAlignment.Center });
            slider.Width = 200;
            sp.Children.Add(slider);
            var valueLabel = new TextBlock { Width = 40, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            valueLabel.Text = ((int)slider.Value).ToString();
            slider.ValueChanged += (s, e) => valueLabel.Text = ((int)slider.Value).ToString();
            sp.Children.Add(valueLabel);
            panel.Children.Add(sp);
        }

        #endregion

        private TextBox _txtProfileName, _txtDescription, _txtGameProcess;
        private ComboBox _cmbIcon, _cmbCategory, _cmbColor, _cmbEffect;
        private CheckBox _chkFavorite, _chkAutoLoad, _chkLivePreview, _chkVibration, _chkColorChange;
        private Slider _sliderR, _sliderG, _sliderB, _sliderBrightness, _sliderThreshold;
        private Border _colorPreview;
        private TextBlock _lblRainbowWarning, _lblThreshold;
        private StackPanel _colorSectionPanel, _rgbSliderPanel;
    }
}
