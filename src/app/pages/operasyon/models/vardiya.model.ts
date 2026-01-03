// ==========================================
// TEMEL ENTİTELER
// ==========================================

// İstasyon modeli
export interface Istasyon {
    id: number;
    ad: string;
    kod: string;
    adres: string;
    pompaSayisi: number;
    marketVar: boolean;
    aktif: boolean;
}

// Personel (Pompacı/Market Sorumlusu) modeli
export interface Personel {
    id: number;
    keyId: string; // El terminali/Otomasyon Key ID
    ad: string;
    soyad: string;
    tamAd: string;
    istasyonId: number;
    rol: PersonelRol;
    telefon?: string;
    aktif: boolean;
}

export enum PersonelRol {
    POMPACI = 'POMPACI',
    MARKET_SORUMLUSU = 'MARKET_SORUMLUSU',
    MARKET_GOREVLISI = 'MARKET_GOREVLISI',
    VARDIYA_SORUMLUSU = 'VARDIYA_SORUMLUSU',
    YONETICI = 'YONETICI'
}

// ==========================================
// POMPA (ÖN SAHA) YÖNETİMİ
// ==========================================

// Vardiya modeli
export interface Vardiya {
    id: number;
    istasyonId: number;
    istasyonAdi: string;
    istasyon?: Istasyon;
    sorumluId: number;
    sorumluAdi: string;
    baslangicTarihi: Date;
    bitisTarihi?: Date;
    durum: VardiyaDurum;
    pompaToplam: number;
    marketToplam: number;
    genelToplam: number;
    toplamFark: number;
    onaylayanId?: number;
    onaylayanAdi?: string;
    onayTarihi?: Date;
    redNedeni?: string;
    olusturmaTarihi: Date;
    guncellemeTarihi?: Date;
    kilitli: boolean; // Mühürlenmiş mi?
    dosyaAdi?: string;

    // Silme Talebi
    silinmeTalebiNedeni?: string;
    silinmeTalebiOlusturanId?: number;
    silinmeTalebiOlusturanAdi?: string;
}

export enum VardiyaDurum {
    ACIK = 'ACIK',
    ONAY_BEKLIYOR = 'ONAY_BEKLIYOR',
    ONAYLANDI = 'ONAYLANDI',
    REDDEDILDI = 'REDDEDILDI',
    SILINME_ONAYI_BEKLIYOR = 'SILINME_ONAYI_BEKLIYOR',
    SILINDI = 'SILINDI'
}

// Otomasyon Satış Verisi (Otomatik gelen)
export interface OtomasyonSatis {
    id: number;
    vardiyaId: number;
    personelId: number;
    personelAdi: string;
    personelKeyId: string;
    pompaNo: number;
    yakitTuru: YakitTuru;
    litre: number;
    birimFiyat: number;
    toplamTutar: number;
    satisTarihi: Date;
    fisNo?: number;   // Fiş numarası
    plaka?: string;   // Filo satışı için araç plakası
}

export interface FiloSatis {
    tarih: Date;
    filoKodu: string;
    plaka: string;
    yakitTuru: YakitTuru;
    litre: number;
    tutar: number;
    pompaNo: number;
    fisNo: number;
    filoAdi?: string;
}

export enum YakitTuru {
    BENZIN = 'BENZIN',
    MOTORIN = 'MOTORIN',
    LPG = 'LPG',
    EURO_DIESEL = 'EURO_DIESEL'
}

// Personel Bazlı Otomasyon Özeti
export interface PersonelOtomasyonOzet {
    personelId: number;
    personelAdi: string;
    personelKeyId: string;
    toplamLitre: number;
    toplamTutar: number;
    yakitDetay: { yakitTuru: YakitTuru; litre: number; tutar: number }[];
}

// Pusula Girişi (Vardiya Sorumlusu girer)
export interface PusulaGirisi {
    id: number;
    vardiyaId: number;
    personelId: number;
    personelAdi: string;
    nakit: number;
    krediKarti: number;

    toplam: number;
    aciklama?: string;
    krediKartiDetay?: { banka: string; tutar: number }[];
    digerOdemeler?: { turKodu: string; turAdi: string; tutar: number; silinemez?: boolean }[];
    olusturmaTarihi: Date;
}

// Personel Fark Analizi
export interface PersonelFarkAnalizi {
    personelId: number;
    personelAdi: string;
    otomasyonToplam: number; // Otomasyon ne diyor
    pusulaToplam: number;    // Sorumlu ne topladı
    fark: number;            // Açık (-) veya Fazla (+)
    farkDurum: FarkDurum;
    pusulaDokum: {
        nakit: number;
        krediKarti: number;
        digerOdemeler?: { turKodu: string; turAdi: string; tutar: number; silinemez?: boolean }[];
    };
}

export enum FarkDurum {
    UYUMLU = 'UYUMLU',       // Fark 0 veya çok küçük
    ACIK = 'ACIK',          // Kasa açığı (kırmızı alarm)
    FAZLA = 'FAZLA'         // Kasa fazlası
}

