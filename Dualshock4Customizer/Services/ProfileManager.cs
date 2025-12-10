using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using Dualshock4Customizer.Models;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Profil yonetim servisi - Gelismis ozellikler
    /// </summary>
    public class ProfileManager
    {
        private const string ProfilesFileName = "ds4_profiles.json";
        private readonly string _profilesFilePath;
        private List<DS4Profile> _profiles = new List<DS4Profile>();

        public IReadOnlyList<DS4Profile> Profiles => _profiles.AsReadOnly();

        public event EventHandler ProfilesChanged;

        public ProfileManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "DS4Customizer");
            
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            
            _profilesFilePath = Path.Combine(appFolder, ProfilesFileName);
            LoadProfiles();
        }

        #region Temel CRUD Islemleri

        public void LoadProfiles()
        {
            try
            {
                if (!File.Exists(_profilesFilePath))
                {
                    _profiles = new List<DS4Profile>();
                    return;
                }

                string json = File.ReadAllText(_profilesFilePath);
                _profiles = JsonSerializer.Deserialize<List<DS4Profile>>(json) ?? new List<DS4Profile>();
                Debug.WriteLine($"[ProfileManager] {_profiles.Count} profil yuklendi");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProfileManager] Yukleme hatasi: {ex.Message}");
                _profiles = new List<DS4Profile>();
            }
        }

        public void SaveProfiles()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_profiles, options);
                File.WriteAllText(_profilesFilePath, json);
                ProfilesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProfileManager] Kaydetme hatasi: {ex.Message}");
            }
        }

        public void AddProfile(DS4Profile profile)
        {
            _profiles.Add(profile);
            SaveProfiles();
        }

        public void UpdateProfile(DS4Profile profile)
        {
            var existing = _profiles.FirstOrDefault(p => p.ProfileName == profile.ProfileName);
            if (existing != null)
                _profiles.Remove(existing);
            _profiles.Add(profile);
            SaveProfiles();
        }

        public void DeleteProfile(string profileName)
        {
            var profile = _profiles.FirstOrDefault(p => p.ProfileName == profileName);
            if (profile != null)
            {
                _profiles.Remove(profile);
                SaveProfiles();
            }
        }

        #endregion

        #region Arama ve Filtreleme

        public DS4Profile GetProfileByMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return null;
            return _profiles.FirstOrDefault(p => 
                !string.IsNullOrWhiteSpace(p.MacAddress) && 
                p.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
        }

        public DS4Profile GetProfileByName(string name)
        {
            return _profiles.FirstOrDefault(p => p.ProfileName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsProfileNameUnique(string name)
        {
            return !_profiles.Any(p => p.ProfileName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Kategoriye gore profilleri getir
        /// </summary>
        public IEnumerable<DS4Profile> GetProfilesByCategory(ProfileCategory category)
        {
            return _profiles.Where(p => p.Category == category);
        }

        /// <summary>
        /// Favori profilleri getir
        /// </summary>
        public IEnumerable<DS4Profile> GetFavoriteProfiles()
        {
            return _profiles.Where(p => p.IsFavorite).OrderBy(p => p.SortOrder);
        }

        /// <summary>
        /// Oyun profillerini getir
        /// </summary>
        public IEnumerable<DS4Profile> GetGameProfiles()
        {
            return _profiles.Where(p => p.AutoLoadForGame && !string.IsNullOrWhiteSpace(p.LinkedGameProcess));
        }

        /// <summary>
        /// Oyun ismine gore profil bul
        /// </summary>
        public DS4Profile GetProfileForGame(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return null;
            return _profiles.FirstOrDefault(p => 
                p.AutoLoadForGame && 
                p.LinkedGameProcess.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// En cok kullanilan profilleri getir
        /// </summary>
        public IEnumerable<DS4Profile> GetMostUsedProfiles(int count = 5)
        {
            return _profiles.OrderByDescending(p => p.UsageCount).Take(count);
        }

        /// <summary>
        /// Son kullanilan profilleri getir
        /// </summary>
        public IEnumerable<DS4Profile> GetRecentProfiles(int count = 5)
        {
            return _profiles.OrderByDescending(p => p.LastUsedDate).Take(count);
        }

        /// <summary>
        /// Profil ara (isim, aciklama, oyun)
        /// </summary>
        public IEnumerable<DS4Profile> SearchProfiles(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return _profiles;
            
            searchText = searchText.ToLower();
            return _profiles.Where(p =>
                p.ProfileName.ToLower().Contains(searchText) ||
                p.Description.ToLower().Contains(searchText) ||
                p.LinkedGameProcess.ToLower().Contains(searchText) ||
                p.CategoryLabel.ToLower().Contains(searchText));
        }

        #endregion

        #region Siralama

        /// <summary>
        /// Profilleri sirala
        /// </summary>
        public IEnumerable<DS4Profile> GetSortedProfiles(ProfileSortType sortType = ProfileSortType.Favorites)
        {
            return sortType switch
            {
                ProfileSortType.Name => _profiles.OrderBy(p => p.ProfileName),
                ProfileSortType.Category => _profiles.OrderBy(p => p.Category).ThenBy(p => p.ProfileName),
                ProfileSortType.MostUsed => _profiles.OrderByDescending(p => p.UsageCount),
                ProfileSortType.Recent => _profiles.OrderByDescending(p => p.LastUsedDate),
                ProfileSortType.Created => _profiles.OrderByDescending(p => p.CreatedDate),
                ProfileSortType.Custom => _profiles.OrderBy(p => p.SortOrder),
                // Varsayilan: Favoriler ust, sonra isim
                _ => _profiles.OrderByDescending(p => p.IsFavorite).ThenBy(p => p.ProfileName)
            };
        }

        #endregion

        #region Profil Islemleri

        /// <summary>
        /// Profili kopyala
        /// </summary>
        public DS4Profile DuplicateProfile(string profileName)
        {
            var original = GetProfileByName(profileName);
            if (original == null) return null;
            
            var copy = original.Clone();
            
            // Benzersiz isim olustur
            string baseName = copy.ProfileName;
            int counter = 1;
            while (!IsProfileNameUnique(copy.ProfileName))
            {
                copy.ProfileName = $"{baseName} ({counter++})";
            }
            
            AddProfile(copy);
            return copy;
        }

        /// <summary>
        /// Favorilere ekle/cikar
        /// </summary>
        public void ToggleFavorite(string profileName)
        {
            var profile = GetProfileByName(profileName);
            if (profile != null)
            {
                profile.IsFavorite = !profile.IsFavorite;
                SaveProfiles();
            }
        }

        /// <summary>
        /// Profil sirasini degistir
        /// </summary>
        public void ReorderProfile(string profileName, int newOrder)
        {
            var profile = GetProfileByName(profileName);
            if (profile != null)
            {
                profile.SortOrder = newOrder;
                SaveProfiles();
            }
        }

        /// <summary>
        /// Profili yeniden adlandir
        /// </summary>
        public bool RenameProfile(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return false;
            if (!IsProfileNameUnique(newName)) return false;
            
            var profile = GetProfileByName(oldName);
            if (profile == null) return false;
            
            profile.ProfileName = newName;
            SaveProfiles();
            return true;
        }

        #endregion

        #region Varsayilan Profiller

        public void CreateDefaultProfiles()
        {
            if (_profiles.Count > 0) return;

            var defaultProfiles = new List<DS4Profile>
            {
                new DS4Profile
                {
                    ProfileName = "Varsayilan",
                    Description = "Standart mavi LED profili",
                    IconEmoji = "??",
                    Category = ProfileCategory.Genel,
                    LedR = 0, LedG = 0, LedB = 255,
                    ColorName = "Mavi",
                    Brightness = 100
                },
                new DS4Profile
                {
                    ProfileName = "Gece Modu",
                    Description = "Dusuk parlaklikli gece profili",
                    IconEmoji = "??",
                    Category = ProfileCategory.Gece,
                    LedR = 50, LedG = 0, LedB = 100,
                    ColorName = "OzelRenk",
                    Brightness = 30
                },
                new DS4Profile
                {
                    ProfileName = "Gaming RGB",
                    Description = "Rainbow efektli oyun profili",
                    IconEmoji = "??",
                    Category = ProfileCategory.Oyun,
                    EffectType = LedEffectType.Rainbow,
                    EffectSpeed = 50,
                    Brightness = 100
                }
            };

            foreach (var profile in defaultProfiles)
                _profiles.Add(profile);
            
            SaveProfiles();
        }

        #endregion

        #region Istatistikler

        /// <summary>
        /// Profil istatistikleri
        /// </summary>
        public ProfileStatistics GetStatistics()
        {
            return new ProfileStatistics
            {
                TotalProfiles = _profiles.Count,
                FavoriteCount = _profiles.Count(p => p.IsFavorite),
                GameProfileCount = _profiles.Count(p => p.AutoLoadForGame),
                MostUsedProfile = _profiles.OrderByDescending(p => p.UsageCount).FirstOrDefault()?.ProfileName ?? "Yok",
                CategoryCounts = _profiles.GroupBy(p => p.Category)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        #endregion
    }

    /// <summary>
    /// Profil siralama turleri
    /// </summary>
    public enum ProfileSortType
    {
        Favorites,
        Name,
        Category,
        MostUsed,
        Recent,
        Created,
        Custom
    }

    /// <summary>
    /// Profil istatistikleri
    /// </summary>
    public class ProfileStatistics
    {
        public int TotalProfiles { get; set; }
        public int FavoriteCount { get; set; }
        public int GameProfileCount { get; set; }
        public string MostUsedProfile { get; set; }
        public Dictionary<ProfileCategory, int> CategoryCounts { get; set; }
    }
}
