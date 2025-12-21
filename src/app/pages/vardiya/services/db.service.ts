import { Injectable } from '@angular/core';
import Dexie, { Table } from 'dexie';

// Veritabanı modelleri
// Veritabanı modelleri
export interface DBIstasyon {
    id?: number;
    ad: string;
    kod: string;
    adres: string;
    pompaSayisi: number;
    marketVar: boolean;
    aktif: boolean;
}

export interface DBPersonel {
    id?: number;
    keyId: string; // Otomasyon ID (P001, V001 vb)
    ad: string;
    soyad: string;
    tamAd: string;
    istasyonId: number;
    rol: string;
    aktif: boolean;
}

export interface DBVardiya {
    id?: number;
    dosyaAdi: string;
    yuklemeTarihi: Date;
    baslangicTarih: Date | null;
    bitisTarih: Date | null;
    toplamTutar: number;
    personelSayisi: number;
    islemSayisi: number;
    durum: 'ACIK' | 'ONAY_BEKLIYOR' | 'ONAYLANDI' | 'REDDEDILDI';
    mutabakatTarihi?: Date;
    onayTarihi?: Date;
    onaylayanId?: number;
    onaylayanAdi?: string;
    redNedeni?: string;
}

export interface DBSatis {
    id?: number;
    vardiyaId: number;
    personelId: number;
    personelAdi: string;
    personelKeyId: string;
    pompaNo: number;
    yakitTuru: string;
    litre: number;
    birimFiyat: number;
    toplamTutar: number;
    satisTarihi: Date;
    fisNo?: number;
    plaka?: string;
}

export interface DBPusula {
    id?: number;
    vardiyaId: number;
    personelId: number;
    personelAdi: string;
    nakit: number;
    krediKarti: number;
    paroPuan: number;
    mobilOdeme: number;
    toplam: number;
    krediKartiDetay?: { banka: string; tutar: number }[];
    olusturmaTarihi: Date;
}

export interface DBGider {
    id?: number;
    vardiyaId: number;
    giderTuru: string;
    tutar: number;
    aciklama: string;
    olusturmaTarihi: Date;
}

export interface DBOnayLog {
    id?: number;
    vardiyaId: number;
    islem: 'OLUSTURULDU' | 'MUTABAKAT_YAPILDI' | 'ONAYA_GONDERILDI' | 'ONAYLANDI' | 'REDDEDILDI';
    kullaniciId?: number;
    kullaniciAdi?: string;
    aciklama?: string;
    tarih: Date;
}

// MARKET TABLES
export interface DBMarketVardiya {
    id?: number;
    tarih: Date;
    durum: string; // Enum string olarak saklanacak
    toplamSatisTutari: number;
    toplamTeslimatTutari: number;
    toplamFark: number;
    onaylayanId?: number;
    onaylayanAdi?: string;
    onayTarihi?: Date;
    redNedeni?: string;
    olusturmaTarihi: Date;
}

export interface DBMarketVardiyaPersonel {
    id?: number;
    vardiyaId: number;
    personelId: number;
    personelAdi: string;
    sistemSatisTutari: number;
    nakit: number;
    krediKarti: number;
    gider: number;
    toplamTeslimat: number;
    fark: number;
    aciklama?: string;
    olusturmaTarihi: Date;
}

export interface DBMarketZRaporu {
    id?: number;
    vardiyaId: number;
    tarih: Date;
    genelToplam: number;
    kdv0: number;
    kdv1: number;
    kdv10: number;
    kdv20: number;
    kdvToplam: number;
    kdvHaric: number;
    olusturmaTarihi: Date;
}

export interface DBMarketGelir {
    id?: number;
    vardiyaId: number;
    tarih: Date;
    gelirTuru: string;
    tutar: number;
    aciklama: string;
    olusturmaTarihi: Date;
}

// Dexie Veritabanı Sınıfı
class IstasyonDB extends Dexie {
    istasyonlar!: Table<DBIstasyon>;
    personeller!: Table<DBPersonel>;
    vardiyalar!: Table<DBVardiya>;
    satislar!: Table<DBSatis>;
    pusulalar!: Table<DBPusula>;
    giderler!: Table<DBGider>;
    onayLoglar!: Table<DBOnayLog>;

    // Market Tables
    marketVardiyalar!: Table<DBMarketVardiya>;
    marketVardiyaPersonel!: Table<DBMarketVardiyaPersonel>;
    marketZRaporu!: Table<DBMarketZRaporu>;
    marketGelirler!: Table<DBMarketGelir>;