// Personel Karnesi
export interface PersonelKarne {
    personelId: number;
    personelAdi: string;
    donem: 'GUNLUK' | 'HAFTALIK' | 'AYLIK';
    baslangicTarihi: Date;
    bitisTarihi: Date;
    toplamSatis: number;
    toplamLitre: number;
    vardiyaSayisi: number;
    toplamAcik: number;
    toplamFazla: number;
    netFark: number;
    basariPuani: number; // 0-100
}

// ==========================================
// MARKET YÖNETİMİ
// ==========================================

export interface MarketVardiya {
    id: number;
    tarih: Date;
    durum: VardiyaDurum;
    toplamSatisTutari?: number;      // Günlük Toplam Sistem Satışı
    toplamTeslimatTutari?: number;   // Günlük Toplam Pusula (Nakit + KK)
    toplamFark?: number;             // Günlük Toplam Fark
    zRaporuTutari?: number;          // Günü kontrol etmek için opsiyonel
    zRaporuNo?: string;              // Z Raporu Numarası
    onaylayanId?: number;
    onaylayanAdi?: string;
    onayTarihi?: Date;
    redNedeni?: string;
    olusturmaTarihi: Date;
}

export interface MarketVardiyaPersonel {
    id: number;
    vardiyaId: number;
    personelId: number;
    personelAdi: string;

    // Satış Verisi (Harici Sistemden)
    sistemSatisTutari: number;

    // Teslimat Verisi (Pusula)
    nakit: number;
    krediKarti: number;
    gider?: number;

    // Hesaplanan
    toplamTeslimat: number; // Nakit + KK
    fark: number;           // ToplamTeslimat - SistemSatis

    aciklama?: string;
    olusturmaTarihi: Date;
}

// Market Z Raporu
export interface MarketZRaporu {
    id: number;
    vardiyaId: number;
    tarih: Date; // Verinin ait olduğu tarih
    genelToplam: number;
    kdv0: number;  // %0 KDV tutarı (muaf ürünler)
    kdv1: number;  // %1 KDV tutarı
    kdv10: number; // %10 KDV tutarı
    kdv20: number; // %20 KDV tutarı
    kdvToplam: number;
    kdvHaricToplam: number;
    olusturmaTarihi: Date;
}

// Market Tahsilat
export interface MarketTahsilat {
    id: number;
    vardiyaId: number;
    tarih: Date; // Verinin ait olduğu tarih
    personelId: number; // Tahsilatı yapan personel
    personelAdi: string;
    nakit: number;
    krediKarti: number;
    paroPuan: number;
    toplam: number;
    aciklama?: string;
    olusturmaTarihi: Date;
}

// Market Gider
export interface MarketGider {
    id: number;
    vardiyaId: number;
    tarih: Date; // Verinin ait olduğu tarih
    giderTuru: GiderTuru;
    tutar: number;
    aciklama: string;
    belgeTarihi?: Date;
    olusturmaTarihi: Date;
}

export enum GiderTuru {
    EKMEK = 'EKMEK',
    TEMIZLIK = 'TEMIZLIK',
    PERSONEL = 'PERSONEL',
    KIRTASIYE = 'KIRTASIYE',
    DIGER = 'DIGER'
}

// Market Gelir
export interface MarketGelir {
    id: number;
    vardiyaId: number;
    tarih: Date; // Verinin ait olduğu tarih
    gelirTuru: GelirTuru;
    tutar: number;
    aciklama: string;
    belgeTarihi?: Date;
    olusturmaTarihi: Date;
}

export enum GelirTuru {
    KOMISYON = 'KOMISYON',
    PRIM = 'PRIM',
    DIGER = 'DIGER'
}

// Market Özet
export interface MarketOzet {
    zRaporuToplam: number;
    tahsilatToplam: number;
    giderToplam: number;
    gelirToplam: number; // Yeni: Gelir toplamı
    netKasa: number; // Tahsilat + Gelir - Gider
    fark: number;    // ZRaporu - NetKasa
    tahsilatNakit: number;
    tahsilatKrediKarti: number;
    tahsilatParoPuan: number;
    kdvDokum: {
        kdv0: number;
        kdv1: number;
        kdv10: number;
        kdv20: number;
        toplam: number;
    };
}

// ==========================================
// POMPA GİDERLERİ
// ==========================================

export interface PompaGider {
    id: number;
    vardiyaId: number;
    giderTuru: PompaGiderTuru;
    tutar: number;
    aciklama: string;
    belgeTarihi?: Date;
    olusturmaTarihi: Date;
}

export enum PompaGiderTuru {
    YIKAMA = 'YIKAMA',
    BAHSIS = 'BAHSIS',
    TEMIZLIK = 'TEMIZLIK',
    TAMIR = 'TAMIR',
    DIGER = 'DIGER'
}

