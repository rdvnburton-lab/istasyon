import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ToolbarModule } from 'primeng/toolbar';
import { VardiyaApiService } from '../../services/vardiya-api.service';
import { PersonelApiService } from '../../services/personel-api.service';

@Component({
    selector: 'app-personel-karnesi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DatePickerModule,
        SelectModule,
        CardModule,
        TagModule,
        ToolbarModule
    ],
    templateUrl: './personel-karnesi.component.html',
    styleUrls: ['./personel-karnesi.component.scss']
})
export class PersonelKarnesiComponent implements OnInit {
    baslangicTarihi: Date = new Date();
    bitisTarihi: Date = new Date();
    secilenPersonel: any = null;
    personeller: any[] = [];

    // Mobile State
    showMobileFilters = false;

    ozet = {
        toplamSatis: 0,
        toplamTahsilat: 0,
        toplamFark: 0,
        toplamLitre: 0,
        aracSayisi: 0,
        ortalamaLitre: 0,
        ortalamaTutar: 0,
        yakitDagilimi: [] as { yakit: string, litre: number, tutar: number, oran: number }[]
    };

    hareketler: any[] = [];
    loading: boolean = false;
    raporGoster: boolean = false;

    constructor(
        private vardiyaApiService: VardiyaApiService,
        private personelApiService: PersonelApiService
    ) {
        const bugun = new Date();
        this.baslangicTarihi = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
        this.bitisTarihi = new Date(bugun.getFullYear(), bugun.getMonth() + 1, 0);
    }

    ngOnInit() {
        this.personelApiService.getPersoneller().subscribe(data => {
            this.personeller = data;
        });
    }

    raporla() {
        if (!this.secilenPersonel) return;

        this.loading = true;
        this.raporGoster = true;

        const start = new Date(this.baslangicTarihi);
        start.setHours(0, 0, 0, 0);

        const end = new Date(this.bitisTarihi);
        end.setHours(23, 59, 59, 999);

        this.vardiyaApiService.getPersonelKarnesi(this.secilenPersonel.id, start, end).subscribe({
            next: (sonuc) => {
                this.ozet = sonuc.ozet;
                this.hareketler = sonuc.hareketler;
                this.loading = false;
            },
            error: (err) => {
                console.error('Personel karnesi hatası:', err);
                this.loading = false;
            }
        });
    }
    get isPompaci(): boolean {
        if (!this.secilenPersonel) return false;
        return this.secilenPersonel.rol === 'POMPACI' || this.secilenPersonel.rol === 'VARDIYA_SORUMLUSU';
    }
}
