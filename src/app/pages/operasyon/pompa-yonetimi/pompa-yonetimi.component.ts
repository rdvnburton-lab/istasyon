import { Component, OnInit, OnDestroy, ViewEncapsulation, NgZone } from '@angular/core';
import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressBarModule } from 'primeng/progressbar';
import { BadgeModule } from 'primeng/badge';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';

import { MessageService } from 'primeng/api';

import { VardiyaApiService } from '../services/vardiya-api.service';
import { PersonelApiService } from '../services/personel-api.service';

import { PusulaApiService, Pusula, KrediKartiDetay } from '../../../services/pusula-api.service';
import { DefinitionsService, DefinitionType } from '../../../services/definitions.service';

interface PersonelOtomasyonOzet {
    personelAdi: string;
    personelId?: number;
    toplamLitre: number;
    toplamTutar: number;
    islemSayisi: number;
}

@Component({
    selector: 'app-pompa-yonetimi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TableModule,
        TagModule,
        DividerModule,
        InputNumberModule,
        TextareaModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ProgressBarModule,
        BadgeModule,
        SelectModule,
        InputTextModule
    ],
    providers: [MessageService],
    templateUrl: './pompa-yonetimi.component.html',
    styleUrls: ['./pompa-yonetimi.component.scss']
})
export class PompaYonetimi implements OnInit, OnDestroy {
    vardiyaId: number | null = null;
    vardiya: any = null;
    pusulalar: Pusula[] = [];
    personelOzetler: PersonelOtomasyonOzet[] = [];

    // Math nesnesini template'de kullanmak için
    Math = Math;

    // Dialog states
    pusulaDialogVisible = false;
    krediKartiDialogVisible = false;
    loading = false;
    analyzing = false;

    // Form
    seciliPersonel: PersonelOtomasyonOzet | null = null;
    pusulaForm: Pusula = this.getEmptyPusula();

    // Kredi Kartı Detayları
    bankalar: string[] = [];
    yeniKrediKarti: KrediKartiDetay = { banka: '', tutar: 0 };
    anlikKrediKartiDetaylari: KrediKartiDetay[] = [];

    // Özet
    otomasyonToplam = 0;
    personelSatisToplam = 0;
    pusulaToplam = 0;
    filoToplam = 0;
    fark = 0;

    // Ödeme Türü Özetleri
    toplamNakit = 0;
    toplamKrediKarti = 0;


    // Dinamik Diğer Ödemeler Toplamları (TurKodu -> Toplam)
    digerOdemelerToplam: { turKodu: string, turAdi: string, toplam: number }[] = [];

    // Dinamik Tanımlar
    odemeYontemleri: any[] = [];
    pusulaTurleri: any[] = [];
    giderTurleri: any[] = [];
    giderler: any[] = [];

    // Gider Dialog
    giderDialogVisible = false;
    giderForm: any = { giderTuru: '', tutar: 0, aciklama: '' };

    // Diğer Ödeme Yöntemleri
    yeniDigerOdeme: any = { tur: null, tutar: 0 };
    digerOdemeDialogVisible = false;

    // Önizleme
    onizlemeDialogVisible = false;
    onizlemeData: any = null;
    currentDate = new Date();

    get isEditable(): boolean {
        return this.vardiya && (
            this.vardiya.durum === 'ACIK' ||
            this.vardiya.durum === 'REDDEDILDI' ||
            this.vardiya.durum === 0 ||
            this.vardiya.durum === 3
        );
    }

    getToplamLitre(): number {
        return this.personelOzetler.reduce((sum, p) => sum + (p.toplamLitre || 0), 0);
    }

    // Personel Düzenleme
    personelDuzenleDialogVisible = false;
    duzenlenecekPersonel: any = {};

    private subscriptions = new Subscription();

    constructor(
        private vardiyaApiService: VardiyaApiService,
        private pusulaApiService: PusulaApiService,
        private personelApiService: PersonelApiService,
        private messageService: MessageService,
        private router: Router,
        private route: ActivatedRoute,
        private ngZone: NgZone,
        private definitionsService: DefinitionsService
    ) { }

    ngOnInit(): void {
        // Route'dan vardiyaId al
        this.route.params.subscribe(params => {
            const id = params['id'];
            if (id) {
                this.vardiyaId = +id;
                this.loadVardiyaData();
            } else {
                this.router.navigate(['/vardiya']);
            }
        });

        this.loadBankalar();
    }