    constructor() {
        super('IstasyonDB');

        this.version(2).stores({
            istasyonlar: '++id, kod',
            personeller: '++id, keyId, istasyonId, rol',
            vardiyalar: '++id, dosyaAdi, durum, yuklemeTarihi',
            satislar: '++id, vardiyaId, personelId',
            pusulalar: '++id, vardiyaId, personelId',
            giderler: '++id, vardiyaId',
            onayLoglar: '++id, vardiyaId, islem, tarih'
        });

        this.version(3).stores({
            marketVardiyalar: '++id, tarih',
            marketVardiyaPersonel: '++id, vardiyaId, personelId',
            marketZRaporu: '++id, vardiyaId'
        });

        this.version(4).stores({
            marketGelirler: '++id, vardiyaId'
        });
    }
}

@Injectable({
    providedIn: 'root'
})
export class DbService {
    private db: IstasyonDB;

    constructor() {
        this.db = new IstasyonDB();
        this.baslangicVerileriniYukle();
    }

    private async baslangicVerileriniYukle() {
        if (await this.db.istasyonlar.count() === 0) {
            await this.db.istasyonlar.add({
                ad: 'Merkez İstasyon',
                kod: 'IST001',
                adres: 'Merkez Mah.',
                pompaSayisi: 12,
                marketVar: true,
                aktif: true
            });
        }

        // Personelleri kontrol et ve eksikleri ekle
        const varsayilanPersoneller: Omit<DBPersonel, 'id'>[] = [
            { keyId: 'M001', ad: 'MARKET', soyad: '', tamAd: 'Market Sorumlusu', istasyonId: 1, rol: 'MARKET_SORUMLUSU', aktif: true },
            { keyId: 'MG01', ad: 'Ayşe', soyad: 'Yılmaz', tamAd: 'Ayşe Yılmaz', istasyonId: 1, rol: 'MARKET_GOREVLISI', aktif: true },
            { keyId: 'MG02', ad: 'Fatma', soyad: 'Demir', tamAd: 'Fatma Demir', istasyonId: 1, rol: 'MARKET_GOREVLISI', aktif: true },
            { keyId: 'MG03', ad: 'Ali', soyad: 'Kaya', tamAd: 'Ali Kaya', istasyonId: 1, rol: 'MARKET_GOREVLISI', aktif: true },
            { keyId: 'V001', ad: 'VARDIYA', soyad: '', tamAd: 'Vardiya Sorumlusu', istasyonId: 1, rol: 'VARDIYA_SORUMLUSU', aktif: true },
            { keyId: 'Y001', ad: 'ONAY', soyad: '', tamAd: 'İstasyon Sahibi', istasyonId: 1, rol: 'YONETICI', aktif: true }
        ];

        for (const p of varsayilanPersoneller) {
            const exists = await this.db.personeller.where('keyId').equals(p.keyId).first();
            if (!exists) {
                await this.db.personeller.add(p);
            }
        }
    }

    // ==========================================
    // TANIMLAMALAR (İstasyon / Personel)
    // ==========================================

    async getIstasyonlar(): Promise<DBIstasyon[]> {
        return this.db.istasyonlar.toArray();
    }

    async getPersoneller(istasyonId?: number): Promise<DBPersonel[]> {
        if (istasyonId) {
            return this.db.personeller.where('istasyonId').equals(istasyonId).toArray();
        }
        return this.db.personeller.toArray();
    }

    async getPersonelByKey(keyId: string): Promise<DBPersonel | undefined> {
        return this.db.personeller.where('keyId').equals(keyId).first();
    }

    async personelEkle(personel: Omit<DBPersonel, 'id'>): Promise<number> {
        return this.db.personeller.add(personel);
    }

    async getPersonel(id: number): Promise<DBPersonel | undefined> {
        return this.db.personeller.get(id);
    }

    async personelGuncelle(id: number, changes: Partial<DBPersonel>): Promise<void> {
        await this.db.personeller.update(id, changes);
    }

    async personelSil(id: number): Promise<void> {
        await this.db.personeller.delete(id);
    }

    // ==========================================
    // İSTASYON İŞLEMLERİ
    // ==========================================

    async istasyonEkle(istasyon: Omit<DBIstasyon, 'id'>): Promise<number> {
        return this.db.istasyonlar.add(istasyon);
    }

    async istasyonGuncelle(id: number, changes: Partial<DBIstasyon>): Promise<void> {
        await this.db.istasyonlar.update(id, changes);
    }

    async istasyonSil(id: number): Promise<void> {
        await this.db.istasyonlar.delete(id);
    }

