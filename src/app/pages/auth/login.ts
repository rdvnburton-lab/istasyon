import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [
        CommonModule,
        ButtonModule,
        FormsModule,
        RouterModule,
        ToastModule
    ],
    providers: [MessageService],
    template: `
        <p-toast></p-toast>
        <div class="container">
            <!-- Arka Plan Filigran (Logo Pattern) -->
            <div class="bg-watermark"></div>

            <div class="img">
                <!-- 3D İllüstrasyon (Statik) -->
                <img src="assets/login_3d.png" alt="3D Illustration" class="main-illustration">
            </div>
            
            <div class="login-content animate-slide-up">
                <div class="glass-card">
                    <form (ngSubmit)="onLogin()">
                        <img src="assets/logo.svg" alt="Logo Icon" class="form-logo">
                        <h1 class="main-title">İstasyon Yönetimi</h1>
                        <p class="subtitle">Sisteme erişmek için bilgilerinizi girin</p>
                        
                        <div class="input-div one" [class.focus]="usernameFocus || username">
                            <div class="i">
                                <i class="pi pi-user"></i>
                            </div>
                            <div class="div">
                                <h5>Kullanıcı Adı</h5>
                                <input type="text" class="input" [(ngModel)]="username" name="username" 
                                    (focus)="usernameFocus = true" (blur)="usernameFocus = false">
                            </div>
                        </div>

                        <div class="input-div pass" [class.focus]="passwordFocus || password">
                            <div class="i"> 
                                <i class="pi pi-lock"></i>
                            </div>
                            <div class="div">
                                <h5>Şifre</h5>
                                <input type="password" class="input" [(ngModel)]="password" name="password"
                                    (focus)="passwordFocus = true" (blur)="passwordFocus = false">
                            </div>
                        </div>

                        <a href="#" class="forgot-pass">Şifremi Unuttum?</a>
                        <input type="submit" class="btn pulse-effect" value="Giriş Yap" [disabled]="loading">
                        
                        <div *ngIf="error" class="error-msg">
                            <i class="pi pi-exclamation-circle"></i> {{ error }}
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `,
    styles: [`
        @import url('https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;800&family=Poppins:wght@400;500;600&display=swap');

        :host {
            display: block;
            font-family: 'Poppins', sans-serif;
            overflow: hidden;
            height: 100vh;
            background: #f8f9fa;
            position: relative;
        }

        * {
            padding: 0;
            margin: 0;
            box-sizing: border-box;
        }

        /* Arka Plan Filigran */
        .bg-watermark {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-image: url('/assets/logo.svg');
            background-size: 150px;
            background-repeat: repeat;
            opacity: 0.03;
            z-index: 0;
            pointer-events: none;
            transform: rotate(-15deg) scale(1.2);
        }

        .container {
            width: 100vw;
            height: 100vh;
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            grid-gap: 2rem;
            padding: 0 2rem;
            position: relative;
            z-index: 1;
        }

        .img {
            display: flex;
            justify-content: center;
            align-items: center;
        }

        .main-illustration {
            width: 90%;
            max-width: 600px;
            filter: drop-shadow(0 20px 30px rgba(0,0,0,0.1));
        }

        .login-content {
            display: flex;
            justify-content: center;
            align-items: center;
            text-align: center;
        }

        .glass-card {
            background: rgba(255, 255, 255, 0.85);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.5);
            padding: 3.5rem 3rem;
            border-radius: 40px;
            box-shadow: 0 30px 60px rgba(0,0,0,0.08);
            width: 100%;
            max-width: 460px;
            transition: transform 0.3s ease;
        }

        .form-logo {
            height: 80px !important;
            margin: 0 auto 1.5rem auto !important;
            display: block;
        }

        .main-title {
            font-family: 'Outfit', sans-serif;
            color: #1a1a1a;
            font-size: 2.2rem;
            font-weight: 800;
            letter-spacing: -0.5px;
            margin-bottom: 8px;
            line-height: 1.1;
        }

        .subtitle {
            font-family: 'Outfit', sans-serif;
            color: #636e72;
            font-size: 1rem;
            font-weight: 400;
            margin-bottom: 2.5rem;
        }

        .input-div {
            position: relative;
            display: grid;
            grid-template-columns: 7% 93%;
            margin: 20px 0;
            padding: 8px 0;
            border-bottom: 2px solid #dfe6e9;
            transition: 0.4s;
        }

        .input-div.focus {
            border-bottom-color: #38d39f;
        }

        .i {
            color: #b2bec3;
            display: flex;
            justify-content: center;
            align-items: center;
        }

        .input-div.focus .i i {
            color: #38d39f;
        }

        .input-div > div {
            position: relative;
            height: 45px;
        }

        .input-div > div > h5 {
            position: absolute;
            left: 10px;
            top: 50%;
            transform: translateY(-50%);
            color: #999;
            font-size: 16px;
            transition: .3s;
            pointer-events: none;
        }

        .input-div.focus > div > h5 {
            top: -8px;
            font-size: 13px;
            color: #38d39f;
            font-weight: 600;
        }

        .input-div > div > input {
            position: absolute;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            border: none;
            outline: none;
            background: none;
            padding: 0.5rem 0.7rem;
            font-size: 1.1rem;
            color: #2d3436;
            font-family: 'Poppins', sans-serif;
        }

        .forgot-pass {
            display: block;
            text-align: right;
            text-decoration: none;
            color: #636e72;
            font-size: 0.85rem;
            transition: .3s;
            margin-bottom: 2rem;
        }

        .forgot-pass:hover {
            color: #38d39f;
        }

        .btn {
            display: block;
            width: 100%;
            height: 55px;
            border-radius: 15px;
            outline: none;
            border: none;
            background: linear-gradient(45deg, #32be8f, #38d39f);
            background-size: 200%;
            font-size: 1.1rem;
            color: #fff;
            font-family: 'Poppins', sans-serif;
            font-weight: 700;
            text-transform: uppercase;
            cursor: pointer;
            transition: all 0.5s ease;
            box-shadow: 0 10px 20px rgba(50, 190, 143, 0.2);
        }

        .btn:hover {
            background-position: right;
            transform: translateY(-2px);
        }

        .pulse-effect:not(:disabled) {
            animation: pulse 3s infinite;
        }

        @keyframes pulse {
            0% { box-shadow: 0 10px 20px rgba(50, 190, 143, 0.2); }
            50% { box-shadow: 0 10px 30px rgba(50, 190, 143, 0.4); }
            100% { box-shadow: 0 10px 20px rgba(50, 190, 143, 0.2); }
        }

        .btn:disabled {
            background: #b2bec3;
            cursor: not-allowed;
        }

        .error-msg {
            color: #d63031;
            margin-top: 1.5rem;
            font-size: 0.9rem;
            background: rgba(214, 48, 49, 0.05);
            padding: 10px;
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 0.5rem;
        }

        .animate-slide-up {
            animation: slideUp 0.8s ease-out;
        }

        @keyframes slideUp {
            from { opacity: 0; transform: translateY(30px); }
            to { opacity: 1; transform: translateY(0); }
        }

        @media screen and (max-width: 900px) {
            .container {
                grid-template-columns: 1fr;
            }
            .img {
                display: none;
            }
            .login-content {
                padding: 1rem;
            }
        }
    `]
})
export class Login {
    username: string = '';
    password: string = '';
    usernameFocus: boolean = false;
    passwordFocus: boolean = false;
    loading: boolean = false;
    error: string = '';

    constructor(private authService: AuthService, private router: Router, private messageService: MessageService) { }

    onLogin() {
        if (!this.username || !this.password) {
            this.error = 'Lütfen kullanıcı adı ve şifre giriniz.';
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen tüm alanları doldurunuz.' });
            return;
        }

        this.loading = true;
        this.error = '';

        this.authService.login(this.username, this.password).subscribe({
            next: () => {
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
