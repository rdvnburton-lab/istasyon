import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, delay } from 'rxjs';
import {
    Vardiya,
    VardiyaDurum,
    Istasyon,
    Personel,
    PersonelRol,
    OtomasyonSatis,
    YakitTuru,
    PusulaGirisi,
    PersonelFarkAnalizi,
    FarkDurum,
    PersonelOtomasyonOzet,
    MarketZRaporu,
    MarketTahsilat,
    MarketGider,
    GiderTuru,
    MarketOzet,
    PompaGider,
    PompaGiderTuru,
    PompaOzet,
    VardiyaOzet,
    GenelOzet,
    VardiyaOnayTalebi,
    GunlukOzet,
    DashboardVeri,
    KasaAlarm,
    PersonelKarne,
    // Eski uyumluluk
    Tahsilat,
    OdemeYontemi,
    KarsilastirmaSonuc,
    KarsilastirmaDurum,
    KarsilastirmaDetay,
    PompaSatis,
    PompaSatisOzet,
    TahsilatOzet,
    Operator,
    FiloSatis
} from '../models/vardiya.model';
import { DbService, DBVardiya, DBSatis, DBPusula, DBGider } from './db.service';

@Injectable({
    providedIn: 'root'
})
export class VardiyaService {
    constructor(private dbService: DbService) { }

    private aktifVardiya = new BehaviorSubject<Vardiya | null>(null);
    private pusulaGirisleri = new BehaviorSubject<PusulaGirisi[]>([]);
    private marketZRaporu = new BehaviorSubject<MarketZRaporu | null>(null);
    private marketTahsilat = new BehaviorSubject<MarketTahsilat | null>(null);
    private marketGiderler = new BehaviorSubject<MarketGider[]>([]);
    private pompaGiderler = new BehaviorSubject<PompaGider[]>([]);

    // Yüklenen dosyadan gelen satışlar
    private yuklenenSatislar = new BehaviorSubject<OtomasyonSatis[]>([]);

    // Eski uyumluluk için
    private tahsilatlar = new BehaviorSubject<Tahsilat[]>([]);

    // ==========================================
    // MOCK VERİLER
    // ==========================================

    private mockIstasyonlar: Istasyon[] = [
        { id: 1, ad: 'Shell Merkez', kod: 'SHL001', adres: 'Atatürk Cad. No:1', pompaSayisi: 8, marketVar: true, aktif: true },
        { id: 2, ad: 'BP Sanayi', kod: 'BP002', adres: 'Sanayi Mah. No:25', pompaSayisi: 6, marketVar: true, aktif: true }
    ];

    private mockPersoneller: Personel[] = [
        { id: 1, keyId: 'P001', ad: 'A.', soyad: 'VURAL', tamAd: 'A. VURAL', istasyonId: 1, rol: PersonelRol.POMPACI, aktif: true },
        { id: 2, keyId: 'P002', ad: 'E.', soyad: 'AKCA', tamAd: 'E. AKCA', istasyonId: 1, rol: PersonelRol.POMPACI, aktif: true },
        { id: 3, keyId: 'P003', ad: 'M.', soyad: 'DUMDUZ', tamAd: 'M. DUMDUZ', istasyonId: 1, rol: PersonelRol.POMPACI, aktif: true },
        { id: 4, keyId: 'P004', ad: 'F.', soyad: 'BAYLAS', tamAd: 'F. BAYLAS', istasyonId: 1, rol: PersonelRol.POMPACI, aktif: true },
        { id: 5, keyId: 'M001', ad: 'Market', soyad: 'Sorumlusu', tamAd: 'Market Sorumlusu', istasyonId: 1, rol: PersonelRol.MARKET_SORUMLUSU, aktif: true },
        { id: 6, keyId: 'V001', ad: 'Vardiya', soyad: 'Sorumlusu', tamAd: 'Vardiya Sorumlusu', istasyonId: 1, rol: PersonelRol.VARDIYA_SORUMLUSU, aktif: true },
        { id: 7, keyId: 'Y001', ad: 'İstasyon', soyad: 'Sahibi', tamAd: 'İstasyon Sahibi', istasyonId: 1, rol: PersonelRol.YONETICI, aktif: true }
    ];

