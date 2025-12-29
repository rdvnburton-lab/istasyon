import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TreeTableModule } from 'primeng/treetable';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TreeNode } from 'primeng/api';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { IstasyonService, Istasyon, CreateIstasyonDto, UpdateIstasyonDto } from '../../../services/istasyon.service';
import { FirmaService, Firma, CreateFirmaDto, UpdateFirmaDto } from '../../../services/firma.service';
import { UserService, UserDto } from '../../../services/user.service';
import { AuthService } from '../../../services/auth.service';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'app-istasyon-yonetimi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TreeTableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        SelectModule,
        TagModule,
        ToastModule,
        ConfirmDialogModule,
        ToggleSwitchModule,
        TooltipModule,
        IconFieldModule,
        InputIconModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './istasyon-yonetimi.component.html',
    styleUrls: ['./istasyon-yonetimi.component.scss']
})
export class IstasyonYonetimiComponent implements OnInit {
    treeData: TreeNode[] = [];
    cols: any[] = [];

    displayDialog: boolean = false;
    dialogHeader: string = '';
    isEditMode: boolean = false;
    isFirmaDialog: boolean = false;

    selectedNode: any = null;

    // Form fields
    name: string = '';
    address: string = '';
    selectedFirmaId: any = null;
    selectedPatronId: any = null;

    // 3 ayrı sorumlu ID
    selectedIstasyonSorumluId: any = null;
    selectedVardiyaSorumluId: any = null;
    selectedMarketSorumluId: any = null;

    apiKey: string = '';
    isActive: boolean = true;

    // Dropdown data
    firmaOptions: any[] = [];
    patronOptions: any[] = [];

    // 3 ayrı sorumlu dropdown
    istasyonSorumluOptions: any[] = [];
    vardiyaSorumluOptions: any[] = [];
    marketSorumluOptions: any[] = [];

    // Yeni sorumlu ekleme mini dialog
    yeniSorumluDialog: boolean = false;
    yeniSorumluRole: string = '';
    yeniSorumluRoleId: number = 0;
    yeniSorumluAdSoyad: string = '';
    yeniSorumluUsername: string = '';
    yeniSorumluPassword: string = '';
    yeniSorumluTelefon: string = '';

    isAdmin: boolean = false;

    constructor(
        private istasyonService: IstasyonService,
        private firmaService: FirmaService,
        private userService: UserService,
        private authService: AuthService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.cols = [
            { field: 'ad', header: 'Ad' },
            { field: 'adres', header: 'Adres' },
            { field: 'patronAdi', header: 'Patron' },
            { field: 'istasyonSorumlusu', header: 'İstasyon Sorumlusu' },
            { field: 'vardiyaSorumlusu', header: 'Vardiya Sorumlusu' },
            { field: 'marketSorumlusu', header: 'Market Sorumlusu' },
            { field: 'durum', header: 'Sağlık / Son Veri' },
            { field: 'kilit', header: 'Cihaz Kilidi' },
            { field: 'aktif', header: 'Durum' }
        ];

        const currentUser = this.authService.getCurrentUser();
        this.isAdmin = currentUser?.role === 'admin';


        this.loadData();

        if (this.isAdmin) {
            this.loadPatronlar();
        }
    }

    loadData() {
        forkJoin({
            firmalar: this.firmaService.getFirmalar(),
            istasyonlar: this.istasyonService.getIstasyonlar(),
            patronlar: this.userService.getUsersByRole('patron')
        }).subscribe(({ firmalar, istasyonlar, patronlar }) => {
            // Patronları her zaman yükle (hem admin hem patron için)
            this.patronOptions = patronlar.map(u => ({ label: u.adSoyad || u.username, value: u.id }));

            // Tree'yi oluştur
            this.buildTree(firmalar, istasyonlar);

            // Firma seçeneklerini yükle
            this.firmaOptions = firmalar.map(f => ({ label: f.ad, value: f.id }));
        });
    }

    loadPatronlar() {
        this.userService.getUsersByRole('patron').subscribe(users => {
            this.patronOptions = users.map(u => ({ label: u.adSoyad || u.username, value: u.id }));
        });
    }

