using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Aktif oyun/uygulama algýlama servisi
    /// </summary>
    public class GameDetectionService : IDisposable
    {
        private System.Timers.Timer _detectionTimer;
        private string _currentProcessName = "";
        private Dictionary<string, string> _gameToProfileMap = new Dictionary<string, string>();

        public bool IsEnabled { get; set; } = false;
        public int CheckIntervalMs { get; set; } = 2000;

        public event EventHandler<GameDetectedEventArgs> GameDetected;
        public event EventHandler<GameDetectedEventArgs> GameChanged;

        public GameDetectionService()
        {
            LoadDefaultGameMappings();
        }

        /// <summary>
        /// Varsayýlan oyun-profil eþleþmelerini yükler
        /// </summary>
        private void LoadDefaultGameMappings()
        {
            _gameToProfileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Popüler oyunlar
                { "FIFA23", "Yesil Stad" },
                { "FIFA24", "Yesil Stad" },
                { "csgo", "CS:GO Kirmizi" },
                { "cs2", "CS:GO Kirmizi" },
                { "valorant", "Valorant" },
                { "eldenring", "Elden Ring" },
                { "RocketLeague", "Rocket League" },
                { "FortniteClient-Win64-Shipping", "Fortnite" },
                { "GTA5", "GTA V" },
                { "RDR2", "Red Dead 2" },
                { "Cyberpunk2077", "Cyberpunk" },
                { "Witcher3", "Witcher 3" },
                { "sekiro", "Sekiro" },
                { "DarkSoulsIII", "Dark Souls 3" },
                { "MonsterHunterWorld", "Monster Hunter" },
                { "ApexLegends", "Apex Legends" },
                { "PUBG", "PUBG" },
                { "Overwatch", "Overwatch" },
                { "LeagueofLegends", "League of Legends" },
                { "Minecraft", "Minecraft" }
            };
        }

        /// <summary>
        /// Oyun eþleþtirmesi ekler
        /// </summary>
        public void AddGameMapping(string processName, string profileName)
        {
            _gameToProfileMap[processName] = profileName;
            Debug.WriteLine($"?? Oyun eslesmes eklendi: {processName} -> {profileName}");
        }

        /// <summary>
        /// Oyun eþleþtirmesini kaldýrýr
        /// </summary>
        public void RemoveGameMapping(string processName)
        {
            if (_gameToProfileMap.ContainsKey(processName))
            {
                _gameToProfileMap.Remove(processName);
                Debug.WriteLine($"Oyun eslesmesi kaldirildi: {processName}");
            }
        }

        /// <summary>
        /// Tüm eþleþtirmeleri getirir
        /// </summary>
        public Dictionary<string, string> GetAllMappings()
        {
            return new Dictionary<string, string>(_gameToProfileMap);
        }

        /// <summary>
        /// Algýlama baþlatýr
        /// </summary>
        public void Start()
        {
            if (_detectionTimer != null)
                return;

            _detectionTimer = new System.Timers.Timer(CheckIntervalMs);
            _detectionTimer.Elapsed += OnTimerElapsed;
            _detectionTimer.AutoReset = true;
            _detectionTimer.Start();

            IsEnabled = true;
            Debug.WriteLine("Oyun algilama servisi baslatildi");
        }

        /// <summary>
        /// Algýlama durdurur
        /// </summary>
        public void Stop()
        {
            if (_detectionTimer != null)
            {
                _detectionTimer.Stop();
                _detectionTimer.Dispose();
                _detectionTimer = null;
            }

            IsEnabled = false;
            _currentProcessName = "";
            Debug.WriteLine("Oyun algilama servisi durduruldu");
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var activeProcess = GetActiveProcess();
                
                if (activeProcess == null)
                    return;

                string processName = activeProcess.ProcessName;

                // Önce eþleþme var mý kontrol et
                string profileName = null;
                foreach (var kvp in _gameToProfileMap)
                {
                    if (processName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        profileName = kvp.Value;
                        break;
                    }
                }

                if (profileName != null)
                {
                    if (_currentProcessName != processName)
                    {
                        _currentProcessName = processName;
                        
                        Debug.WriteLine($"Oyun algilandi: {processName} -> Profil: {profileName}");
                        
                        var eventArgs = new GameDetectedEventArgs
                        {
                            ProcessName = processName,
                            ProfileName = profileName,
                            WindowTitle = activeProcess.MainWindowTitle
                        };

                        GameDetected?.Invoke(this, eventArgs);
                        GameChanged?.Invoke(this, eventArgs);
                    }
                }
                else if (_currentProcessName != "")
                {
                    // Oyun deðiþti ama eþleþme yok
                    _currentProcessName = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Oyun algilama hatasi: {ex.Message}");
            }
        }

        /// <summary>
        /// Aktif pencereyi/iþlemi getirir
        /// </summary>
        private Process GetActiveProcess()
        {
            try
            {
                // En üstteki pencereye sahip iþlemleri bul
                var processes = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                    .OrderByDescending(p => p.MainWindowHandle)
                    .ToArray();

                return processes.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Manuel olarak aktif oyunu kontrol eder
        /// </summary>
        public GameDetectedEventArgs CheckCurrentGame()
        {
            var activeProcess = GetActiveProcess();
            if (activeProcess == null)
                return null;

            string processName = activeProcess.ProcessName;
            string profileName = null;

            foreach (var kvp in _gameToProfileMap)
            {
                if (processName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    profileName = kvp.Value;
                    break;
                }
            }

            if (profileName != null)
            {
                return new GameDetectedEventArgs
                {
                    ProcessName = processName,
                    ProfileName = profileName,
                    WindowTitle = activeProcess.MainWindowTitle
                };
            }

            return null;
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public class GameDetectedEventArgs : EventArgs
    {
        public string ProcessName { get; set; }
        public string ProfileName { get; set; }
        public string WindowTitle { get; set; }
    }
}
