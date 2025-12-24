import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, delay, from, map } from 'rxjs';
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
    MarketGelir,
    GelirTuru,
    MarketOzet,
    MarketVardiya,
    MarketVardiyaPersonel,
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
import { DbService, DBVardiya, DBSatis, DBPusula, DBGider, DBMarketVardiya, DBMarketVardiyaPersonel, DBMarketZRaporu } from './db.service';
import { AuthService } from '../../../services/auth.service';

@Injectable({
    providedIn: 'root'
})
export class VardiyaService {
    constructor(
        private dbService: DbService,
        private authService: AuthService
    ) { }

    private aktifVardiya = new BehaviorSubject<Vardiya | null>(null);
    private pusulaGirisleri = new BehaviorSubject<PusulaGirisi[]>([]);
    private marketZRaporu = new BehaviorSubject<MarketZRaporu | null>(null);
    private marketTahsilat = new BehaviorSubject<MarketTahsilat | null>(null);
    private marketGiderler = new BehaviorSubject<MarketGider[]>([]);

    private marketGelirler = new BehaviorSubject<MarketGelir[]>([]);
    private pompaGiderler = new BehaviorSubject<PompaGider[]>([]);

    // Yüklenen dosyadan gelen satışlar
    private yuklenenSatislar = new BehaviorSubject<OtomasyonSatis[]>([]);

    // Eski uyumluluk için
    private tahsilatlar = new BehaviorSubject<Tahsilat[]>([]);

    // ==========================================
    // İSTASYON / PERSONEL
    // ==========================================

    getIstasyonlar(): Observable<Istasyon[]> {
        return from(this.dbService.getIstasyonlar()).pipe(
            map(dbStations => dbStations.map(s => ({ ...s, id: s.id! })))
        );
    }

