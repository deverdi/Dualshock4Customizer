# Dualshock 4 Customizer 🎮

**Dualshock 4 Customizer**, Windows üzerinde Dualshock 4 oyun kolunuzu tam anlamıyla kontrol etmenizi, kişiselleştirmenizi ve yönetmenizi sağlayan WPF tabanlı modern bir araçtır. **HidLibrary** altyapısını kullanarak kontrolcü ile doğrudan ve düşük gecikmeli iletişim kurar.

![License](https://img.shields.io/badge/license-MIT-blue.svg) ![Platform](https://img.shields.io/badge/platform-Windows-lightgrey) ![Status](https://img.shields.io/badge/status-Active%20Development-green)

## 🌟 Özellikler

### 🎨 Aydınlatma ve Görünüm
* **Tam Renk Kontrolü:** LED çubuğu (Lightbar) için dilediğiniz rengi seçin.
* **Gelişmiş Efektler:**
    * 🌈 **Rainbow:** Renkler arası yumuşak geçiş.
    * 🚨 **Strobe:** Çakar lamba efekti.
    * 💓 **Pulse:** Nabız atışı efekti.
    * 🌬️ **Breathing:** Nefes alma efekti.
* **LED Test Mekanizması:** Renk ve efektlerin doğruluğunu anlık test edin.

### 🔋 Güç Yönetimi
* **Şarj Durumu:** Oyun kolunun şarj seviyesini anlık olarak görüntüleyin.
* **Akıllı Bildirimler:** Şarj azaldığında sizi uyarır.
* **Özelleştirilebilir Sınır:** Düşük şarj uyarısının hangi yüzdede (%) verileceğini kendiniz belirleyin.

### 💾 Profil Sistemi
* **Otomatik Tanıma:** Oyun kolunuzu bağladığınızda, ona atadığınız profil otomatik olarak yüklenir.
* **Profil Yönetimi:** Farklı oyunlar veya durumlar için sınırsız profil oluşturun.
* **Paylaşım:** Profillerinizi dışa aktarın (Export) veya arkadaşlarınızın profillerini içe aktarın (Import).

## 🐛 Bilinen Sorunlar ve Kısıtlamalar (Known Issues)

Proje geliştirme aşamasındadır ve şu an için aşağıdaki hatalar ve kısıtlamalar mevcuttur:

### Teknik Sorunlar
* **❗ HID Çakışması (Input Bloklanması):** Program, kontrolcüye HID üzerinden doğrudan erişim sağladığı için, program açıkken oyunlar kontrolcü girdilerini algılayamayabilir. *(Çözüm için Roadmap'teki ViGEm entegrasyonu beklenmektedir.)*
* **🔌 Bağlantı Algılama (Hot-Plug):** Uygulama başlatıldıktan sonra takılan oyun kolları şu an için otomatik olarak listeye gelmemektedir. Kontrolcüyü bağladıktan sonra uygulamayı başlatmanız gerekir.
* **⚠️ USB Bağlantı Sorunu:** Bluetooth bağlantısı kararlıyken, USB kablosu ile yapılan bağlantılarda bazı algılama ve stabilite sorunları yaşanabilmektedir.

### Arayüz (UI) Hataları
* **🌑 3D Model Aydınlatması:** Orta menüde yer alan Dualshock 4 3D modeli şu an gereğinden fazla karanlık görünmektedir. Materyal ve ışıklandırma ayarları üzerinde çalışılmaktadır.
* **🔋 Arayüz Şarj Göstergesi:** 3D modelin yanında duran görsel şarj göstergesi kararsız çalışabilmekte, bazen titreme yapmakta veya veriyi geç yansıtmaktadır.

## 🗺️ Yol Haritası (Roadmap) ve Gelecek Özellikler

Proje aktif geliştirme aşamasındadır ve aşağıdaki özelliklerin eklenmesi planlanmaktadır:

### 🎮 Emülasyon ve Uyumluluk
* **ViGEm Entegrasyonu (XInput):** Dualshock 4'ü sistemde bir Xbox 360 kontrolcüsü gibi göstererek tüm Windows oyunlarıyla %100 uyumlu hale getirme ve HID çakışmasını çözme.
* **Gizleme Modu (Hide DS4):** Çift kontrolcü girişini (Double Input) engellemek için fiziksel kontrolcüyü sistemden gizleme özelliği.

### 🎯 Hassasiyet ve Performans
* **Gelişmiş Analog Ayarları (Aim Smoothing):**
    * Ölü bölge (Deadzone) yönetimi.
    * Tepki eğrileri (Response Curves - Linear, Exponential vb.) ile nişan alma hassasiyetini özelleştirme.
* **Input Lag Optimizasyonu:** Giriş gecikmesini minimize edecek özel poling rate ayarları ve optimizasyonlar.

### 🖥️ Kullanıcı Deneyimi (UI/UX)
* **Oyun İçi Arayüz (In-Game Overlay):** Oyundan çıkmadan profil değiştirmek veya pil durumunu görmek için Xbox Game Bar benzeri bir katman.
* **Oyun Algılama (Auto-Switch):** Başlatılan .exe'ye göre otomatik profil geçişi.

### 🛠️ Ekstra Kontrol Özellikleri (Planlanan)
* **Tuş Atama (Remapping) ve Makrolar:** Tuşların yerini değiştirme veya tek tuşa kombo atama özellikleri.
* **Touchpad Desteği:** Touchpad'i Windows üzerinde mouse (fare) olarak kullanabilme özelliği.
* **Jiroskop (Gyro) Kullanımı:** Hareket sensörlerini nişan alma veya direksiyon olarak kullanma desteği.
* **Titreşim Test Mekanizması:** Rumble motorlarının (ağır ve hafif) ayrı ayrı test edilmesi.

## 🛠️ Teknolojiler

* **Dil:** C#
* **Framework:** WPF (Windows Presentation Foundation)
* **Kütüphane:** [HidLibrary](https://github.com/mikeobrien/HidLibrary) (HID cihaz iletişimi için)

## 🚀 Kurulum ve Kullanım

1.  Bu projeyi klonlayın:
    ```bash
    git clone [https://github.com/deverdi/Dualshock4Customizer.git](https://github.com/deverdi/Dualshock4Customizer.git)
    ```
2.  Projeyi **Visual Studio** ile açın.
3.  NuGet paketlerinin yüklendiğinden emin olun.
4.  Projeyi derleyin ve çalıştırın (`F5`).
5.  Dualshock 4 kolunuzu USB veya Bluetooth üzerinden bilgisayara bağlayın (Programı açmadan önce bağlamanız önerilir).

## 📸 Ekran Görüntüleri


| Ana Ekran | LED Efektleri |
| :---: | :---: |
| ![Ana Ekran](https://hizliresim.com/ebo90db) | ![Led Efektleri](https://hizliresim.com/jukytru) | ![Led Efektleri](https://hizliresim.com/imrq8b6) | ![Led Efektleri](https://hizliresim.com/lncc34l) | ![Led Efektleri](https://hizliresim.com/ifngknl) |

## 🤝 Katkıda Bulunma

Katkılarınızı bekliyoruz! Özellikle **ViGEm entegrasyonu** ve **USB bağlantı sorunları** konusunda yardıma ihtiyacımız var.

1.  Bu depoyu Fork'layın.
2.  Yeni bir özellik dalı (branch) oluşturun (`git checkout -b yeni-ozellik`).
3.  Değişikliklerinizi yapın ve Commit'leyin (`git commit -m 'Yeni özellik eklendi'`).
4.  Dalınızı Push'layın (`git push origin yeni-ozellik`).
5.  Bir **Pull Request** oluşturun.

## 🔗 İletişim

GitHub: [deverdi](https://github.com/deverdi)

---
*Bu proje hobi amaçlı geliştirilmektedir ve resmi Sony PlayStation yazılımı değildir.*