    loadBankalar() {
        this.definitionsService.getByType(DefinitionType.BANKA).subscribe(data => {
            this.bankalar = data.map(b => b.name);
        });

        // Ödeme Yöntemleri
        this.definitionsService.getByType(DefinitionType.ODEME).subscribe(data => {
            this.odemeYontemleri = data;
        });

        // Pusula Türleri (Pusula için 'Diğer' ödeme tipleri)
        this.definitionsService.getByType(DefinitionType.PUSULA_TURU).subscribe(data => {
            // Ödeme yöntemlerinden Paro ve Mobil'i de ekle (eğer yoksa)
            const methods = [...data];
            if (!methods.find(m => m.code === 'PARO_PUAN')) {
                methods.push({ name: 'Paro Puan', code: 'PARO_PUAN' } as any);
            }
            if (!methods.find(m => m.code === 'MOBIL_ODEME')) {
                methods.push({ name: 'Mobil Ödeme', code: 'MOBIL_ODEME' } as any);
            }
            this.pusulaTurleri = methods;
        });

        // Gider Türleri
        this.definitionsService.getByType(DefinitionType.POMPA_GIDER).subscribe(data => {
            this.giderTurleri = data;
        });
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    loadVardiyaData(): void {
        if (!this.vardiyaId) return;

        this.loading = true;
        console.time('⚡ Mutabakat Data Load');

        // OPTIMIZED: Single API call with pre-aggregated data
        this.vardiyaApiService.getMutabakat(this.vardiyaId).subscribe({
            next: (data) => {
                console.timeEnd('⚡ Mutabakat Data Load');
                console.log(`📊 Performance: ${data._performanceMs}ms (server-side)`);

                // Vardiya bilgilerini al
                this.vardiya = data.vardiya;
                this.otomasyonToplam = data.vardiya.genelToplam;

                // Personel özetleri zaten gruplu geliyor
                this.personelOzetler = (data.personelOzetler || []).map((p: any) => ({
                    personelAdi: p.personelAdi,
                    personelId: p.personelId,
                    toplamLitre: p.toplamLitre,
                    toplamTutar: p.toplamTutar,
                    islemSayisi: p.islemSayisi
                }));

                // Filo Satışları
                if (data.filoOzet && data.filoOzet.toplamTutar > 0) {
                    this.filoToplam = data.filoOzet.toplamTutar;
                    this.personelOzetler.push({
                        personelAdi: 'FİLO SATIŞLARI',
                        personelId: -1,
                        toplamTutar: data.filoOzet.toplamTutar,
                        toplamLitre: data.filoOzet.toplamLitre,
                        islemSayisi: data.filoOzet.islemSayisi
                    });

                    // Filo detaylarını sakla (önizleme için)
                    this.vardiya.filoDetaylari = data.filoDetaylari;
                }

                // Pompacı satış toplamı = Personel satışları (Filo hariç)
                this.personelSatisToplam = this.personelOzetler
                    .filter(p => p.personelAdi !== 'FİLO SATIŞLARI')
                    .reduce((sum, p) => sum + p.toplamTutar, 0);

                // Pusulalar (Paro ve Mobil'i DigerOdemeler listesine konsolide et)
                this.pusulalar = (data.pusulalar || []).map((p: any) => {
                    const mappedDigerOdemeler = [...(p.digerOdemeler || [])];

                    return {
                        ...p,
                        nakit: p.nakit,
                        krediKarti: p.krediKarti,
                        krediKartiDetay: p.krediKartiDetay ? (typeof p.krediKartiDetay === 'string' ? JSON.parse(p.krediKartiDetay) : p.krediKartiDetay) : [],
                        digerOdemeler: mappedDigerOdemeler,
                        aciklama: p.aciklama,
                        toplam: p.toplam
                    };
                });

                this.giderler = data.giderler || [];

                this.calculateOzet();
                this.loading = false;
            },
            error: (err) => {
                console.error('❌ Mutabakat yüklenirken hata:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Vardiya bilgileri yüklenemedi'
                });
                this.loading = false;
            }
        });
    }

    // REMOVED: createPersonelOzetler - no longer needed, data comes pre-aggregated
    // REMOVED: loadPusulalar - data comes in single getMutabakat call