    // ==========================================
    // VARDİYA İŞLEMLERİ
    // ==========================================

    async vardiyaEkle(vardiya: Omit<DBVardiya, 'id'>): Promise<number> {
        const id = await this.db.vardiyalar.add(vardiya);
        await this.onayLogEkle({
            vardiyaId: id,
            islem: 'OLUSTURULDU',
            aciklama: `${vardiya.dosyaAdi} dosyası yüklendi`,
            tarih: new Date()
        });
        return id;
    }

    async vardiyaGuncelle(id: number, changes: Partial<DBVardiya>): Promise<void> {
        await this.db.vardiyalar.update(id, changes);
    }

    async vardiyaSil(id: number): Promise<void> {
        // İlişkili verileri de sil
        await this.db.satislar.where('vardiyaId').equals(id).delete();
        await this.db.pusulalar.where('vardiyaId').equals(id).delete();
        await this.db.giderler.where('vardiyaId').equals(id).delete();
        await this.db.onayLoglar.where('vardiyaId').equals(id).delete();
        await this.db.vardiyalar.delete(id);
    }

    async vardiyaGetir(id: number): Promise<DBVardiya | undefined> {
        return this.db.vardiyalar.get(id);
    }

    async tumVardiyalariGetir(): Promise<DBVardiya[]> {
        return this.db.vardiyalar.orderBy('yuklemeTarihi').reverse().toArray();
    }

    async bekleyenVardiyalar(): Promise<DBVardiya[]> {
        return this.db.vardiyalar.where('durum').equals('ACIK').toArray();
    }

    async onayBekleyenVardiyalar(): Promise<DBVardiya[]> {
        return this.db.vardiyalar.where('durum').equals('ONAY_BEKLIYOR').toArray();
    }

    // ==========================================
    // SATIŞ İŞLEMLERİ
    // ==========================================

    async satislarEkle(satislar: Omit<DBSatis, 'id'>[]): Promise<void> {
        await this.db.satislar.bulkAdd(satislar);
    }

    async vardiyaSatislariGetir(vardiyaId: number): Promise<DBSatis[]> {
        return this.db.satislar.where('vardiyaId').equals(vardiyaId).toArray();
    }

    async personelOzetGetir(vardiyaId: number): Promise<{
        personelId: number;
        personelAdi: string;
        personelKeyId: string;
        toplamLitre: number;
        toplamTutar: number;
    }[]> {
        const satislar = await this.vardiyaSatislariGetir(vardiyaId);
        const personelMap = new Map<number, {
            personelId: number;
            personelAdi: string;
            personelKeyId: string;
            toplamLitre: number;
            toplamTutar: number;
        }>();

        satislar.forEach(satis => {
            if (!personelMap.has(satis.personelId)) {
                personelMap.set(satis.personelId, {
                    personelId: satis.personelId,
                    personelAdi: satis.personelAdi,
                    personelKeyId: satis.personelKeyId,
                    toplamLitre: 0,
                    toplamTutar: 0
                });
            }
            const ozet = personelMap.get(satis.personelId)!;
            ozet.toplamLitre += satis.litre;
            ozet.toplamTutar += satis.toplamTutar;
        });

        return Array.from(personelMap.values());
    }

    // ==========================================
    // PUSULA İŞLEMLERİ
    // ==========================================

    async pusulaEkle(pusula: Omit<DBPusula, 'id'>): Promise<number> {
        // Aynı personel için varsa güncelle
        const mevcut = await this.db.pusulalar
            .where('vardiyaId')
            .equals(pusula.vardiyaId)
            .and(p => p.personelId === pusula.personelId)
            .first();

        if (mevcut) {
            await this.db.pusulalar.update(mevcut.id!, pusula);
            return mevcut.id!;
        }
        return this.db.pusulalar.add(pusula);
    }

    async pusulaSil(id: number): Promise<void> {
        await this.db.pusulalar.delete(id);
    }

    async vardiyaPusulalariGetir(vardiyaId: number): Promise<DBPusula[]> {
        return this.db.pusulalar.where('vardiyaId').equals(vardiyaId).toArray();
    }

    // ==========================================
    // GİDER İŞLEMLERİ
    // ==========================================

    async giderEkle(gider: Omit<DBGider, 'id'>): Promise<number> {
        return this.db.giderler.add(gider);
    }

    async giderSil(id: number): Promise<void> {
        await this.db.giderler.delete(id);
    }

    async vardiyaGiderlerGetir(vardiyaId: number): Promise<DBGider[]> {
        return this.db.giderler.where('vardiyaId').equals(vardiyaId).toArray();
    }