    loadSorumlular() {
        // Tüm kullanıcıları ve istasyonları yükle
        this.userService.getUsers().subscribe(users => {
            this.istasyonService.getIstasyonlar().subscribe(istasyonlar => {
                // Zaten atanmış sorumluların ID'lerini topla (düzenlenen istasyon hariç)
                const editingIstasyonId = this.selectedNode?.id;
                const atanmisIstasyonSorumlulari = istasyonlar
                    .filter(i => i.id !== editingIstasyonId && i.istasyonSorumluId)
                    .map(i => i.istasyonSorumluId);
                const atanmisVardiyaSorumlulari = istasyonlar
                    .filter(i => i.id !== editingIstasyonId && i.vardiyaSorumluId)
                    .map(i => i.vardiyaSorumluId);
                const atanmisMarketSorumlulari = istasyonlar
                    .filter(i => i.id !== editingIstasyonId && i.marketSorumluId)
                    .map(i => i.marketSorumluId);

                // Tüm atanmış sorumluları birleştir
                const tumAtanmisSorumlular = [
                    ...atanmisIstasyonSorumlulari,
                    ...atanmisVardiyaSorumlulari,
                    ...atanmisMarketSorumlulari
                ];

                // İstasyon sorumluları - başka yerde atanmamış olanlar
                this.istasyonSorumluOptions = users
                    .filter(u => u.role === 'istasyon sorumlusu' && !tumAtanmisSorumlular.includes(u.id))
                    .map(u => ({ label: u.adSoyad || u.username, value: u.id }));

                // Vardiya sorumluları - başka yerde atanmamış olanlar
                this.vardiyaSorumluOptions = users
                    .filter(u => u.role === 'vardiya sorumlusu' && !tumAtanmisSorumlular.includes(u.id))
                    .map(u => ({ label: u.adSoyad || u.username, value: u.id }));

                // Market sorumluları - başka yerde atanmamış olanlar
                this.marketSorumluOptions = users
                    .filter(u => u.role === 'market sorumlusu' && !tumAtanmisSorumlular.includes(u.id))
                    .map(u => ({ label: u.adSoyad || u.username, value: u.id }));
            });
        });
    }

    buildTree(firmalar: Firma[], istasyonlar: Istasyon[]) {
        const roots: TreeNode[] = [];

        firmalar.forEach(f => {
            const patronName = this.patronOptions.find(p => p.value === f.patronId)?.label || '-';
            const firmaNode: TreeNode = {
                data: {
                    ...f,
                    type: 'firma',
                    patronAdi: patronName,
                    istasyonSorumlusu: '-',
                    vardiyaSorumlusu: '-',
                    marketSorumlusu: '-'
                },
                children: [],
                expanded: true
            };

            const relatedIstasyonlar = istasyonlar.filter(i => i.firmaId === f.id);
            relatedIstasyonlar.forEach(i => {
                firmaNode.children?.push({
                    data: {
                        ...i,
                        type: 'istasyon',
                        patronAdi: '-',
                        // Backend'den gelen hazır veriler - frontend'de işlem yok!
                        istasyonSorumlusu: i.istasyonSorumlusu || '-',
                        vardiyaSorumlusu: i.vardiyaSorumlusu || '-',
                        marketSorumlusu: i.marketSorumlusu || '-'
                    },
                    children: [],
                    expanded: true
                });
            });

            roots.push(firmaNode);
        });

        this.treeData = roots;
    }

    showFirmaDialog(editMode: boolean, data?: any) {
        this.isEditMode = editMode;
        this.isFirmaDialog = true;
        this.displayDialog = true;
        this.dialogHeader = editMode ? 'Firma Düzenle' : 'Yeni Firma Ekle';

        if (editMode && data) {
            this.selectedNode = data;
            this.name = data.ad;
            this.address = data.adres || '';
            this.selectedPatronId = data.patronId;
            this.isActive = data.aktif;
        } else {
            this.name = '';
            this.address = '';
            this.selectedPatronId = null;
            this.isActive = true;
        }
    }

    showIstasyonDialog(editMode: boolean, data?: any, firmaId?: number) {
        this.isEditMode = editMode;
        this.isFirmaDialog = false;
        this.displayDialog = true;
        this.dialogHeader = editMode ? 'İstasyon Düzenle' : 'Yeni İstasyon Ekle';

        if (editMode && data) {
            this.selectedNode = data;
            this.name = data.ad;
            this.address = data.adres || '';
            this.selectedFirmaId = data.firmaId;
            this.selectedIstasyonSorumluId = data.istasyonSorumluId;
            this.selectedVardiyaSorumluId = data.vardiyaSorumluId;
            this.selectedMarketSorumluId = data.marketSorumluId;
            this.apiKey = data.apiKey || ''
            this.isActive = data.aktif;
        } else {
            this.selectedNode = null;
            this.name = '';
            this.address = '';
            this.selectedFirmaId = firmaId || (this.firmaOptions.length > 0 ? this.firmaOptions[0].value : null);
            this.selectedIstasyonSorumluId = null;
            this.selectedVardiyaSorumluId = null;
            this.selectedMarketSorumluId = null;
            this.apiKey = '';
            this.isActive = true;
        }

        // Dropdown'ları güncelle (zaten atanmış sorumluları filtrele)
        this.loadSorumlular();
    }

    save() {
        if (this.isFirmaDialog) {
            this.saveFirma();
        } else {
            this.saveIstasyon();
        }
    }

