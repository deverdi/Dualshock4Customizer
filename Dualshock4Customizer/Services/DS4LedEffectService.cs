using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dualshock4Customizer.Services
{
    public enum LedEffectType
    {
        None,
        Rainbow,
        Breathing,
        HealthBar,
        Pulse,
        Strobe
    }

    public class DS4LedEffectService
    {
        private DS4LedService _ledService;
        private DS4Controller _controller;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _effectTask;
        private LedEffectType _currentEffect = LedEffectType.None;
        private bool _isRunning = false;

        public LedEffectType CurrentEffect => _currentEffect;
        public bool IsRunning => _isRunning;

        // Efekt hizi: 10 (cok hizli) - 100 (cok yavas)
        public int EffectSpeed { get; set; } = 50;
        public int HealthPercentage { get; set; } = 100;
        
        // Breathing efekti icin baz renk
        public byte BreathingBaseR { get; set; } = 0;
        public byte BreathingBaseG { get; set; } = 0;
        public byte BreathingBaseB { get; set; } = 255;

        public DS4LedEffectService(DS4Controller controller, DS4LedService ledService)
        {
            _controller = controller;
            _ledService = ledService;
        }

        /// <summary>
        /// Hiza gore frame delay hesapla (ms)
        /// EffectSpeed 10 = 8ms (cok hizli), EffectSpeed 100 = 50ms (yavas)
        /// </summary>
        private int GetFrameDelay()
        {
            return (int)(8 + (EffectSpeed - 10) * (50 - 8) / 90.0);
        }

        public void StartBreathingWithColor(byte r, byte g, byte b)
        {
            BreathingBaseR = r;
            BreathingBaseG = g;
            BreathingBaseB = b;
            
            if (r == 0 && g == 0 && b == 0)
            {
                BreathingBaseR = 0;
                BreathingBaseG = 0;
                BreathingBaseB = 255;
            }
            
            StartEffect(LedEffectType.Breathing);
        }

        public void StartEffect(LedEffectType effectType)
        {
            StopEffect();

            _currentEffect = effectType;
            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _effectTask = Task.Run(async () =>
            {
                try
                {
                    switch (effectType)
                    {
                        case LedEffectType.Rainbow:
                            await RunRainbowEffect(_cancellationTokenSource.Token);
                            break;
                        case LedEffectType.Breathing:
                            await RunBreathingEffect(_cancellationTokenSource.Token);
                            break;
                        case LedEffectType.HealthBar:
                            await RunHealthBarEffect(_cancellationTokenSource.Token);
                            break;
                        case LedEffectType.Pulse:
                            await RunPulseEffect(_cancellationTokenSource.Token);
                            break;
                        case LedEffectType.Strobe:
                            await RunStrobeEffect(_cancellationTokenSource.Token);
                            break;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Debug.WriteLine($"Efekt hatasi: {ex.Message}"); }
                finally { _isRunning = false; }
            });
        }

        public void StopEffect()
        {
            if (_isRunning)
            {
                _cancellationTokenSource?.Cancel();
                try { _effectTask?.Wait(1000); } catch { }
                _cancellationTokenSource?.Dispose();
                _isRunning = false;
                _currentEffect = LedEffectType.None;
            }
        }

        private async Task RunRainbowEffect(CancellationToken token)
        {
            float progress = 0f;
            
            while (!token.IsCancellationRequested)
            {
                var rgb = CalculateRainbowColor(progress);
                
                try
                {
                    _ledService.SetLedColor(rgb.r, rgb.g, rgb.b, 0x00, false);
                    _controller.LedR = rgb.r;
                    _controller.LedG = rgb.g;
                    _controller.LedB = rgb.b;
                }
                catch { }
                
                // Hiza gore ilerleme - dusuk hiz = hizli gecis
                float speedFactor = (110 - EffectSpeed) / 100f;
                progress += 0.005f * speedFactor;
                if (progress >= 1.0f) progress = 0f;
                
                await Task.Delay(GetFrameDelay(), token);
            }
        }

        private (byte r, byte g, byte b) CalculateRainbowColor(float progress)
        {
            progress = Math.Abs(progress % 1.0f);
            float div = progress * 6.0f;
            int region = (int)div;
            float regionProgress = div - region;
            int ascending = (int)(regionProgress * 255);
            int descending = 255 - ascending;

            switch (region)
            {
                case 0: return ((byte)255, (byte)ascending, (byte)0);
                case 1: return ((byte)descending, (byte)255, (byte)0);
                case 2: return ((byte)0, (byte)255, (byte)ascending);
                case 3: return ((byte)0, (byte)descending, (byte)255);
                case 4: return ((byte)ascending, (byte)0, (byte)255);
                default: return ((byte)255, (byte)0, (byte)descending);
            }
        }

        private async Task RunBreathingEffect(CancellationToken token)
        {
            byte baseR = BreathingBaseR;
            byte baseG = BreathingBaseG;
            byte baseB = BreathingBaseB;
            
            if (baseR == 0 && baseG == 0 && baseB == 0)
            {
                baseR = 0; baseG = 0; baseB = 255;
            }

            double brightness = 0.1;
            bool increasing = true;

            while (!token.IsCancellationRequested)
            {
                byte r = (byte)(baseR * brightness);
                byte g = (byte)(baseG * brightness);
                byte b = (byte)(baseB * brightness);

                try { _ledService.SetLedColor(r, g, b, 0x00, false); }
                catch { }

                // Hiza gore adim
                double speedFactor = (110 - EffectSpeed) / 100.0;
                double step = 0.03 * speedFactor;
                
                if (increasing)
                {
                    brightness += step;
                    if (brightness >= 1.0) { brightness = 1.0; increasing = false; }
                }
                else
                {
                    brightness -= step;
                    if (brightness <= 0.1) { brightness = 0.1; increasing = true; }
                }

                await Task.Delay(GetFrameDelay(), token);
            }
        }

        private async Task RunHealthBarEffect(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                byte r, g, b;
                if (HealthPercentage > 75) { r = 0; g = 255; b = 0; }
                else if (HealthPercentage > 50) { r = 255; g = 255; b = 0; }
                else if (HealthPercentage > 25) { r = 255; g = 165; b = 0; }
                else { r = 255; g = 0; b = 0; _ledService.SetLedColor(r, g, b, 0x00, true); await Task.Delay(500, token); continue; }
                _ledService.SetLedColor(r, g, b, 0x00, false);
                await Task.Delay(100, token);
            }
        }

        private async Task RunPulseEffect(CancellationToken token)
        {
            byte r = BreathingBaseR > 0 ? BreathingBaseR : (byte)0;
            byte g = BreathingBaseG > 0 ? BreathingBaseG : (byte)0;
            byte b = BreathingBaseB > 0 ? BreathingBaseB : (byte)255;

            // Hiza gore delay
            int baseDelay = (int)(100 + EffectSpeed * 1.5);

            while (!token.IsCancellationRequested)
            {
                _ledService.SetLedColor(r, g, b, 0x00, false);
                await Task.Delay(baseDelay, token);
                _ledService.SetLedColor((byte)(r / 3), (byte)(g / 3), (byte)(b / 3), 0x00, false);
                await Task.Delay(baseDelay, token);
                _ledService.SetLedColor(r, g, b, 0x00, false);
                await Task.Delay(baseDelay, token);
                _ledService.SetLedColor((byte)(r / 3), (byte)(g / 3), (byte)(b / 3), 0x00, false);
                await Task.Delay(baseDelay * 3, token);
            }
        }

        private async Task RunStrobeEffect(CancellationToken token)
        {
            byte r = BreathingBaseR > 0 ? BreathingBaseR : (byte)255;
            byte g = BreathingBaseG;
            byte b = BreathingBaseB;

            bool on = true;
            int strobeDelay = (int)(20 + EffectSpeed * 0.8);

            while (!token.IsCancellationRequested)
            {
                if (on) _ledService.SetLedColor(r, g, b, 0x00, false);
                else _ledService.SetLedColor(0, 0, 0, 0x00, false);
                on = !on;
                await Task.Delay(strobeDelay, token);
            }
        }

        public void Dispose() { StopEffect(); }
    }
}