    // ==========================================
    // MUTABAKAT KONTROLÜ
    // ==========================================

    async mutabakatDurumu(vardiyaId: number): Promise<{
        tamamlandi: boolean;
        personelSayisi: number;
        pusulaGirilenSayisi: number;
        eksikPersoneller: string[];
        toplamOtomasyon: number;
        toplamPusula: number;
        fark: number;
    }> {
        const personelOzet = await this.personelOzetGetir(vardiyaId);
        const pusulalar = await this.vardiyaPusulalariGetir(vardiyaId);

        const personelIds = personelOzet.map(p => p.personelId);
        const pusulaPersonelIds = pusulalar.map(p => p.personelId);

        const eksikPersonelIds = personelIds.filter(id => !pusulaPersonelIds.includes(id));
        const eksikPersoneller = personelOzet
            .filter(p => eksikPersonelIds.includes(p.personelId))
            .map(p => p.personelAdi);

        const toplamOtomasyon = personelOzet.reduce((sum, p) => sum + p.toplamTutar, 0);
        const toplamPusula = pusulalar.reduce((sum, p) => sum + p.toplam, 0);

        return {
            tamamlandi: eksikPersoneller.length === 0,
            personelSayisi: personelOzet.length,
            pusulaGirilenSayisi: pusulalar.length,
            eksikPersoneller,
            toplamOtomasyon,
            toplamPusula,
            fark: toplamPusula - toplamOtomasyon
        };
    }


    // ==========================================
    // MARKET GELİR İŞLEMLERİ
    // ==========================================

    async marketGelirEkle(gelir: Omit<DBMarketGelir, 'id'>): Promise<number> {
        return this.db.marketGelirler.add(gelir);
    }

    async marketGelirSil(id: number): Promise<void> {
        await this.db.marketGelirler.delete(id);
    }

    async vardiyaGelirleriGetir(vardiyaId: number): Promise<DBMarketGelir[]> {
        return this.db.marketGelirler.where('vardiyaId').equals(vardiyaId).toArray();
    }

    async getOnayBekleyenMarketVardiyalar(): Promise<DBMarketVardiya[]> {
        return this.db.marketVardiyalar.where('durum').equals('ONAY_BEKLIYOR').toArray();
    }

    async marketVardiyaOnayaGonder(vardiyaId: number): Promise<void> {
        await this.db.marketVardiyalar.update(vardiyaId, {
            durum: 'ONAY_BEKLIYOR'
        });

        // Log ekle
        await this.onayLogEkle({
            vardiyaId, // Market vardiya ID'si ile normal vardiya ID çakışabilir, bunu ayrıştırmak gerekebilir ama şimdilik ID üzerinden gidiyoruz
            islem: 'ONAYA_GONDERILDI',
            aciklama: 'Market Vardiyası Onaya Gönderildi',
            tarih: new Date()
        });
    }

    async marketVardiyaOnayla(vardiyaId: number, onaylayanId: number, onaylayanAdi: string): Promise<void> {
        await this.db.marketVardiyalar.update(vardiyaId, {
            durum: 'ONAYLANDI',
            onaylayanId,
            onaylayanAdi,
            onayTarihi: new Date()
        });
    }

    async marketVardiyaReddet(vardiyaId: number, neden: string): Promise<void> {
        await this.db.marketVardiyalar.update(vardiyaId, {
            durum: 'REDDEDILDI',
            redNedeni: neden,
            onayTarihi: new Date()
        });
    }

    // ==========================================
    // ONAY İŞLEMLERİ
    // ==========================================

    async onayaGonder(vardiyaId: number): Promise<boolean> {
        const durum = await this.mutabakatDurumu(vardiyaId);

        if (!durum.tamamlandi) {
            console.warn('Mutabakat tamamlanmamış. Eksik personeller:', durum.eksikPersoneller);
            return false;
        }

        await this.db.vardiyalar.update(vardiyaId, {
            durum: 'ONAY_BEKLIYOR',
            mutabakatTarihi: new Date()
        });

        await this.onayLogEkle({
            vardiyaId,
            islem: 'ONAYA_GONDERILDI',
            aciklama: `Toplam: ₺${durum.toplamPusula.toFixed(2)}, Fark: ₺${durum.fark.toFixed(2)}`,
            tarih: new Date()
        });

        return true;
    }

