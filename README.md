# Dualshock 4 Customizer 🎮

![License](https://img.shields.io/badge/license-MIT-blue.svg) ![Platform](https://img.shields.io/badge/platform-Windows-lightgrey) ![Status](https://img.shields.io/badge/status-Active%20Development-green)

**[🇹🇷 Türkçe](#-türkçe) | [🇬🇧 English](#-english)**

---

<div align="center">

## 📥 İndir / Download
**v0.9.0 Beta (Portable)**
<br>
<a href="../../releases/latest">
  <img src="https://img.shields.io/badge/Download-Latest%20Release-blueviolet?style=for-the-badge&logo=windows" alt="Download">
</a>

</div>

---

## 🇹🇷 Türkçe

**Dualshock 4 Customizer**, Windows üzerinde Dualshock 4 oyun kolunuzu tam anlamıyla kontrol etmenizi, kişiselleştirmenizi ve yönetmenizi sağlayan WPF tabanlı modern bir araçtır. **HidLibrary** altyapısını kullanarak kontrolcü ile doğrudan ve düşük gecikmeli iletişim kurar.

### 🌟 Özellikler

#### 🎨 Aydınlatma ve Görünüm
* **Tam Renk Kontrolü:** LED çubuğu (Lightbar) için dilediğiniz rengi seçin.
* **Gelişmiş Efektler:** *Rainbow (Gökkuşağı), Strobe (Çakar), Pulse (Nabız), Breathing (Nefes).*
* **LED Test Mekanizması:** Renk ve efektlerin doğruluğunu anlık test edin.

#### 🔋 Güç Yönetimi
* **Şarj Durumu:** Oyun kolunun şarj seviyesini anlık görüntüleme.
* **Akıllı Bildirimler:** Şarj azaldığında uyarı sistemi.
* **Özelleştirilebilir Sınır:** Düşük şarj uyarısının yüzdesini (%) kendiniz belirleyin.

#### 💾 Profil Sistemi
* **Otomatik Tanıma:** Kol bağlandığında atanan profil otomatik yüklenir.
* **Profil Yönetimi:** Sınırsız profil oluşturma ve düzenleme.
* **Paylaşım:** Profilleri dışa (Export) ve içe (Import) aktarma.

### 🐛 Bilinen Sorunlar (Known Issues)

* **❗ HID Çakışması:** Program açıkken oyunlar kontrolcüyü algılamayabilir (Exclusive Mode benzeri durum).
* **🔌 Hot-Plug Yok:** Programı açmadan önce oyun kolunu bağlamanız gerekir. Sonradan takılan kollar listelenmeyebilir.
* **⚠️ USB Sorunu:** USB bağlantısı kararsız olabilir, Bluetooth önerilir.
* **🌑 Arayüz:** 3D model biraz karanlık görünebilir ve şarj göstergesi bazen görsel olarak titreme yapabilir.

### 🗺️ Yol Haritası (Roadmap)

* **ViGEm Entegrasyonu:** XInput desteği ile %100 oyun uyumluluğu.
* **Hide DS4:** Çift giriş (Double Input) sorununu çözme.
* **Oyun Algılama:** Oyuna göre otomatik profil değiştirme.
* **Makro & Tuş Atama:** Tuşların yerini değiştirme ve makro desteği.

---

## 🇬🇧 English

**Dualshock 4 Customizer** is a modern WPF-based tool that allows you to fully control, customize, and manage your Dualshock 4 controller on Windows. It uses **HidLibrary** infrastructure to communicate directly with the controller with low latency.

### 🌟 Features

#### 🎨 Lighting & Appearance
* **Full Color Control:** Pick any color for the Lightbar.
* **Advanced Effects:** *Rainbow, Strobe, Pulse, Breathing.*
* **LED Test Mechanism:** Test colors and effects instantly.

#### 🔋 Power Management
* **Battery Status:** View real-time battery level.
* **Smart Notifications:** Get notified when the battery is low.
* **Customizable Limit:** Set your own percentage (%) for the low battery warning.

#### 💾 Profile System
* **Auto-Detection:** Automatically loads the assigned profile when the controller connects.
* **Profile Management:** Create and edit unlimited profiles.
* **Sharing:** Export and Import profiles to share with friends.

### 🐛 Known Issues

* **❗ HID Conflict:** Games might not detect the controller while the app is running (due to exclusive access).
* **🔌 No Hot-Plug:** You must connect the controller *before* launching the app.
* **⚠️ USB Issue:** USB connection might be unstable; Bluetooth is recommended.
* **🌑 UI Glitches:** The 3D model appears slightly dark, and the battery indicator may flicker visually.

### 🗺️ Roadmap

* **ViGEm Integration:** Full XInput support for 100% game compatibility.
* **Hide DS4:** Fix "Double Input" issues.
* **Game Detection:** Auto-switch profiles based on the active game.
* **Macros & Remapping:** Button remapping and macro support.

---

## 📸 Screenshots / Ekran Görüntüleri

| Main Screen / Ana Ekran | Interface / Arayüz |
| :---: | :---: |
| ![Main Screen](https://i.hizliresim.com/ebo90db.png) | ![Interface](https://i.hizliresim.com/jukytru.png) |
| **Settings / Ayarlar** | **Color Picker / Renk Seçimi** |
| ![Settings](https://i.hizliresim.com/imrq8b6.png) | ![Color](https://i.hizliresim.com/lncc34l.png) |
| **Effects / Efektler** | |
| ![Effects](https://i.hizliresim.com/ifngknl.png) | |

## 🚀 Setup / Kurulum

1.  Download the latest release from the **[Releases](../../releases)** page.
2.  Extract the `.zip` file.
3.  Run `Dualshock4Customizer.exe`.
4.  Connect your DS4 controller via Bluetooth (recommended) or USB.

## 🤝 Contribution / Katkı

Contributions are welcome! We specifically need help with **ViGEm integration** and **USB stability**.

1.  Fork this repository.
2.  Create a feature branch (`git checkout -b new-feature`).
3.  Commit your changes.
4.  Push to the branch.
5.  Create a **Pull Request**.

---
*Disclaimer: This is a hobby project and is not affiliated with Sony PlayStation.*