    // Otomasyon verileri - 17.12.2025 Gece Vardiyası (Gerçek Veri)
    private mockOtomasyonSatislari: OtomasyonSatis[] = [
        // A. VURAL - LPG Satışları
        { id: 1, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 14, yakitTuru: YakitTuru.LPG, litre: 12.21, birimFiyat: 25.99, toplamTutar: 317.34, satisTarihi: new Date('2025-12-17T00:02:13') },
        { id: 2, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 13, yakitTuru: YakitTuru.LPG, litre: 18.06, birimFiyat: 25.99, toplamTutar: 469.38, satisTarihi: new Date('2025-12-17T00:02:37') },
        { id: 3, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 12, yakitTuru: YakitTuru.LPG, litre: 15.39, birimFiyat: 25.99, toplamTutar: 400.00, satisTarihi: new Date('2025-12-17T00:02:43') },
        { id: 4, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 13, yakitTuru: YakitTuru.LPG, litre: 25.01, birimFiyat: 25.99, toplamTutar: 650.00, satisTarihi: new Date('2025-12-17T00:13:40') },
        { id: 5, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 14, yakitTuru: YakitTuru.LPG, litre: 27.86, birimFiyat: 25.99, toplamTutar: 724.08, satisTarihi: new Date('2025-12-17T00:41:37') },
        { id: 6, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 14, yakitTuru: YakitTuru.LPG, litre: 36.39, birimFiyat: 25.99, toplamTutar: 945.78, satisTarihi: new Date('2025-12-17T03:08:02') },
        // A. VURAL - Motorin Satışları
        { id: 7, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 3, yakitTuru: YakitTuru.MOTORIN, litre: 26.59, birimFiyat: 54.16, toplamTutar: 1440.00, satisTarihi: new Date('2025-12-17T00:17:37') },
        { id: 8, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 7, yakitTuru: YakitTuru.MOTORIN, litre: 18.46, birimFiyat: 54.16, toplamTutar: 1000.00, satisTarihi: new Date('2025-12-17T00:43:43') },
        { id: 9, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 4, yakitTuru: YakitTuru.MOTORIN, litre: 53.73, birimFiyat: 54.16, toplamTutar: 2910.00, satisTarihi: new Date('2025-12-17T01:24:12') },
        { id: 10, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 3, yakitTuru: YakitTuru.MOTORIN, litre: 27.70, birimFiyat: 54.16, toplamTutar: 1500.00, satisTarihi: new Date('2025-12-17T02:16:11') },
        { id: 11, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 5, yakitTuru: YakitTuru.MOTORIN, litre: 49.85, birimFiyat: 54.16, toplamTutar: 2700.00, satisTarihi: new Date('2025-12-17T05:40:56') },
        { id: 12, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 3, yakitTuru: YakitTuru.MOTORIN, litre: 42.47, birimFiyat: 54.16, toplamTutar: 2300.00, satisTarihi: new Date('2025-12-17T05:59:31') },
        { id: 13, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 3, yakitTuru: YakitTuru.MOTORIN, litre: 36.93, birimFiyat: 54.16, toplamTutar: 2000.00, satisTarihi: new Date('2025-12-17T06:55:45') },
        // A. VURAL - Kurşunsuz (Benzin) Satışları
        { id: 14, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 4, yakitTuru: YakitTuru.BENZIN, litre: 9.51, birimFiyat: 52.57, toplamTutar: 500.00, satisTarihi: new Date('2025-12-17T01:07:12') },
        { id: 15, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 7, yakitTuru: YakitTuru.BENZIN, litre: 41.85, birimFiyat: 52.57, toplamTutar: 2200.00, satisTarihi: new Date('2025-12-17T05:26:23') },
        { id: 16, vardiyaId: 1, personelId: 1, personelAdi: 'A. VURAL', personelKeyId: 'P001', pompaNo: 8, yakitTuru: YakitTuru.BENZIN, litre: 19.02, birimFiyat: 52.57, toplamTutar: 1000.00, satisTarihi: new Date('2025-12-17T06:54:54') },

        // E. AKCA Satışları
        { id: 17, vardiyaId: 1, personelId: 2, personelAdi: 'E. AKCA', personelKeyId: 'P002', pompaNo: 16, yakitTuru: YakitTuru.LPG, litre: 21.28, birimFiyat: 25.99, toplamTutar: 553.07, satisTarihi: new Date('2025-12-17T07:04:09') },
        { id: 18, vardiyaId: 1, personelId: 2, personelAdi: 'E. AKCA', personelKeyId: 'P002', pompaNo: 2, yakitTuru: YakitTuru.BENZIN, litre: 9.51, birimFiyat: 52.57, toplamTutar: 500.00, satisTarihi: new Date('2025-12-17T07:07:16') },
        { id: 19, vardiyaId: 1, personelId: 2, personelAdi: 'E. AKCA', personelKeyId: 'P002', pompaNo: 6, yakitTuru: YakitTuru.BENZIN, litre: 22.55, birimFiyat: 52.57, toplamTutar: 1185.45, satisTarihi: new Date('2025-12-17T07:15:10') },
        { id: 20, vardiyaId: 1, personelId: 2, personelAdi: 'E. AKCA', personelKeyId: 'P002', pompaNo: 4, yakitTuru: YakitTuru.BENZIN, litre: 27.39, birimFiyat: 52.57, toplamTutar: 1440.00, satisTarihi: new Date('2025-12-17T07:27:20') },
        { id: 21, vardiyaId: 1, personelId: 2, personelAdi: 'E. AKCA', personelKeyId: 'P002', pompaNo: 3, yakitTuru: YakitTuru.BENZIN, litre: 40.53, birimFiyat: 52.57, toplamTutar: 2130.66, satisTarihi: new Date('2025-12-17T07:30:10') },
        { id: 22, vardiyaId: 1, personelId: 2, personelAdi: 'E. AKCA', personelKeyId: 'P002', pompaNo: 6, yakitTuru: YakitTuru.BENZIN, litre: 38.62, birimFiyat: 52.57, toplamTutar: 2030.25, satisTarihi: new Date('2025-12-17T07:32:50') },

        // M. DUMDUZ Satışları
        { id: 23, vardiyaId: 1, personelId: 3, personelAdi: 'M. DUMDUZ', personelKeyId: 'P003', pompaNo: 9, yakitTuru: YakitTuru.LPG, litre: 31.88, birimFiyat: 25.99, toplamTutar: 828.56, satisTarihi: new Date('2025-12-17T07:11:19') },
        { id: 24, vardiyaId: 1, personelId: 3, personelAdi: 'M. DUMDUZ', personelKeyId: 'P003', pompaNo: 9, yakitTuru: YakitTuru.LPG, litre: 19.24, birimFiyat: 25.99, toplamTutar: 500.00, satisTarihi: new Date('2025-12-17T07:14:20') },
        { id: 25, vardiyaId: 1, personelId: 3, personelAdi: 'M. DUMDUZ', personelKeyId: 'P003', pompaNo: 14, yakitTuru: YakitTuru.LPG, litre: 18.02, birimFiyat: 25.99, toplamTutar: 468.34, satisTarihi: new Date('2025-12-17T07:15:23') },
        { id: 26, vardiyaId: 1, personelId: 3, personelAdi: 'M. DUMDUZ', personelKeyId: 'P003', pompaNo: 7, yakitTuru: YakitTuru.MOTORIN, litre: 31.02, birimFiyat: 54.16, toplamTutar: 1680.00, satisTarihi: new Date('2025-12-17T07:17:19') },
        { id: 27, vardiyaId: 1, personelId: 3, personelAdi: 'M. DUMDUZ', personelKeyId: 'P003', pompaNo: 8, yakitTuru: YakitTuru.MOTORIN, litre: 36.93, birimFiyat: 54.16, toplamTutar: 2000.00, satisTarihi: new Date('2025-12-17T07:27:14') },
        { id: 28, vardiyaId: 1, personelId: 3, personelAdi: 'M. DUMDUZ', personelKeyId: 'P003', pompaNo: 14, yakitTuru: YakitTuru.LPG, litre: 24.80, birimFiyat: 25.99, toplamTutar: 644.55, satisTarihi: new Date('2025-12-17T07:30:09') },

        // F. BAYLAS Satışları
        { id: 29, vardiyaId: 1, personelId: 4, personelAdi: 'F. BAYLAS', personelKeyId: 'P004', pompaNo: 11, yakitTuru: YakitTuru.LPG, litre: 32.50, birimFiyat: 25.99, toplamTutar: 844.68, satisTarihi: new Date('2025-12-17T07:23:25') },
        { id: 30, vardiyaId: 1, personelId: 4, personelAdi: 'F. BAYLAS', personelKeyId: 'P004', pompaNo: 6, yakitTuru: YakitTuru.MOTORIN, litre: 3.69, birimFiyat: 54.16, toplamTutar: 200.00, satisTarihi: new Date('2025-12-17T07:25:07') },
        { id: 31, vardiyaId: 1, personelId: 4, personelAdi: 'F. BAYLAS', personelKeyId: 'P004', pompaNo: 15, yakitTuru: YakitTuru.LPG, litre: 7.70, birimFiyat: 25.99, toplamTutar: 200.00, satisTarihi: new Date('2025-12-17T07:28:36') },
        { id: 32, vardiyaId: 1, personelId: 4, personelAdi: 'F. BAYLAS', personelKeyId: 'P004', pompaNo: 11, yakitTuru: YakitTuru.LPG, litre: 28.09, birimFiyat: 25.99, toplamTutar: 730.06, satisTarihi: new Date('2025-12-17T07:29:35') },
        { id: 33, vardiyaId: 1, personelId: 4, personelAdi: 'F. BAYLAS', personelKeyId: 'P004', pompaNo: 16, yakitTuru: YakitTuru.LPG, litre: 25.12, birimFiyat: 25.99, toplamTutar: 652.87, satisTarihi: new Date('2025-12-17T07:32:56') },
        { id: 34, vardiyaId: 1, personelId: 4, personelAdi: 'F. BAYLAS', personelKeyId: 'P004', pompaNo: 11, yakitTuru: YakitTuru.LPG, litre: 44.62, birimFiyat: 25.99, toplamTutar: 1159.67, satisTarihi: new Date('2025-12-17T07:34:25') }
    ];



