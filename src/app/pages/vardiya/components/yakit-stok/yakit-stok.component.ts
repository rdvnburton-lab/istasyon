import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { AccordionModule } from 'primeng/accordion';
import { StokService, TankGiris, TankStokOzet, StokGirisFis, CreateFaturaGiris } from '../../services/stok.service';
import { YakitService, Yakit } from '../../../../services/yakit.service';
import { DefinitionsService, DefinitionType } from '../../../../services/definitions.service';

@Component({
    selector: 'app-yakit-stok',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        CardModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        InputNumberModule,
        DatePickerModule,
        SelectModule,
        SelectButtonModule,
        ToastModule,
        ConfirmDialogModule,
        DialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './yakit-stok.component.html',
    styleUrls: ['./yakit-stok.component.scss']
})
export class YakitStokComponent implements OnInit {
    activeTab: string = '0'; // 0: Ozet, 1: Giris

    tabOptions = [
        { label: 'Özet', value: '0', icon: 'pi pi-chart-pie' },
        { label: 'Geçmiş', value: '1', icon: 'pi pi-history' },
        { label: 'Ekle', value: '2', icon: 'pi pi-plus-circle' }
    ];

    // Date Filters
    selectedDate: Date = new Date();
    selectedMonth: any;
    selectedYear: any;

    filterType: 'MONTH' | 'YEAR' | 'RANGE' = 'MONTH';
    rangeDates: Date[] | undefined;

    filterOptions = [
        { label: 'Aylık', value: 'MONTH' },
        { label: 'Yıllık', value: 'YEAR' },
        { label: 'Tarih Aralığı', value: 'RANGE' }
    ];

    // Data
    stokOzet: TankStokOzet[] = [];
    debugInfo: any = null; // Debug bilgisi ekranında göstermek için
    aylikRapor: any[] = []; // Kapsamlı aylık stok raporu
    faturaStokDurumu: any[] = []; // Fatura bazında stok takibi (FIFO)
    girisler: StokGirisFis[] = [];
    yakitList: Yakit[] = [];
    gelisYontemleri: { label: string; value: string }[] = [];

    // Form - Invoice Based
    yeniFatura: {
        tarih: Date;
        faturaNo: string;
        gelisYontemi?: string;
        plaka?: string;
        urunGirisTarihi?: Date;
        kalemler: {
            yakitId: number | undefined;
            litre: number | null;
            birimFiyat: number | null;
            toplamTutar: number;
        }[];
    } = {
            tarih: new Date(),
            faturaNo: '',
            gelisYontemi: '',
            plaka: '',
            urunGirisTarihi: undefined,
            kalemler: []
        };

    months = [
        { label: 'Ocak', value: 0 }, { label: 'Şubat', value: 1 }, { label: 'Mart', value: 2 },
        { label: 'Nisan', value: 3 }, { label: 'Mayıs', value: 4 }, { label: 'Haziran', value: 5 },
        { label: 'Temmuz', value: 6 }, { label: 'Ağustos', value: 7 }, { label: 'Eylül', value: 8 },
        { label: 'Ekim', value: 9 }, { label: 'Kasım', value: 10 }, { label: 'Aralık', value: 11 }
    ];

    years: any[] = [];
    loading: boolean = false;
    expandedRows: { [key: string]: boolean } = {};
    editingFaturaNo: string | null = null; // Track if editing
    displayEditModal: boolean = false; // Toggle for modal

