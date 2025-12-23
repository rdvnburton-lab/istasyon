import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ToolbarModule } from 'primeng/toolbar';
import { VardiyaApiService } from '../../services/vardiya-api.service';

@Component({
    selector: 'app-vardiya-raporu',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DatePickerModule,
        CardModule,
        TagModule,
        ToolbarModule
    ],
    templateUrl: './vardiya-raporu.component.html',
    styles: [`:host { display: block; }`]
})
export class VardiyaRaporuComponent implements OnInit {
    baslangicTarihi: Date = new Date();
    bitisTarihi: Date = new Date();

    ozet = {
        toplamVardiya: 0,
        toplamTutar: 0,
        toplamLitre: 0,
        toplamIade: 0,
        toplamGider: 0
    };

    vardiyalar: any[] = [];
    loading: boolean = false;

    constructor(private vardiyaApiService: VardiyaApiService) {
        // Varsayılan olarak bu ayın başı ve sonu
        const bugun = new Date();
        this.baslangicTarihi = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
        this.bitisTarihi = new Date(bugun.getFullYear(), bugun.getMonth() + 1, 0);
    }

    ngOnInit() {
        this.raporla();
    }

    raporla() {
        this.loading = true;

        // Saat ayarları: Başlangıç 00:00:00, Bitiş 23:59:59
        const start = new Date(this.baslangicTarihi);
        start.setHours(0, 0, 0, 0);

        const end = new Date(this.bitisTarihi);
        end.setHours(23, 59, 59, 999);

        this.vardiyaApiService.getVardiyaRaporu(start, end).subscribe({
            next: (sonuc) => {
                this.ozet = sonuc.ozet;
                this.vardiyalar = sonuc.vardiyalar;
                this.loading = false;
            },
            error: (err) => {
                console.error('Rapor hatası:', err);
                this.loading = false;
            }
        });
    }

    tarihAyarla(tip: 'bugun' | 'dun' | 'buAy' | 'gecenAy') {
        const bugun = new Date();

        switch (tip) {
            case 'bugun':
                this.baslangicTarihi = bugun;
                this.bitisTarihi = bugun;
                break;
            case 'dun':
                const dun = new Date(bugun);
                dun.setDate(dun.getDate() - 1);
                this.baslangicTarihi = dun;
                this.bitisTarihi = dun;
                break;
            case 'buAy':
                this.baslangicTarihi = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
                this.bitisTarihi = new Date(bugun.getFullYear(), bugun.getMonth() + 1, 0);
                break;
            case 'gecenAy':
                this.baslangicTarihi = new Date(bugun.getFullYear(), bugun.getMonth() - 1, 1);
                this.bitisTarihi = new Date(bugun.getFullYear(), bugun.getMonth(), 0);
                break;
        }
        this.raporla();
    }
}