    // ==========================================
    // İSTASYON / PERSONEL
    // ==========================================

    getIstasyonlar(): Observable<Istasyon[]> {
        return of(this.mockIstasyonlar).pipe(delay(300));
    }

    getPersoneller(istasyonId: number, rol?: PersonelRol): Observable<Personel[]> {
        let personeller = this.mockPersoneller.filter(p => p.istasyonId === istasyonId);
        if (rol) {
            personeller = personeller.filter(p => p.rol === rol);
        }
        return of(personeller).pipe(delay(300));
    }

    getPompacılar(istasyonId: number): Observable<Personel[]> {
        return this.getPersoneller(istasyonId, PersonelRol.POMPACI);
    }

    getVardiyaSorumlulari(istasyonId: number): Observable<Personel[]> {
        return this.getPersoneller(istasyonId, PersonelRol.VARDIYA_SORUMLUSU);
    }

    // ==========================================
    // VARDİYA İŞLEMLERİ
    // ==========================================

    getAktifVardiya(): Observable<Vardiya | null> {
        return this.aktifVardiya.asObservable();
    }

    setAktifVardiya(vardiya: Vardiya): void {
        this.aktifVardiya.next(vardiya);
    }

    async vardiyaEkle(vardiya: Vardiya, satislar: OtomasyonSatis[], filoSatislari: FiloSatis[] = []): Promise<number> {
        // 1. Vardiya kaydı
        const personelToplam = satislar.reduce((sum, s) => sum + s.toplamTutar, 0);
        const filoToplam = filoSatislari.reduce((sum, s) => sum + s.tutar, 0);

        const dbVardiya: Omit<DBVardiya, 'id'> = {
            dosyaAdi: vardiya.dosyaAdi || 'Bilinmeyen Dosya',
            yuklemeTarihi: vardiya.baslangicTarihi,
            baslangicTarih: vardiya.baslangicTarihi,
            bitisTarih: vardiya.bitisTarihi || null,
            toplamTutar: personelToplam + filoToplam,
            personelSayisi: new Set(satislar.map(s => s.personelId)).size,
            islemSayisi: satislar.length + filoSatislari.length,
            durum: 'ACIK',
            mutabakatTarihi: undefined,
            onayTarihi: undefined
        };

        const vardiyaId = await this.dbService.vardiyaEkle(dbVardiya);

        // 2. Satış kayıtları
        const dbSatislar: Omit<DBSatis, 'id'>[] = satislar.map(s => ({
            vardiyaId,
            personelId: s.personelId,
            personelAdi: s.personelAdi,
            personelKeyId: s.personelKeyId,
            pompaNo: s.pompaNo,
            yakitTuru: s.yakitTuru,
            litre: s.litre,
            birimFiyat: s.birimFiyat,
            toplamTutar: s.toplamTutar,
            satisTarihi: s.satisTarihi,
            fisNo: s.fisNo,
            plaka: s.plaka
        }));

        // Filo satışlarını da ekle
        const dbFiloSatislar: Omit<DBSatis, 'id'>[] = filoSatislari.map(s => ({
            vardiyaId,
            personelId: 999, // Dummy ID
            personelAdi: 'OTOMASYON',
            personelKeyId: 'SYS001',
            pompaNo: 0,
            yakitTuru: s.yakitTuru,
            litre: s.litre,
            birimFiyat: s.litre > 0 ? s.tutar / s.litre : 0,
            toplamTutar: s.tutar,
            satisTarihi: s.tarih,
            fisNo: s.fisNo,
            plaka: s.plaka
        }));

        await this.dbService.satislarEkle([...dbSatislar, ...dbFiloSatislar]);

        // Aktif vardiyayı güncelle
        await this.setAktifVardiyaById(vardiyaId);

        return vardiyaId;
    }