    constructor(
        private stokService: StokService,
        private yakitService: YakitService,
        private definitionsService: DefinitionsService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {
        const currentYear = new Date().getFullYear();
        for (let i = currentYear; i >= currentYear - 5; i--) {
            this.years.push({ label: i.toString(), value: i });
        }
    }

    ngOnInit() {
        this.selectedMonth = this.months[new Date().getMonth()];
        this.selectedYear = this.years[0];

        this.yakitService.getAll().subscribe(data => {
            this.yakitList = data;
            // Initialize with one empty line item
            this.addItem();
        });

        this.definitionsService.getDropdownList(DefinitionType.GELIS_YONTEMI).subscribe(data => {
            this.gelisYontemleri = data;
        });

        this.loadData();
    }

    loadData() {
        this.loading = true;

        if (this.activeTab === '0') {
            // Fetch both legacy ozet and new aylik rapor
            this.stokService.getStokDurumuWithDebug(this.selectedMonth.value, this.selectedYear.value).subscribe({
                next: (response) => {
                    this.stokOzet = response.ozet || response;
                    this.debugInfo = response.debug || null;
                    this.loading = false;
                },
                error: (err) => { console.error(err); this.loading = false; }
            });

            // Kapsamlı aylık rapor (ay 1-indexed olarak gönderilmeli)
            this.stokService.getAylikRapor(this.selectedYear.value, this.selectedMonth.value + 1).subscribe({
                next: (data) => { this.aylikRapor = data; },
                error: (err) => { console.error('Aylık rapor yüklenemedi:', err); }
            });

            // Fatura stok durumu
            this.stokService.getFaturaStokDurumu().subscribe({
                next: (data) => { this.faturaStokDurumu = data; },
                error: (err) => { console.error('Fatura stok durumu yüklenemedi:', err); }
            });
        }

        if (this.activeTab === '1') {
            let m: number | undefined;
            let y: number | undefined;
            let d1: Date | undefined;
            let d2: Date | undefined;

            if (this.filterType === 'MONTH') {
                m = this.selectedMonth.value;
                y = this.selectedYear.value;
            } else if (this.filterType === 'YEAR') {
                y = this.selectedYear.value;
            } else if (this.filterType === 'RANGE') {
                if (this.rangeDates && this.rangeDates[1]) { // Both dates selected
                    d1 = this.rangeDates[0];
                    d2 = this.rangeDates[1];
                } else {
                    // Waiting for selection
                    this.loading = false;
                    return;
                }
            }

            this.stokService.getGirisler(m, y, d1, d2).subscribe({
                next: (data) => {
                    // Add unique ID for PrimeNG Table expansion (FaturaNo might not be unique across days)
                    this.girisler = data.map((item, index) => ({
                        ...item,
                        uniqueId: `${item.faturaNo}_${item.tarih}_${index}`
                    }));
                    this.loading = false;
                },
                error: (err) => {
                    console.error(err);
                    this.loading = false;
                }
            });
        } else {
            // If not on history tab, we might not need to load history, but existing code loaded everything on init.
            // To be safe, if activeTab is not 1, we don't strictly need history, but if user switches tab?
            // onTabChange calls loadData. So it's fine.
        }
    }

    onDateChange() {
        this.loadData();
    }

    onFilterTypeChange() {
        // Reset defaults if needed
        this.loadData();
    }

    // --- Invoice Item Logic ---

    addItem() {
        this.yeniFatura.kalemler.push({
            yakitId: undefined,
            litre: null,
            birimFiyat: null,
            toplamTutar: 0
        });
    }

    removeItem(index: number) {
        if (this.yeniFatura.kalemler.length > 1) {
            this.yeniFatura.kalemler.splice(index, 1);
        } else {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'En az bir kalem olmalıdır.' });
        }
    }

    calculateItemTotal(item: any) {
        if (item.litre && item.birimFiyat) {
            item.toplamTutar = item.litre * item.birimFiyat;
        } else {
            item.toplamTutar = 0;
        }
    }

    get invoiceTotal(): number {
        return this.yeniFatura.kalemler.reduce((acc, item) => acc + (item.toplamTutar || 0), 0);
    }

    get listTotalTutar(): number {
        return this.girisler.reduce((acc, curr) => acc + curr.toplamTutar, 0);
    }

    get listTotalLitre(): number {
        return this.girisler.reduce((acc, curr) => acc + curr.toplamLitre, 0);
    }

    saveGiris() {
        if (!this.yeniFatura.faturaNo) {
            this.messageService.add({ severity: 'warn', summary: 'Eksik', detail: 'Lütfen Fatura No giriniz.' });
            return;
        }

        // Validate Items
        // Check if ANY item is invalid (missing fuel, or zero/null litre)
        const invalidIndex = this.yeniFatura.kalemler.findIndex(x => !x.yakitId || !x.litre || x.litre <= 0);

        if (invalidIndex !== -1) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Eksik Bilgi',
                detail: `${invalidIndex + 1}. satırdaki yakıt veya litre bilgisi eksik/hatalı.`
            });
            return;
        }

        const payload: CreateFaturaGiris = {
            tarih: this.yeniFatura.tarih,
            faturaNo: this.yeniFatura.faturaNo,
            gelisYontemi: this.yeniFatura.gelisYontemi,
            plaka: this.yeniFatura.plaka,
            urunGirisTarihi: this.yeniFatura.urunGirisTarihi,
            kalemler: this.yeniFatura.kalemler.map(x => ({
                yakitId: x.yakitId!,
                litre: x.litre!,
                birimFiyat: x.birimFiyat || 0
            }))
        };

        this.loading = true;

        if (this.editingFaturaNo) {
            // UPDATE MODE
            this.stokService.updateFatura(this.editingFaturaNo, payload).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Güncellendi', detail: 'Fatura güncellendi.' });
                    this.resetForm();
                    this.displayEditModal = false;
                    this.loadData();
                    this.loading = false;
                },
                error: (err) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error?.message || 'Güncellenemedi.' });
                    this.loading = false;
                }
            });
        } else {
            // CREATE MODE
            this.stokService.addFaturaGiris(payload).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Fatura kaydedildi.' });
                    this.resetForm();
                    this.activeTab = '1';
                    this.loadData();
                    this.loading = false;
                },
                error: (err: any) => {
                    console.error(err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Hata',
                        detail: err.error?.message || 'Kaydedilemedi.'
                    });
                    this.loading = false;
                }
            });
        }
    }

    // --- Actions ---

    deleteFatura(faturaNo: string) {
        this.confirmationService.confirm({
            message: `${faturaNo} nolu faturayı ve tüm kalemlerini silmek istediğinize emin misiniz?`,
            header: 'Fatura Sil',
            icon: 'pi pi-trash',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.stokService.deleteFatura(faturaNo).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Silindi', detail: 'Fatura silindi.' });
                        this.loadData();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Silinemedi.' });
                    }
                });
            }
        });
    }

    editFatura(fis: StokGirisFis) {
        this.editingFaturaNo = fis.faturaNo;
        // Populate form
        this.yeniFatura = {
            tarih: new Date(fis.tarih),
            faturaNo: fis.faturaNo,
            gelisYontemi: fis.gelisYontemi || '',
            plaka: fis.plaka || '',
            urunGirisTarihi: fis.urunGirisTarihi ? new Date(fis.urunGirisTarihi) : undefined,
            kalemler: fis.kalemler.map(k => ({
                yakitId: k.yakitId,
                litre: k.litre,
                birimFiyat: k.birimFiyat,
                toplamTutar: k.toplamTutar
            }))
        };

        this.activeTab = '1'; // Stay on list
        this.displayEditModal = true; // Open modal
    }

    deleteLineItem(item: TankGiris) {
        this.confirmationService.confirm({
            message: 'Bu satırı silmek istediğinize emin misiniz?',
            accept: () => {
                this.stokService.deleteGiris(item.id).subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Satır silindi.' });
                    this.loadData();
                });
            }
        });
    }

    resetForm() {
        this.editingFaturaNo = null; // Reset edit mode
        this.displayEditModal = false;
        this.yeniFatura = {
            tarih: new Date(),
            faturaNo: '',
            gelisYontemi: '',
            plaka: '',
            urunGirisTarihi: undefined,
            kalemler: []
        };
        this.addItem();
    }

    onTabChange(event: any) {
        if (this.activeTab === '0') {
            // "Güncel Durum" - Reset to Current Month/Year
            const now = new Date();
            this.selectedMonth = this.months[now.getMonth()];
            this.selectedYear = this.years.find(y => y.value === now.getFullYear());
            this.loadData();
        } else if (this.activeTab === '1') {
            // History Tab - Load Data!
            this.loadData();
        } else if (this.activeTab === '2') {
            // Add Tab - Always reset form for new entry
            this.resetForm();
        }
    }

    cancelEdit() {
        this.resetForm();
        this.activeTab = '1'; // Return to list
    }

    toggleRow(id: string) {
        this.expandedRows = { ...this.expandedRows, [id]: !this.expandedRows[id] };
    }

    getYakitName(id: number): string {
        const y = this.yakitList.find(x => x.id === id);
        return y ? y.ad : '?';
    }
}