    getPersoneller(istasyonId: number, rol?: PersonelRol): Observable<Personel[]> {
        return from(this.dbService.getPersoneller(istasyonId)).pipe(
            map(dbPersoneller => {
                let pList = dbPersoneller.map(p => ({
                    id: p.id!,
                    keyId: p.keyId,
                    ad: p.ad,
                    soyad: p.soyad,
                    tamAd: p.tamAd,
                    istasyonId: p.istasyonId,
                    rol: p.rol as PersonelRol,
                    aktif: p.aktif
                }));

                if (rol) {
                    pList = pList.filter(p => p.rol === rol);
                }
                return pList;
            })
        );
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

    getOnayBekleyenVardiyalar(): Observable<Vardiya[]> {
        return from(this.dbService.onayBekleyenVardiyalar()).pipe(
            map(dbVardiyalar => dbVardiyalar.map(v => ({
                id: v.id!,
                istasyonId: 1, // Default
                istasyonAdi: 'Merkez İstasyon', // Default
                baslangicTarihi: v.baslangicTarih || v.yuklemeTarihi,
                bitisTarihi: v.bitisTarih || undefined,
                durum: VardiyaDurum.ONAY_BEKLIYOR,
                sorumluId: 0,
                sorumluAdi: 'Bilinmiyor',
                olusturmaTarihi: v.yuklemeTarihi,
                guncellemeTarihi: v.mutabakatTarihi || new Date(),
                dosyaAdi: v.dosyaAdi,
                pompaToplam: v.toplamTutar, // Tahmini
                marketToplam: 0,
                genelToplam: v.toplamTutar,
                toplamFark: 0,
                kilitli: true
            })))
        );
    }

    async vardiyaEkle(vardiya: Vardiya, satislar: OtomasyonSatis[], filoSatislari: FiloSatis[] = []): Promise<number> {
        // 1. Vardiya kaydı
        const personelToplam = satislar.reduce((sum, s) => sum + s.toplamTutar, 0);
        const filoToplam = filoSatislari.reduce((sum, s) => sum + s.tutar, 0);

        // 0. Personel Kontrolü ve ID Eşleştirme
        const personelIdMap = new Map<number, number>(); // FileID -> DbID

        const uniqueNames = [...new Set(satislar.map(s => s.personelAdi))];
        for (const ad of uniqueNames) {
            // Otomasyon genelde adı 'A. VURAL' gibi verir.
            // Veritabanında KeyId veya Ad ile eşleşme arayalım.
            // Basitçe KeyId kontrolü yapalım (parse edilen keyId üzerinden)
            const ornekSatis = satislar.find(s => s.personelAdi === ad);
            if (ornekSatis) {
                let mevcutPersonel = await this.dbService.getPersonelByKey(ornekSatis.personelKeyId);

                if (!mevcutPersonel) {
                    console.log(`Yeni personel tespit edildi: ${ad} (${ornekSatis.personelKeyId})`);
                    const yeniId = await this.dbService.personelEkle({
                        keyId: ornekSatis.personelKeyId,
                        ad: ad, // Otomasyondan gelen isim (örn: A. VURAL)
                        soyad: '', // Soyad ayrıştırması zor olabilir, boş bırakalım
                        tamAd: ad, // Kullanıcı sonra bunu düzeltecek (örn: Ahmet Vural)
                        istasyonId: 1, // Varsayılan
                        rol: 'POMPACI',
                        aktif: true
                    });

                    // Yeni eklenen personeli getir (ID için)
                    mevcutPersonel = await this.dbService.getPersonel(yeniId);
                }

                if (mevcutPersonel) {
                    // Eşleşme bulundu, haritala
                    // ornekSatis.personelId (File ID) -> mevcutPersonel.id (DB ID)
                    personelIdMap.set(ornekSatis.personelId, mevcutPersonel.id!);
                }
            }
        }

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

        // 2. Satış kayıtları (ID Eşleştirme ile)
        const dbSatislar: Omit<DBSatis, 'id'>[] = satislar.map(s => {
            // Doğru Personel ID'yi bul
            // Eğer haritada yoksa (ki olmalı), eski ID'yi kullan (fallback)
            const dbPersonelId = personelIdMap.get(s.personelId) || s.personelId;

            return {
                vardiyaId,
                personelId: dbPersonelId,
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
            };
        });

        // Filo satışlarını da ekle
        const dbFiloSatislar: Omit<DBSatis, 'id'>[] = filoSatislari.map(s => ({
            vardiyaId,
            personelId: 999, // Dummy ID
            personelAdi: 'OTOMASYON',
            personelKeyId: 'SYS001',
            pompaNo: s.pompaNo,
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

    getOnayBekleyenMarketVardiyalar(): Observable<MarketVardiya[]> {
        return from(this.dbService.marketVardiyalarGetir()).pipe(
            map(list => list
                .filter(v => v.durum === 'ONAY_BEKLIYOR')
                .map(v => ({
                    ...v,
                    id: v.id!,
                    durum: v.durum as VardiyaDurum
                }))
            )
        );
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
                paroPuan: p.paroPuan,
                mobilOdeme: p.mobilOdeme,
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

            // MARKET VERİLERİNİ YÜKLE
            // 1. Z Raporu
            const zRaporu = await this.dbService.marketZRaporuGetir(vardiyaId);
            if (zRaporu) {
                const mappedZRaporu: MarketZRaporu = {
                    id: zRaporu.id!,
                    vardiyaId: zRaporu.vardiyaId,
                    tarih: zRaporu.tarih,
                    genelToplam: zRaporu.genelToplam,
                    kdv0: zRaporu.kdv0,
                    kdv1: zRaporu.kdv1,
                    kdv10: zRaporu.kdv10,
                    kdv20: zRaporu.kdv20,
                    kdvToplam: zRaporu.kdvToplam,
                    kdvHaricToplam: zRaporu.kdvHaric,
                    olusturmaTarihi: zRaporu.olusturmaTarihi
                };
                this.marketZRaporu.next(mappedZRaporu);
            } else {
                this.marketZRaporu.next(null);
            }

            // 2. Market Tahsilat
            // Not: Tahsilat şu an sadece memory'de veya dbService'de eksik olabilir.
            // Fakat varsa yükleyelim:
            // const marketTahsilat = await this.dbService.marketTahsilatGetir(vardiyaId); // Böyle bir metod varsa...
            // Şimdilik subject'i sıfırlayalım ki eski data kalmasın
            this.marketTahsilat.next(null);

            // 3. Market Gelirleri
            const dbGelirler = await this.dbService.vardiyaGelirleriGetir(vardiyaId);
            const gelirler: MarketGelir[] = dbGelirler.map(g => ({
                id: g.id!,
                vardiyaId: g.vardiyaId,
                tarih: g.tarih,
                gelirTuru: g.gelirTuru as GelirTuru,
                tutar: g.tutar,
                aciklama: g.aciklama,
                olusturmaTarihi: g.olusturmaTarihi,
                belgeTarihi: g.tarih
            }));
            this.marketGelirler.next(gelirler);

            // 4. Market Giderleri
            // Şu an DB desteği yoksa boş array bas
            this.marketGiderler.next([]);

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

    // vardiyaBaslat silindi.
    vardiyaBaslat(sorumluId: number, istasyonId: number): Observable<Vardiya> {
        // Bu özellik kaldırıldı ancak type güvenliği için boş bırakıyorum,
        // bileşenlerden çağrılmayacak.
        return of({} as Vardiya);
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

    async marketVardiyaOnayaGonder(vardiyaId: number): Promise<void> {
        console.log('Market Vardiya onaya gönderiliyor. ID:', vardiyaId);
        // Burada DB seviyesinde onaya gönder işlemi yapılmalı (durum güncelleme)
        // Şimdilik doğrudan durumu güncelliyoruz.
        await this.dbService.marketVardiyaGuncelle(vardiyaId, { durum: 'ONAY_BEKLIYOR' });

        // Subject'i güncelle
        const vardiyalar = this.marketVardiyalar.value;
        const index = vardiyalar.findIndex(v => v.id === vardiyaId);
        if (index !== -1) {
            vardiyalar[index] = { ...vardiyalar[index], durum: VardiyaDurum.ONAY_BEKLIYOR };
            this.marketVardiyalar.next([...vardiyalar]);
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

    vardiyaOnayla(vardiyaId: number, onaylayanId?: number): Observable<Vardiya> {
        const vardiya = this.aktifVardiya.value;
        const currentUser = this.authService.getCurrentUser();

        // Eğer parametre olarak gelmediyse giriş yapan kullanıcıyı al
        const finalOnaylayanId = onaylayanId || 0; // Token'dan ID alamıyoruz şimdilik, 0 veya token decode edilmeli
        const finalOnaylayanAdi = currentUser ? currentUser.username : 'Sistem';

        // DB'de güncelle
        return from(this.dbService.onayla(vardiyaId, finalOnaylayanId, finalOnaylayanAdi)).pipe(
            map(() => {
                if (vardiya && vardiya.id === vardiyaId) {
                    const muhurlu: Vardiya = {
                        ...vardiya,
                        durum: VardiyaDurum.ONAYLANDI,
                        onaylayanId: finalOnaylayanId,
                        onaylayanAdi: finalOnaylayanAdi,
                        onayTarihi: new Date(),
                        guncellemeTarihi: new Date(),
                        kilitli: true // Mühürlendi
                    };
                    this.aktifVardiya.next(muhurlu);
                    return muhurlu;
                }
                // Eğer aktif vardiya bu değilse bile işlem başarılı, boş veya yeni halini döndürebiliriz
                // Ancak bu metod genellikle aktif vardiya üzerinde çağrılır.
                // API tutarlılığı için dummy bir obje veya tekrar fetch gerekebilir ama şimdilik:
                return { id: vardiyaId, durum: VardiyaDurum.ONAYLANDI } as Vardiya;
            })
        );
    }

    vardiyaReddet(vardiyaId: number, redNedeni: string): Observable<Vardiya> {
        const vardiya = this.aktifVardiya.value;
        const currentUser = this.authService.getCurrentUser();
        const onaylayanId = 0; // Token'dan ID alamıyoruz şimdilik
        const onaylayanAdi = currentUser ? currentUser.username : 'Sistem';

        return from(this.dbService.reddet(vardiyaId, onaylayanId, onaylayanAdi, redNedeni)).pipe(
            map(() => {
                if (vardiya && vardiya.id === vardiyaId) {
                    const reddedilmis: Vardiya = {
                        ...vardiya,
                        durum: VardiyaDurum.REDDEDILDI,
                        redNedeni,
                        guncellemeTarihi: new Date(),
                        kilitli: false // Düzenleme için açık
                    };
                    this.aktifVardiya.next(reddedilmis);
                    return reddedilmis;
                }
                return { id: vardiyaId, durum: VardiyaDurum.REDDEDILDI } as Vardiya;
            })
        );
    }

    async marketVardiyaOnayla(vardiyaId: number, onaylayanId: number, onaylayanAdi: string): Promise<void> {
        // DB güncelle
        await this.dbService.marketVardiyaGuncelle(vardiyaId, {
            durum: 'ONAYLANDI',
            onaylayanId,
            onaylayanAdi,
            onayTarihi: new Date()
        });

        // Subject güncelle
        const vardiyalar = this.marketVardiyalar.value;
        const index = vardiyalar.findIndex(v => v.id === vardiyaId);
        if (index !== -1) {
            vardiyalar[index] = {
                ...vardiyalar[index],
                durum: VardiyaDurum.ONAYLANDI,
                onaylayanId,
                onaylayanAdi,
                onayTarihi: new Date()
            };
            this.marketVardiyalar.next([...vardiyalar]);
        }
    }

    async marketVardiyaReddet(vardiyaId: number, redNedeni: string): Promise<void> {
        // DB güncelle
        await this.dbService.marketVardiyaGuncelle(vardiyaId, {
            durum: 'REDDEDILDI',
            redNedeni
        });

        // Subject güncelle
        const vardiyalar = this.marketVardiyalar.value;
        const index = vardiyalar.findIndex(v => v.id === vardiyaId);
        if (index !== -1) {
            vardiyalar[index] = {
                ...vardiyalar[index],
                durum: VardiyaDurum.REDDEDILDI,
                redNedeni
            };
            this.marketVardiyalar.next([...vardiyalar]);
        }
    }

    private temizle(): void {
        this.pusulaGirisleri.next([]);
        this.marketZRaporu.next(null);
        this.marketTahsilat.next(null);
        this.marketGiderler.next([]);
        this.marketGiderler.next([]);
        this.marketGelirler.next([]);
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
        const satislar = this.yuklenenSatislar.value;

        if (satislar.length === 0) {
            return of([]);
        }

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
            toplam: pusula.nakit + pusula.krediKarti + pusula.paroPuan + pusula.mobilOdeme,
            olusturmaTarihi: new Date()
        };

        const dbPusula: Omit<DBPusula, 'id'> = {
            vardiyaId: pusula.vardiyaId,
            personelId: pusula.personelId,
            personelAdi: pusula.personelAdi,
            nakit: pusula.nakit,
            krediKarti: pusula.krediKarti,
            paroPuan: pusula.paroPuan,
            mobilOdeme: pusula.mobilOdeme,
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
        return from(this.hesaplaFarkAnalizi(vardiyaId));
    }

    private async hesaplaFarkAnalizi(vardiyaId: number): Promise<PersonelFarkAnalizi[]> {
        const pusulalar = this.pusulaGirisleri.value;
        const otomasyonSatislari = this.yuklenenSatislar.value;

        // Otomasyon verilerini personellere göre grupla
        const otomasyonMap = new Map<number, { tutar: number, adi: string }>();
        otomasyonSatislari.forEach(s => {
            const mevcut = otomasyonMap.get(s.personelId) || { tutar: 0, adi: s.personelAdi };
            otomasyonMap.set(s.personelId, {
                tutar: mevcut.tutar + s.toplamTutar,
                adi: s.personelAdi
            });
        });

        // Pusulaları da ekle (Otomasyonda olmayan ama pusula girilen durumlar için)
        pusulalar.forEach(p => {
            if (!otomasyonMap.has(p.personelId)) {
                otomasyonMap.set(p.personelId, { tutar: 0, adi: p.personelAdi });
            }
        });

        const analizler: PersonelFarkAnalizi[] = [];

        for (const [personelId, veri] of otomasyonMap.entries()) {
            const pusula = pusulalar.find(p => p.personelId === personelId);

            const pusulaToplam = pusula?.toplam || 0;
            const otomasyonToplam = veri.tutar;
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
                personelAdi: veri.adi,
                otomasyonToplam,
                pusulaToplam,
                fark,
                farkDurum,
                pusulaDokum: {
                    nakit: pusula?.nakit || 0,
                    krediKarti: pusula?.krediKarti || 0,
                    paroPuan: pusula?.paroPuan || 0,
                    mobilOdeme: pusula?.mobilOdeme || 0
                }
            });
        }

        return analizler;
    }

    // ==========================================
    // MARKET YÖNETİMİ
    // ==========================================
    // MARKET YÖNETİMİ
    private marketVardiyalar = new BehaviorSubject<MarketVardiya[]>([]);
    private marketVardiyaPersonelList = new BehaviorSubject<MarketVardiyaPersonel[]>([]);

    getMarketVardiyalar(): Observable<MarketVardiya[]> {
        // DB'den yükle ve subject'i güncelle
        this.dbService.marketVardiyalarGetir().then(list => {
            const mappedList: MarketVardiya[] = list.map(v => ({
                ...v,
                id: v.id!, // DB'den gelen kayıtta id kesin vardır
                durum: v.durum as VardiyaDurum,
            }));
            this.marketVardiyalar.next(mappedList);
        });
        return this.marketVardiyalar.asObservable();
    }

    marketVardiyaBaslat(veri: { tarih: Date }): Observable<MarketVardiya> {
        const yeniVardiya: Omit<DBMarketVardiya, 'id'> = {
            tarih: veri.tarih,
            durum: 'ACIK',
            toplamSatisTutari: 0,
            toplamTeslimatTutari: 0,
            toplamFark: 0,
            olusturmaTarihi: new Date()
        };

        return from(this.dbService.marketVardiyaEkle(yeniVardiya)).pipe(
            map(id => {
                const olusturulan: MarketVardiya = {
                    ...yeniVardiya,
                    id,
                    durum: yeniVardiya.durum as VardiyaDurum,
                    toplamSatisTutari: yeniVardiya.toplamSatisTutari,
                    toplamTeslimatTutari: yeniVardiya.toplamTeslimatTutari,
                    toplamFark: yeniVardiya.toplamFark
                };
                const mevcut = this.marketVardiyalar.value;
                this.marketVardiyalar.next([olusturulan, ...mevcut]);
                return olusturulan;
            })
        );
    }

    getMarketVardiyaPersonelList(vardiyaId: number): Observable<MarketVardiyaPersonel[]> {
        this.dbService.marketPersonelIslemleriGetir(vardiyaId).then(list => {
            const mappedList: MarketVardiyaPersonel[] = list.map(p => ({
                ...p,
                id: p.id!
            }));
            this.marketVardiyaPersonelList.next(mappedList);
        });
        return this.marketVardiyaPersonelList.asObservable();
    }

    marketPersonelIslemKaydet(veri: Omit<MarketVardiyaPersonel, 'id' | 'olusturmaTarihi' | 'toplamTeslimat' | 'fark'>): Observable<MarketVardiyaPersonel> {
        const toplamTeslimat = veri.nakit + veri.krediKarti + (veri.gider || 0);
        const fark = toplamTeslimat - veri.sistemSatisTutari;

        const yeniKayit: any = {
            ...veri,
            toplamTeslimat,
            fark,
            olusturmaTarihi: new Date()
        };

        // Önce işlemi kaydet
        return from(this.dbService.marketPersonelIslemEkle(yeniKayit)).pipe(
            map(id => {
                yeniKayit.id = id;
                // Listeyi güncelle
                const mevcutList = this.marketVardiyaPersonelList.value;
                const existingIndex = mevcutList.findIndex(p => p.personelId === veri.personelId);

                let updatedList;
                if (existingIndex !== -1) {
                    updatedList = [...mevcutList];
                    updatedList[existingIndex] = yeniKayit;
                } else {
                    updatedList = [...mevcutList, yeniKayit];
                }
                this.marketVardiyaPersonelList.next(updatedList);

                // Vardiya Ozetini güncelle (DB + Subject)
                this.updateMarketVardiyaOzetPersistent(veri.vardiyaId);

                return yeniKayit;
            })
        );
    }

    private async updateMarketVardiyaOzetPersistent(vardiyaId: number) {
        // DB'den güncel listeyi çek (Transaction safe olması için)
        const personelList = await this.dbService.marketPersonelIslemleriGetir(vardiyaId);

        const toplamSatis = personelList.reduce((sum, p) => sum + p.sistemSatisTutari, 0);
        const toplamTeslimat = personelList.reduce((sum, p) => sum + p.toplamTeslimat, 0);
        const toplamFark = personelList.reduce((sum, p) => sum + p.fark, 0);

        // DB'yi güncelle
        await this.dbService.marketVardiyaGuncelle(vardiyaId, {
            toplamSatisTutari: toplamSatis,
            toplamTeslimatTutari: toplamTeslimat,
            toplamFark: toplamFark
        });

        // Subject'i güncelle
        const vardiyalar = this.marketVardiyalar.value;
        const index = vardiyalar.findIndex(v => v.id === vardiyaId);
        if (index !== -1) {
            vardiyalar[index] = {
                ...vardiyalar[index],
                toplamSatisTutari: toplamSatis,
                toplamTeslimatTutari: toplamTeslimat,
                toplamFark: toplamFark
            };
            this.marketVardiyalar.next([...vardiyalar]);
        }
    }

    marketZRaporuKaydet(zRaporu: Omit<MarketZRaporu, 'id' | 'olusturmaTarihi' | 'kdvToplam' | 'kdvHaricToplam'>): Observable<MarketZRaporu> {
        const kdvToplam = zRaporu.kdv0 + zRaporu.kdv1 + zRaporu.kdv10 + zRaporu.kdv20;
        const yeniZRaporu: any = {
            ...zRaporu,
            kdvToplam,
            kdvHaricToplam: zRaporu.genelToplam - kdvToplam,
            olusturmaTarihi: new Date()
        };

        return from(this.dbService.marketZRaporuKaydet(yeniZRaporu)).pipe(
            map(id => {
                const result = { ...yeniZRaporu, id };
                this.marketZRaporu.next(result);
                // Eğer Z Raporu varsa vardiyaya da işleyebiliriz ama şimdilik bağımsız tutuyoruz
                return result;
            })
        );
    }

    getMarketZRaporu(vardiyaId: number): Observable<MarketZRaporu | null> {
        // DB'den çek
        this.dbService.marketZRaporuGetir(vardiyaId).then(z => {
            // DB type ile Model type uyuşmazlığı olabilir, maplemek gerekebilir
            // DBMarketZRaporu -> MarketZRaporu
            if (z) {
                const mapped: MarketZRaporu = {
                    id: z.id!,
                    vardiyaId: z.vardiyaId,
                    tarih: z.tarih,
                    genelToplam: z.genelToplam,
                    kdv0: z.kdv0,
                    kdv1: z.kdv1,
                    kdv10: z.kdv10,
                    kdv20: z.kdv20,
                    kdvToplam: z.kdvToplam,
                    kdvHaricToplam: z.kdvHaric,
                    olusturmaTarihi: z.olusturmaTarihi
                };
                this.marketZRaporu.next(mapped);
            } else {
                this.marketZRaporu.next(null);
            }
        });
        return this.marketZRaporu.asObservable();
    }

    getMarketTahsilat(): Observable<MarketTahsilat | null> {
        return this.marketTahsilat.asObservable();
    }

    marketTahsilatKaydet(tahsilat: Omit<MarketTahsilat, 'id' | 'olusturmaTarihi' | 'toplam'>): Observable<MarketTahsilat> {
        const yeniTahsilat: MarketTahsilat = {
            ...tahsilat,
            id: Date.now(),
            toplam: tahsilat.nakit + tahsilat.krediKarti,
            olusturmaTarihi: new Date()
        };

        this.marketTahsilat.next(yeniTahsilat);
        return of(yeniTahsilat).pipe(delay(300));
    }

    // ==========================================
    // MARKET GELİR İŞLEMLERİ
    // ==========================================

    getMarketGelirler(vardiyaId: number): Observable<MarketGelir[]> {
        this.dbService.vardiyaGelirleriGetir(vardiyaId).then(list => {
            const mapped: MarketGelir[] = list.map(g => ({
                id: g.id!,
                vardiyaId: g.vardiyaId,
                tarih: g.tarih,
                gelirTuru: g.gelirTuru as GelirTuru,
                tutar: g.tutar,
                aciklama: g.aciklama,
                olusturmaTarihi: g.olusturmaTarihi,
                belgeTarihi: g.tarih
            }));
            this.marketGelirler.next(mapped);
        });
        return this.marketGelirler.asObservable();
    }

    marketGelirEkle(gelir: Omit<MarketGelir, 'id' | 'olusturmaTarihi'>): Observable<MarketGelir> {
        const dbGelir: any = {
            vardiyaId: gelir.vardiyaId,
            tarih: gelir.tarih,
            gelirTuru: gelir.gelirTuru,
            tutar: gelir.tutar,
            aciklama: gelir.aciklama,
            olusturmaTarihi: new Date()
        };

        return from(this.dbService.marketGelirEkle(dbGelir)).pipe(
            map(id => {
                const yeniGelir: MarketGelir = {
                    ...gelir,
                    id: id,
                    olusturmaTarihi: dbGelir.olusturmaTarihi
                };
                const mevcut = this.marketGelirler.value;
                this.marketGelirler.next([...mevcut, yeniGelir]);
                return yeniGelir;
            })
        );
    }

    marketGelirSil(gelirId: number): Observable<boolean> {
        return from(this.dbService.marketGelirSil(gelirId)).pipe(
            map(() => {
                const mevcut = this.marketGelirler.value;
                this.marketGelirler.next(mevcut.filter(g => g.id !== gelirId));
                return true;
            })
        );
    }

    getMarketGiderler(vardiyaId?: number): Observable<MarketGider[]> {
        // Market giderleri şu an memory-only görünüyor veya db entegrasyonu eksik.
        // İleride burası da DB'den çekilecek şekilde güncellenmeli.
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
        const gelirler = this.marketGelirler.value;

        const zRaporuToplam = zRaporu?.genelToplam || 0;
        const tahsilatToplam = tahsilat?.toplam || 0;
        const giderToplam = giderler.reduce((sum, g) => sum + g.tutar, 0);
        const gelirToplam = gelirler.reduce((sum, g) => sum + g.tutar, 0);
        const netKasa = tahsilatToplam + gelirToplam - giderToplam;

        const ozet: MarketOzet = {
            zRaporuToplam,
            tahsilatToplam,
            giderToplam,
            gelirToplam,
            netKasa,
            fark: zRaporuToplam - netKasa,
            tahsilatNakit: tahsilat?.nakit || 0,
            tahsilatKrediKarti: tahsilat?.krediKarti || 0,
            tahsilatParoPuan: 0,
            kdvDokum: {
                kdv0: zRaporu?.kdv0 || 0,
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
        return from(this.hesaplaPompaOzet(vardiyaId));
    }

    private async hesaplaPompaOzet(vardiyaId: number): Promise<PompaOzet> {
        const pusulalar = this.pusulaGirisleri.value;
        const giderler = this.pompaGiderler.value;
        const otomasyonSatislari = this.yuklenenSatislar.value;

        const personelIds = new Set(otomasyonSatislari.map(s => s.personelId));
        pusulalar.forEach(p => personelIds.add(p.personelId));

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
        for (const personelId of personelIds) {
            const pusula = pusulalar.find(p => p.personelId === personelId);
            const personelSatislari = otomasyonSatislari.filter(s => s.personelId === personelId);

            // Personel adını bul
            let personelAdi = 'Bilinmeyen';
            if (personelSatislari.length > 0) {
                personelAdi = personelSatislari[0].personelAdi;
            } else if (pusula) {
                personelAdi = pusula.personelAdi;
            }

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
                    paroPuan: pusula?.paroPuan || 0,
                    mobilOdeme: pusula?.mobilOdeme || 0
                }
            });
        }

        return {
            personelSayisi: personelIds.size,
            toplamOtomasyonSatis,
            toplamPusulaTahsilat,
            toplamFark,
            farkDurum,
            personelFarklari,
            giderToplam,
            netTahsilat: toplamPusulaTahsilat - giderToplam
        };
    }

    // ==========================================
    // GENEL ÖZET VE DASHBOARD
    // ==========================================

    getGenelOzet(vardiyaId: number): Observable<GenelOzet> {
        return from(this.hesaplaGenelOzet(vardiyaId));
    }

    private async hesaplaGenelOzet(vardiyaId: number): Promise<GenelOzet> {
        const pusulalar = this.pusulaGirisleri.value;
        const marketTahsilat = this.marketTahsilat.value;
        const pompaGiderler = this.pompaGiderler.value;
        const marketGiderler = this.marketGiderler.value;
        const zRaporu = this.marketZRaporu.value;
        const otomasyonSatislari = this.yuklenenSatislar.value;

        const pompaToplam = otomasyonSatislari.reduce((sum, s) => sum + s.toplamTutar, 0);
        const marketToplam = zRaporu?.genelToplam || 0;
        const genelToplam = pompaToplam + marketToplam;

        // Tahsilat Toplamları
        let toplamNakit = pusulalar.reduce((sum, p) => sum + p.nakit, 0);
        let toplamKrediKarti = pusulalar.reduce((sum, p) => sum + p.krediKarti, 0);
        let toplamParoPuan = pusulalar.reduce((sum, p) => sum + p.paroPuan, 0);
        let toplamMobilOdeme = pusulalar.reduce((sum, p) => sum + p.mobilOdeme, 0);

        if (marketTahsilat) {
            toplamNakit += marketTahsilat.nakit;
            toplamKrediKarti += marketTahsilat.krediKarti;
        }

        const toplamGider = pompaGiderler.reduce((sum, g) => sum + g.tutar, 0) +
            marketGiderler.reduce((sum, g) => sum + g.tutar, 0);

        const toplamTahsilat = toplamNakit + toplamKrediKarti + toplamParoPuan + toplamMobilOdeme;
        const netKasa = toplamTahsilat - toplamGider;
        const toplamFark = netKasa - genelToplam; // Basit hesap

        let durumRenk: 'success' | 'warn' | 'danger' = 'success';
        if (Math.abs(toplamFark) < 10) {
            durumRenk = 'success';
        } else if (Math.abs(toplamFark) < 100) {
            durumRenk = 'warn';
        } else {
            durumRenk = 'danger';
        }

        return {
            pompaToplam,
            marketToplam,
            genelToplam,
            toplamNakit,
            toplamKrediKarti,
            toplamParoPuan,
            toplamMobilOdeme,

            toplamGider,
            toplamFark,
            durumRenk
        };
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
            { label: 'Paro Puan', value: OdemeYontemi.PARO_PUAN },
            { label: 'Mobil Ödeme', value: OdemeYontemi.MOBIL_ODEME },
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

    getMarketPersonelleri(): Observable<{ label: string, value: number }[]> {
        return from(this.dbService.getPersoneller()).pipe(
            map(personeller => personeller
                .filter(p => p.rol === PersonelRol.MARKET_GOREVLISI)
                .map(p => ({
                    label: p.tamAd,
                    value: p.id!
                }))
            )
        );
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

    getGelirTurleri(): { label: string; value: GelirTuru }[] {
        return [
            { label: 'Komisyon', value: GelirTuru.KOMISYON },
            { label: 'Prim', value: GelirTuru.PRIM },
            { label: 'Diğer', value: GelirTuru.DIGER }
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
        return from(this.dbService.getPersoneller(istasyonId)).pipe(
            map(personeller => {
                return personeller
                    .filter(p => p.rol === 'VARDIYA_SORUMLUSU' || p.rol === PersonelRol.VARDIYA_SORUMLUSU)
                    .map(p => ({
                        id: p.id!,
                        ad: p.ad,
                        soyad: p.soyad,
                        kullaniciAdi: p.keyId,
                        istasyonId: p.istasyonId,
                        aktif: p.aktif
                    }));
            })
        );
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
        const source = this.yuklenenSatislar.value;

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
        const toplamSatis = this.yuklenenSatislar.value
            .reduce((sum, s) => sum + s.toplamTutar, 0);

        // Varsayılan dağılım
        return of([
            { odemeYontemi: OdemeYontemi.NAKIT, tutar: toplamSatis * 0.4 },
            { odemeYontemi: OdemeYontemi.KREDI_KARTI, tutar: toplamSatis * 0.35 },
            { odemeYontemi: OdemeYontemi.PARO_PUAN, tutar: toplamSatis * 0.15 },
            { odemeYontemi: OdemeYontemi.MOBIL_ODEME, tutar: toplamSatis * 0.1 }
        ]).pipe(delay(500));
    }

    karsilastirmaYap(vardiyaId: number): Observable<KarsilastirmaSonuc> {
        const otomasyonSatislari = this.yuklenenSatislar.value;
        const pusulalar = this.pusulaGirisleri.value;

        const sistemToplam = otomasyonSatislari.reduce((sum, s) => sum + s.toplamTutar, 0);

        // Calculate totals from Pusula
        const nakitToplam = pusulalar.reduce((sum, p) => sum + p.nakit, 0);
        const kkToplam = pusulalar.reduce((sum, p) => sum + p.krediKarti, 0);
        const paroPuanToplam = pusulalar.reduce((sum, p) => sum + p.paroPuan, 0);
        const mobilOdemeToplam = pusulalar.reduce((sum, p) => sum + p.mobilOdeme, 0);

        const tahsilatToplam = nakitToplam + kkToplam + paroPuanToplam + mobilOdemeToplam;

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
            { odemeYontemi: OdemeYontemi.PARO_PUAN, sistemTutar: 0, tahsilatTutar: paroPuanToplam, fark: paroPuanToplam },
            { odemeYontemi: OdemeYontemi.MOBIL_ODEME, sistemTutar: 0, tahsilatTutar: mobilOdemeToplam, fark: mobilOdemeToplam }
        ];

        return of({
            vardiyaId,
            sistemToplam,
            tahsilatToplam,
            fark,
            farkYuzde,
            detaylar,
            durum,
            pompaSatislari: []
        }).pipe(delay(500));
    }

    getVardiyaOzet(vardiyaId: number): Observable<VardiyaOzet> {
        const vardiya = this.aktifVardiya.value;
        const tahsilatlar = this.tahsilatlar.value;
        const otomasyonSatislari = this.yuklenenSatislar.value;

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
