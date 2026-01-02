import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { MessageService } from 'primeng/api';
import { VardiyaApiService } from '../../operasyon/services/vardiya-api.service';
import { VardiyaLog } from '../../operasyon/models/vardiya-log.model';

@Component({
    selector: 'app-vardiya-loglar',
    standalone: true,
    imports: [
        CommonModule,
        TableModule,
        TagModule,
        ButtonModule,
        CardModule,
        TooltipModule,
        ToastModule,
        InputTextModule,
        IconFieldModule,
        InputIconModule
    ],
    providers: [MessageService],
    templateUrl: './vardiya-loglar.component.html',
    styleUrls: ['./vardiya-loglar.component.scss']
})
export class VardiyaLoglarComponent implements OnInit {
    loglar: VardiyaLog[] = [];
    loading = false;
    searchTerm = '';

    constructor(private vardiyaApiService: VardiyaApiService) { }

    ngOnInit(): void {
        this.loglariYukle();
    }

    loglariYukle(): void {
        this.loading = true;
        this.vardiyaApiService.getVardiyaLoglari(undefined, 200).subscribe({
            next: (data) => {
                this.loglar = data.map(log => ({
                    ...log,
                    islemTarihi: new Date(log.islemTarihi)
                }));
                this.loading = false;
            },
            error: (err) => {
                console.error('Loglar yüklenirken hata:', err);
                this.loading = false;
            }
        });
    }

    getIslemSeverity(islem: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
        const severities: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary'> = {
            'OLUSTURULDU': 'info',
            'ONAYA_GONDERILDI': 'warn',
            'ONAYLANDI': 'success',
            'REDDEDILDI': 'danger',
            'SILME_TALEP_EDILDI': 'warn',
            'SILINDI': 'secondary',
            'SILME_REDDEDILDI': 'info'
        };
        return severities[islem] || 'secondary';
    }

    getIslemIcon(islem: string): string {
        const icons: Record<string, string> = {
            'OLUSTURULDU': 'pi-plus-circle',
            'ONAYA_GONDERILDI': 'pi-send',
            'ONAYLANDI': 'pi-check-circle',
            'REDDEDILDI': 'pi-times-circle',
            'SILME_TALEP_EDILDI': 'pi-trash',
            'SILINDI': 'pi-ban',
            'SILME_REDDEDILDI': 'pi-undo'
        };
        return icons[islem] || 'pi-info-circle';
    }

    getIslemLabel(islem: string): string {
        const labels: Record<string, string> = {
            'OLUSTURULDU': 'Oluşturuldu',
            'ONAYA_GONDERILDI': 'Onaya Gönderildi',
            'ONAYLANDI': 'Onaylandı',
            'REDDEDILDI': 'Reddedildi',
            'SILME_TALEP_EDILDI': 'Silme Talep Edildi',
            'SILINDI': 'Silindi',
            'SILME_REDDEDILDI': 'Silme Reddedildi'
        };
        return labels[islem] || islem;
    }
}