    async onayla(vardiyaId: number, onaylayanId: number, onaylayanAdi: string): Promise<void> {
        await this.db.vardiyalar.update(vardiyaId, {
            durum: 'ONAYLANDI',
            onayTarihi: new Date(),
            onaylayanId,
            onaylayanAdi
        });

        await this.onayLogEkle({
            vardiyaId,
            islem: 'ONAYLANDI',
            kullaniciId: onaylayanId,
            kullaniciAdi: onaylayanAdi,
            tarih: new Date()
        });
    }

    async reddet(vardiyaId: number, onaylayanId: number, onaylayanAdi: string, neden: string): Promise<void> {
        await this.db.vardiyalar.update(vardiyaId, {
            durum: 'REDDEDILDI',
            onayTarihi: new Date(),
            onaylayanId,
            onaylayanAdi,
            redNedeni: neden
        });

        await this.onayLogEkle({
            vardiyaId,
            islem: 'REDDEDILDI',
            kullaniciId: onaylayanId,
            kullaniciAdi: onaylayanAdi,
            aciklama: neden,
            tarih: new Date()
        });
    }

    // ==========================================
    // LOG İŞLEMLERİ
    // ==========================================

    async onayLogEkle(log: Omit<DBOnayLog, 'id'>): Promise<number> {
        return this.db.onayLoglar.add(log);
    }

    async vardiyaLoglarGetir(vardiyaId: number): Promise<DBOnayLog[]> {
        return this.db.onayLoglar.where('vardiyaId').equals(vardiyaId).toArray();
    }

    // ==========================================
    // İSTATİSTİKLER
    // ==========================================

    async istatistikler(): Promise<{
        toplamVardiya: number;
        bekleyenMutabakat: number;
        onayBekleyen: number;
        onaylanan: number;
        toplamCiro: number;
    }> {
        const vardiyalar = await this.tumVardiyalariGetir();

        return {
            toplamVardiya: vardiyalar.length,
            bekleyenMutabakat: vardiyalar.filter(v => v.durum === 'ACIK').length,
            onayBekleyen: vardiyalar.filter(v => v.durum === 'ONAY_BEKLIYOR').length,
            onaylanan: vardiyalar.filter(v => v.durum === 'ONAYLANDI').length,
            toplamCiro: vardiyalar.reduce((sum, v) => sum + v.toplamTutar, 0)
        };
    }

    // ==========================================
    // RAPORLAR
    // ==========================================

    async getVardiyaRaporu(baslangic: Date, bitis: Date): Promise<{
        ozet: {
            toplamVardiya: number;
            toplamTutar: number;
            toplamLitre: number;
            toplamIade: number;
            toplamGider: number;
        };
        vardiyalar: {
            tarih: Date;
            dosyaAdi: string;
            tutar: number;
            durum: string;
        }[];
    }> {
        // Tarih aralığındaki vardiyaları bul (başlangıç ve bitiş dahil)
        // Dexie'de tarih sorgusu yaparken sadece tarih kısmına bakmak zor olabilir, 
        // bu yüzden geniş bir aralık çekip JS tarafında filtreleyeceğiz veya
        // tam timestamp karşılaştırması yapacağız.
        const vardiyalar = await this.db.vardiyalar
            .where('yuklemeTarihi')
            .between(baslangic, bitis, true, true)
            .toArray();

        // Satışları da çekelim (litre hesabı için)
        // Performans için sadece ilgili vardiyaların satışlarını çekmek daha iyi olur
        // Ancak karmaşık sorgular yerine JS'de hesaplamak şimdilik daha güvenli

        const vardiyaIds = vardiyalar.map(v => v.id!);
        const satislar = await this.db.satislar
            .where('vardiyaId')
            .anyOf(vardiyaIds)
            .toArray();

        const giderler = await this.db.giderler
            .where('vardiyaId')
            .anyOf(vardiyaIds)
            .toArray();

        const ozet = {
            toplamVardiya: vardiyalar.length,
            toplamTutar: vardiyalar.reduce((sum, v) => sum + v.toplamTutar, 0),
            toplamLitre: satislar.reduce((sum, s) => sum + s.litre, 0),
            toplamIade: 0, // İade mantığı henüz yok
            toplamGider: giderler.reduce((sum, g) => sum + g.tutar, 0)
        };

        const raporVardiyalar = vardiyalar.map(v => ({
            tarih: v.yuklemeTarihi,
            dosyaAdi: v.dosyaAdi,
            tutar: v.toplamTutar,
            durum: v.durum
        }));

        return { ozet, vardiyalar: raporVardiyalar };
    }

