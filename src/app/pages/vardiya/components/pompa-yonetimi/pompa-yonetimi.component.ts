import { Component, OnInit, OnDestroy } from '@angular/core';
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

import { VardiyaApiService } from '../../services/vardiya-api.service';
import { PusulaApiService, Pusula, KrediKartiDetay } from '../../../../services/pusula-api.service';

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

    // Form
    seciliPersonel: PersonelOtomasyonOzet | null = null;
    pusulaForm: Pusula = this.getEmptyPusula();

    // Kredi Kartı Detayları
    bankalar = ['Ziraat Bankası', 'Garanti BBVA', 'İş Bankası', 'Yapı Kredi', 'Akbank', 'Halkbank', 'Vakıfbank', 'QNB Finansbank', 'Denizbank'];
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
    toplamParoPuan = 0;
    toplamMobilOdeme = 0;

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

    private subscriptions = new Subscription();

    constructor(
        private vardiyaApiService: VardiyaApiService,
        private pusulaApiService: PusulaApiService,
        private messageService: MessageService,
        private router: Router,
        private route: ActivatedRoute
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

                // Pusulalar zaten geliyor
                this.pusulalar = (data.pusulalar || []).map((p: any) => ({
                    id: p.id,
                    vardiyaId: this.vardiyaId,
                    personelAdi: p.personelAdi,
                    personelId: p.personelId,
                    nakit: p.nakit,
                    krediKarti: p.krediKarti,
                    paroPuan: p.paroPuan,
                    mobilOdeme: p.mobilOdeme,
                    krediKartiDetay: p.krediKartiDetay ? JSON.parse(p.krediKartiDetay) : [],
                    aciklama: p.aciklama,
                    toplam: p.toplam
                }));

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
        this.toplamParoPuan = this.pusulalar.reduce((sum, p) => sum + p.paroPuan, 0);
        this.toplamMobilOdeme = this.pusulalar.reduce((sum, p) => sum + p.mobilOdeme, 0);

        this.pusulaToplam = this.toplamNakit + this.toplamKrediKarti + this.toplamParoPuan + this.toplamMobilOdeme;

        // Fark = (Pusula Toplamı + Filo Satışları) - Otomasyon Toplamı
        this.fark = (this.pusulaToplam + this.filoToplam) - this.otomasyonToplam;
    }

    personelSec(personel: PersonelOtomasyonOzet): void {
        this.seciliPersonel = personel;

        // Bu personel için pusula var mı kontrol et
        const mevcutPusula = this.pusulalar.find(p => p.personelAdi === personel.personelAdi);

        if (mevcutPusula) {
            this.pusulaForm = { ...mevcutPusula };
        } else {
            this.pusulaForm = this.getEmptyPusula();
            this.pusulaForm.personelAdi = personel.personelAdi;
            this.pusulaForm.personelId = personel.personelId;
        }

        this.pusulaDialogVisible = true;
    }

    getPusulaFormToplam(): number {
        return this.pusulaForm.nakit + this.pusulaForm.krediKarti +
            this.pusulaForm.paroPuan + this.pusulaForm.mobilOdeme;
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
        return pusula ? (pusula.nakit + pusula.krediKarti + pusula.paroPuan + pusula.mobilOdeme) : 0;
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
            paroPuan: 0,
            mobilOdeme: 0,
            krediKartiDetay: []
        };
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
                    paroPuan: 0,
                    mobilOdeme: 0,
                    aciklama: 'Otomatik Filo Tahsilatı'
                },
                fark: 0 // Filo satışlarında fark olmaz (teorik olarak)
            };
            this.onizlemeDialogVisible = true;
            return;
        }

        const pusula = this.pusulalar.find(p => p.personelAdi === personel.personelAdi);
        if (!pusula) return;

        const toplamTahsilat = pusula.nakit + pusula.krediKarti + pusula.paroPuan + pusula.mobilOdeme;

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
}
