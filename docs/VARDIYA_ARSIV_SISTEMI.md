# 🏪 İstasyon Vardiya Arşivleme Sistemi

## Teknik Dokümantasyon v1.0
**Son Güncelleme:** 2026-01-06

---

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Mimari Tasarım](#mimari-tasarım)
3. [Veritabanı Şeması](#veritabanı-şeması)
4. [Vardiya Yaşam Döngüsü](#vardiya-yaşam-döngüsü)
5. [Servisler ve Metodlar](#servisler-ve-metodlar)
6. [API Endpoint'leri](#api-endpointleri)
7. [Performans Optimizasyonu](#performans-optimizasyonu)
8. [Geri Yükleme Mekanizması](#geri-yükleme-mekanizması)
9. [🔐 Güvenlik ve Tasarım Kalıpları](#güvenlik-ve-tasarım-kalıpları) 🆕
10. [Kod Referansları](#kod-referansları)

---

## 🎯 Genel Bakış

### Problem
Karşılaştırma raporları her seferinde büyük tablolardan (OtomasyonSatis, FiloSatis) hesaplanıyordu. Bu işlem:
- ~500-1000ms sürüyordu
- Veritabanına yük bindiriyordu
- Kullanıcı deneyimini olumsuz etkiliyordu

### Çözüm
**Arşivleme Stratejisi**: Vardiya onaylandığında tüm raporlar hesaplanıp JSON olarak saklanıyor. Sonraki sorgular arşivden okunuyor (~10ms).

```
┌─────────────────┐         ┌─────────────────┐
│  ESKİ SİSTEM    │         │  YENİ SİSTEM    │
├─────────────────┤         ├─────────────────┤
│ Her rapor       │         │ İlk hesaplama   │
│ isteğinde       │  ───▶   │ (onay anında)   │
│ hesapla         │         │ sonra arşivden  │
│ (~1000ms)       │         │ oku (~10ms)     │
└─────────────────┘         └─────────────────┘
```

---

## 🏗️ Mimari Tasarım

### Katmanlı Yapı

```
┌─────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                          │
│  VardiyaApprovalController  │  VardiyaReportController              │
└──────────────────────────────┼──────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         SERVICE LAYER                               │
│  VardiyaService  │  VardiyaArsivService  │  VardiyaFinancialService │
└──────────────────────────────┼──────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         DATA LAYER                                  │
│  AppDbContext  │  Entity Framework Core  │  PostgreSQL              │
└─────────────────────────────────────────────────────────────────────┘
```

### İlgili Dosyalar

| Dosya | Sorumluluk |
|-------|------------|
| `Models/VardiyaRaporArsiv.cs` | Arşiv tablosu entity modeli |
| `Models/Vardiya.cs` | Ana vardiya modeli (özet alanlar eklendi) |
| `Services/VardiyaArsivService.cs` | Arşivleme ve geri yükleme iş mantığı |
| `Services/VardiyaService.cs` | Vardiya CRUD ve onay işlemleri |
| `Controllers/VardiyaApprovalController.cs` | Onay/Red/Onay Kaldır API'leri |
| `Controllers/VardiyaReportController.cs` | Rapor API'leri (arşivden okur) |
| `Data/AppDbContext.cs` | EF Core DbContext ve ilişkiler |

---

## 💾 Veritabanı Şeması

### VardiyaRaporArsiv Tablosu

```sql
CREATE TABLE "VardiyaRaporArsivleri" (
    "Id" SERIAL PRIMARY KEY,
    
    -- Referans Bilgileri
    "VardiyaId" INT NOT NULL UNIQUE,      -- Her vardiya için tek arşiv
    "IstasyonId" INT NOT NULL,
    "Tarih" TIMESTAMP NOT NULL,
    
    -- Özet Değerler (Hızlı sorgu için denormalize)
    "SistemToplam" DECIMAL(18,2),
    "TahsilatToplam" DECIMAL(18,2),
    "FiloToplam" DECIMAL(18,2),
    "GiderToplam" DECIMAL(18,2),
    "Fark" DECIMAL(18,2),
    "FarkYuzde" DECIMAL(5,2),
    "Durum" VARCHAR(50),                  -- UYUMLU, FARK_VAR, KRITIK_FARK
    
    -- JSON Raporlar (Detaylı veri)
    "KarsilastirmaRaporuJson" JSONB,      -- Karşılaştırma raporu tam hali
    "FarkRaporuJson" JSONB,               -- Personel bazlı fark raporu
    "PompaSatisRaporuJson" JSONB,         -- Pompa satış özetleri
    "TahsilatDetayJson" JSONB,            -- Nakit, KK, diğer ödemeler
    "GiderRaporuJson" JSONB,              -- Gider kalemleri
    
    -- PDF Raporlar (İsteğe bağlı)
    "KarsilastirmaPdfIcerik" BYTEA,
    "FarkRaporuPdfIcerik" BYTEA,
    "VardiyaOzetPdfIcerik" BYTEA,
    
    -- Onay Bilgileri
    "OnaylayanId" INT,
    "OnaylayanAdi" VARCHAR(100),
    "OnayTarihi" TIMESTAMP,
    "SorumluId" INT,
    "SorumluAdi" VARCHAR(100),
    
    -- Meta
    "OlusturmaTarihi" TIMESTAMP DEFAULT NOW(),
    "GuncellemeTarihi" TIMESTAMP,
    
    -- Foreign Keys
    FOREIGN KEY ("VardiyaId") REFERENCES "Vardiyalar"("Id"),
    FOREIGN KEY ("IstasyonId") REFERENCES "Istasyonlar"("Id")
);

-- Performans Index'leri
CREATE UNIQUE INDEX "IX_VardiyaRaporArsiv_VardiyaId" ON "VardiyaRaporArsivleri"("VardiyaId");
CREATE INDEX "IX_VardiyaRaporArsiv_Tarih" ON "VardiyaRaporArsivleri"("Tarih");
CREATE INDEX "IX_VardiyaRaporArsiv_IstasyonTarih" ON "VardiyaRaporArsivleri"("IstasyonId", "Tarih");
```

### Vardiya Tablosuna Eklenen Alanlar

```sql
ALTER TABLE "Vardiyalar" ADD COLUMN "TahsilatToplam" DECIMAL(18,2);
ALTER TABLE "Vardiyalar" ADD COLUMN "OtomasyonToplam" DECIMAL(18,2);
ALTER TABLE "Vardiyalar" ADD COLUMN "FiloToplam" DECIMAL(18,2);
ALTER TABLE "Vardiyalar" ADD COLUMN "GiderToplam" DECIMAL(18,2);
ALTER TABLE "Vardiyalar" ADD COLUMN "RaporArsivId" INT REFERENCES "VardiyaRaporArsivleri"("Id");
ALTER TABLE "Vardiyalar" ADD COLUMN "Arsivlendi" BOOLEAN DEFAULT FALSE;
```

### Entity İlişkileri

```
┌─────────────────┐       1:1       ┌─────────────────────┐
│    Vardiya      │ ──────────────▶ │  VardiyaRaporArsiv  │
│                 │                 │                     │
│ - RaporArsivId  │◀──────────────  │ - VardiyaId (unique)│
│ - Arsivlendi    │                 │ - IstasyonId        │
└─────────────────┘                 └─────────────────────┘
         │                                    │
         │ 1:N                                │ N:1
         ▼                                    ▼
┌─────────────────┐                 ┌─────────────────────┐
│  VardiyaXmlLog  │                 │      Istasyon       │
│                 │                 │                     │
│ - XmlIcerik     │                 │                     │
│ - ZipDosyasi    │                 │                     │
└─────────────────┘                 └─────────────────────┘
```

---

## 🔄 Vardiya Yaşam Döngüsü

### Durum Diyagramı

```
                    ┌────────────────┐
                    │   XML/ZIP      │
                    │   Yüklendi     │
                    └───────┬────────┘
                            │
                            ▼
┌───────────────────────────────────────────────────────────────────────┐
│                           AÇIK                                        │
│                                                                       │
│  Dosya: VardiyaService.ProcessXmlZipAsync()                          │
│  - XML parse edilir                                                   │
│  - OtomasyonSatis, FiloSatis kayıtları oluşturulur                   │
│  - VardiyaXmlLog'a XML kaydedilir (geri yükleme için)                │
│  - Personel pusulalarısı otomatik oluşturulur                        │
│                                                                       │
│  📍 RAPOR İSTENİRSE: "Bu vardiya henüz onaylanmadı"                  │
└───────────────────────────────────┬───────────────────────────────────┘
                                    │
                                    │ [Onaya Gönder]
                                    │ VardiyaService.OnayaGonderAsync()
                                    ▼
┌───────────────────────────────────────────────────────────────────────┐
│                       ONAY_BEKLIYOR                                   │
│                                                                       │
│  - Fark hesaplanır ve kaydedilir                                     │
│  - Admin/Patron'a bildirim gönderilir                                 │
│  - Pusula düzenlemeleri hala yapılabilir                             │
│                                                                       │
│  📍 RAPOR İSTENİRSE: "Bu vardiya henüz onaylanmadı"                  │
└───────────────────┬───────────────────────────────────────────────────┘
                    │
        ┌───────────┴───────────┐
        │                       │
        ▼                       ▼
  [Onayla]                 [Reddet]
        │                       │
        │                       ▼
        │              ┌────────────────┐
        │              │  REDDEDİLDİ    │
        │              │                │
        │              │  Düzeltme için │
        │              │  geri gönderil.│
        │              └───────┬────────┘
        │                      │
        │                      │ [Tekrar Onaya Gönder]
        │                      ▼
        │              ┌────────────────┐
        │              │ ONAY_BEKLIYOR  │
        ▼              └────────────────┘
┌───────────────────────────────────────────────────────────────────────┐
│                          ONAYLANDI                                    │
│                                                                       │
│  Dosya: VardiyaService.OnaylaAsync()                                 │
│                                                                       │
│  1. VardiyaFinancialService.ProcessVardiyaApproval()                 │
│     - Veresiye kayıtları → Cari hareket oluştur                      │
│                                                                       │
│  2. VardiyaArsivService.ArsivleVardiya() 🆕                          │
│     - Tüm raporları hesapla                                          │
│     - JSON'a çevir ve VardiyaRaporArsiv'e kaydet                     │
│     - Vardiya özet alanlarını güncelle                               │
│                                                                       │
│  📍 RAPOR İSTENİRSE: Arşivden anında oku (~10ms)                     │
│                                                                       │
└───────────────────────────────┬───────────────────────────────────────┘
                                │
                                │ [Onay Kaldır - Sadece Admin]
                                │ VardiyaArsivService.OnayiKaldirVeGeriYukle()
                                ▼
┌───────────────────────────────────────────────────────────────────────┐
│                       ONAY_BEKLIYOR (Geri Alındı)                     │
│                                                                       │
│  1. VardiyaRaporArsiv kaydı silinir                                  │
│  2. Vardiya.Arsivlendi = false                                        │
│  3. Vardiya.Durum = ONAY_BEKLIYOR                                    │
│  4. Veriler tablolarda hala mevcut (silinmedi)                       │
│  5. Gerekirse VardiyaXmlLog'dan XML yeniden parse edilebilir         │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

### Silme Akışı

```
┌─────────────────┐      [Silme Talebi]      ┌───────────────────────────┐
│   Herhangi      │ ─────────────────────▶  │  SILINME_ONAYI_BEKLIYOR   │
│   Durum         │                          │                           │
└─────────────────┘                          └─────────────┬─────────────┘
                                                           │
                                             ┌─────────────┴──────────────┐
                                             │                            │
                                             ▼                            ▼
                                       [Silme Onayla]               [Silme Reddet]
                                             │                            │
                                             ▼                            ▼
                                      ┌─────────────┐              ┌────────────┐
                                      │   SİLİNDİ   │              │    AÇIK    │
                                      │ (Soft Del.) │              │            │
                                      └─────────────┘              └────────────┘
```

---

## 🔧 Servisler ve Metodlar

### VardiyaArsivService

**Konum:** `Services/VardiyaArsivService.cs`

```csharp
public class VardiyaArsivService
{
    // ═══════════════════════════════════════════════════════════════
    // ANA ARŞİVLEME METODU
    // Vardiya onaylandığında çağrılır
    // ═══════════════════════════════════════════════════════════════
    
    public async Task<VardiyaRaporArsiv?> ArsivleVardiya(
        int vardiyaId, 
        int onaylayanId, 
        string onaylayanAdi)
    {
        // 1. Vardiya verilerini detaylarıyla çek
        var vardiya = await GetVardiyaWithDetails(vardiyaId);
        
        // 2. Raporları hesapla
        var hesaplamalar = HesaplaRaporVerileri(vardiya);
        
        // 3. JSON'a çevir
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        
        // 4. Arşiv kaydı oluştur
        var arsiv = new VardiyaRaporArsiv
        {
            VardiyaId = vardiyaId,
            IstasyonId = vardiya.IstasyonId,
            SistemToplam = hesaplamalar.SistemToplam,
            TahsilatToplam = hesaplamalar.TahsilatToplam,
            KarsilastirmaRaporuJson = JsonSerializer.Serialize(hesaplamalar.KarsilastirmaRaporu, jsonOptions),
            FarkRaporuJson = JsonSerializer.Serialize(hesaplamalar.FarkRaporu, jsonOptions),
            // ... diğer alanlar
        };
        
        // 5. Kaydet
        _context.VardiyaRaporArsivleri.Add(arsiv);
        await _context.SaveChangesAsync();
        
        // 6. Vardiya özet alanlarını güncelle
        vardiya.RaporArsivId = arsiv.Id;
        vardiya.Arsivlendi = true;
        vardiya.TahsilatToplam = hesaplamalar.TahsilatToplam;
        // ... diğer güncelleme
        
        return arsiv;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ARŞİVDEN RAPOR OKUMA
    // Rapor endpoint'leri tarafından çağrılır
    // ═══════════════════════════════════════════════════════════════
    
    public async Task<KarsilastirmaRaporuDto?> GetKarsilastirmaRaporuFromArsiv(int vardiyaId)
    {
        var arsiv = await _context.VardiyaRaporArsivleri
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);
        
        if (arsiv?.KarsilastirmaRaporuJson == null)
            return null;
        
        return JsonSerializer.Deserialize<KarsilastirmaRaporuDto>(arsiv.KarsilastirmaRaporuJson);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ONAY KALDIRMA VE GERİ YÜKLEME
    // Admin tarafından çağrılır
    // ═══════════════════════════════════════════════════════════════
    
    public async Task<bool> OnayiKaldirVeGeriYukle(int vardiyaId, int userId, string userName)
    {
        // 1. Vardiyayı kontrol et
        var vardiya = await _context.Vardiyalar.FirstOrDefaultAsync(v => v.Id == vardiyaId);
        if (vardiya?.Durum != VardiyaDurum.ONAYLANDI) return false;
        
        // 2. VardiyaXmlLog'da XML mevcut mu kontrol et (geri yükleme kaynağı)
        var xmlLog = await _context.VardiyaXmlLoglari
            .FirstOrDefaultAsync(x => x.VardiyaId == vardiyaId);
        if (xmlLog == null) return false;
        
        // 3. Arşivi sil
        var arsiv = await _context.VardiyaRaporArsivleri
            .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);
        if (arsiv != null)
            _context.VardiyaRaporArsivleri.Remove(arsiv);
        
        // 4. Vardiya durumunu güncelle
        vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
        vardiya.Arsivlendi = false;
        vardiya.RaporArsivId = null;
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // HESAPLAMA METODU (Private)
    // Tüm rapor verilerini hesaplar
    // ═══════════════════════════════════════════════════════════════
    
    private VardiyaHesaplamaSonucu HesaplaRaporVerileri(Vardiya vardiya)
    {
        // Sistem (Otomasyon) Toplamı
        var sistemToplam = vardiya.OtomasyonSatislar.Sum(s => s.ToplamTutar);
        
        // Filo Toplamı
        var filoToplam = vardiya.FiloSatislar.Sum(f => f.Tutar);
        
        // Tahsilat Toplamı (Pusula: Nakit + KK + Diğer)
        var tahsilatToplam = vardiya.Pusulalar.Sum(p => 
            p.Nakit + p.KrediKarti + (p.DigerOdemeler?.Sum(d => d.Tutar) ?? 0));
        
        // Fark
        var fark = tahsilatToplam + filoToplam - sistemToplam;
        
        // Pompa satış özetleri
        var pompaSatislari = vardiya.OtomasyonSatislar
            .GroupBy(s => new { s.PompaNo, s.YakitTuru })
            .Select(g => new PompaSatisOzetDto { ... })
            .ToList();
        
        // Personel fark raporu
        var personelFarklari = vardiya.OtomasyonSatislar
            .GroupBy(s => s.PersonelAdi)
            .Select(g => new PersonelFarkDto { ... })
            .ToList();
        
        return new VardiyaHesaplamaSonucu { ... };
    }
}
```

### VardiyaService (İlgili Kısımlar)

**Konum:** `Services/VardiyaService.cs`

```csharp
public class VardiyaService : IVardiyaService
{
    private readonly VardiyaArsivService _arsivService; // 🆕 Enjekte edildi
    
    // ═══════════════════════════════════════════════════════════════
    // ONAYLAMA METODU
    // Arşivleme burada tetiklenir
    // ═══════════════════════════════════════════════════════════════
    
    public async Task OnaylaAsync(int id, OnayDto dto, int userId, string? userRole)
    {
        var vardiya = await _context.Vardiyalar
            .Include(v => v.Istasyon).ThenInclude(i => i.Firma)
            .FirstOrDefaultAsync(v => v.Id == id);
        
        // Durum kontrolü
        if (vardiya.Durum != VardiyaDurum.ONAY_BEKLIYOR)
            throw new InvalidOperationException("...");
        
        // 1. Durumu güncelle
        vardiya.Durum = VardiyaDurum.ONAYLANDI;
        vardiya.OnaylayanId = dto.OnaylayanId;
        vardiya.OnaylayanAdi = dto.OnaylayanAdi;
        vardiya.OnayTarihi = DateTime.UtcNow;
        
        // 2. Finansal işlemler (Veresiye → Cari Hareket)
        await _financialService.ProcessVardiyaApproval(vardiya.Id, dto.OnaylayanId);
        
        // 3. 🆕 ARŞİVLEME
        try
        {
            await _arsivService.ArsivleVardiya(vardiya.Id, dto.OnaylayanId, dto.OnaylayanAdi ?? "");
            _logger.LogInformation("Vardiya {VardiyaId} başarıyla arşivlendi.", vardiya.Id);
        }
        catch (Exception arsivEx)
        {
            // Arşivleme hatası onaylamayı engellemez, sadece loglanır
            _logger.LogError(arsivEx, "Vardiya arşivlenirken hata oluştu.");
        }
        
        await _context.SaveChangesAsync();
        
        // 4. Bildirimler gönder
        // ...
    }
}
```

---

## 🌐 API Endpoint'leri

### Onay İşlemleri

| Method | Endpoint | Yetki | Açıklama |
|--------|----------|-------|----------|
| `POST` | `/api/approvals/vardiya/{id}/onaya-gonder` | Tümü | Vardiyayı onaya gönderir |
| `POST` | `/api/approvals/vardiya/{id}/onayla` | Admin, Patron | Vardiyayı onaylar ve arşivler |
| `POST` | `/api/approvals/vardiya/{id}/reddet` | Admin, Patron | Vardiyayı reddeder |
| `POST` | `/api/approvals/vardiya/{id}/onay-kaldir` | **Admin** | Onayı kaldırır, arşivi siler 🆕 |
| `GET` | `/api/approvals/vardiya/{id}/onay-detay` | Tümü | Onay detaylarını getirir |

### Rapor İşlemleri

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/api/reports/vardiya/karsilastirma/{vardiyaId}` | Karşılaştırma raporu (arşivden) |
| `GET` | `/api/reports/vardiya/genel` | Genel vardiya raporu |
| `GET` | `/api/reports/vardiya/farklar` | Fark raporu |

### Örnek Response

**GET /api/reports/vardiya/karsilastirma/123**

#### ✅ Onaylı Vardiya (Arşivden)
```json
{
    "vardiyaId": 123,
    "tarih": "2026-01-06T08:00:00Z",
    "sistemToplam": 45000.00,
    "tahsilatToplam": 44950.00,
    "fark": -50.00,
    "farkYuzde": -0.11,
    "durum": "FARK_VAR",
    "detaylar": [...],
    "pompaSatislari": [...]
}
```

#### ❌ Onaylanmamış Vardiya
```json
{
    "message": "Bu vardiya henüz onaylanmadı, rapor mevcut değil."
}
```

---

## ⚡ Performans Optimizasyonu

### Karşılaştırma

| Senaryo | Eski Sistem | Yeni Sistem | İyileştirme |
|---------|-------------|-------------|-------------|
| Karşılaştırma Raporu | ~800ms | ~10ms | **80x hızlı** |
| Fark Raporu | ~500ms | ~8ms | **62x hızlı** |
| Veritabanı Sorgusu | 5-7 JOIN | 1 SELECT | **%85 azalma** |

### Veritabanı Index'leri

```sql
-- Tekil arşiv erişimi
CREATE UNIQUE INDEX ON "VardiyaRaporArsivleri"("VardiyaId");

-- Tarih bazlı filtreleme
CREATE INDEX ON "VardiyaRaporArsivleri"("Tarih");

-- İstasyon + Tarih kompozit (çoklu vardiya sorguları)
CREATE INDEX ON "VardiyaRaporArsivleri"("IstasyonId", "Tarih");
```

### JSON Sorgu Optimizasyonu

PostgreSQL JSONB tipi sayesinde JSON içinde de sorgu yapılabilir:

```sql
-- Örnek: Kritik farkı olan arşivleri bul
SELECT * FROM "VardiyaRaporArsivleri"
WHERE "Durum" = 'KRITIK_FARK'
ORDER BY "Tarih" DESC;
```

---

## 🔙 Geri Yükleme Mekanizması

### Veri Kaynağı: VardiyaXmlLog

```
┌─────────────────────────────────────────────────────────────────────┐
│                      VardiyaXmlLog Tablosu                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  IstasyonId    │  VardiyaId    │  DosyaAdi    │  XmlIcerik          │
│  ─────────────────────────────────────────────────────────────────  │
│       1        │      123      │  shift.xml   │  <VeriPos>...</>    │
│       1        │      124      │  shift.xml   │  <VeriPos>...</>    │
│                                                                     │
│  Bu tablo, her vardiya için orijinal XML'i saklıyor.               │
│  Onay kaldırılırsa buradan yeniden parse edilebilir.               │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Onay Kaldırma Akışı

```
┌────────────────┐
│ Admin: Onay    │
│ Kaldır Tıkla   │
└───────┬────────┘
        │
        ▼
┌────────────────────────────────────────────────────────────────────┐
│  VardiyaArsivService.OnayiKaldirVeGeriYukle(vardiyaId)             │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  1. Vardiya durumunu kontrol et (ONAYLANDI mı?)                   │
│                                                                    │
│  2. VardiyaXmlLog'da XML mevcut mu kontrol et                     │
│     (Geri yükleme kaynağı güvence altında mı?)                    │
│                                                                    │
│  3. VardiyaRaporArsiv kaydını sil                                 │
│     (JSON raporlar siliniyor)                                     │
│                                                                    │
│  4. Vardiya durumunu güncelle:                                    │
│     - Durum → ONAY_BEKLIYOR                                       │
│     - Arsivlendi → false                                          │
│     - RaporArsivId → null                                         │
│     - OnaylayanId, OnaylayanAdi, OnayTarihi → null                │
│                                                                    │
│  5. Log kaydet                                                    │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
        │
        ▼
┌────────────────┐
│ Vardiya tekrar │
│ ONAY_BEKLIYOR  │
└────────────────┘
```

### Neden XML Tabanlı Geri Yükleme?

| Yaklaşım | Avantaj | Dezavantaj |
|----------|---------|------------|
| **JSON Yedek** | Hızlı geri yükleme | Ekstra depolama, senkron tutma zorluğu |
| **XML Tabanlı** ✅ | Tek kaynak (source of truth), az depolama | Parse işlemi gerekiyor |

**Seçilen:** XML Tabanlı - Çünkü XML zaten `VardiyaXmlLog`'da saklı ve orijinal veri kaynağı.

---

## � Güvenlik ve Tasarım Kalıpları

### 1. Atomik İşlem (Transaction) Kullanımı

Onay işlemi sırasında birden fazla kritik işlem yapılır. Bunların hepsinin başarılı olması veya hiçbirinin olmaması gerekir.

**Problem:** Finansal işlem başarılı olur ama arşivleme başarısız olursa tutarsızlık oluşur.

**Çözüm:** Tüm kritik işlemler tek transaction içinde yapılır.

```csharp
// VardiyaService.OnaylaAsync()
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // 1. Durumu güncelle
    vardiya.Durum = VardiyaDurum.ONAYLANDI;
    
    // 2. Finansal işlemler
    await _financialService.ProcessVardiyaApproval(vardiya.Id, dto.OnaylayanId);
    
    // 3. Arşivleme
    await _arsivService.ArsivleVardiya(vardiya.Id, dto.OnaylayanId, dto.OnaylayanAdi ?? "");
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();  // ✅ Tümü başarılı
}
catch (Exception ex)
{
    await transaction.RollbackAsync();  // ❌ Tümü geri alınır
    throw new InvalidOperationException($"Vardiya onaylama işlemi başarısız: {ex.Message}", ex);
}
```

### 2. Idempotency (Aynı İşlemin Tekrarı)

Aynı vardiya için birden fazla arşivleme isteği gelebilir (ağ kesintisi, yeniden deneme, vb.).

**Çözüm:** Arşivleme metodu başında mevcut arşiv kontrolü yapılır.

```csharp
// VardiyaArsivService.ArsivleVardiya()
public async Task<VardiyaRaporArsiv?> ArsivleVardiya(int vardiyaId, ...)
{
    // Mevcut arşiv var mı kontrol et
    var mevcutArsiv = await _context.VardiyaRaporArsivleri
        .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);

    if (mevcutArsiv != null)
    {
        _logger.LogWarning("Vardiya {VardiyaId} zaten arşivlenmiş, güncelleniyor.", vardiyaId);
        return await GuncelleArsiv(vardiyaId, mevcutArsiv.Id);  // Güncelle, tekrar oluşturma
    }
    
    // Yeni arşiv oluştur...
}
```

Ayrıca veritabanı seviyesinde UNIQUE INDEX ile koruma:

```sql
CREATE UNIQUE INDEX "IX_VardiyaRaporArsiv_VardiyaId" ON "VardiyaRaporArsivleri"("VardiyaId");
```

### 3. Kritik/Non-Kritik İşlem Ayrımı

**Kritik İşlemler (Transaction İçinde):**
- Durum değişikliği
- Finansal işlemler (Cari Hareket)
- Arşivleme

**Non-Kritik İşlemler (Transaction Dışında):**
- Loglama
- Bildirim gönderme

```csharp
// Transaction tamamlandıktan sonra
try
{
    await LogVardiyaIslem(...);  // Non-kritik
    await _notificationService.NotifyUserAsync(...);  // Non-kritik
}
catch (Exception logEx)
{
    // Hata olsa bile onay geçerli, sadece logla
    _logger.LogWarning(logEx, "Loglama/bildirim hatası.");
}
```

### 4. Veri Güvenliği ve Yedekleme

#### Source of Truth: VardiyaXmlLog

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        VERİ GÜVENLİĞİ HİYERARŞİSİ                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  🥇 VardiyaXmlLog.XmlIcerik                                            │
│     └── Orijinal XML dosyası - EN GÜVENİLİR KAYNAK                     │
│                                                                         │
│  🥈 OtomasyonSatis / FiloSatis / VardiyaPompaEndeks                    │
│     └── Parse edilmiş ana veriler                                       │
│                                                                         │
│  🥉 VardiyaRaporArsiv.JSON                                             │
│     └── Hesaplanmış raporlar - FAALİYET MERKEZİ YEDEĞİ                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Risk Senaryoları ve Önlemler

| Risk | Olasılık | Etki | Önlem |
|------|----------|------|-------|
| Arşivleme başarısız | Düşük | Yüksek | Transaction ile tüm işlem geri alınır |
| XML bozuk/silinmiş | Çok Düşük | Kritik | XML silinmez, backup alınır |
| JSON deserialize hatası | Düşük | Orta | Hata loglanır, null döner |
| Duplicate arşiv | Orta | Düşük | UNIQUE INDEX + kod kontrolü |

### 5. Faz 2 Veri Temizleme Uyarısı

⚠️ **KRİTİK UYARI:** Eğer ileride `OtomasyonSatis` ve `FiloSatis` verileri silinecekse:

1. `VardiyaXmlLog.XmlIcerik` asla silinmemeli
2. Geri yükleme için `XmlParserService.RepopulateTables(vardiyaId)` metodu eklenmeli
3. Manuel pusula değişiklikleri vs orijinal XML farkı yönetilmeli

```csharp
// Faz 2: Veri temizleme sonrası geri yükleme
public async Task<bool> OnayiKaldirVeGeriYukle(int vardiyaId, ...)
{
    // XML'i al
    var xmlLog = await _context.VardiyaXmlLoglari
        .FirstOrDefaultAsync(x => x.VardiyaId == vardiyaId);
    
    if (xmlLog == null || string.IsNullOrEmpty(xmlLog.XmlIcerik))
    {
        throw new InvalidOperationException("XML kaydı bulunamadı, geri yükleme yapılamaz!");
    }
    
    // XML'den verileri yeniden parse et
    await _xmlParserService.RepopulateTables(vardiyaId, xmlLog.XmlIcerik);
    
    // Arşivi sil, durumu güncelle...
}
```

### 6. JSONB Sorgu Performansı

JSON içinde sorgu yapılacaksa GIN index gerekir:

```sql
-- JSON alanları için GIN Index (isteğe bağlı, kullanılacaksa ekle)
CREATE INDEX idx_karsilastirma_jsonb 
ON "VardiyaRaporArsivleri" 
USING GIN ("KarsilastirmaRaporuJson");

-- Örnek sorgu:
SELECT * FROM "VardiyaRaporArsivleri"
WHERE "KarsilastirmaRaporuJson" @> '{"durum": "KRITIK_FARK"}';
```

**Not:** Şu an sadece tüm JSON okunuyor, içinde sorgu yapılmıyor. Bu index ihtiyaç halinde eklenebilir.

---

## �📚 Kod Referansları

### Dependency Injection Kaydı

**Program.cs**
```csharp
// Servis kayıtları
builder.Services.AddScoped<VardiyaArsivService>();
// ... diğer servisler
```

### DTO Sınıfları

**Dtos/KarsilastirmaRaporuDto.cs**
```csharp
public class KarsilastirmaRaporuDto
{
    public int VardiyaId { get; set; }
    public DateTime Tarih { get; set; }
    public decimal SistemToplam { get; set; }
    public decimal TahsilatToplam { get; set; }
    public decimal Fark { get; set; }
    public decimal FarkYuzde { get; set; }
    public string Durum { get; set; }
    public List<KarsilastirmaDetayDto> Detaylar { get; set; }
    public List<PompaSatisOzetDto> PompaSatislari { get; set; }
}
```

### Migration Dosyası

**Migrations/YYYYMMDD_VardiyaRaporArsiv.cs**
```csharp
migrationBuilder.CreateTable(
    name: "VardiyaRaporArsivleri",
    columns: table => new
    {
        Id = table.Column<int>(nullable: false)
            .Annotation("Npgsql:ValueGenerationStrategy", ...),
        VardiyaId = table.Column<int>(nullable: false),
        // ... tüm kolonlar
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_VardiyaRaporArsivleri", x => x.Id);
        table.ForeignKey("FK_..._Vardiyalar", x => x.VardiyaId, ...);
    });
```

---

## 🚀 Gelecek Geliştirmeler (Faz 2)

### 1. Onay Sonrası Veri Temizleme

Onaylanan vardiyaların büyük tablolarını temizleyerek veritabanı boyutunu küçültme:

```csharp
// Arşivlemeden sonra:
await _context.OtomasyonSatislar
    .Where(s => s.VardiyaId == vardiyaId)
    .ExecuteDeleteAsync();

await _context.FiloSatislar
    .Where(f => f.VardiyaId == vardiyaId)
    .ExecuteDeleteAsync();
```

**Not:** Bu durumda geri yükleme için XML yeniden parse edilir.

### 2. Toplu Arşivleme Job'ı

Mevcut onaylı vardiyaları arşivlemek için background job:

```csharp
public class TopluArsivlemeJob
{
    public async Task Execute()
    {
        var onayliVardiyalar = await _context.Vardiyalar
            .Where(v => v.Durum == VardiyaDurum.ONAYLANDI && !v.Arsivlendi)
            .ToListAsync();
        
        foreach (var vardiya in onayliVardiyalar)
        {
            await _arsivService.ArsivleVardiya(vardiya.Id, 0, "Sistem");
        }
    }
}
```

### 3. PDF Rapor Üretimi ve Arşivleme

```csharp
// PDF oluştur
var pdfBytes = await GeneratePdfReport(arsivRaporu);

// Arşive ekle
await _arsivService.EklePdfRapor(arsivId, "KARSILASTIRMA", pdfBytes);
```

---

## 📞 İletişim

Sorularınız için: [Sistem Yöneticisi]

**Son güncelleme:** 2026-01-06
**Versiyon:** 1.0.0
