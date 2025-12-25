import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, delay, map } from 'rxjs';
import {
    Vardiya,
    VardiyaDurum,
    OtomasyonSatis,
    PusulaGirisi,
    FarkDurum,
    MarketZRaporu,
    MarketTahsilat,
    MarketGider,
    MarketGelir,
    MarketVardiya,
    MarketVardiyaPersonel,
    PompaGider,
    VardiyaOzet,
    Tahsilat,
    OdemeYontemi,
    KarsilastirmaSonuc,
    KarsilastirmaDurum,
    KarsilastirmaDetay,
    PompaSatis,
    FiloSatis,
    GiderTuru,
    GelirTuru,
    MarketOzet
} from '../models/vardiya.model';

import { VardiyaApiService } from './vardiya-api.service';
import { PusulaApiService } from '../../../services/pusula-api.service';
import { MarketApiService } from './market-api.service';
import { AuthService } from '../../../services/auth.service';

@Injectable({
    providedIn: 'root'
})
export class VardiyaService {
    constructor(

        private vardiyaApiService: VardiyaApiService,
        private pusulaApiService: PusulaApiService,
        private marketApiService: MarketApiService,
        private authService: AuthService
    ) { }

    private aktifVardiya = new BehaviorSubject<Vardiya | null>(null);
    private pusulaGirisleri = new BehaviorSubject<PusulaGirisi[]>([]);
    private marketZRaporu = new BehaviorSubject<MarketZRaporu | null>(null);
    private marketTahsilat = new BehaviorSubject<MarketTahsilat | null>(null);
    private marketGiderler = new BehaviorSubject<MarketGider[]>([]);

    private marketGelirler = new BehaviorSubject<MarketGelir[]>([]);
    private pompaGiderler = new BehaviorSubject<PompaGider[]>([]);
    private marketVardiyalar = new BehaviorSubject<MarketVardiya[]>([]);
    private marketVardiyaPersonelList = new BehaviorSubject<MarketVardiyaPersonel[]>([]);

    // Yüklenen dosyadan gelen satışlar
    private yuklenenSatislar = new BehaviorSubject<OtomasyonSatis[]>([]);

    // Eski uyumluluk için
    private tahsilatlar = new BehaviorSubject<Tahsilat[]>([]);

    // ==========================================
    // İSTASYON / PERSONEL
    // ==========================================

    // ==========================================
    // İSTASYON / PERSONEL
    // ==========================================