    calculateOzet(): void {
        this.toplamNakit = this.pusulalar.reduce((sum, p) => sum + p.nakit, 0);
        this.toplamKrediKarti = this.pusulalar.reduce((sum, p) => sum + p.krediKarti, 0);


        const toplamDigerOdemeler = this.pusulalar.reduce((sum, p) =>
            sum + (p.digerOdemeler?.reduce((subSum, d) => subSum + (d.tutar || 0), 0) || 0), 0);

        // Diğer ödemeleri türe göre grupla
        const digerOdemelerMap = new Map<string, { turAdi: string, toplam: number }>();
        this.pusulalar.forEach(p => {
            p.digerOdemeler?.forEach(d => {
                const existing = digerOdemelerMap.get(d.turKodu);
                if (existing) {
                    existing.toplam += d.tutar;
                } else {
                    digerOdemelerMap.set(d.turKodu, { turAdi: d.turAdi, toplam: d.tutar });
                }
            });
        });

        // Map'i array'e çevir
        this.digerOdemelerToplam = Array.from(digerOdemelerMap.entries()).map(([turKodu, data]) => ({
            turKodu,
            turAdi: data.turAdi,
            toplam: data.toplam
        }));

        this.pusulaToplam = this.toplamNakit + this.toplamKrediKarti + toplamDigerOdemeler;

        const toplamGider = this.getToplamGider();

        // Fark = (Pusula Toplamı + Filo Satışları + Giderler) - Otomasyon Toplamı
        this.fark = (this.pusulaToplam + this.filoToplam + toplamGider) - this.otomasyonToplam;
    }

    personelSec(personel: PersonelOtomasyonOzet): void {
        this.seciliPersonel = personel;

        // Bu personel için pusula var mı kontrol et
        const mevcutPusula = this.pusulalar.find(p => p.personelAdi === personel.personelAdi);

        if (mevcutPusula) {
            // Arrays need deep copy
            this.pusulaForm = {
                ...mevcutPusula,
                digerOdemeler: [...(mevcutPusula.digerOdemeler || [])],
                krediKartiDetay: [...(mevcutPusula.krediKartiDetay || [])]
            };
        } else {
            this.pusulaForm = this.getEmptyPusula();
            this.pusulaForm.personelAdi = personel.personelAdi;
            this.pusulaForm.personelId = personel.personelId;
        }

        this.pusulaDialogVisible = true;
    }

    getPusulaFormToplam(): number {
        if (!this.pusulaForm) return 0;
        const digerToplam = this.pusulaForm.digerOdemeler?.reduce((sum, d) => sum + (d.tutar || 0), 0) || 0;
        return (this.pusulaForm.nakit || 0) +
            (this.pusulaForm.krediKarti || 0) +
            digerToplam;
    }

    // Diğer Ödeme İşlemleri
    digerOdemeEkle() {
        if (!this.yeniDigerOdeme.tur || this.yeniDigerOdeme.tutar <= 0) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen tür ve tutar giriniz' });
            return;
        }

        if (!this.pusulaForm.digerOdemeler) {
            this.pusulaForm.digerOdemeler = [];
        }

        this.pusulaForm.digerOdemeler.push({
            turKodu: this.yeniDigerOdeme.tur.code,
            turAdi: this.yeniDigerOdeme.tur.name,
            tutar: this.yeniDigerOdeme.tutar
        });

