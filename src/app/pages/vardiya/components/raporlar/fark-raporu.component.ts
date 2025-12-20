import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ToolbarModule } from 'primeng/toolbar';
import { DbService } from '../../services/db.service';

@Component({
    selector: 'app-fark-raporu',
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
    templateUrl: './fark-raporu.component.html',
    styles: [`:host { display: block; }`]
})
export class FarkRaporuComponent implements OnInit {
    baslangicTarihi: Date = new Date();
    bitisTarihi: Date = new Date();

    ozet = {
        toplamFark: 0,
        toplamAcik: 0,
        toplamFazla: 0,
        vardiyaSayisi: 0,
        acikVardiyaSayisi: 0,
        fazlaVardiyaSayisi: 0
    };

    vardiyalar: any[] = [];
    loading: boolean = false;
    expandedRows: Record<string, boolean> = {};
    Math = Math;

    constructor(private dbService: DbService) {
        const bugun = new Date();
        this.baslangicTarihi = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
        this.bitisTarihi = new Date(bugun.getFullYear(), bugun.getMonth() + 1, 0);
    }

    ngOnInit() {
        this.raporla();
    }

    async raporla() {
        this.loading = true;
        try {
            const start = new Date(this.baslangicTarihi);
            start.setHours(0, 0, 0, 0);

            const end = new Date(this.bitisTarihi);
            end.setHours(23, 59, 59, 999);

            const sonuc = await this.dbService.getFarkRaporu(start, end);
            this.ozet = sonuc.ozet;
            this.vardiyalar = sonuc.vardiyalar;
        } finally {
            this.loading = false;
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

    toggleRow(vardiyaId: number) {
        const newExpandedRows = { ...this.expandedRows };
        if (newExpandedRows[vardiyaId]) {
            delete newExpandedRows[vardiyaId];
        } else {
            newExpandedRows[vardiyaId] = true;
        }
        this.expandedRows = newExpandedRows;
    }
}
