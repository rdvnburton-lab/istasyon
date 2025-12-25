import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { AuthService, User } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { SorumluDashboardDto } from '../../models/dashboard.model';

@Component({
    selector: 'app-profile',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        CardModule,
        ButtonModule,
        InputTextModule,
        PasswordModule,
        ToastModule
    ],
    providers: [MessageService],
    templateUrl: './profile.component.html',
    styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
    currentUser: User | null = null;
    userInfo: SorumluDashboardDto | null = null;
    loading: boolean = false;

    // Password change form
    passwordForm = {
        currentPassword: '',
        newPassword: '',
        confirmPassword: ''
    };

    constructor(
        private authService: AuthService,
        private dashboardService: DashboardService,
        private messageService: MessageService
    ) { }

    ngOnInit() {
        this.currentUser = this.authService.getCurrentUser();
        this.loadUserInfo();
    }

    loadUserInfo() {
        this.loading = true;
        this.dashboardService.getSorumluSummary().subscribe({
            next: (data) => {
                this.userInfo = data;
                this.loading = false;
            },
            error: (err) => {
                console.error('Kullanıcı bilgileri yüklenirken hata:', err);
                this.loading = false;
            }
        });
    }

    getRoleLabel(role: string): string {
        const roleLower = role?.toLowerCase();
        switch (roleLower) {
            case 'admin':
                return 'Admin';
            case 'patron':
                return 'Patron';
            case 'vardiya sorumlusu':
            case 'vardiya_sorumlusu':
                return 'Vardiya Sorumlusu';
            case 'market sorumlusu':
            case 'market_sorumlusu':
                return 'Market Sorumlusu';
            case 'istasyon sorumlusu':
            case 'istasyon_sorumlusu':
                return 'İstasyon Sorumlusu';
            case 'pompaci':
                return 'Pompacı';
            case 'market_gorevlisi':
            case 'market gorevlisi':
                return 'Market Görevlisi';
            default:
                return role;
        }
    }

    changePassword() {
        if (!this.passwordForm.currentPassword || !this.passwordForm.newPassword || !this.passwordForm.confirmPassword) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Uyarı',
                detail: 'Lütfen tüm alanları doldurun'
            });
            return;
        }

        if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Yeni şifreler eşleşmiyor'
            });
            return;
        }

        if (this.passwordForm.newPassword.length < 6) {
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Şifre en az 6 karakter olmalıdır'
            });
            return;
        }

        // TODO: API call to change password
        this.messageService.add({
            severity: 'info',
            summary: 'Bilgi',
            detail: 'Şifre değiştirme özelliği yakında eklenecek'
        });

        // Reset form
        this.passwordForm = {
            currentPassword: '',
            newPassword: '',
            confirmPassword: ''
        };
    }
}