    async setAktifVardiyaById(vardiyaId: number): Promise<void> {
        console.log('setAktifVardiyaById çağrıldı. Vardiya ID:', vardiyaId);

        const dbVardiya = await this.dbService.vardiyaGetir(vardiyaId);

        if (dbVardiya) {
            const vardiya: Vardiya = {
                id: dbVardiya.id!,
                istasyonId: 1,
                istasyonAdi: 'Merkez İstasyon',
                baslangicTarihi: dbVardiya.baslangicTarih || new Date(),
                bitisTarihi: dbVardiya.bitisTarih || undefined,
                durum: dbVardiya.durum === 'ACIK' ? VardiyaDurum.ACIK :
                    dbVardiya.durum === 'ONAY_BEKLIYOR' ? VardiyaDurum.ONAY_BEKLIYOR :
                        dbVardiya.durum === 'ONAYLANDI' ? VardiyaDurum.ONAYLANDI : VardiyaDurum.REDDEDILDI,
                sorumluId: 6,
                sorumluAdi: 'Vardiya Sorumlusu',
                olusturmaTarihi: dbVardiya.yuklemeTarihi,
                guncellemeTarihi: new Date(),
                dosyaAdi: dbVardiya.dosyaAdi,
                pompaToplam: dbVardiya.toplamTutar,
                marketToplam: 0,
                genelToplam: dbVardiya.toplamTutar,
                toplamFark: 0,
                kilitli: dbVardiya.durum === 'ONAYLANDI'
            };

            this.aktifVardiya.next(vardiya);

            // Satışları yükle
            const dbSatislar = await this.dbService.vardiyaSatislariGetir(vardiyaId);
            const satislar: OtomasyonSatis[] = dbSatislar.map(s => ({
                id: s.id!,
                vardiyaId: s.vardiyaId,
                personelId: s.personelId,
                personelAdi: s.personelAdi,
                personelKeyId: s.personelKeyId,
                pompaNo: s.pompaNo,
                yakitTuru: s.yakitTuru as YakitTuru,
                litre: s.litre,
                birimFiyat: s.birimFiyat,
                toplamTutar: s.toplamTutar,
                satisTarihi: s.satisTarihi,
                fisNo: s.fisNo,
                plaka: s.plaka
            }));

            this.yuklenenSatislar.next(satislar);

            // Pusulaları yükle
            const dbPusulalar = await this.dbService.vardiyaPusulalariGetir(vardiyaId);
            const pusulalar: PusulaGirisi[] = dbPusulalar.map(p => ({
                id: p.id!,
                vardiyaId: p.vardiyaId,
                personelId: p.personelId,
                personelAdi: p.personelAdi,
                nakit: p.nakit,
                krediKarti: p.krediKarti,
                veresiye: p.veresiye,
                filoKarti: p.filoKarti,
                toplam: p.toplam,
                krediKartiDetay: p.krediKartiDetay,
                olusturmaTarihi: p.olusturmaTarihi
            }));
            this.pusulaGirisleri.next(pusulalar);

            // Giderleri yükle
            const dbGiderler = await this.dbService.vardiyaGiderlerGetir(vardiyaId);
            const giderler: PompaGider[] = dbGiderler.map(g => ({
                id: g.id!,
                vardiyaId: g.vardiyaId,
                giderTuru: g.giderTuru as PompaGiderTuru,
                tutar: g.tutar,
                aciklama: g.aciklama,
                olusturmaTarihi: g.olusturmaTarihi
            }));
            this.pompaGiderler.next(giderler);

        } else {
            console.warn('Vardiya bulunamadı:', vardiyaId);
            this.aktifVardiya.next(null);
        }
    }

    setSatislar(satislar: OtomasyonSatis[]): void {
        this.yuklenenSatislar.next(satislar);
    }

    getYuklenenSatislar(): OtomasyonSatis[] {
        return this.yuklenenSatislar.value;
    }

    vardiyaBaslat(sorumluId: number, istasyonId: number): Observable<Vardiya> {
        const sorumlu = this.mockPersoneller.find(p => p.id === sorumluId);
        const istasyon = this.mockIstasyonlar.find(i => i.id === istasyonId);

        const yeniVardiya: Vardiya = {
            id: Date.now(),
            istasyonId,
            istasyonAdi: istasyon?.ad || 'Bilinmeyen',
            sorumluId,
            sorumluAdi: sorumlu?.tamAd || 'Bilinmeyen',
            baslangicTarihi: new Date(),
            durum: VardiyaDurum.ACIK,
            pompaToplam: 0,
            marketToplam: 0,
            genelToplam: 0,
            toplamFark: 0,
            olusturmaTarihi: new Date(),
            kilitli: false
        };

        this.aktifVardiya.next(yeniVardiya);
        this.temizle(); // Önceki verileri temizle
        return of(yeniVardiya).pipe(delay(500));
    }

    async vardiyaOnayaGonder(vardiyaId: number): Promise<void> {
        console.log('Vardiya onaya gönderiliyor. ID:', vardiyaId);

        const basarili = await this.dbService.onayaGonder(vardiyaId);

        if (basarili) {
            const vardiya = this.aktifVardiya.value;
            if (vardiya && vardiya.id === vardiyaId) {
                const guncellenmis: Vardiya = {
                    ...vardiya,
                    durum: VardiyaDurum.ONAY_BEKLIYOR,
                    guncellemeTarihi: new Date()
                };
                this.aktifVardiya.next(guncellenmis);
            }
        } else {
            throw new Error('Mutabakat tamamlanmadığı için onaya gönderilemedi.');
        }
    }

    private updateLocalStorageVardiya(vardiyaId: number, yeniDurum: VardiyaDurum): void {
        try {
            const kayitli = localStorage.getItem('yuklenenVardiyalar');
            if (kayitli) {
                const vardiyalar = JSON.parse(kayitli);
                const index = vardiyalar.findIndex((v: any) => v.id === vardiyaId);
                if (index !== -1) {
                    vardiyalar[index].durum = yeniDurum;
                    vardiyalar[index].guncellemeTarihi = new Date().toISOString();
                    localStorage.setItem('yuklenenVardiyalar', JSON.stringify(vardiyalar));
                    console.log('LocalStorage güncellendi. Yeni durum:', yeniDurum);
                }
            }
        } catch (e) {
            console.error('LocalStorage güncelleme hatası:', e);
        }
    }

    vardiyaOnayla(vardiyaId: number, onaylayanId: number): Observable<Vardiya> {
        const vardiya = this.aktifVardiya.value;
        if (vardiya && vardiya.id === vardiyaId) {
            const onayli = this.mockPersoneller.find(p => p.id === onaylayanId);
            const muhurlu: Vardiya = {
                ...vardiya,
                durum: VardiyaDurum.ONAYLANDI,
                onaylayanId,
                onaylayanAdi: onayli?.tamAd || 'Yönetici',
                onayTarihi: new Date(),
                guncellemeTarihi: new Date(),
                kilitli: true // Mühürlendi
            };
            this.aktifVardiya.next(muhurlu);
            return of(muhurlu).pipe(delay(500));
        }
        throw new Error('Vardiya bulunamadı');
    }

