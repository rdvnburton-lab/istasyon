import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { NotificationService } from '../../../services/notification.service';
import { FirmaService } from '../../../services/firma.service';
import { IstasyonService } from '../../../services/istasyon.service';
import { UserService } from '../../../services/user.service';
import { RoleService } from '../../../services/role.service';

@Component({
    selector: 'app-notification-sender',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        CardModule,
        SelectModule,
        MultiSelectModule,
        InputTextModule,
        TextareaModule,
        ButtonModule,
        ToastModule,
        TableModule
    ],
    providers: [MessageService],
    templateUrl: './notification-sender.component.html',
    styleUrls: ['./notification-sender.component.scss']
})
export class NotificationSenderComponent implements OnInit {
    firmalar: any[] = [];
    istasyonlar: any[] = [];
    kullanicilar: any[] = [];
    roles: any[] = [];

    filteredIstasyonlar: any[] = [];
    filteredKullanicilar: any[] = [];

    selectedFirma: any = null;
    selectedIstasyon: any = null;
    selectedRole: any = null;
    selectedUsers: any[] = [];

    notification = {
        title: '',
        message: ''
    };

    loading: boolean = false;

    constructor(
        private notificationService: NotificationService,
        private firmaService: FirmaService,
        private istasyonService: IstasyonService,
        private userService: UserService,
        private roleService: RoleService,
        private messageService: MessageService
    ) { }

    ngOnInit() {
        this.loadInitialData();
    }

    loadInitialData() {
        this.loading = true;

        // Rolleri yükle
        this.roleService.getAll().subscribe(data => {
            // Pompacı rolünü filtrele (eğer varsa) ve dropdown formatına çevir
            this.roles = data
                .filter(r => r.ad.toLowerCase() !== 'pompaci')
                .map(r => ({ label: r.ad, value: r.ad })); // Backend'den gelen rol adı ile eşleşmeli

            // "Tümü" seçeneğini başa ekle
            this.roles.unshift({ label: 'Tümü', value: null });
        });

        // Firmaları yükle
        this.firmaService.getFirmalar().subscribe(data => {
            this.firmalar = data;
        });

        // İstasyonları yükle
        this.istasyonService.getIstasyonlar().subscribe(data => {
            this.istasyonlar = data;
        });

        // Kullanıcıları yükle
        this.userService.getUsers().subscribe({
            next: (data) => {
                this.kullanicilar = data;
                this.filterUsers();
                this.loading = false;
            },
            error: (err) => {
                console.error('Kullanıcılar yüklenemedi', err);
                this.loading = false;
            }
        });
    }

    onFirmaChange() {
        this.selectedIstasyon = null;
        if (this.selectedFirma) {
            this.filteredIstasyonlar = this.istasyonlar.filter(i => i.firmaId === this.selectedFirma.id);
        } else {
            this.filteredIstasyonlar = [];
        }
        this.filterUsers();
    }

    onIstasyonChange() {
        this.filterUsers();
    }

    onRoleChange() {
        this.filterUsers();
    }

    filterUsers() {
        this.filteredKullanicilar = this.kullanicilar.filter(user => {
            let matchFirma = true;
            let matchIstasyon = true;
            let matchRole = true;

            if (this.selectedFirma) {
                const userStation = this.istasyonlar.find(i => i.id === user.istasyonId);
                if (userStation) {
                    matchFirma = userStation.firmaId === this.selectedFirma.id;
                } else {
                    matchFirma = false;
                }
            }

            if (this.selectedIstasyon) {
                matchIstasyon = user.istasyonId === this.selectedIstasyon.id;
            }

            if (this.selectedRole) {
                matchRole = user.role === this.selectedRole || user.roleName === this.selectedRole;
            }

            return matchFirma && matchIstasyon && matchRole;
        });
    }

    sendNotification() {
        if (!this.notification.title || !this.notification.message) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Başlık ve mesaj alanları zorunludur.' });
            return;
        }

        if (this.selectedUsers.length === 0) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen en az bir kullanıcı seçin.' });
            return;
        }

        this.loading = true;
        const userIds = this.selectedUsers.map(u => u.id);

        this.notificationService.sendTestNotification({
            userIds: userIds,
            title: this.notification.title,
            message: this.notification.message
        }).subscribe({
            next: (res) => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: res.message });
                this.loading = false;
                this.notification = { title: '', message: '' };
                this.selectedUsers = [];
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Bildirim gönderilemedi.' });
                this.loading = false;
            }
        });
    }
}