    async getPersonelKarnesi(personelId: number, baslangic: Date, bitis: Date): Promise<{
        personel: DBPersonel | undefined;
        hareketler: {
            tarih: Date;
            vardiyaId: number;
            otomasyonSatis: number;
            manuelTahsilat: number;
            fark: number;
            aracSayisi: number;
            litre: number;
            aciklama?: string;
        }[];
        ozet: {
            toplamSatis: number;
            toplamTahsilat: number;
            toplamFark: number;
            toplamLitre: number;
            aracSayisi: number;
            ortalamaLitre: number;
            ortalamaTutar: number;
            yakitDagilimi: { yakit: string, litre: number, tutar: number, oran: number }[];
        }
    }> {
        const personel = await this.db.personeller.get(personelId);

        if (!personel) {
            return {
                personel: undefined,
                hareketler: [],
                ozet: {
                    toplamSatis: 0,
                    toplamTahsilat: 0,
                    toplamFark: 0,
                    toplamLitre: 0,
                    aracSayisi: 0,
                    ortalamaLitre: 0,
                    ortalamaTutar: 0,
                    yakitDagilimi: []
                }
            };
        }

        // MARKET PERSONELİ İŞLEMLERİ
        if (personel.rol === 'MARKET_GOREVLISI' || personel.rol === 'MARKET_SORUMLUSU') {
            const marketVardiyalar = await this.db.marketVardiyalar
                .where('tarih')
                .between(baslangic, bitis, true, true)
                .toArray();

            const vardiyaIds = marketVardiyalar.map(v => v.id!);

            const personelIslemler = await this.db.marketVardiyaPersonel
                .where('vardiyaId')
                .anyOf(vardiyaIds)
                .filter(p => p.personelId === personelId)
                .toArray();

            const hareketler = marketVardiyalar.map(v => {
                const islem = personelIslemler.find(p => p.vardiyaId === v.id);
                if (!islem) return null;

                const otomasyonSatis = islem.sistemSatisTutari;
                const manuelTahsilat = (islem.nakit || 0) + (islem.krediKarti || 0); // veya islem.toplamTeslimat
                const fark = manuelTahsilat - otomasyonSatis;

                return {
                    tarih: v.tarih,
                    vardiyaId: v.id!,
                    otomasyonSatis,
                    manuelTahsilat,
                    fark,
                    litre: 0,
                    aracSayisi: 0,
                    aciklama: islem.aciklama
                };
            }).filter((h): h is NonNullable<typeof h> => h !== null)
                .filter(h => h.otomasyonSatis > 0 || h.manuelTahsilat > 0);

            const toplamSatis = hareketler.reduce((sum, h) => sum + h.otomasyonSatis, 0);
            const toplamTahsilat = hareketler.reduce((sum, h) => sum + h.manuelTahsilat, 0);
            const toplamFark = hareketler.reduce((sum, h) => sum + h.fark, 0);

            return {
                personel,
                hareketler,
                ozet: {
                    toplamSatis,
                    toplamTahsilat,
                    toplamFark,
                    toplamLitre: 0,
                    aracSayisi: 0,
                    ortalamaLitre: 0,
                    ortalamaTutar: hareketler.length > 0 ? toplamSatis / hareketler.length : 0,
                    yakitDagilimi: []
                }
            };
        }

        // POMPACI / VARDIYA SORUMLUSU İŞLEMLERİ (MEVCUT MANTIK)
        // Tarih aralığındaki vardiyalar
        const vardiyalar = await this.db.vardiyalar
            .where('yuklemeTarihi')
            .between(baslangic, bitis, true, true)
            .toArray();

        const vardiyaIds = vardiyalar.map(v => v.id!);

        // İlgili satışlar
        const satislar = await this.db.satislar
            .where('vardiyaId').anyOf(vardiyaIds)
            .and(s => s.personelId === personelId)
            .toArray();

        // İlgili pusulalar (tahsilatlar)
        const pusulalar = await this.db.pusulalar
            .where('vardiyaId').anyOf(vardiyaIds)
            .and(p => p.personelId === personelId)
            .toArray();

        // Yakıt Dağılımı Hesaplama
        const yakitMap = new Map<string, { litre: number, tutar: number }>();
        let globalToplamLitre = 0;

        satislar.forEach(s => {
            if (!yakitMap.has(s.yakitTuru)) {
                yakitMap.set(s.yakitTuru, { litre: 0, tutar: 0 });
            }
            const yakit = yakitMap.get(s.yakitTuru)!;
            yakit.litre += s.litre;
            yakit.tutar += s.toplamTutar;
            globalToplamLitre += s.litre;
        });

        const yakitDagilimi = Array.from(yakitMap.entries()).map(([yakit, data]) => ({
            yakit,
            litre: data.litre,
            tutar: data.tutar,
            oran: globalToplamLitre > 0 ? (data.litre / globalToplamLitre) * 100 : 0
        })).sort((a, b) => b.litre - a.litre);

        const hareketler = vardiyalar.map(v => {
            const vSatislar = satislar.filter(s => s.vardiyaId === v.id);
            const vPusula = pusulalar.find(p => p.vardiyaId === v.id);

            const otomasyonSatis = vSatislar.reduce((sum, s) => sum + s.toplamTutar, 0);
            const litre = vSatislar.reduce((sum, s) => sum + s.litre, 0);
            const aracSayisi = vSatislar.length;
            const manuelTahsilat = vPusula ? vPusula.toplam : 0;
            const fark = manuelTahsilat - otomasyonSatis;

            return {
                tarih: v.yuklemeTarihi,
                vardiyaId: v.id!,
                otomasyonSatis,
                manuelTahsilat,
                fark,
                litre,
                aracSayisi,
                aciklama: vPusula?.krediKartiDetay ? `${vPusula.krediKartiDetay.length} adet KK` : undefined
            };
        }).filter(h => h.otomasyonSatis > 0 || h.manuelTahsilat > 0); // Hareketsiz günleri gösterme

        const toplamSatis = hareketler.reduce((sum, h) => sum + h.otomasyonSatis, 0);
        const toplamTahsilat = hareketler.reduce((sum, h) => sum + h.manuelTahsilat, 0);
        const toplamFark = hareketler.reduce((sum, h) => sum + h.fark, 0);
        const toplamLitre = hareketler.reduce((sum, h) => sum + h.litre, 0);
        const toplamArac = hareketler.reduce((sum, h) => sum + h.aracSayisi, 0);

        const ozet = {
            toplamSatis,
            toplamTahsilat,
            toplamFark,
            toplamLitre,
            aracSayisi: toplamArac,
            ortalamaLitre: toplamArac > 0 ? toplamLitre / toplamArac : 0,
            ortalamaTutar: toplamArac > 0 ? toplamSatis / toplamArac : 0,
            yakitDagilimi
        };

        return { personel, hareketler, ozet };
    }