    vardiyaReddet(vardiyaId: number, redNedeni: string): Observable<Vardiya> {
        const vardiya = this.aktifVardiya.value;
        if (vardiya && vardiya.id === vardiyaId) {
            const reddedilmis: Vardiya = {
                ...vardiya,
                durum: VardiyaDurum.REDDEDILDI,
                redNedeni,
                guncellemeTarihi: new Date(),
                kilitli: false // Düzenleme için açık
            };
            this.aktifVardiya.next(reddedilmis);
            return of(reddedilmis).pipe(delay(500));
        }
        throw new Error('Vardiya bulunamadı');
    }

    private temizle(): void {
        this.pusulaGirisleri.next([]);
        this.marketZRaporu.next(null);
        this.marketTahsilat.next(null);
        this.marketGiderler.next([]);
        this.pompaGiderler.next([]);
        this.yuklenenSatislar.next([]);
    }

    // ==========================================
    // OTOMASYON VERİLERİ (Otomatik Gelen)
    // ==========================================

    getOtomasyonSatislari(vardiyaId: number): Observable<OtomasyonSatis[]> {
        return this.yuklenenSatislar.asObservable();
    }

    getPersonelOtomasyonOzet(vardiyaId: number): Observable<PersonelOtomasyonOzet[]> {
        const satislar = this.yuklenenSatislar.value.length > 0
            ? this.yuklenenSatislar.value
            : this.mockOtomasyonSatislari.filter(s => s.vardiyaId === vardiyaId);

        const personelMap = new Map<number, PersonelOtomasyonOzet>();

        satislar.forEach(satis => {
            if (!personelMap.has(satis.personelId)) {
                personelMap.set(satis.personelId, {
                    personelId: satis.personelId,
                    personelAdi: satis.personelAdi,
                    personelKeyId: satis.personelKeyId,
                    toplamLitre: 0,
                    toplamTutar: 0,
                    yakitDetay: []
                });
            }

            const ozet = personelMap.get(satis.personelId)!;
            ozet.toplamLitre += satis.litre;
            ozet.toplamTutar += satis.toplamTutar;

            const yakitIndex = ozet.yakitDetay.findIndex(y => y.yakitTuru === satis.yakitTuru);
            if (yakitIndex === -1) {
                ozet.yakitDetay.push({
                    yakitTuru: satis.yakitTuru,
                    litre: satis.litre,
                    tutar: satis.toplamTutar
                });
            } else {
                ozet.yakitDetay[yakitIndex].litre += satis.litre;
                ozet.yakitDetay[yakitIndex].tutar += satis.toplamTutar;
            }
        });

        return of(Array.from(personelMap.values())).pipe(delay(100));
    }

    // ==========================================
    // PUSULA GİRİŞİ
    // ==========================================

    getPusulaGirisleri(): Observable<PusulaGirisi[]> {
        return this.pusulaGirisleri.asObservable();
    }

    async pusulaGirisiEkle(pusula: Omit<PusulaGirisi, 'id' | 'olusturmaTarihi' | 'toplam'>): Promise<PusulaGirisi> {
        const yeniPusula: PusulaGirisi = {
            ...pusula,
            id: 0,
            toplam: pusula.nakit + pusula.krediKarti + pusula.veresiye + pusula.filoKarti,
            olusturmaTarihi: new Date()
        };

        const dbPusula: Omit<DBPusula, 'id'> = {
            vardiyaId: pusula.vardiyaId,
            personelId: pusula.personelId,
            personelAdi: pusula.personelAdi,
            nakit: pusula.nakit,
            krediKarti: pusula.krediKarti,
            veresiye: pusula.veresiye,
            filoKarti: pusula.filoKarti,
            toplam: yeniPusula.toplam,
            krediKartiDetay: pusula.krediKartiDetay,
            olusturmaTarihi: yeniPusula.olusturmaTarihi
        };

        const id = await this.dbService.pusulaEkle(dbPusula);
        yeniPusula.id = id;

        const mevcut = this.pusulaGirisleri.value;
        const index = mevcut.findIndex(p => p.personelId === pusula.personelId);
        if (index !== -1) {
            mevcut[index] = yeniPusula;
            this.pusulaGirisleri.next([...mevcut]);
        } else {
            this.pusulaGirisleri.next([...mevcut, yeniPusula]);
        }

        return yeniPusula;
    }

    async pusulaGirisiSil(personelId: number): Promise<boolean> {
        const mevcut = this.pusulaGirisleri.value;
        const silinecek = mevcut.find(p => p.personelId === personelId);

        if (silinecek && silinecek.id) {
            await this.dbService.pusulaSil(silinecek.id);
        }

        this.pusulaGirisleri.next(mevcut.filter(p => p.personelId !== personelId));
        return true;
    }

    // ==========================================
    // FARK ANALİZİ
    // ==========================================

    getFarkAnalizi(vardiyaId: number): Observable<PersonelFarkAnalizi[]> {
        const pusulalar = this.pusulaGirisleri.value;
        const otomasyonSatislari = this.yuklenenSatislar.value.length > 0
            ? this.yuklenenSatislar.value
            : this.mockOtomasyonSatislari.filter(s => s.vardiyaId === vardiyaId);

        const otomasyonMap = new Map<number, number>();
        otomasyonSatislari.forEach(s => {
            const mevcut = otomasyonMap.get(s.personelId) || 0;
            otomasyonMap.set(s.personelId, mevcut + s.toplamTutar);
        });

        const analizler: PersonelFarkAnalizi[] = [];

        otomasyonMap.forEach((otomasyonToplam, personelId) => {
            const personel = this.mockPersoneller.find(p => p.id === personelId);
            const pusula = pusulalar.find(p => p.personelId === personelId);

            const pusulaToplam = pusula?.toplam || 0;
            const fark = pusulaToplam - otomasyonToplam;

            let farkDurum: FarkDurum;
            if (Math.abs(fark) < 1) {
                farkDurum = FarkDurum.UYUMLU;
            } else if (fark < 0) {
                farkDurum = FarkDurum.ACIK;
            } else {
                farkDurum = FarkDurum.FAZLA;
            }

            analizler.push({
                personelId,
                personelAdi: personel?.tamAd || 'Bilinmeyen',
                otomasyonToplam,
                pusulaToplam,
                fark,
                farkDurum,
                pusulaDokum: {
                    nakit: pusula?.nakit || 0,
                    krediKarti: pusula?.krediKarti || 0,
                    veresiye: pusula?.veresiye || 0,
                    filoKarti: pusula?.filoKarti || 0
                }
            });
        });

        return of(analizler).pipe(delay(500));
    }

