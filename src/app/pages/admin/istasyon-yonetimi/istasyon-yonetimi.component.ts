import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TreeTableModule } from 'primeng/treetable';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TreeNode } from 'primeng/api';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { IstasyonService, Istasyon, CreateIstasyonDto } from '../../../services/istasyon.service';
import { UserService, UserDto } from '../../../services/user.service';
import { AuthService } from '../../../services/auth.service';

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
        ToastModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './istasyon-yonetimi.component.html'
})
export class IstasyonYonetimiComponent implements OnInit {
    istasyonlar: TreeNode[] = [];
    cols: any[] = [];

    displayDialog: boolean = false;
    dialogHeader: string = '';
    isEditMode: boolean = false;

    selectedIstasyon: Istasyon | null = null;

    // Form fields
    istasyonAd: string = '';
    istasyonAdres: string = '';
    selectedParent: any = null;
    selectedPatron: any = null;
    selectedSorumlu: any = null;

    // Dropdown data
    parentOptions: any[] = [];
    patronOptions: any[] = [];
    sorumluOptions: any[] = [];

    isAdmin: boolean = false;

    constructor(
        private istasyonService: IstasyonService,
        private userService: UserService,
        private authService: AuthService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.cols = [
            { field: 'ad', header: 'İstasyon Adı' },
            { field: 'adres', header: 'Adres' },
            { field: 'patronId', header: 'Patron ID' }
        ];

        const currentUser = this.authService.getCurrentUser();
        this.isAdmin = currentUser?.role === 'admin';

        this.loadIstasyonlar();

        if (this.isAdmin) {
            this.loadPatronlar();
        }

        // Sorumluları her durumda yükleyebiliriz (Patron veya Admin)
        this.loadSorumlular();
    }

    loadIstasyonlar() {
        this.istasyonService.getIstasyonlar().subscribe(data => {
            this.istasyonlar = this.buildTree(data);
            this.updateParentOptions(data);
        });
    }

    loadPatronlar() {
        this.userService.getUsersByRole('patron').subscribe(users => {
            this.patronOptions = users.map(u => ({ label: u.username, value: u.id }));
        });
    }

    loadSorumlular() {
        this.userService.getUsers().subscribe(users => {
            const supervisorRoles = ['vardiya_sorumlusu', 'market_sorumlusu', 'istasyon_sorumlusu'];
            this.sorumluOptions = users
                .filter(u => supervisorRoles.includes(u.role))
                .map(u => ({ label: `${u.username} (${u.role})`, value: u.id }));
        });
    }

    buildTree(data: Istasyon[]): TreeNode[] {
        const map = new Map<number, TreeNode>();
        const roots: TreeNode[] = [];

        // Önce tüm node'ları oluştur
        data.forEach(item => {
            map.set(item.id, {
                data: item,
                children: [],
                expanded: true
            });
        });

        // Sonra hiyerarşiyi kur
        data.forEach(item => {
            const node = map.get(item.id);
            if (item.parentIstasyonId) {
                const parent = map.get(item.parentIstasyonId);
                if (parent) {
                    parent.children?.push(node!);
                }
            } else {
                roots.push(node!);
            }
        });

        return roots;
    }

    updateParentOptions(data: Istasyon[]) {
        // Admin için tüm istasyonlar, Patron için sadece kendi "Firma" niteliğindeki (parent'ı olmayan veya kendisi parent olan) istasyonları
        if (this.isAdmin) {
            this.parentOptions = data.map(i => ({ label: i.ad, value: i.id }));
        } else {
            // Patron sadece sahibi olduğu ve "Firma" olarak nitelendirilebilecek (örneğin parent'ı olmayan) istasyonları seçebilmeli
            // Mevcut backend mantığına göre PatronId'si eşleşenler zaten geliyor.
            // Burada sadece Root node'ları (Firmaları) listelemek mantıklı olabilir.
            this.parentOptions = data
                .filter(i => !i.parentIstasyonId) // Sadece en üst seviye (Firma) olanları listele
                .map(i => ({ label: i.ad, value: i.id }));
        }
    }

    showDialog(editMode: boolean, istasyon?: Istasyon) {
        this.isEditMode = editMode;
        this.displayDialog = true;

        if (editMode && istasyon) {
            this.dialogHeader = this.isAdmin && !istasyon.parentIstasyonId ? 'Firma Düzenle' : 'İstasyon Düzenle';
            this.selectedIstasyon = istasyon;
            this.istasyonAd = istasyon.ad;
            this.istasyonAdres = istasyon.adres || '';
            this.selectedParent = istasyon.parentIstasyonId ? this.parentOptions.find(p => p.value === istasyon.parentIstasyonId)?.value : null;
            this.selectedPatron = istasyon.patronId ? this.patronOptions.find(p => p.value === istasyon.patronId)?.value : null;
            this.selectedSorumlu = istasyon.sorumluId ? this.sorumluOptions.find(s => s.value === istasyon.sorumluId)?.value : null;
        } else {
            // Yeni Ekleme Modu
            if (this.isAdmin) {
                this.dialogHeader = 'Yeni Firma Ekle';
                this.selectedParent = null; // Admin firma eklerken parent seçmez
            } else {
                this.dialogHeader = 'Yeni İstasyon Ekle';
                // Patron için varsayılan firmayı (ilk firma) otomatik seç
                if (this.parentOptions.length > 0) {
                    this.selectedParent = this.parentOptions[0].value;
                }
            }

            this.selectedIstasyon = null;
            this.istasyonAd = '';
            this.istasyonAdres = '';
            this.istasyonAdres = '';
            this.selectedPatron = null;
            this.selectedSorumlu = null;
        }
    }

    saveIstasyon() {
        if (!this.istasyonAd) {
            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'İstasyon adı zorunludur.' });
            return;
        }

        if (!this.isAdmin && !this.selectedParent && !this.isEditMode) {
            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Patronlar sadece mevcut bir istasyonun altına yeni istasyon ekleyebilir. Lütfen bir üst istasyon seçiniz.' });
            return;
        }

        if (this.isEditMode && this.selectedIstasyon) {
            const updateDto = {
                ad: this.istasyonAd,
                adres: this.istasyonAdres,
                aktif: this.selectedIstasyon.aktif,
                sorumluId: this.selectedSorumlu
            };

            this.istasyonService.updateIstasyon(this.selectedIstasyon.id, updateDto).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon güncellendi.' });
                    this.displayDialog = false;
                    this.loadIstasyonlar();
                },
                error: (err) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Güncelleme başarısız.' });
                }
            });
        } else {
            const createDto: CreateIstasyonDto = {
                ad: this.istasyonAd,
                adres: this.istasyonAdres,
                parentIstasyonId: this.selectedParent,
                patronId: this.selectedPatron,
                sorumluId: this.selectedSorumlu
            };

            this.istasyonService.createIstasyon(createDto).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon oluşturuldu.' });
                    this.displayDialog = false;
                    this.loadIstasyonlar();
                },
                error: (err) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Oluşturma başarısız: ' + (err.error?.message || err.message) });
                }
            });
        }
    }

    deleteIstasyon(istasyon: Istasyon) {
        this.confirmationService.confirm({
            message: `${istasyon.ad} istasyonunu silmek istediğinize emin misiniz?`,
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.istasyonService.deleteIstasyon(istasyon.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon silindi.' });
                        this.loadIstasyonlar();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Silme işlemi başarısız.' });
                    }
                });
            }
        });
    }
}