    async getFarkRaporu(baslangic: Date, bitis: Date): Promise<{
        ozet: {
            toplamFark: number;
            toplamAcik: number;
            toplamFazla: number;
            vardiyaSayisi: number;
            acikVardiyaSayisi: number;
            fazlaVardiyaSayisi: number;
        };
        vardiyalar: {
            vardiyaId: number;
            tarih: Date;
            dosyaAdi: string;
            otomasyonToplam: number;
            tahsilatToplam: number;
            fark: number;
            durum: string;
            personelFarklari: {
                personelAdi: string;
                personelKeyId: string;
                otomasyon: number;
                tahsilat: number;
                fark: number;
            }[];
        }[];
    }> {
        const vardiyalar = await this.db.vardiyalar
            .where('yuklemeTarihi')
            .between(baslangic, bitis, true, true)
            .toArray();

        const vardiyaIds = vardiyalar.map(v => v.id!);
        const satislar = await this.db.satislar.where('vardiyaId').anyOf(vardiyaIds).toArray();
        const pusulalar = await this.db.pusulalar.where('vardiyaId').anyOf(vardiyaIds).toArray();

        // Satışlardan personelId -> personelKeyId eşleştirmesi oluştur (bu daha güvenilir)
        const personelIdToKeyFromSales = new Map<number, string>();
        satislar.forEach(s => {
            personelIdToKeyFromSales.set(s.personelId, s.personelKeyId);
        });

        const vardiyaDetaylari = vardiyalar.map(v => {
            const vSatislar = satislar.filter(s => s.vardiyaId === v.id);
            const vPusulalar = pusulalar.filter(p => p.vardiyaId === v.id);

            // Personel bazında grupla
            const personelMap = new Map<string, { personelAdi: string, personelKeyId: string, otomasyon: number, tahsilat: number }>();

            vSatislar.forEach(s => {
                const key = s.personelKeyId;
                if (!personelMap.has(key)) {
                    personelMap.set(key, { personelAdi: s.personelAdi, personelKeyId: s.personelKeyId, otomasyon: 0, tahsilat: 0 });
                }
                personelMap.get(key)!.otomasyon += s.toplamTutar;
            });

            vPusulalar.forEach(p => {
                const key = personelIdToKeyFromSales.get(p.personelId) || `UNKNOWN_${p.personelId}`;
                if (!personelMap.has(key)) {
                    personelMap.set(key, { personelAdi: p.personelAdi, personelKeyId: key, otomasyon: 0, tahsilat: 0 });
                }
                personelMap.get(key)!.tahsilat += p.toplam;
            });

            const personelFarklari = Array.from(personelMap.values()).map(p => ({
                personelAdi: p.personelAdi,
                personelKeyId: p.personelKeyId,
                otomasyon: p.otomasyon,
                tahsilat: p.tahsilat,
                fark: p.tahsilat - p.otomasyon
            }));

            const otomasyonToplam = vSatislar.reduce((sum, s) => sum + s.toplamTutar, 0);
            const tahsilatToplam = vPusulalar.reduce((sum, p) => sum + p.toplam, 0);
            const fark = tahsilatToplam - otomasyonToplam;

            return {
                vardiyaId: v.id!,
                tarih: v.yuklemeTarihi,
                dosyaAdi: v.dosyaAdi,
                otomasyonToplam,
                tahsilatToplam,
                fark,
                durum: v.durum,
                personelFarklari
            };
        });

        const toplamFark = vardiyaDetaylari.reduce((sum, v) => sum + v.fark, 0);
        const toplamAcik = vardiyaDetaylari.reduce((sum, v) => sum + (v.fark < 0 ? Math.abs(v.fark) : 0), 0);
        const toplamFazla = vardiyaDetaylari.reduce((sum, v) => sum + (v.fark > 0 ? v.fark : 0), 0);
        const acikVardiyaSayisi = vardiyaDetaylari.filter(v => v.fark < -0.01).length;
        const fazlaVardiyaSayisi = vardiyaDetaylari.filter(v => v.fark > 0.01).length;

        return {
            ozet: {
                toplamFark,
                toplamAcik,
                toplamFazla,
                vardiyaSayisi: vardiyaDetaylari.length,
                acikVardiyaSayisi,
                fazlaVardiyaSayisi
            },
            vardiyalar: vardiyaDetaylari.sort((a, b) => Math.abs(b.fark) - Math.abs(a.fark))
        };
    }