    // ==========================================
    // MARKET YÖNETİMİ
    // ==========================================

    getMarketZRaporu(): Observable<MarketZRaporu | null> {
        return this.marketZRaporu.asObservable();
    }

    marketZRaporuKaydet(zRaporu: Omit<MarketZRaporu, 'id' | 'olusturmaTarihi' | 'kdvToplam' | 'kdvHaricToplam'>): Observable<MarketZRaporu> {
        const kdvToplam = zRaporu.kdv1 + zRaporu.kdv10 + zRaporu.kdv20;
        const yeniZRaporu: MarketZRaporu = {
            ...zRaporu,
            id: Date.now(),
            kdvToplam,
            kdvHaricToplam: zRaporu.genelToplam - kdvToplam,
            olusturmaTarihi: new Date()
        };

        this.marketZRaporu.next(yeniZRaporu);
        return of(yeniZRaporu).pipe(delay(300));
    }

    getMarketTahsilat(): Observable<MarketTahsilat | null> {
        return this.marketTahsilat.asObservable();
    }

    marketTahsilatKaydet(tahsilat: Omit<MarketTahsilat, 'id' | 'olusturmaTarihi' | 'toplam'>): Observable<MarketTahsilat> {
        const yeniTahsilat: MarketTahsilat = {
            ...tahsilat,
            id: Date.now(),
            toplam: tahsilat.nakit + tahsilat.krediKarti + tahsilat.yemekKarti,
            olusturmaTarihi: new Date()
        };

        this.marketTahsilat.next(yeniTahsilat);
        return of(yeniTahsilat).pipe(delay(300));
    }

    getMarketGiderler(): Observable<MarketGider[]> {
        return this.marketGiderler.asObservable();
    }

    marketGiderEkle(gider: Omit<MarketGider, 'id' | 'olusturmaTarihi'>): Observable<MarketGider> {
        const yeniGider: MarketGider = {
            ...gider,
            id: Date.now(),
            olusturmaTarihi: new Date()
        };

        const mevcut = this.marketGiderler.value;
        this.marketGiderler.next([...mevcut, yeniGider]);
        return of(yeniGider).pipe(delay(300));
    }

    marketGiderSil(giderId: number): Observable<boolean> {
        const mevcut = this.marketGiderler.value;
        this.marketGiderler.next(mevcut.filter(g => g.id !== giderId));
        return of(true).pipe(delay(300));
    }

    getMarketOzet(): Observable<MarketOzet> {
        const zRaporu = this.marketZRaporu.value;
        const tahsilat = this.marketTahsilat.value;
        const giderler = this.marketGiderler.value;

        const zRaporuToplam = zRaporu?.genelToplam || 0;
        const tahsilatToplam = tahsilat?.toplam || 0;
        const giderToplam = giderler.reduce((sum, g) => sum + g.tutar, 0);
        const netKasa = tahsilatToplam - giderToplam;

        const ozet: MarketOzet = {
            zRaporuToplam,
            tahsilatToplam,
            giderToplam,
            netKasa,
            fark: zRaporuToplam - netKasa,
            kdvDokum: {
                kdv1: zRaporu?.kdv1 || 0,
                kdv10: zRaporu?.kdv10 || 0,
                kdv20: zRaporu?.kdv20 || 0,
                toplam: zRaporu?.kdvToplam || 0
            }
        };

        return of(ozet).pipe(delay(300));
    }

    // ==========================================
    // POMPA GİDERLERİ
    // ==========================================

    getPompaGiderler(): Observable<PompaGider[]> {
        return this.pompaGiderler.asObservable();
    }

    async pompaGiderEkle(gider: Omit<PompaGider, 'id' | 'olusturmaTarihi'>): Promise<PompaGider> {
        const yeniGider: PompaGider = {
            ...gider,
            id: 0,
            olusturmaTarihi: new Date()
        };

        const dbGider: Omit<DBGider, 'id'> = {
            vardiyaId: gider.vardiyaId,
            giderTuru: gider.giderTuru,
            tutar: gider.tutar,
            aciklama: gider.aciklama,
            olusturmaTarihi: yeniGider.olusturmaTarihi
        };

        const id = await this.dbService.giderEkle(dbGider);
        yeniGider.id = id;

        const mevcut = this.pompaGiderler.value;
        this.pompaGiderler.next([...mevcut, yeniGider]);
        return yeniGider;
    }

    async pompaGiderSil(giderId: number): Promise<boolean> {
        await this.dbService.giderSil(giderId);
        const mevcut = this.pompaGiderler.value;
        this.pompaGiderler.next(mevcut.filter(g => g.id !== giderId));
        return true;
    }

    // ==========================================
    // POMPA ÖZETİ
    // ==========================================

    getPompaOzet(vardiyaId: number): Observable<PompaOzet> {
        const pusulalar = this.pusulaGirisleri.value;
        const giderler = this.pompaGiderler.value;

        const otomasyonSatislari = this.yuklenenSatislar.value.length > 0
            ? this.yuklenenSatislar.value
            : this.mockOtomasyonSatislari.filter(s => s.vardiyaId === vardiyaId);

        const personelIds = new Set(otomasyonSatislari.map(s => s.personelId));

        const toplamOtomasyonSatis = otomasyonSatislari.reduce((sum, s) => sum + s.toplamTutar, 0);
        const toplamPusulaTahsilat = pusulalar.reduce((sum, p) => sum + p.toplam, 0);
        const giderToplam = giderler.reduce((sum, g) => sum + g.tutar, 0);
        const toplamFark = toplamPusulaTahsilat - toplamOtomasyonSatis;

        let farkDurum: FarkDurum;
        if (Math.abs(toplamFark) < 1) {
            farkDurum = FarkDurum.UYUMLU;
        } else if (toplamFark < 0) {
            farkDurum = FarkDurum.ACIK;
        } else {
            farkDurum = FarkDurum.FAZLA;
        }

        const personelFarklari: PersonelFarkAnalizi[] = [];
        personelIds.forEach(personelId => {
            const pusula = pusulalar.find(p => p.personelId === personelId);
            const personelSatislari = otomasyonSatislari.filter(s => s.personelId === personelId);
            const personelAdi = personelSatislari[0]?.personelAdi || 'Bilinmeyen';
            const otomasyonToplam = personelSatislari.reduce((sum, s) => sum + s.toplamTutar, 0);

            const pusulaToplam = pusula?.toplam || 0;
            const fark = pusulaToplam - otomasyonToplam;

            let pFarkDurum: FarkDurum;
            if (Math.abs(fark) < 1) {
                pFarkDurum = FarkDurum.UYUMLU;
            } else if (fark < 0) {
                pFarkDurum = FarkDurum.ACIK;
            } else {
                pFarkDurum = FarkDurum.FAZLA;
            }

            personelFarklari.push({
                personelId,
                personelAdi,
                otomasyonToplam,
                pusulaToplam,
                fark,
                farkDurum: pFarkDurum,
                pusulaDokum: {
                    nakit: pusula?.nakit || 0,
                    krediKarti: pusula?.krediKarti || 0,
                    veresiye: pusula?.veresiye || 0,
                    filoKarti: pusula?.filoKarti || 0
                }
            });
        });

        const ozet: PompaOzet = {
            personelSayisi: personelIds.size,
            toplamOtomasyonSatis,
            toplamPusulaTahsilat,
            toplamFark,
            farkDurum,
            personelFarklari,
            giderToplam,
            netTahsilat: toplamPusulaTahsilat - giderToplam
        };

        return of(ozet).pipe(delay(500));
    }