        this.yeniDigerOdeme = { tur: null, tutar: 0 };
    }

    addDigerOdemeByCode(code: string, tutar: number) {
        if (!this.pusulaForm.digerOdemeler) {
            this.pusulaForm.digerOdemeler = [];
        }

        const existing = this.pusulaForm.digerOdemeler.find(d => d.turKodu === code);
        if (existing) {
            existing.tutar += tutar;
        } else {
            const def = this.pusulaTurleri.find(t => t.code === code);
            this.pusulaForm.digerOdemeler.push({
                turKodu: code,
                turAdi: def ? def.name : code,
                tutar: tutar
            });
        }
    }

    digerOdemeSil(index: number) {
        this.pusulaForm.digerOdemeler?.splice(index, 1);
    }

    getDigerOdemeToplam(): number {
        return this.pusulaForm.digerOdemeler?.reduce((sum, item) => sum + (item.tutar || 0), 0) || 0;
    }

    getPusulaFormFark(): number {
        if (!this.seciliPersonel) return 0;
        return this.getPusulaFormToplam() - this.seciliPersonel.toplamTutar;
    }

    getPusulaFarkClass(): string {
        const fark = this.getPusulaFormFark();
        if (Math.abs(fark) < 1) return 'text-green-600';
        if (fark < 0) return 'text-red-600';
        return 'text-blue-600';
    }

    savePusula(): void {
        if (!this.vardiyaId || !this.seciliPersonel) return;

        this.loading = true;
        this.pusulaForm.vardiyaId = this.vardiyaId;



        if (this.pusulaForm.id) {
            // Güncelleme
            this.pusulaApiService.update(this.vardiyaId, this.pusulaForm.id, this.pusulaForm).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Başarılı',
                        detail: 'Pusula güncellendi'
                    });
                    this.loadVardiyaData();
                    this.hideDialog();
                },
                error: (err) => {
                    console.error('Güncelleme hatası:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Hata',
                        detail: err.error?.message || 'Pusula güncellenemedi'
                    });
                    this.loading = false;
                }
            });
        } else {
            // Yeni ekleme
            this.pusulaApiService.create(this.vardiyaId, this.pusulaForm).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Başarılı',
                        detail: 'Pusula kaydedildi'
                    });
                    this.loadVardiyaData();
                    this.hideDialog();
                },
                error: (err) => {
                    console.error('Ekleme hatası:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Hata',
                        detail: err.error?.message || 'Pusula eklenemedi'
                    });
                    this.loading = false;
                }
            });
        }
    }


    async scanReceipt() {
        try {
            const image = await Camera.getPhoto({
                quality: 90,
                allowEditing: false,
                width: 1024, // Resize to reduce token usage
                resultType: CameraResultType.Base64,
                source: CameraSource.Prompt // Ask user: Camera or Photos
            });

            if (image.base64String) {
                // Capacitor plugins runs outside Angular zone, need to re-enter
                this.ngZone.run(() => {
                    this.analyzing = true;
                    this.messageService.add({ severity: 'info', summary: 'İşleniyor', detail: 'Fiş taranıyor, lütfen bekleyin...' });

                    this.pusulaApiService.analyzeImage(image.base64String!).subscribe({
                        next: (data) => {
                            this.ngZone.run(() => {
                                this.analyzing = false;

                                // Temel alanları doldur
                                if (data.nakit !== undefined) this.pusulaForm.nakit = data.nakit;
                                if (data.krediKarti !== undefined) this.pusulaForm.krediKarti = data.krediKarti;

                                // Listeleri temizle (yeni dolum için)
                                this.pusulaForm.digerOdemeler = [];
                                this.pusulaForm.krediKartiDetay = [];

                                // Diğer Ödemeler (Paro, Mobil vs.) - Yeni yapı: digerOdemeler listesi
                                if (data.digerOdemeler && Array.isArray(data.digerOdemeler)) {
                                    data.digerOdemeler.forEach((d: any) => {
                                        this.addDigerOdemeByCode(d.turKodu, d.tutar);
                                    });
                                } else {
                                    // Fallback: Eski yapıdan gelen paroPuan ve mobilOdeme varsa ekle
                                    if (data.paroPuan > 0) this.addDigerOdemeByCode('PARO_PUAN', data.paroPuan);
                                    if (data.mobilOdeme > 0) this.addDigerOdemeByCode('MOBIL_ODEME', data.mobilOdeme);
                                }

                                // Kredi kartı detaylarını işle
                                if (data.krediKartiDetay && data.krediKartiDetay.length > 0) {
                                    this.pusulaForm.krediKartiDetay = data.krediKartiDetay.map((d: any) => ({
                                        banka: d.banka,
                                        tutar: d.tutar
                                    }));
                                }

                                this.messageService.add({ severity: 'success', summary: 'Tamamlandı', detail: 'Fiş bilgileri forma dolduruldu.' });
                            });
                        },
                        error: (err) => {
                            this.analyzing = false;
                            console.error('OCR Error Full:', JSON.stringify(err));
                            console.error('OCR Error Status:', err.status);
                            console.error('OCR Error Message:', err.message);
                            if (err.error) {
                                console.error('OCR Error Body:', JSON.stringify(err.error));
                            }
                            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Fiş okunamadı veya bir hata oluştu. Detaylar konsolda.' });
                        }
                    });
                });
            } else {
                console.warn('Image capture returned no base64 string');
                this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Fotoğraf verisi alınamadı.' });
            }
        } catch (error) {
            console.error('Camera Error:', error);
            // User cancelled or permission denied
            if (error !== 'User cancelled photos app') {
                this.messageService.add({ severity: 'error', summary: 'Kamera Hatası', detail: 'Kamera açılamadı veya işlem iptal edildi.' });
            }
        }
    }

    deletePusula(pusula: Pusula): void {
        if (!pusula.id || !this.vardiyaId) return;

        if (!confirm(`${pusula.personelAdi} için girilen pusulayı silmek istediğinize emin misiniz?`)) {
            return;
        }

        this.pusulaApiService.delete(this.vardiyaId, pusula.id).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Pusula silindi'
                });
                this.loadVardiyaData();
            },
            error: (err) => {
                console.error('Silme hatası:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Pusula silinemedi'
                });
            }
        });
    }

    // Kredi Kartı İşlemleri
    krediKartiDialogAc(): void {
        this.anlikKrediKartiDetaylari = [...(this.pusulaForm.krediKartiDetay || [])];
        this.yeniKrediKarti = { banka: '', tutar: 0 };
        this.krediKartiDialogVisible = true;
    }

    krediKartiEkle(): void {
        if (this.yeniKrediKarti.banka && this.yeniKrediKarti.tutar > 0) {
            this.anlikKrediKartiDetaylari.push({ ...this.yeniKrediKarti });
            this.yeniKrediKarti = { banka: '', tutar: 0 };
        }
    }

    krediKartiSil(index: number): void {
        this.anlikKrediKartiDetaylari.splice(index, 1);
    }

    getAnlikKrediKartiToplam(): number {
        return this.anlikKrediKartiDetaylari.reduce((sum, item) => sum + item.tutar, 0);
    }

    krediKartiOnayla(): void {
        this.pusulaForm.krediKartiDetay = [...this.anlikKrediKartiDetaylari];
        this.pusulaForm.krediKarti = this.getAnlikKrediKartiToplam();
        this.krediKartiDialogVisible = false;
    }

    hideDialog(): void {
        this.pusulaDialogVisible = false;
        this.seciliPersonel = null;
    }

    getFarkClass(): string {
        if (Math.abs(this.fark) < 1) return 'text-green-600';
        if (this.fark < 0) return 'text-red-600';
        return 'text-blue-600';
    }

    getFarkIcon(): string {
        if (Math.abs(this.fark) < 1) return 'pi-check-circle';
        if (this.fark < 0) return 'pi-arrow-down';
        return 'pi-arrow-up';
    }

    isPusulaGirildi(personelAdi: string): boolean {
        if (personelAdi === 'FİLO SATIŞLARI') return true;
        return this.pusulalar.some(p => p.personelAdi === personelAdi);
    }

    getPusulaTutar(personelAdi: string): number {
        if (personelAdi === 'FİLO SATIŞLARI') {
            const filo = this.personelOzetler.find(p => p.personelAdi === 'FİLO SATIŞLARI');
            return filo ? filo.toplamTutar : 0;
        }
        const pusula = this.pusulalar.find(p => p.personelAdi === personelAdi);
        if (!pusula) return 0;

        const digerToplam = pusula.digerOdemeler?.reduce((sum, d) => sum + (d.tutar || 0), 0) || 0;
        return (pusula.nakit || 0) + (pusula.krediKarti || 0) + digerToplam;
    }

    getPusulaFark(personelAdi: string, otomasyonTutar: number): number {
        const pusulaTutar = this.getPusulaTutar(personelAdi);
        return pusulaTutar - otomasyonTutar;
    }

    getPersonelFarkClass(fark: number): string {
        if (Math.abs(fark) < 1) return 'text-green-600';
        if (fark < 0) return 'text-red-600';
        return 'text-blue-600';
    }

    private getEmptyPusula(): Pusula {
        return {
            vardiyaId: this.vardiyaId || 0,
            personelAdi: '',
            nakit: 0,
            krediKarti: 0,

            krediKartiDetay: [],
            digerOdemeler: [],
            aciklama: ''
        };
    }

    getOdemeAdi(code: string): string {
        const item = this.odemeYontemleri.find(x => x.code === code);
        return item ? item.name : code;
    }

    onizle(personel: PersonelOtomasyonOzet): void {
        if (personel.personelAdi === 'FİLO SATIŞLARI') {
            const filoSatislar = this.vardiya.filoSatislar || [];
            const groups: { [key: string]: number } = {};

            filoSatislar.forEach((s: any) => {
                let groupName = s.filoKodu;
                // Eğer filo kodu sayı içeriyorsa 'Otobilim' olarak grupla
                if (/\d/.test(s.filoKodu)) {
                    groupName = 'Otobilim';
                }

                if (!groups[groupName]) groups[groupName] = 0;
                groups[groupName] += s.tutar;
            });

            this.onizlemeData = {
                personel: personel,
                isFilo: true,
                filoDetaylari: Object.keys(groups).map(k => ({ ad: k, tutar: groups[k] })),
                toplamTutar: personel.toplamTutar,
                pusula: { // Dummy pusula objesi
                    nakit: 0,
                    krediKarti: 0,

                    aciklama: 'Otomatik Filo Tahsilatı'
                },
                fark: 0 // Filo satışlarında fark olmaz (teorik olarak)
            };
            this.onizlemeDialogVisible = true;
            return;
        }

        const pusula = this.pusulalar.find(p => p.personelAdi === personel.personelAdi);
        if (!pusula) return;

        const toplamTahsilat = this.getPusulaTutar(personel.personelAdi);

        this.onizlemeData = {
            personel: personel,
            isFilo: false,
            pusula: pusula,
            fark: toplamTahsilat - personel.toplamTutar
        };
        this.onizlemeDialogVisible = true;
    }

    onizlemeDuzenle(): void {
        if (this.onizlemeData) {
            this.onizlemeDialogVisible = false;
            this.personelSec(this.onizlemeData.personel);
        }
    }

    geriDon(): void {
        this.router.navigate(['/vardiya']);
    }

    onayaGonder(): void {
        if (!this.vardiyaId) return;

        if (!confirm('Mutabakatı tamamlayıp onaya göndermek istediğinize emin misiniz? Bu işlemden sonra değişiklik yapılamaz.')) {
            return;
        }

        this.loading = true;
        this.vardiyaApiService.vardiyaOnayaGonder(this.vardiyaId).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Vardiya onaya gönderildi.'
                });
                this.router.navigate(['/vardiya']);
            },
            error: (err) => {
                console.error('Onaya gönderme hatası:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: err.error?.message || 'Onaya gönderilemedi.'
                });
                this.loading = false;
            }
        });
    }


    personelDuzenle(personel: PersonelOtomasyonOzet): void {
        if (!personel.personelId || personel.personelId <= 0) return;

        this.loading = true;
        this.personelApiService.getPersonelById(personel.personelId).subscribe({
            next: (data) => {
                this.duzenlenecekPersonel = { ...data };
                this.personelDuzenleDialogVisible = true;
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Personel bilgileri alınamadı' });
                this.loading = false;
            }
        });
    }

    personelKaydet(): void {
        if (!this.duzenlenecekPersonel.id) return;

        this.loading = true;
        this.personelApiService.updatePersonel(this.duzenlenecekPersonel.id, this.duzenlenecekPersonel).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Personel güncellendi' });
                this.personelDuzenleDialogVisible = false;
                this.loadVardiyaData();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Güncelleme başarısız' });
                this.loading = false;
            }
        });
    }

    // Gider İşlemleri
    giderEkle(): void {
        this.giderForm = { giderTuru: '', tutar: 0, aciklama: '' };
        this.giderDialogVisible = true;
    }

    saveGider(): void {
        if (!this.vardiyaId || !this.giderForm.giderTuru || this.giderForm.tutar <= 0) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen tür ve tutar girin' });
            return;
        }

        this.loading = true;
        this.vardiyaApiService.addPompaGider(this.vardiyaId, this.giderForm).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider eklendi' });
                this.giderDialogVisible = false;
                this.loadVardiyaData();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Gider eklenemedi' });
                this.loading = false;
            }
        });
    }

    deleteGider(id: number): void {
        if (!confirm('Bu gideri silmek istediğinize emin misiniz?')) return;

        this.vardiyaApiService.deletePompaGider(this.vardiyaId!, id).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider silindi' });
                this.loadVardiyaData();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Gider silinemedi' });
            }
        });
    }

    getGiderAdi(code: string): string {
        const item = this.giderTurleri.find(x => x.code === code);
        return item ? item.name : code;
    }

    getToplamGider(): number {
        return this.giderler.reduce((sum, item) => sum + item.tutar, 0);
    }
}
