import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { ChartModule } from 'primeng/chart';
import { ProgressBarModule } from 'primeng/progressbar';

import { VardiyaService } from '../services/vardiya.service';
import { Vardiya, VardiyaDurum, GenelOzet, PompaOzet, FarkDurum } from '../models/vardiya.model';

@Component({
    selector: 'app-vardiya-dashboard',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TagModule,
        DividerModule,
        ChartModule,
        ProgressBarModule
    ],
    templateUrl: './vardiya-dashboard.component.html',
    styleUrls: ['./vardiya-dashboard.component.scss']
})
export class VardiyaDashboard implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    pompaOzet: PompaOzet | null = null;
    genelOzet: GenelOzet | null = null;

    private subscriptions = new Subscription();

    constructor(private vardiyaService: VardiyaService) { }

    ngOnInit(): void {
        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                this.aktifVardiya = vardiya;
                if (vardiya) {
                    this.loadOzetler(vardiya.id);
                }
            })
        );
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    loadOzetler(vardiyaId: number): void {
        this.vardiyaService.getPompaOzet(vardiyaId).subscribe(ozet => {
            this.pompaOzet = ozet;
        });

        this.vardiyaService.getGenelOzet(vardiyaId).subscribe(ozet => {
            this.genelOzet = ozet;
        });
    }

    getVardiyaDurumLabel(): string {
        if (!this.aktifVardiya) return 'Kapalı';

        const labels: Record<VardiyaDurum, string> = {
            [VardiyaDurum.ACIK]: 'Açık',
            [VardiyaDurum.ONAY_BEKLIYOR]: 'Onay Bekliyor',
            [VardiyaDurum.ONAYLANDI]: 'Onaylandı',
            [VardiyaDurum.REDDEDILDI]: 'Reddedildi',
            [VardiyaDurum.SILINME_ONAYI_BEKLIYOR]: 'Silinme Onayı Bekliyor',
            [VardiyaDurum.SILINDI]: 'Silindi'
        };
        return labels[this.aktifVardiya.durum];
    }

    getVardiyaDurumSeverity(): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        if (!this.aktifVardiya) return 'secondary';

        const severities: Record<VardiyaDurum, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            [VardiyaDurum.ACIK]: 'success',
            [VardiyaDurum.ONAY_BEKLIYOR]: 'warn',
            [VardiyaDurum.ONAYLANDI]: 'info',
            [VardiyaDurum.REDDEDILDI]: 'danger',
            [VardiyaDurum.SILINME_ONAYI_BEKLIYOR]: 'warn',
            [VardiyaDurum.SILINDI]: 'secondary'
        };
        return severities[this.aktifVardiya.durum];
    }

    getGecenSure(): string {
        if (!this.aktifVardiya) return '-';

        const now = new Date();
        const start = new Date(this.aktifVardiya.baslangicTarihi);
        const diff = now.getTime() - start.getTime();

        const hours = Math.floor(diff / (1000 * 60 * 60));
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

        return `${hours} saat ${minutes} dakika`;
    }

    getPompaFarkCardClass(): string {
        if (!this.pompaOzet) return 'bg-gradient-to-br from-gray-500 to-gray-600';

        switch (this.pompaOzet.farkDurum) {
            case FarkDurum.UYUMLU:
                return 'bg-gradient-to-br from-green-500 to-green-600';
            case FarkDurum.ACIK:
                return 'bg-gradient-to-br from-red-500 to-red-600';
            case FarkDurum.FAZLA:
                return 'bg-gradient-to-br from-blue-500 to-blue-600';
            default:
                return 'bg-gradient-to-br from-gray-500 to-gray-600';
        }
    }

    getPompaFarkIcon(): string {
        if (!this.pompaOzet) return 'pi-minus';

        switch (this.pompaOzet.farkDurum) {
            case FarkDurum.UYUMLU:
                return 'pi-check';
            case FarkDurum.ACIK:
                return 'pi-arrow-down';
            case FarkDurum.FAZLA:
                return 'pi-arrow-up';
            default:
                return 'pi-minus';
        }
    }

    getPompaFarkAciklama(): string {
        if (!this.pompaOzet) return 'Veri bekleniyor';

        switch (this.pompaOzet.farkDurum) {
            case FarkDurum.UYUMLU:
                return 'Uyumlu';
            case FarkDurum.ACIK:
                return 'Kasa Açığı!';
            case FarkDurum.FAZLA:
                return 'Kasa Fazlası';
            default:
                return 'Belirsiz';
        }
    }

    async onayaGonder(): Promise<void> {
        if (this.aktifVardiya) {
            try {
                await this.vardiyaService.vardiyaOnayaGonder(this.aktifVardiya.id);
                // Başarılı
            } catch (err) {
                console.error('Onaya gönderilirken hata:', err);
            }
        }
    }
}
