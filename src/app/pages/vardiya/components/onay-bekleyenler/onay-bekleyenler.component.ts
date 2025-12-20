import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TabsModule } from 'primeng/tabs';
import { TextareaModule } from 'primeng/textarea';
import { ConfirmationService, MessageService } from 'primeng/api';
import { VardiyaService } from '../../services/vardiya.service';
import { Vardiya, VardiyaOzet, PersonelFarkAnalizi, MarketOzet, GenelOzet } from '../../models/vardiya.model';

@Component({
    selector: 'app-onay-bekleyenler',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        TagModule,
        TooltipModule,
        DialogModule,
        ConfirmDialogModule,
        TabsModule,
        TextareaModule
    ],
    templateUrl: './onay-bekleyenler.component.html',
    styleUrls: ['./onay-bekleyenler.component.scss'],
    providers: [ConfirmationService, MessageService]
})
export class OnayBekleyenlerComponent implements OnInit {
    vardiyalar: Vardiya[] = [];
    seciliVardiya: Vardiya | null = null;
    detayVisible: boolean = false;

    // Detay Verileri
    genelOzet: GenelOzet | null = null;
    farkAnalizi: PersonelFarkAnalizi[] = [];
    marketOzet: MarketOzet | null = null;
    vardiyaOzet: VardiyaOzet | null = null;

    // Reddetme
    redDialogVisible: boolean = false;
    redNedeni: string = '';

    constructor(
        private vardiyaService: VardiyaService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) { }

    ngOnInit() {
        this.yukle();
    }

    yukle() {
        this.vardiyaService.getOnayBekleyenVardiyalar().subscribe(data => {
            this.vardiyalar = data;
        });
    }

    incele(vardiya: Vardiya) {
        this.seciliVardiya = vardiya;
        this.vardiyaService.setAktifVardiyaById(vardiya.id).then(() => {
            // Tüm verileri paralel çek
            this.vardiyaService.getGenelOzet(vardiya.id).subscribe(val => this.genelOzet = val);
            this.vardiyaService.getFarkAnalizi(vardiya.id).subscribe(val => this.farkAnalizi = val);
            this.vardiyaService.getMarketOzet().subscribe(val => this.marketOzet = val);
            this.vardiyaService.getVardiyaOzet(vardiya.id).subscribe(val => this.vardiyaOzet = val);

            this.detayVisible = true;
        });
    }

    onayla(vardiya: Vardiya) {
        this.confirmationService.confirm({
            message: `${vardiya.dosyaAdi} dosyasını ve ilgili vardiya mutabakatını onaylıyor musunuz? Bu işlem geri alınamaz.`,
            header: 'Onay İşlemi',
            icon: 'pi pi-check-circle',
            acceptLabel: 'Evet, Onayla',
            rejectLabel: 'Vazgeç',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.vardiyaService.vardiyaOnayla(vardiya.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Vardiya başarıyla onaylandı.' });
                        this.detayVisible = false;
                        this.yukle();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Onay işlemi sırasında bir hata oluştu.' });
                    }
                });
            }
        });
    }

    reddetDialogAc(vardiya: Vardiya) {
        this.seciliVardiya = vardiya;
        this.redNedeni = '';
        this.redDialogVisible = true;
    }

    reddetIslemi() {
        if (!this.seciliVardiya) return;
        if (!this.redNedeni.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen bir red nedeni giriniz.' });
            return;
        }

        this.vardiyaService.vardiyaReddet(this.seciliVardiya.id, this.redNedeni).subscribe({
            next: () => {
                this.messageService.add({ severity: 'info', summary: 'Reddedildi', detail: 'Vardiya reddedildi.' });
                this.redDialogVisible = false;
                this.detayVisible = false;
                this.yukle();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Red işlemi başarısız.' });
            }
        });
    }
}