    // Veritabanını temizle (test için)
    async temizle(): Promise<void> {
        await this.db.vardiyalar.clear();
        await this.db.satislar.clear();
        await this.db.pusulalar.clear();
        await this.db.giderler.clear();
        await this.db.onayLoglar.clear();
        await this.db.marketVardiyalar.clear();
        await this.db.marketGelirler.clear();
        await this.db.marketVardiyaPersonel.clear();
        await this.db.marketZRaporu.clear();
    }

    // ==========================================
    // MARKET METODLARI
    // ==========================================

    async marketVardiyaEkle(vardiya: Omit<DBMarketVardiya, 'id'>): Promise<number> {
        return this.db.marketVardiyalar.add(vardiya);
    }

    async marketVardiyalarGetir(): Promise<DBMarketVardiya[]> {
        return this.db.marketVardiyalar.orderBy('tarih').reverse().toArray();
    }

    async marketVardiyaGuncelle(id: number, changes: Partial<DBMarketVardiya>): Promise<void> {
        await this.db.marketVardiyalar.update(id, changes);
    }

    async marketPersonelIslemEkle(islem: Omit<DBMarketVardiyaPersonel, 'id'>): Promise<number> {
        // Varsa güncelle
        const mevcut = await this.db.marketVardiyaPersonel
            .where('vardiyaId').equals(islem.vardiyaId)
            .and(p => p.personelId === islem.personelId)
            .first();

        if (mevcut) {
            await this.db.marketVardiyaPersonel.update(mevcut.id!, islem);
            return mevcut.id!;
        }
        return this.db.marketVardiyaPersonel.add(islem);
    }

    async marketPersonelIslemleriGetir(vardiyaId: number): Promise<DBMarketVardiyaPersonel[]> {
        return this.db.marketVardiyaPersonel.where('vardiyaId').equals(vardiyaId).toArray();
    }

    async marketZRaporuKaydet(zRaporu: Omit<DBMarketZRaporu, 'id'>): Promise<number> {
        const mevcut = await this.db.marketZRaporu.where('vardiyaId').equals(zRaporu.vardiyaId).first();
        if (mevcut) {
            await this.db.marketZRaporu.update(mevcut.id!, zRaporu);
            return mevcut.id!;
        }
        return this.db.marketZRaporu.add(zRaporu);
    }

    async marketZRaporuGetir(vardiyaId: number): Promise<DBMarketZRaporu | undefined> {
        return this.db.marketZRaporu.where('vardiyaId').equals(vardiyaId).first();
    }
}
