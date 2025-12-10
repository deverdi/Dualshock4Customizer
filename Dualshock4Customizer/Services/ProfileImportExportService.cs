using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics;
using Dualshock4Customizer.Models;
using Microsoft.Win32;

namespace Dualshock4Customizer.Services
{
    /// <summary>
    /// Profil import/export servisi
    /// </summary>
    public class ProfileImportExportService
    {
        private const string FileFilter = "DS4 Profil Dosyasý (*.ds4profile)|*.ds4profile|JSON Dosyasý (*.json)|*.json|Tüm Dosyalar (*.*)|*.*";

        /// <summary>
        /// Tek bir profili dosyaya aktarýr
        /// </summary>
        public static bool ExportProfile(DS4Profile profile, string suggestedFileName = null)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = FileFilter,
                    FileName = suggestedFileName ?? $"{profile.ProfileName}.ds4profile",
                    DefaultExt = ".ds4profile",
                    Title = "Profili Dýþa Aktar"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    File.WriteAllText(saveDialog.FileName, json);
                    Debug.WriteLine($"? Profil dýþa aktarýldý: {saveDialog.FileName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Profil dýþa aktarma hatasý: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dosyadan profil içe aktarýr
        /// </summary>
        public static DS4Profile ImportProfile(out bool success)
        {
            success = false;
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = FileFilter,
                    DefaultExt = ".ds4profile",
                    Title = "Profil Ýçe Aktar"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var profile = JsonSerializer.Deserialize<DS4Profile>(json);
                    
                    if (profile != null)
                    {
                        Debug.WriteLine($"? Profil içe aktarýldý: {profile.ProfileName}");
                        success = true;
                        return profile;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Profil içe aktarma hatasý: {ex.Message}");
                success = false;
                return null;
            }
        }

        /// <summary>
        /// Birden fazla profili tek bir dosyaya aktarýr
        /// </summary>
        public static bool ExportMultipleProfiles(List<DS4Profile> profiles)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "DS4 Profil Paketi (*.ds4pack)|*.ds4pack|JSON Dosyasý (*.json)|*.json",
                    FileName = $"DS4_Profiles_{DateTime.Now:yyyyMMdd}.ds4pack",
                    DefaultExt = ".ds4pack",
                    Title = "Profilleri Dýþa Aktar"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var wrapper = new
                    {
                        ExportDate = DateTime.Now,
                        ProfileCount = profiles.Count,
                        Profiles = profiles
                    };

                    var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    File.WriteAllText(saveDialog.FileName, json);
                    Debug.WriteLine($"? {profiles.Count} profil dýþa aktarýldý: {saveDialog.FileName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Çoklu profil dýþa aktarma hatasý: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dosyadan birden fazla profil içe aktarýr
        /// </summary>
        public static List<DS4Profile> ImportMultipleProfiles(out bool success)
        {
            success = false;
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "DS4 Profil Paketi (*.ds4pack)|*.ds4pack|JSON Dosyasý (*.json)|*.json",
                    DefaultExt = ".ds4pack",
                    Title = "Profilleri Ýçe Aktar"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    
                    // Önce çoklu profil formatýný dene
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("Profiles", out var profilesElement))
                        {
                            var profiles = JsonSerializer.Deserialize<List<DS4Profile>>(profilesElement.GetRawText());
                            if (profiles != null && profiles.Count > 0)
                            {
                                Debug.WriteLine($"? {profiles.Count} profil içe aktarýldý");
                                success = true;
                                return profiles;
                            }
                        }
                    }
                    catch { }

                    // Tek profil formatýný dene
                    var singleProfile = JsonSerializer.Deserialize<DS4Profile>(json);
                    if (singleProfile != null)
                    {
                        Debug.WriteLine($"? 1 profil içe aktarýldý: {singleProfile.ProfileName}");
                        success = true;
                        return new List<DS4Profile> { singleProfile };
                    }
                }

                return new List<DS4Profile>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Çoklu profil içe aktarma hatasý: {ex.Message}");
                success = false;
                return new List<DS4Profile>();
            }
        }

        /// <summary>
        /// Tüm profilleri yedekler
        /// </summary>
        public static bool BackupAllProfiles(List<DS4Profile> profiles)
        {
            try
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DS4Customizer",
                    "Backups"
                );

                Directory.CreateDirectory(backupDir);

                string backupFile = Path.Combine(backupDir, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.ds4pack");

                var wrapper = new
                {
                    BackupDate = DateTime.Now,
                    ProfileCount = profiles.Count,
                    Profiles = profiles
                };

                var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(backupFile, json);
                Debug.WriteLine($"? Yedekleme tamamlandý: {backupFile}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Yedekleme hatasý: {ex.Message}");
                return false;
            }
        }
    }
}