// ==========================================
// ONAY SİSTEMİ
// ==========================================

export interface VardiyaOnayTalebi {
    vardiyaId: number;
    vardiya: Vardiya;
    pompaOzet: PompaOzet;
    marketOzet: MarketOzet | null;
    genelOzet: GenelOzet;
    talepTarihi: Date;
}

export interface GenelOzet {
    pompaToplam: number;
    marketToplam: number;
    genelToplam: number;
    toplamNakit: number;
    toplamKrediKarti: number;

    digerOdemeler?: { turKodu: string; turAdi: string; toplam: number; silinemez?: boolean }[];
    filoToplam?: number;
    toplamGider: number;
    toplamVeresiye: number;
    toplamFark: number;
    durumRenk: 'success' | 'warn' | 'danger';
}

// ==========================================
// DASHBOARD (İSTASYON SAHİBİ)
// ==========================================

export interface GunlukOzet {
    tarih: Date;
    pompaCiro: number;
    marketCiro: number;
    toplamCiro: number;
    toplamNakit: number;
    toplamKrediKarti: number;

    digerOdemeler?: { turKodu: string; turAdi: string; toplam: number; silinemez?: boolean }[];
    toplamGider: number;
    kasaFarki: number;
    farkDurum: FarkDurum;
    onayBekleyenVardiya: number;
}

export interface DashboardVeri {
    gunlukOzet: GunlukOzet;
    sonVardiyalar: VardiyaOzet[];
    onayBekleyenler: VardiyaOnayTalebi[];
    kasaAlarmlari: KasaAlarm[];
    personelPerformans: PersonelKarne[];
}

export interface KasaAlarm {
    vardiyaId: number;
    vardiyaTarih: Date;
    personelAdi: string;
    farkTutar: number;
    farkTuru: 'ACIK' | 'FAZLA';
    aciklama: string;
}

// ==========================================
// YARDIMCI TİPLER
// ==========================================

export interface PompaOzet {
    personelSayisi: number;
    toplamOtomasyonSatis: number;
    toplamPusulaTahsilat: number;
    toplamFark: number;
    farkDurum: FarkDurum;
    personelFarklari: PersonelFarkAnalizi[];
    giderToplam: number;
    netTahsilat: number;
}

export interface VardiyaOzet {
    vardiya: Vardiya;
    pompaOzet: PompaOzet;
    marketOzet: MarketOzet | null;
    genelToplam: number;
    genelFark: number;
}

// Ödeme yöntemleri
export enum OdemeYontemi {
    NAKIT = 'NAKIT',
    KREDI_KARTI = 'KREDI_KARTI',
    PARO_PUAN = 'PARO_PUAN',
    MOBIL_ODEME = 'MOBIL_ODEME'
}

// Tahsilat (Eski uyumluluk için)
export interface Tahsilat {
    id: number;
    vardiyaId: number;
    odemeYontemi: OdemeYontemi;
    tutar: number;
    aciklama?: string;
    olusturmaTarihi: Date;
}

// Karşılaştırma sonucu
export interface KarsilastirmaSonuc {
    vardiyaId: number;
    sistemToplam: number;
    tahsilatToplam: number;
    fark: number;
    farkYuzde: number;
    detaylar: KarsilastirmaDetay[];
    durum: KarsilastirmaDurum;
    pompaSatislari: any[];
}

export interface KarsilastirmaDetay {
    odemeYontemi: OdemeYontemi;
    sistemTutar: number;
    tahsilatTutar: number;
    fark: number;
}

export enum KarsilastirmaDurum {
    UYUMLU = 'UYUMLU',
    FARK_VAR = 'FARK_VAR',
    KRITIK_FARK = 'KRITIK_FARK'
}

// Operator (Eski uyumluluk için)
export interface Operator {
    id: number;
    ad: string;
    soyad: string;
    kullaniciAdi: string;
    istasyonId: number;
    aktif: boolean;
}

// Pompa Satış (Eski uyumluluk için)
export interface PompaSatis {
    id: number;
    vardiyaId: number;
    pompaNo: number;
    yakitTuru: YakitTuru;
    litre: number;
    birimFiyat: number;
    toplamTutar: number;
    islemSayisi?: number;
    satisTarihi: Date;
}

export interface PompaSatisOzet {
    yakitTuru: YakitTuru;
    toplamLitre: number;
    toplamTutar: number;
}

export interface TahsilatOzet {
    odemeYontemi: OdemeYontemi;
    toplamTutar: number;
}

// ==========================================
// TANK ENVANTER
// ==========================================

export interface VardiyaTankEnvanteri {
    id: number;
    vardiyaId: number;
    tankNo: number;
    tankAdi: string;
    yakitTipi: string;
    baslangicStok: number;
    bitisStok: number;
    satilanMiktar: number;
    sevkiyatMiktar: number;
    beklenenTuketim: number;
    farkMiktar: number;
    kayitTarihi: Date;
}