    // ==========================================
    // GENEL ÖZET VE DASHBOARD
    // ==========================================

    getGenelOzet(vardiyaId: number): Observable<GenelOzet> {
        const pusulalar = this.pusulaGirisleri.value;
        const marketTahsilat = this.marketTahsilat.value;
        const pompaGiderler = this.pompaGiderler.value;
        const marketGiderler = this.marketGiderler.value;
        const zRaporu = this.marketZRaporu.value;

        const otomasyonSatislari = this.yuklenenSatislar.value.length > 0
            ? this.yuklenenSatislar.value
            : this.mockOtomasyonSatislari.filter(s => s.vardiyaId === vardiyaId);

        const pompaToplam = otomasyonSatislari.reduce((sum, s) => sum + s.toplamTutar, 0);

        const pompaTahsilat = pusulalar.reduce((sum, p) => sum + p.toplam, 0);

        // Market toplamı
        const marketToplam = zRaporu?.genelToplam || 0;

        // Genel toplam
        const genelToplam = pompaToplam + marketToplam;

        // Tahsilat dağılımı
        const toplamNakit = pusulalar.reduce((sum, p) => sum + p.nakit, 0) + (marketTahsilat?.nakit || 0);
        const toplamKrediKarti = pusulalar.reduce((sum, p) => sum + p.krediKarti, 0) + (marketTahsilat?.krediKarti || 0);
        const toplamVeresiye = pusulalar.reduce((sum, p) => sum + p.veresiye, 0);

        // Giderler
        const toplamGider = pompaGiderler.reduce((sum, g) => sum + g.tutar, 0) +
            marketGiderler.reduce((sum, g) => sum + g.tutar, 0);

        // Fark
        const toplamFark = (pompaTahsilat + (marketTahsilat?.toplam || 0)) - genelToplam;

        let durumRenk: 'success' | 'warn' | 'danger';
        if (Math.abs(toplamFark) < 10) {
            durumRenk = 'success';
        } else if (Math.abs(toplamFark) < 100) {
            durumRenk = 'warn';
        } else {
            durumRenk = 'danger';
        }

        const ozet: GenelOzet = {
            pompaToplam,
            marketToplam,
            genelToplam,
            toplamNakit,
            toplamKrediKarti,
            toplamVeresiye,
            toplamGider,
            toplamFark,
            durumRenk
        };

        return of(ozet).pipe(delay(500));
    }

    // ==========================================
    // TEMİZLİK
    // ==========================================



    // ==========================================
    // YARDIMCI METODLAR
    // ==========================================

    getOdemeYontemleri(): { label: string; value: OdemeYontemi }[] {
        return [
            { label: 'Nakit', value: OdemeYontemi.NAKIT },
            { label: 'Kredi Kartı', value: OdemeYontemi.KREDI_KARTI },
            { label: 'Veresiye', value: OdemeYontemi.VERESIYE },
            { label: 'Filo Kartı', value: OdemeYontemi.FILO_KARTI },
            { label: 'Yemek Kartı', value: OdemeYontemi.YEMEK_KARTI }
        ];
    }

    getYakitTurleri(): { label: string; value: YakitTuru }[] {
        return [
            { label: 'Benzin', value: YakitTuru.BENZIN },
            { label: 'Motorin', value: YakitTuru.MOTORIN },
            { label: 'LPG', value: YakitTuru.LPG },
            { label: 'Euro Diesel', value: YakitTuru.EURO_DIESEL }
        ];
    }

    getGiderTurleri(): { label: string; value: GiderTuru }[] {
        return [
            { label: 'Ekmek', value: GiderTuru.EKMEK },
            { label: 'Temizlik', value: GiderTuru.TEMIZLIK },
            { label: 'Personel', value: GiderTuru.PERSONEL },
            { label: 'Kırtasiye', value: GiderTuru.KIRTASIYE },
            { label: 'Diğer', value: GiderTuru.DIGER }
        ];
    }

    getPompaGiderTurleri(): { label: string; value: PompaGiderTuru }[] {
        return [
            { label: 'Yıkama', value: PompaGiderTuru.YIKAMA },
            { label: 'Bahşiş', value: PompaGiderTuru.BAHSIS },
            { label: 'Temizlik', value: PompaGiderTuru.TEMIZLIK },
            { label: 'Tamir', value: PompaGiderTuru.TAMIR },
            { label: 'Diğer', value: PompaGiderTuru.DIGER }
        ];
    }

    // ==========================================
    // ESKİ UYUMLULUK (Mevcut bileşenler için)
    // ==========================================

    getOperatorler(istasyonId?: number): Observable<Operator[]> {
        const sorumlular = this.mockPersoneller
            .filter(p => p.rol === PersonelRol.VARDIYA_SORUMLUSU)
            .filter(p => !istasyonId || p.istasyonId === istasyonId)
            .map(p => ({
                id: p.id,
                ad: p.ad,
                soyad: p.soyad,
                kullaniciAdi: p.keyId,
                istasyonId: p.istasyonId,
                aktif: p.aktif
            }));
        return of(sorumlular).pipe(delay(300));
    }

    getTahsilatlar(): Observable<Tahsilat[]> {
        return this.tahsilatlar.asObservable();
    }

