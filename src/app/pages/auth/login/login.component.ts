import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { NotificationService } from '../../../services/notification.service';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [
        CommonModule,
        ButtonModule,
        FormsModule,
        RouterModule,
        ToastModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss']
})
export class Login {
    username: string = '';
    password: string = '';
    usernameFocus: boolean = false;
    passwordFocus: boolean = false;
    loading: boolean = false;
    error: string = '';

    biometricAvailable: boolean = false;

    constructor(
        private authService: AuthService,
        private router: Router,
        private messageService: MessageService,
        private notificationService: NotificationService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.checkBiometric();
    }

    async checkBiometric() {
        const hardwareAvailable = await this.authService.checkBiometricAvailability();
        if (hardwareAvailable) {
            // Sadece kayıtlı veri varsa butonu göster
            const hasCredentials = await this.authService.hasBiometricCredentials();
            this.biometricAvailable = hasCredentials;
        } else {
            this.biometricAvailable = false;
        }
    }

    async loginWithBiometric() {
        this.loading = true;
        const result = await this.authService.loginWithBiometric();
        if (result) {
            // Biyometrik giriş başarılıysa da push notification başlat
            this.notificationService.initPush();
            this.router.navigate(['/']);
        } else {
            this.loading = false;
            // Sessiz başarısızlık veya mesaj
        }
    }

    onLogin() {
        if (!this.username || !this.password) {
            this.error = 'Lütfen kullanıcı adı ve şifre giriniz.';
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen tüm alanları doldurunuz.' });
            return;
        }

        this.loading = true;
        this.error = '';

        this.authService.login(this.username.toLowerCase(), this.password).subscribe({
            next: async () => {
                // Push Notification izni iste ve başlat
                this.notificationService.initPush();

                // Başarılı girişte bilgileri biometrik depoya kaydetmek için sor
                const hardwareAvailable = await this.authService.checkBiometricAvailability();
                if (hardwareAvailable) {
                    const hasCredentials = await this.authService.hasBiometricCredentials();
                    if (!hasCredentials) {
                        this.confirmationService.confirm({
                            header: 'Biyometrik Giriş',
                            message: 'Sonraki girişlerinizde Face ID / Touch ID kullanmak ister misiniz?',
                            icon: 'pi pi-face-smile',
                            acceptLabel: 'Evet',
                            rejectLabel: 'Hayır',
                            accept: async () => {
                                await this.authService.saveCredentialsForBiometric(this.username, this.password);
                            }
                        });
                    }
                }

                this.loading = false;
                this.router.navigate(['/']);
            },
            error: (err) => {
                this.loading = false;
                this.error = 'Giriş başarısız. Bilgilerinizi kontrol edin.';
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Giriş yapılamadı.' });
                console.error(err);
            }
        });
    }
}
