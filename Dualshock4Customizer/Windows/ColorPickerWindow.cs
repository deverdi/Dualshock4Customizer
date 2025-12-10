using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

namespace Dualshock4Customizer.Windows
{
    /// <summary>
    /// RGB Renk Seçici Penceresi
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public byte SelectedR { get; private set; }
        public byte SelectedG { get; private set; }
        public byte SelectedB { get; private set; }

        private Slider _redSlider;
        private Slider _greenSlider;
        private Slider _blueSlider;
        private System.Windows.Controls.TextBox _redTextBox;
        private System.Windows.Controls.TextBox _greenTextBox;
        private System.Windows.Controls.TextBox _blueTextBox;
        private System.Windows.Controls.TextBox _hexTextBox;
        private System.Windows.Shapes.Rectangle _previewRectangle;

        public ColorPickerWindow(byte initialR, byte initialG, byte initialB)
        {
            Title = "RGB Renk Secici";
            Width = 450;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            SelectedR = initialR;
            SelectedG = initialG;
            SelectedB = initialB;

            BuildUI();
            UpdatePreview();
        }

        private void BuildUI()
        {
            var mainPanel = new StackPanel { Margin = new Thickness(20) };

            // Önizleme kutusu
            var previewLabel = new TextBlock 
            { 
                Text = "Onizleme:", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 0, 0, 5) 
            };
            mainPanel.Children.Add(previewLabel);

            _previewRectangle = new System.Windows.Shapes.Rectangle
            {
                Height = 80,
                Fill = new SolidColorBrush(Color.FromRgb(SelectedR, SelectedG, SelectedB)),
                Stroke = Brushes.Gray,
                StrokeThickness = 2,
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainPanel.Children.Add(_previewRectangle);

            // Kýrmýzý slider
            mainPanel.Children.Add(CreateColorSlider("Kirmizi (R):", out _redSlider, out _redTextBox, SelectedR));
            _redSlider.ValueChanged += (s, e) => OnColorChanged();
            _redTextBox.TextChanged += (s, e) => OnTextBoxChanged(_redTextBox, _redSlider);

            // Yeþil slider
            mainPanel.Children.Add(CreateColorSlider("Yesil (G):", out _greenSlider, out _greenTextBox, SelectedG));
            _greenSlider.ValueChanged += (s, e) => OnColorChanged();
            _greenTextBox.TextChanged += (s, e) => OnTextBoxChanged(_greenTextBox, _greenSlider);

            // Mavi slider
            mainPanel.Children.Add(CreateColorSlider("Mavi (B):", out _blueSlider, out _blueTextBox, SelectedB));
            _blueSlider.ValueChanged += (s, e) => OnColorChanged();
            _blueTextBox.TextChanged += (s, e) => OnTextBoxChanged(_blueTextBox, _blueSlider);

            // Hex renk kodu
            var hexPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 0) };
            hexPanel.Children.Add(new TextBlock 
            { 
                Text = "HEX Renk Kodu:", 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold
            });
            
            _hexTextBox = new System.Windows.Controls.TextBox 
            { 
                Width = 120, 
                VerticalAlignment = VerticalAlignment.Center,
                MaxLength = 7
            };
            _hexTextBox.TextChanged += HexTextBox_TextChanged;
            hexPanel.Children.Add(_hexTextBox);
            mainPanel.Children.Add(hexPanel);

            // Hýzlý renk paletleri
            mainPanel.Children.Add(new TextBlock 
            { 
                Text = "Hizli Renkler:", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 20, 0, 5) 
            });
            mainPanel.Children.Add(CreateQuickColorPalette());

            // Butonlar
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var okButton = new Button 
            { 
                Content = "Tamam", 
                Width = 100, 
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };

            var cancelButton = new Button 
            { 
                Content = "Iptal", 
                Width = 100, 
                Height = 30
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            mainPanel.Children.Add(buttonPanel);

            Content = mainPanel;
        }

        private StackPanel CreateColorSlider(string label, out Slider slider, out System.Windows.Controls.TextBox textBox, byte initialValue)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            
            var headerPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
            headerPanel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            
            textBox = new System.Windows.Controls.TextBox 
            { 
                Width = 50, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = initialValue.ToString()
            };
            DockPanel.SetDock(textBox, Dock.Right);
            headerPanel.Children.Add(textBox);
            
            panel.Children.Add(headerPanel);

            slider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = initialValue,
                TickFrequency = 1,
                IsSnapToTickEnabled = true
            };
            panel.Children.Add(slider);

            return panel;
        }

        private WrapPanel CreateQuickColorPalette()
        {
            var panel = new WrapPanel();
            
            var quickColors = new[]
            {
                ("Kirmizi", 255, 0, 0),
                ("Yesil", 0, 255, 0),
                ("Mavi", 0, 0, 255),
                ("Sari", 255, 255, 0),
                ("Turuncu", 255, 165, 0),
                ("Mor", 128, 0, 128),
                ("Pembe", 255, 192, 203),
                ("Turkuaz", 64, 224, 208),
                ("Beyaz", 255, 255, 255),
                ("Kapali", 0, 0, 0)
            };

            foreach (var (name, r, g, b) in quickColors)
            {
                var colorButton = new Button
                {
                    Width = 35,
                    Height = 35,
                    Margin = new Thickness(5, 5, 5, 5),
                    Background = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b)),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(2),
                    ToolTip = name,
                    Cursor = Cursors.Hand
                };

                colorButton.Click += (s, e) =>
                {
                    _redSlider.Value = r;
                    _greenSlider.Value = g;
                    _blueSlider.Value = b;
                };

                panel.Children.Add(colorButton);
            }

            return panel;
        }

        private void OnColorChanged()
        {
            SelectedR = (byte)_redSlider.Value;
            SelectedG = (byte)_greenSlider.Value;
            SelectedB = (byte)_blueSlider.Value;

            _redTextBox.Text = SelectedR.ToString();
            _greenTextBox.Text = SelectedG.ToString();
            _blueTextBox.Text = SelectedB.ToString();

            UpdatePreview();
            UpdateHexCode();
        }

        private void OnTextBoxChanged(System.Windows.Controls.TextBox textBox, Slider slider)
        {
            if (byte.TryParse(textBox.Text, out byte value))
            {
                slider.Value = value;
            }
        }

        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string hex = _hexTextBox.Text.Trim();
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int color))
            {
                _redSlider.Value = (color >> 16) & 0xFF;
                _greenSlider.Value = (color >> 8) & 0xFF;
                _blueSlider.Value = color & 0xFF;
            }
        }

        private void UpdatePreview()
        {
            _previewRectangle.Fill = new SolidColorBrush(Color.FromRgb(SelectedR, SelectedG, SelectedB));
        }

        private void UpdateHexCode()
        {
            string hex = $"#{SelectedR:X2}{SelectedG:X2}{SelectedB:X2}";
            _hexTextBox.TextChanged -= HexTextBox_TextChanged;
            _hexTextBox.Text = hex;
            _hexTextBox.TextChanged += HexTextBox_TextChanged;
        }
    }
}