    tahsilatEkle(tahsilat: Omit<Tahsilat, 'id' | 'olusturmaTarihi'>): Observable<Tahsilat> {
        const yeniTahsilat: Tahsilat = {
            ...tahsilat,
            id: Date.now(),
            olusturmaTarihi: new Date()
        };

        const mevcut = this.tahsilatlar.value;
        this.tahsilatlar.next([...mevcut, yeniTahsilat]);
        return of(yeniTahsilat).pipe(delay(300));
    }

    tahsilatGuncelle(tahsilat: Tahsilat): Observable<Tahsilat> {
        const mevcut = this.tahsilatlar.value;
        const index = mevcut.findIndex(t => t.id === tahsilat.id);
        if (index !== -1) {
            mevcut[index] = tahsilat;
            this.tahsilatlar.next([...mevcut]);
        }
        return of(tahsilat).pipe(delay(300));
    }

    tahsilatSil(tahsilatId: number): Observable<boolean> {
        const mevcut = this.tahsilatlar.value;
        this.tahsilatlar.next(mevcut.filter(t => t.id !== tahsilatId));
        return of(true).pipe(delay(300));
    }

    tahsilatlariTemizle(): void {
        this.tahsilatlar.next([]);
    }

    getPompaSatislari(vardiyaId: number): Observable<PompaSatis[]> {
        const source = this.yuklenenSatislar.value.length > 0
            ? this.yuklenenSatislar.value
            : this.mockOtomasyonSatislari.filter(s => s.vardiyaId === vardiyaId);

        const satislar = source.map(s => ({
            id: s.id,
            vardiyaId: s.vardiyaId,
            pompaNo: s.pompaNo,
            yakitTuru: s.yakitTuru,
            litre: s.litre,
            birimFiyat: s.birimFiyat,
            toplamTutar: s.toplamTutar,
            satisTarihi: s.satisTarihi
        }));
        return of(satislar).pipe(delay(500));
    }

    getSistemTahsilatlari(vardiyaId: number): Observable<{ odemeYontemi: OdemeYontemi; tutar: number }[]> {
        // Pompa satışlarından hesapla
        const toplamSatis = this.mockOtomasyonSatislari
            .filter(s => s.vardiyaId === vardiyaId)
            .reduce((sum, s) => sum + s.toplamTutar, 0);

        // Varsayılan dağılım
        return of([
            { odemeYontemi: OdemeYontemi.NAKIT, tutar: toplamSatis * 0.4 },
            { odemeYontemi: OdemeYontemi.KREDI_KARTI, tutar: toplamSatis * 0.35 },
            { odemeYontemi: OdemeYontemi.VERESIYE, tutar: toplamSatis * 0.15 },
            { odemeYontemi: OdemeYontemi.FILO_KARTI, tutar: toplamSatis * 0.1 }
        ]).pipe(delay(500));
    }

    karsilastirmaYap(vardiyaId: number): Observable<KarsilastirmaSonuc> {
        const otomasyonSatislari = this.yuklenenSatislar.value.length > 0
            ? this.yuklenenSatislar.value
            : this.mockOtomasyonSatislari.filter(s => s.vardiyaId === vardiyaId);

        const pusulalar = this.pusulaGirisleri.value;

        const sistemToplam = otomasyonSatislari.reduce((sum, s) => sum + s.toplamTutar, 0);

        // Calculate totals from Pusula
        const nakitToplam = pusulalar.reduce((sum, p) => sum + p.nakit, 0);
        const kkToplam = pusulalar.reduce((sum, p) => sum + p.krediKarti, 0);
        const veresiyeToplam = pusulalar.reduce((sum, p) => sum + p.veresiye, 0);
        const filoToplam = pusulalar.reduce((sum, p) => sum + p.filoKarti, 0);

        const tahsilatToplam = nakitToplam + kkToplam + veresiyeToplam + filoToplam;

        const fark = tahsilatToplam - sistemToplam;
        const farkYuzde = sistemToplam > 0 ? (fark / sistemToplam) * 100 : 0;

        let durum: KarsilastirmaDurum;
        if (Math.abs(fark) < 1) { // 1 TL tolerance
            durum = KarsilastirmaDurum.UYUMLU;
        } else if (Math.abs(farkYuzde) < 1) {
            durum = KarsilastirmaDurum.FARK_VAR;
        } else {
            durum = KarsilastirmaDurum.KRITIK_FARK;
        }

        const detaylar: KarsilastirmaDetay[] = [
            { odemeYontemi: OdemeYontemi.NAKIT, sistemTutar: 0, tahsilatTutar: nakitToplam, fark: nakitToplam },
            { odemeYontemi: OdemeYontemi.KREDI_KARTI, sistemTutar: 0, tahsilatTutar: kkToplam, fark: kkToplam },
            { odemeYontemi: OdemeYontemi.VERESIYE, sistemTutar: 0, tahsilatTutar: veresiyeToplam, fark: veresiyeToplam },
            { odemeYontemi: OdemeYontemi.FILO_KARTI, sistemTutar: 0, tahsilatTutar: filoToplam, fark: filoToplam }
        ];

        return of({
            vardiyaId,
            sistemToplam,
            tahsilatToplam,
            fark,
            farkYuzde,
            detaylar,
            durum
        }).pipe(delay(500));
    }

    getVardiyaOzet(vardiyaId: number): Observable<VardiyaOzet> {
        const vardiya = this.aktifVardiya.value;
        const tahsilatlar = this.tahsilatlar.value;
        const otomasyonSatislari = this.mockOtomasyonSatislari.filter(s => s.vardiyaId === vardiyaId);

        if (!vardiya) {
            throw new Error('Vardiya bulunamadı');
        }

        const toplamSatis = otomasyonSatislari.reduce((sum, s) => sum + s.toplamTutar, 0);
        const toplamTahsilat = tahsilatlar.reduce((sum, t) => sum + t.tutar, 0);

        return of({
            vardiya,
            pompaOzet: {
                personelSayisi: 3,
                toplamOtomasyonSatis: toplamSatis,
                toplamPusulaTahsilat: toplamTahsilat,
                toplamFark: toplamTahsilat - toplamSatis,
                farkDurum: FarkDurum.UYUMLU,
                personelFarklari: [],
                giderToplam: 0,
                netTahsilat: toplamTahsilat
            },
            marketOzet: null,
            genelToplam: toplamSatis,
            genelFark: toplamTahsilat - toplamSatis
        }).pipe(delay(500));
    }
}
