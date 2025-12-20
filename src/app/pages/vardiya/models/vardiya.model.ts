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
}

export enum VardiyaDurum {
    ACIK = 'ACIK',
    ONAY_BEKLIYOR = 'ONAY_BEKLIYOR',
    ONAYLANDI = 'ONAYLANDI',
    REDDEDILDI = 'REDDEDILDI'
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
    fisNo: number;
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
    veresiye: number;
    filoKarti: number;
    toplam: number;
    aciklama?: string;
    krediKartiDetay?: { banka: string; tutar: number }[];
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
        veresiye: number;
        filoKarti: number;
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

// Market Z Raporu
export interface MarketZRaporu {
    id: number;
    vardiyaId: number;
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
    nakit: number;
    krediKarti: number;
    toplam: number;
    aciklama?: string;
    olusturmaTarihi: Date;
}

// Market Gider
export interface MarketGider {
    id: number;
    vardiyaId: number;
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

// Market Özet
export interface MarketOzet {
    zRaporuToplam: number;
    tahsilatToplam: number;
    giderToplam: number;
    netKasa: number; // Tahsilat - Gider
    fark: number;    // ZRaporu - NetKasa
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
    toplamVeresiye: number;
    toplamGider: number;
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
    toplamVeresiye: number;
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
    VERESIYE = 'VERESIYE',
    FILO_KARTI = 'FILO_KARTI'
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