    // İstasyon ve Personel işlemleri artık ilgili servisler üzerinden yapılmalıdır.


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
        return this.vardiyaApiService.getOnayBekleyenVardiyalar().pipe(
            map(list => list.map(v => ({
                ...v,
                durum: VardiyaDurum.ONAY_BEKLIYOR,
                istasyonId: 1, // Default
                istasyonAdi: 'Merkez İstasyon', // Default
                kilitli: true
            })))
        );
    }

    async vardiyaEkle(vardiya: Vardiya, satislar: OtomasyonSatis[], filoSatislari: FiloSatis[] = []): Promise<number> {
        // Bu metod artık kullanılmıyor, VardiyaApiService.createVardiya kullanılıyor.
        // Geriye dönük uyumluluk için dummy implementation
        return 0;
    }

    getOnayBekleyenMarketVardiyalar(): Observable<MarketVardiya[]> {
        return this.marketApiService.getMarketVardiyalar().pipe(
            map(list => list.filter(v => v.durum === 'ONAY_BEKLIYOR'))
        );
    }

    async setAktifVardiyaById(vardiyaId: number): Promise<void> {
        // Backend'den çek
        this.vardiyaApiService.getMutabakat(vardiyaId).subscribe(data => {
            const vardiya = data.vardiya;
            this.aktifVardiya.next(vardiya);

            // Pusulaları yükle
            if (data.pusulalar) {
                const pusulalar: PusulaGirisi[] = data.pusulalar.map((p: any) => ({
                    ...p,
                    krediKartiDetay: typeof p.krediKartiDetay === 'string' ? JSON.parse(p.krediKartiDetay) : p.krediKartiDetay
                }));
                this.pusulaGirisleri.next(pusulalar);
            } else {
                // Eğer mutabakat endpoint'i pusulaları dönmüyorsa ayrıca çek
                this.pusulaApiService.getAll(vardiyaId).subscribe(pusulalar => {
                    // Mapping gerekebilir
                    const mapped = pusulalar.map(p => ({
                        id: p.id!,
                        vardiyaId: p.vardiyaId,
                        personelId: p.personelId || 0,
                        personelAdi: p.personelAdi,
                        nakit: p.nakit,
                        krediKarti: p.krediKarti,
                        paroPuan: p.paroPuan,
                        mobilOdeme: p.mobilOdeme,
                        toplam: (p.nakit + p.krediKarti + p.paroPuan + p.mobilOdeme),
                        krediKartiDetay: p.krediKartiDetay || [],
                        olusturmaTarihi: p.olusturmaTarihi || new Date()
                    }));
                    this.pusulaGirisleri.next(mapped);
                });
            }

            // Pompa Giderleri
            if (data.giderler) {
                this.pompaGiderler.next(data.giderler);
            }

            // Market Verileri
            if (data.marketVardiya) {
                // Market verilerini ilgili subjectlere dağıt
                // Şimdilik basitçe marketVardiyalar listesine ekleyelim veya güncelleyelim
                const mv = data.marketVardiya;
                const currentList = this.marketVardiyalar.value;
                const index = currentList.findIndex(x => x.id === mv.id);
                if (index > -1) {
                    currentList[index] = mv;
                    this.marketVardiyalar.next([...currentList]);
                } else {
                    this.marketVardiyalar.next([...currentList, mv]);
                }
            }
        });
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

        return new Promise((resolve, reject) => {
            this.vardiyaApiService.vardiyaOnayaGonder(vardiyaId).subscribe({
                next: () => {
                    const vardiya = this.aktifVardiya.value;
                    if (vardiya && vardiya.id === vardiyaId) {
                        const guncellenmis: Vardiya = {
                            ...vardiya,
                            durum: VardiyaDurum.ONAY_BEKLIYOR,
                            guncellemeTarihi: new Date()
                        };
                        this.aktifVardiya.next(guncellenmis);
                    }
                    resolve();
                },
                error: (err) => reject(err)
            });
        });
    }

    async marketVardiyaOnayaGonder(vardiyaId: number): Promise<void> {
        this.marketApiService.onayaGonder(vardiyaId).subscribe(() => {
            // Subject'i güncelle
            const vardiyalar = this.marketVardiyalar.value;
            const index = vardiyalar.findIndex(v => v.id === vardiyaId);
            if (index !== -1) {
                vardiyalar[index] = { ...vardiyalar[index], durum: VardiyaDurum.ONAY_BEKLIYOR };
                this.marketVardiyalar.next([...vardiyalar]);
            }
        });
    }



    vardiyaOnayla(vardiyaId: number, onaylayanId?: number): Observable<Vardiya> {
        const vardiya = this.aktifVardiya.value;
        const currentUser = this.authService.getCurrentUser();

        // Eğer parametre olarak gelmediyse giriş yapan kullanıcıyı al
        const finalOnaylayanId = onaylayanId || 0;
        const finalOnaylayanAdi = currentUser ? currentUser.username : 'Sistem';

        return this.vardiyaApiService.vardiyaOnayla(vardiyaId, finalOnaylayanId, finalOnaylayanAdi).pipe(
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
                return { id: vardiyaId, durum: VardiyaDurum.ONAYLANDI } as Vardiya;
            })
        );
    }

    vardiyaReddet(vardiyaId: number, redNedeni: string): Observable<Vardiya> {
        const vardiya = this.aktifVardiya.value;
        const currentUser = this.authService.getCurrentUser();
        const onaylayanId = 0; // Token'dan ID alamıyoruz şimdilik
        const onaylayanAdi = currentUser ? currentUser.username : 'Sistem';

        return this.vardiyaApiService.vardiyaReddet(vardiyaId, onaylayanId, onaylayanAdi, redNedeni).pipe(
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

    getGiderTurleri() {
        return Object.keys(GiderTuru).map(key => ({ label: key, value: GiderTuru[key as keyof typeof GiderTuru] }));
    }

    getGelirTurleri() {
        return Object.keys(GelirTuru).map(key => ({ label: key, value: GelirTuru[key as keyof typeof GelirTuru] }));
    }

    getMarketOzet(): Observable<MarketOzet | null> {
        return of(null);
    }
}