    saveFirma() {
        if (this.isEditMode) {
            const dto: UpdateFirmaDto = { ad: this.name, adres: this.address, aktif: this.isActive };
            this.firmaService.updateFirma(this.selectedNode.id, dto).subscribe(() => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Firma güncellendi.' });
                this.displayDialog = false;
                this.loadData();
            });
        } else {
            const dto: CreateFirmaDto = { ad: this.name, adres: this.address, patronId: this.selectedPatronId };
            this.firmaService.createFirma(dto).subscribe(() => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Firma oluşturuldu.' });
                this.displayDialog = false;
                this.loadData();
            });
        }
    }

    saveIstasyon() {
        if (this.isEditMode) {
            const dto: UpdateIstasyonDto = {
                ad: this.name,
                adres: this.address,
                aktif: this.isActive,
                istasyonSorumluId: this.selectedIstasyonSorumluId,
                vardiyaSorumluId: this.selectedVardiyaSorumluId,
                marketSorumluId: this.selectedMarketSorumluId,
                apiKey: this.apiKey
            };
            this.istasyonService.updateIstasyon(this.selectedNode.id, dto).subscribe(() => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon güncellendi.' });
                this.displayDialog = false;
                this.loadData();
            });
        } else {
            const dto: CreateIstasyonDto = {
                ad: this.name,
                adres: this.address,
                firmaId: this.selectedFirmaId,
                istasyonSorumluId: this.selectedIstasyonSorumluId,
                vardiyaSorumluId: this.selectedVardiyaSorumluId,
                marketSorumluId: this.selectedMarketSorumluId,
                apiKey: this.apiKey
            };
            this.istasyonService.createIstasyon(dto).subscribe(() => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon oluşturuldu.' });
                this.displayDialog = false;
                this.loadData();
            });
        }
    }

    delete(data: any) {
        const isFirma = data.type === 'firma';
        this.confirmationService.confirm({
            message: `${data.ad} ${isFirma ? 'firmasını' : 'istasyonunu'} silmek istediğinize emin misiniz?`,
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                const obs = isFirma ? this.firmaService.deleteFirma(data.id) : this.istasyonService.deleteIstasyon(data.id);
                obs.subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Silindi.' });
                    this.loadData();
                });
            }
        });
    }

    unlockStation(rowData: any) {
        this.confirmationService.confirm({
            message: `${rowData.ad} istasyonunun cihaz kilidini kaldırmak istediğinize emin misiniz? (Bu işlemden sonra ilk bağlanan cihaz yeni sahip olacaktır)`,
            header: 'Kilit Kaldırma Onayı',
            icon: 'pi pi-unlock',
            accept: () => {
                this.istasyonService.unlockStation(rowData.id).subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Cihaz kilidi kaldırıldı.' });
                    this.loadData();
                });
            }
        });
    }


    generateApiKey() {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        let result = '';
        for (let i = 0; i < 32; i++) {
            result += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        this.apiKey = result;
    }

    showYeniSorumluDialog(role: string) {
        // Role map
        const roleMap: { [key: string]: number } = {
            'istasyon sorumlusu': 5,
            'vardiya sorumlusu': 3,
            'market sorumlusu': 4
        };

        this.yeniSorumluRole = role;
        this.yeniSorumluRoleId = roleMap[role];
        this.yeniSorumluAdSoyad = '';
        this.yeniSorumluUsername = '';
        this.yeniSorumluPassword = '';
        this.yeniSorumluTelefon = '';
        this.yeniSorumluDialog = true;
    }

    kaydetYeniSorumlu() {
        if (!this.yeniSorumluAdSoyad || !this.yeniSorumluUsername || !this.yeniSorumluPassword) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen zorunlu alanları doldurun.' });
            return;
        }

        // Eğer düzenleme modundaysak ve bir istasyon seçiliyse, ID'yi al
        let istasyonId = undefined;
        if (this.isEditMode && this.selectedNode && this.selectedNode.id) {
            istasyonId = this.selectedNode.id;
        }

        const createDto = {
            username: this.yeniSorumluUsername,
            password: this.yeniSorumluPassword,
            roleId: this.yeniSorumluRoleId,
            adSoyad: this.yeniSorumluAdSoyad,
            telefon: this.yeniSorumluTelefon,
            istasyonId: istasyonId,
            firmaId: this.selectedFirmaId || undefined
        };

        this.userService.createUser(createDto).subscribe({
            next: (newUser) => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Yeni sorumlu eklendi.' });
                this.yeniSorumluDialog = false;

                // Dropdown'ları yenile
                this.loadSorumlular();

                // 500ms bekle (dropdown'ların yenilenmesi için) sonra otomatik seç
                setTimeout(() => {
                    if (this.yeniSorumluRole === 'istasyon sorumlusu') {
                        this.selectedIstasyonSorumluId = newUser.id;
                    } else if (this.yeniSorumluRole === 'vardiya sorumlusu') {
                        this.selectedVardiyaSorumluId = newUser.id;
                    } else if (this.yeniSorumluRole === 'market sorumlusu') {
                        this.selectedMarketSorumluId = newUser.id;
                    }
                }, 500);
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: err.error || 'Kullanıcı eklenemedi.'
                });
            }
        });
    }
}
