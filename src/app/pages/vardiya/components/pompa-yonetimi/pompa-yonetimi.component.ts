import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
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

import { MessageService } from 'primeng/api';

import { VardiyaService } from '../../services/vardiya.service';
import {
    Vardiya,
    PersonelOtomasyonOzet,
    PusulaGirisi,
    PersonelFarkAnalizi,
    FarkDurum,
    PompaGider,
    PompaGiderTuru,
    PompaOzet
} from '../../models/vardiya.model';

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
        SelectModule
    ],
    providers: [MessageService],
    templateUrl: './pompa-yonetimi.component.html',
    styleUrls: ['./pompa-yonetimi.component.scss']
})
export class PompaYonetimi implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    personelOtomasyonlar: PersonelOtomasyonOzet[] = [];
    pusulalar: PusulaGirisi[] = [];
    giderler: PompaGider[] = [];
    farkAnalizleri: PersonelFarkAnalizi[] = [];
    pompaOzet: PompaOzet | null = null;

    // Mutabakat durumu
    mutabakatTamamlandi = false;
    eksikPersonelSayisi = 0;

    seciliPersonel: PersonelOtomasyonOzet | null = null;
    pusulaForm: {
        nakit: number;
        krediKarti: number;
        veresiye: number;
        filoKarti: number;
        krediKartiDetay: { banka: string; tutar: number }[];
    } = { nakit: 0, krediKarti: 0, veresiye: 0, filoKarti: 0, krediKartiDetay: [] };

    giderDialogVisible = false;
    giderTurleri: { label: string; value: PompaGiderTuru }[] = [];
    giderForm = {
        giderTuru: null as PompaGiderTuru | null,
        tutar: 0,
        aciklama: ''
    };

    // Kredi Kartı Detayları
    bankalar = ['Ziraat Bankası', 'Garanti BBVA', 'İş Bankası', 'Yapı Kredi', 'Akbank', 'Halkbank', 'Vakıfbank', 'QNB Finansbank', 'Denizbank'];
    krediKartiDialogVisible = false;
    yeniKrediKarti = { banka: '', tutar: 0 };
    anlikKrediKartiDetaylari: { banka: string; tutar: number }[] = [];

    loading = false;
    private subscriptions = new Subscription();

    constructor(
        private vardiyaService: VardiyaService,
        private messageService: MessageService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.giderTurleri = this.vardiyaService.getPompaGiderTurleri();

        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                this.aktifVardiya = vardiya;
                if (vardiya) {
                    this.loadData(vardiya.id);
                } else {
                    this.router.navigate(['/vardiya']);
                }
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getPusulaGirisleri().subscribe((pusulalar: PusulaGirisi[]) => {
                this.pusulalar = pusulalar;
                this.updateOzet();
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getPompaGiderler().subscribe((giderler: PompaGider[]) => {
                this.giderler = giderler;
                this.updateOzet();
            })
        );
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    loadData(vardiyaId: number): void {
        this.vardiyaService.getPersonelOtomasyonOzet(vardiyaId).subscribe((data: PersonelOtomasyonOzet[]) => {
            this.personelOtomasyonlar = data;
            this.updateOzet();
        });
    }

    updateOzet(): void {
        if (this.aktifVardiya) {
            this.vardiyaService.getPompaOzet(this.aktifVardiya.id).subscribe((ozet: PompaOzet) => {
                this.pompaOzet = ozet;
                this.checkMutabakatDurumu();
            });
        }
    }

    checkMutabakatDurumu(): void {
        const personelIds = this.personelOtomasyonlar.map(p => p.personelId);
        const pusulaPersonelIds = this.pusulalar.map(p => p.personelId);
        const eksikPersonelIds = personelIds.filter(id => !pusulaPersonelIds.includes(id));

        this.eksikPersonelSayisi = eksikPersonelIds.length;
        this.mutabakatTamamlandi = eksikPersonelIds.length === 0 && personelIds.length > 0;
    }

    async onayaGonder(): Promise<void> {
        if (!this.aktifVardiya) return;

        if (!this.mutabakatTamamlandi) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Eksik Pusula',
                detail: `${this.eksikPersonelSayisi} personelin pusula girişi eksik!`
            });
            return;
        }

        try {
            await this.vardiyaService.vardiyaOnayaGonder(this.aktifVardiya.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Başarılı',
                detail: 'Vardiya onaya gönderildi!'
            });
            this.router.navigate(['/vardiya']);
        } catch (error) {
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Onaya gönderme başarısız!'
            });
        }
    }

    personelSec(personel: PersonelOtomasyonOzet): void {
        this.seciliPersonel = personel;
        this.onPersonelSec();
    }

    onPersonelSec(): void {
        if (this.seciliPersonel) {
            const mevcutPusula = this.pusulalar.find(p => p.personelId === this.seciliPersonel!.personelId);
            if (mevcutPusula) {
                this.pusulaForm = {
                    nakit: mevcutPusula.nakit,
                    krediKarti: mevcutPusula.krediKarti,
                    veresiye: mevcutPusula.veresiye,
                    filoKarti: mevcutPusula.filoKarti,
                    krediKartiDetay: mevcutPusula.krediKartiDetay || []
                };
            } else {
                this.pusulaForm = { nakit: 0, krediKarti: 0, veresiye: 0, filoKarti: 0, krediKartiDetay: [] };
            }
        }
    }

    getPusulaFormToplam(): number {
        return this.pusulaForm.nakit + this.pusulaForm.krediKarti +
            this.pusulaForm.veresiye + this.pusulaForm.filoKarti;
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

    async pusulaKaydet(): Promise<void> {
        if (!this.seciliPersonel || !this.aktifVardiya) return;

        this.loading = true;

        try {
            await this.vardiyaService.pusulaGirisiEkle({
                vardiyaId: this.aktifVardiya.id,
                personelId: this.seciliPersonel.personelId,
                personelAdi: this.seciliPersonel.personelAdi,
                nakit: this.pusulaForm.nakit,
                krediKarti: this.pusulaForm.krediKarti,
                veresiye: this.pusulaForm.veresiye,
                filoKarti: this.pusulaForm.filoKarti,
                krediKartiDetay: this.pusulaForm.krediKartiDetay
            });

            this.loading = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Pusula kaydedildi' });
            this.seciliPersonel = null;
            this.pusulaForm = { nakit: 0, krediKarti: 0, veresiye: 0, filoKarti: 0, krediKartiDetay: [] };
        } catch (error) {
            this.loading = false;
            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kayıt başarısız' });
        }
    }

    async pusulaSil(personelId: number): Promise<void> {
        await this.vardiyaService.pusulaGirisiSil(personelId);
        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Pusula silindi' });
    }

    farkAnaliziYap(): void {
        if (this.aktifVardiya) {
            this.vardiyaService.getFarkAnalizi(this.aktifVardiya.id).subscribe((analizler: PersonelFarkAnalizi[]) => {
                this.farkAnalizleri = analizler;
            });
        }
    }

    getTotalLitre(): number {
        return this.personelOtomasyonlar.reduce((sum, p) => sum + p.toplamLitre, 0);
    }

    getPusulaTutar(personelId: number): number {
        const pusula = this.pusulalar.find(p => p.personelId === personelId);
        return pusula ? pusula.toplam : 0;
    }

    getPusulaFark(personelId: number, otomasyonTutar: number): number {
        const pusulaTutar = this.getPusulaTutar(personelId);
        // Eğer pusula girişi hiç yoksa farkı 0 veya eksi otomasyon tutarı olarak gösterebiliriz.
        // Genelde pusula girilmediyse farkın tamamı açık olarak görünür.
        return pusulaTutar - otomasyonTutar;
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

    // Gider işlemleri
    giderDialogAc(): void {
        this.giderForm = { giderTuru: null, tutar: 0, aciklama: '' };
        this.giderDialogVisible = true;
    }

    async giderEkle(): Promise<void> {
        if (!this.giderForm.giderTuru || !this.giderForm.tutar || !this.aktifVardiya) return;

        try {
            await this.vardiyaService.pompaGiderEkle({
                vardiyaId: this.aktifVardiya.id,
                giderTuru: this.giderForm.giderTuru,
                tutar: this.giderForm.tutar,
                aciklama: this.giderForm.aciklama || this.getGiderTuruLabel(this.giderForm.giderTuru)
            });

            this.giderDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider eklendi' });
        } catch (error) {
            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Gider eklenemedi' });
        }
    }

    async giderSil(giderId: number): Promise<void> {
        try {
            await this.vardiyaService.pompaGiderSil(giderId);
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider silindi' });
        } catch (error) {
            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Gider silinemedi' });
        }
    }

    getGiderToplam(): number {
        return this.giderler.reduce((sum, g) => sum + g.tutar, 0);
    }

    getGiderTuruLabel(turu: PompaGiderTuru): string {
        const found = this.giderTurleri.find(t => t.value === turu);
        return found?.label || turu;
    }

    // Yardımcı metodlar
    getFarkCardClass(): string {
        if (!this.pompaOzet) return 'bg-gradient-to-br from-gray-500 to-gray-600';
        if (this.pompaOzet.farkDurum === FarkDurum.UYUMLU) return 'bg-gradient-to-br from-green-500 to-green-600';
        if (this.pompaOzet.farkDurum === FarkDurum.ACIK) return 'bg-gradient-to-br from-red-500 to-red-600';
        return 'bg-gradient-to-br from-blue-500 to-blue-600';
    }

    getFarkIcon(): string {
        if (!this.pompaOzet) return 'pi-minus';
        if (this.pompaOzet.farkDurum === FarkDurum.UYUMLU) return 'pi-check';
        if (this.pompaOzet.farkDurum === FarkDurum.ACIK) return 'pi-arrow-down';
        return 'pi-arrow-up';
    }

    getFarkAciklama(): string {
        if (!this.pompaOzet) return 'Veri bekleniyor';
        if (this.pompaOzet.farkDurum === FarkDurum.UYUMLU) return 'Uyumlu';
        if (this.pompaOzet.farkDurum === FarkDurum.ACIK) return 'Kasa Açığı!';
        return 'Kasa Fazlası';
    }

    getFarkBackground(): string {
        if (!this.pompaOzet) return 'linear-gradient(135deg, #6b7280 0%, #4b5563 100%)';
        if (this.pompaOzet.farkDurum === FarkDurum.UYUMLU) return 'linear-gradient(135deg, #22c55e 0%, #16a34a 100%)';
        if (this.pompaOzet.farkDurum === FarkDurum.ACIK) return 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)';
        return 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)';
    }

    getFarkShadow(): string {
        if (!this.pompaOzet) return '0 10px 40px rgba(107, 114, 128, 0.3)';
        if (this.pompaOzet.farkDurum === FarkDurum.UYUMLU) return '0 10px 40px rgba(34, 197, 94, 0.3)';
        if (this.pompaOzet.farkDurum === FarkDurum.ACIK) return '0 10px 40px rgba(239, 68, 68, 0.3)';
        return '0 10px 40px rgba(59, 130, 246, 0.3)';
    }


    getPersonelFarkClass(fark: number): string {
        if (Math.abs(fark) < 1) return 'text-green-600';
        if (fark < 0) return 'text-red-600';
        return 'text-blue-600';
    }

    getFarkDurumLabel(durum: FarkDurum): string {
        const labels = { [FarkDurum.UYUMLU]: 'Uyumlu', [FarkDurum.ACIK]: 'Açık', [FarkDurum.FAZLA]: 'Fazla' };
        return labels[durum];
    }

    isPusulaGirildi(personelId: number): boolean {
        return this.pusulalar.some(p => p.personelId === personelId);
    }

    getFarkDurumSeverity(durum: FarkDurum): 'success' | 'danger' | 'info' {
        const severities = { [FarkDurum.UYUMLU]: 'success' as const, [FarkDurum.ACIK]: 'danger' as const, [FarkDurum.FAZLA]: 'info' as const };
        return severities[durum];
    }
}
