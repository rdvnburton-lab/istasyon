import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ToolbarModule } from 'primeng/toolbar';
import { SelectButtonModule } from 'primeng/selectbutton';
import { VardiyaApiService } from '../../services/vardiya-api.service';
import { MarketApiService } from '../../services/market-api.service';
import { AuthService } from '../../../../services/auth.service';

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
        ToolbarModule,
        SelectButtonModule
    ],
    templateUrl: './vardiya-raporu.component.html',
    styles: [`:host { display: block; }`]
})
export class VardiyaRaporuComponent implements OnInit {
    baslangicTarihi: Date = new Date();
    bitisTarihi: Date = new Date();

    ozet: any = {
        toplamVardiya: 0,
        toplamTutar: 0,
        toplamLitre: 0,
        toplamIade: 0,
        toplamGider: 0,
        toplamSatis: 0,
        toplamTeslimat: 0,
        toplamFark: 0
    };

    vardiyalar: any[] = [];
    loading: boolean = false;

    raporTuru: 'pompa' | 'market' = 'pompa';
    raporTurleri = [
        { label: 'Pompa Raporu', value: 'pompa', icon: 'pi pi-bolt' },
        { label: 'Market Raporu', value: 'market', icon: 'pi pi-shopping-bag' }
    ];

    userRole: string = '';

    constructor(
        private vardiyaApiService: VardiyaApiService,
        private marketApiService: MarketApiService,
        private authService: AuthService
    ) {
        // Varsayılan olarak bu ayın başı ve sonu
        const bugun = new Date();
        this.baslangicTarihi = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
        this.bitisTarihi = new Date(bugun.getFullYear(), bugun.getMonth() + 1, 0);
    }

    ngOnInit() {
        const user = this.authService.getCurrentUser();
        this.userRole = user?.role?.toLowerCase() || '';

        const isMarketSorumlusu = this.userRole === 'market sorumlusu' || this.userRole === 'market_sorumlusu';

        if (isMarketSorumlusu) {
            this.raporTuru = 'market';
            this.raporTurleri = [
                { label: 'Market Raporu', value: 'market', icon: 'pi pi-shopping-bag' }
            ];
        } else {
            this.raporTuru = 'pompa';
        }

        this.raporla();
    }

    raporla() {
        this.loading = true;

        const start = new Date(this.baslangicTarihi);
        start.setHours(0, 0, 0, 0);

        const end = new Date(this.bitisTarihi);
        end.setHours(23, 59, 59, 999);

        if (this.raporTuru === 'pompa') {
            this.vardiyaApiService.getVardiyaRaporu(start, end).subscribe({
                next: (sonuc) => {
                    this.ozet = sonuc.ozet;
                    this.vardiyalar = sonuc.vardiyalar;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Pompa raporu hatası:', err);
                    this.loading = false;
                }
            });
        } else {
            this.marketApiService.getMarketRaporu(start, end).subscribe({
                next: (sonuc) => {
                    this.ozet = sonuc.ozet;
                    this.vardiyalar = sonuc.vardiyalar;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Market raporu hatası:', err);
                    this.loading = false;
                }
            });
        }
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
