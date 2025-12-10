using System;
using System.Windows;

namespace Dualshock4Customizer
{
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            Title = "DS4 Customizer - Test";
            Width = 600;
            Height = 400;
            
            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = "Program calisiyor!\n\nEger bunu goruyorsaniz, temel program akisi basarili.",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = System.Windows.TextAlignment.Center
            };
            
            Content = textBlock;
        }
    }
}
