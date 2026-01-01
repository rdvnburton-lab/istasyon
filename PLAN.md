# Göç Planı: Frontend'den Backend'e Mantık Transferi

Bu belge, iş mantığını Angular frontend tarafından .NET backend tarafına taşımak için gereken kritik görevleri özetlemektedir. Amaç güvenliği artırmak, veri bütünlüğünü sağlamak ve sistem performansını iyileştirmektir.

## 🚨 Faz 1: Güvenlik ve Veri Bütünlüğü (Yüksek Öncelik)
Bu görevler, finansal hesaplamaların istemci (tarayıcı) tarafında yapıldığı ve manipülasyon veya hata riski taşıyan güvenlik açıklarını giderir.

- [x] **1.1. Sunucu Taraflı Vardiya Farkı/Açığı Hesaplaması (`Vardiya Farkı`)**
  - **Mevcut Durum:** "Fark" (Kasa Açığı/Fazlası) `pompa-yonetimi.component.ts` içinde hesaplanıp gösteriliyor. `OnayaGonder` servisi bu durumu doğrulamadan kabul ediyor.
  - **Yapılacak İş:**
    - `VardiyaController.OnayaGonder` (veya `Onayla`) metodunu güncelle.
    - `Toplam Satış - (Nakit + Kredi Kartı + vb.)` işlemini sunucuda tekrar hesapla.
    - Eğer fark varsa, onayı reddet veya bu farkı veritabanına otomatik "Vardiya Fark Kaydı" olarak işle.
    - **Dosyalar:** `VardiyaController.cs`, `VardiyaService.cs`

- [x] **1.2. Backend Fatura Toplamı Hesaplaması (`Yakıt Fatura`)**
  - **Mevcut Durum:** `yakit-stok.component.ts` faturayı `Litre * Birim Fiyat` formülüyle tarayıcıda hesaplayıp API'ye gönderiyor.
  - **Yapılacak İş:**
    - `StokController.AddFaturaGiris` metodunu güncelle.
    - İstemciden sadece `Litre` ve `BirimFiyat` (ve `YakitId`) bilgisini kabul et.
    - `ToplamTutar` hesaplamasını veritabanına kaydetmeden önce sunucuda yap.
    - İstemciden gelen `ToplamTutar` verisini güvenlik için yoksay.
    - **Dosyalar:** `StokController.cs`, `CreateFaturaGirisDto.cs`

- [x] **1.3. Pompa Sayaç Doğrulaması (`Pompa Sayaç`)**
  - **Mevcut Durum:** Pompa son endeks mantığının frontend matematiğine güvenip güvenmediği kontrol edilmeli.
  - **Yapılacak İş:**
    - `OtomasyonSatis` kayıtlarının kesinlikle otomasyon dosyalarından veya ham sayaç verilerinden türetildiğinden emin ol, arayüzdeki hesaplamalara güvenme.
    - **Dosyalar:** `VardiyaService.cs`, `OtomasyonSatis` mantığı.

## 🚀 Faz 2: Performans Optimizasyonu (Yüksek Öncelik)
Bu görevler, ağır istemci taraflı işlemler veya verimsiz veri çekme nedeniyle oluşabilecek sistem çökme risklerini ele alır.

- [x] **2.1. Stok Özeti Refaktörü (`StokController.GetOzet`)**
  - **Mevcut Durum:** TÜM `TankGiris` ve `OtomasyonSatis` kayıtlarını hafızaya (`ToList()`) çeker ve sonra döngü ile toplar. Bu işlem O(N) hafıza kullanımı ile büyük veride sunucuyu çökertebilir.
  - **Yapılacak İş:**
    - İşlemi `_context` üzerinde SQL toplama fonksiyonları (`SumAsync`, `GroupBy`) kullanarak yeniden yaz.
    - Mantığı veritabanı katmanına taşı.
    - **Dosyalar:** `StokController.cs`

- [x] **2.2. Sunucu Taraflı Raporlama (`Vardiya Raporları`)**
  - **Mevcut Durum:** Raporlar çok büyük JSON veri setlerini çekip tarayıcıda filtreliyor.
  - **Yapılacak İş:**
    - `VardiyaController` içinde Tarih Aralığı, İstasyon ve Personel için `IQueryable` (SQL Where) filtrelemesi uygula.
    - Büyük listeler için Sayfalama (Pagination - `Skip`, `Take`) uygula.
    - **Dosyalar:** `VardiyaController.cs`

## 🛠 Faz 3: Mimari Tutarlılık ve Standartlar
Uzun vadeli bakım ve güvenlik için görevler.

- [x] **3.1. Rol Tabanlı Yetkilendirmeyi Zorunlu Kıl**
  - **Mevcut Durum:** Bazı arayüz elemanları `*ngIf` ile gizleniyor ancak API uç noktalarında katı `[Authorize(Roles=...)]` kontrolleri eksik olabilir.
  - **Yapılacak İş:**
    - Tüm `Controller` metodlarını denetle.
    - Kritik işlemlere (Silme, Güncelleme, Onaylama) `[Authorize(Roles = "admin,patron")]` ekle.
    - **Dosyalar:** Tüm Controller'lar.

- [x] **3.2. Rapor DTO'larını Merkezileştirme**

## 🏗️ Faz 4: Market Vardiya Refaktörü (Kısa Vade & Öncelikli)
Kullanıcı isteği üzerine Market modülü teknik olarak yeniden yapılandırılacak.
- [x] **4.1. Market Servis Katmanı Oluşturma** (`IMarketVardiyaService`)
- [x] **4.2. Controller Temizliği** (Logic -> Service)
- [x] **4.3. Gereksiz Kod Temizliği** (Console.Write, WeatherForecast)
- [x] **4.4. Backend Z-Raporu Validasyonu** (KDV Toplam Kontrolü)
- [x] **4.5. Z-Raporu Giriş Ekranı Yenileme** (UX/UI Geliştirmesi)
  - **Mevcut Durum:** Raporlama DTO'ları dağınık veya veritabanı modellerinden tekrar kullanılıyor.
  - **Yapılacak İş:**
    - Optimize veri transferi için özel `ReportDto` sınıfları oluştur.
    - **Dosyalar:** `Dtos/Reports/*.cs`
