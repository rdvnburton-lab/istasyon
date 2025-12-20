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
import { DbService, DBPersonel } from '../../services/db.service';

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
    styles: [`:host { display: block; }`]
})
export class PersonelKarnesiComponent implements OnInit {
    baslangicTarihi: Date = new Date();
    bitisTarihi: Date = new Date();
    secilenPersonel: DBPersonel | null = null;
    personeller: DBPersonel[] = [];

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

    constructor(private dbService: DbService) {
        const bugun = new Date();
        this.baslangicTarihi = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
        this.bitisTarihi = new Date(bugun.getFullYear(), bugun.getMonth() + 1, 0);
    }

    async ngOnInit() {
        this.personeller = await this.dbService.getPersoneller();
    }

    async raporla() {
        if (!this.secilenPersonel) return;

        this.loading = true;
        this.raporGoster = true;
        try {
            const start = new Date(this.baslangicTarihi);
            start.setHours(0, 0, 0, 0);

            const end = new Date(this.bitisTarihi);
            end.setHours(23, 59, 59, 999);

            const sonuc = await this.dbService.getPersonelKarnesi(this.secilenPersonel.id!, start, end);
            this.ozet = sonuc.ozet;
            this.hareketler = sonuc.hareketler;
        } finally {
            this.loading = false;
        }
    }
}
