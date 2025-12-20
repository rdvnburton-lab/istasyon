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
    veresiye: number;
    filoKarti: number;
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

// Dexie Veritabanı Sınıfı
class IstasyonDB extends Dexie {
    istasyonlar!: Table<DBIstasyon>;
    personeller!: Table<DBPersonel>;
    vardiyalar!: Table<DBVardiya>;
    satislar!: Table<DBSatis>;
    pusulalar!: Table<DBPusula>;
    giderler!: Table<DBGider>;
    onayLoglar!: Table<DBOnayLog>;

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

        if (await this.db.personeller.count() === 0) {
            const varsayilanPersoneller: Omit<DBPersonel, 'id'>[] = [
                { keyId: 'P001', ad: 'A.', soyad: 'VURAL', tamAd: 'A. VURAL', istasyonId: 1, rol: 'POMPACI', aktif: true },
                { keyId: 'P002', ad: 'E.', soyad: 'AKCA', tamAd: 'E. AKCA', istasyonId: 1, rol: 'POMPACI', aktif: true },
                { keyId: 'P003', ad: 'M.', soyad: 'DUMDUZ', tamAd: 'M. DUMDUZ', istasyonId: 1, rol: 'POMPACI', aktif: true },
                { keyId: 'P004', ad: 'F.', soyad: 'BAYLAS', tamAd: 'F. BAYLAS', istasyonId: 1, rol: 'POMPACI', aktif: true },
                { keyId: 'M001', ad: 'Market', soyad: 'Sorumlusu', tamAd: 'Market Sorumlusu', istasyonId: 1, rol: 'MARKET_SORUMLUSU', aktif: true },
                { keyId: 'V001', ad: 'Vardiya', soyad: 'Sorumlusu', tamAd: 'Vardiya Sorumlusu', istasyonId: 1, rol: 'VARDIYA_SORUMLUSU', aktif: true },
                { keyId: 'Y001', ad: 'İstasyon', soyad: 'Sahibi', tamAd: 'İstasyon Sahibi', istasyonId: 1, rol: 'YONETICI', aktif: true }
            ];
            await this.db.personeller.bulkAdd(varsayilanPersoneller);
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

    // Veritabanını temizle (test için)
    async temizle(): Promise<void> {
        await this.db.vardiyalar.clear();
        await this.db.satislar.clear();
        await this.db.pusulalar.clear();
        await this.db.giderler.clear();
        await this.db.onayLoglar.clear();
    }
}
