import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AuthService } from './app/services/auth.service';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule, ConfirmDialogModule],
    template: `
        <router-outlet></router-outlet>
        <p-confirmDialog header="Oturum Sonlanıyor" icon="pi pi-exclamation-triangle"></p-confirmDialog>
    `
})
export class AppComponent implements OnInit {
    constructor(private authService: AuthService, private confirmationService: ConfirmationService) { }

    ngOnInit() {
        this.initMobileStatusBar();

        this.authService.idleWarning$.subscribe(() => {
            this.confirmationService.confirm({
                message: 'Uzun süredir işlem yapmadınız. Oturumunuz güvenlik nedeniyle kapatılacaktır. Devam etmek istiyor musunuz?',
                acceptLabel: 'Devam Et',
                rejectLabel: 'Çıkış Yap',
                accept: () => {
                    this.authService.resetIdle();
                },
                reject: () => {
                    this.authService.logout();
                }
            });
        });
    }

    private async initMobileStatusBar() {
        const { Capacitor } = await import('@capacitor/core');
        if (Capacitor.isNativePlatform()) {
            const { StatusBar, Style } = await import('@capacitor/status-bar');
            try {
                await StatusBar.setStyle({ style: Style.Light });
                // Android için statüs bar şeffaf veya renkli yapılabilir
                if (Capacitor.getPlatform() === 'android') {
                    await StatusBar.setOverlaysWebView({ overlay: false });
                    await StatusBar.setBackgroundColor({ color: '#ffffff' });
                }
            } catch (e) {
                console.error('Status Bar config failed', e);
            }
        }
    }
}
