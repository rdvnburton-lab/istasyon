import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { DividerModule } from 'primeng/divider';
import { SettingsService, UserSettingsDto } from '../../../services/settings.service';
import { AuthService } from '../../../services/auth.service';
import { LayoutService } from '../../../layout/service/layout.service';

interface AppSettings {
    bildirimler: {
        vardiyaOnayBildirimi: boolean;
        farkUyarisi: boolean;
        farkEsikTutari: number;
    };
    gorunum: {
        satirSayisi: number;
        tema: 'light' | 'dark';
        varsayilanTarihAraligi: 'bugun' | 'buAy' | 'gecenAy';
    };
    sistem: {
        otomatikYedekleme: boolean;
        yedeklemeGunu: number;
        varsayilanIstasyon: number;
    };
}

@Component({
    selector: 'app-ayarlar',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        CardModule,
        ToggleSwitchModule,
        InputNumberModule,
        ButtonModule,
        SelectModule,
        ToastModule,
        DividerModule
    ],
    providers: [MessageService],
    templateUrl: './ayarlar.component.html',
    styles: [`:host { display: block; }`]
})
export class AyarlarComponent implements OnInit {
    settings: AppSettings = {
        bildirimler: {
            vardiyaOnayBildirimi: true,
            farkUyarisi: true,
            farkEsikTutari: 100
        },
        gorunum: {
            satirSayisi: 10,
            tema: 'light',
            varsayilanTarihAraligi: 'buAy'
        },
        sistem: {
            otomatikYedekleme: false,
            yedeklemeGunu: 1,
            varsayilanIstasyon: 1
        }
    };

    satirSayisiOptions = [
        { label: '10 satır', value: 10 },
        { label: '25 satır', value: 25 },
        { label: '50 satır', value: 50 },
        { label: '100 satır', value: 100 }
    ];

    temaOptions = [
        { label: 'Açık Tema', value: 'light' },
        { label: 'Koyu Tema', value: 'dark' }
    ];

    tarihAraligiOptions = [
        { label: 'Bugün', value: 'bugun' },
        { label: 'Bu Ay', value: 'buAy' },
        { label: 'Geçen Ay', value: 'gecenAy' }
    ];

    gunOptions = Array.from({ length: 31 }, (_, i) => ({ label: `${i + 1}. Gün`, value: i + 1 }));

    isAdmin = false;

    constructor(
        private messageService: MessageService,
        private settingsService: SettingsService,
        private authService: AuthService,
        private layoutService: LayoutService
    ) { }

    ngOnInit() {
        this.isAdmin = this.authService.isAdmin();
        this.loadSettings();
    }

    loadSettings() {
        this.settingsService.getSettings().subscribe({
            next: (dto: UserSettingsDto) => {
                // Map basic fields
                this.settings.gorunum.tema = dto.theme as 'light' | 'dark';
                this.settings.bildirimler.vardiyaOnayBildirimi = dto.notificationsEnabled;

                // Map extra fields from JSON
                if (dto.extraSettingsJson) {
                    try {
                        const extra = JSON.parse(dto.extraSettingsJson);
                        // Merge extra settings carefully
                        if (extra.bildirimler) {
                            this.settings.bildirimler.farkUyarisi = extra.bildirimler.farkUyarisi;
                            this.settings.bildirimler.farkEsikTutari = extra.bildirimler.farkEsikTutari;
                        }
                        if (extra.gorunum) {
                            this.settings.gorunum.satirSayisi = extra.gorunum.satirSayisi;
                            this.settings.gorunum.varsayilanTarihAraligi = extra.gorunum.varsayilanTarihAraligi;
                        }
                        if (extra.sistem) {
                            this.settings.sistem = extra.sistem;
                        }
                    } catch (e) {
                        console.error('Error parsing extra settings', e);
                    }
                }
            },
            error: (err: any) => {
                console.error('Error loading settings', err);
                // Fallback to local storage if API fails
                const savedSettings = localStorage.getItem('appSettings');
                if (savedSettings) {
                    this.settings = JSON.parse(savedSettings);
                }
            }
        });
    }

    saveSettings() {
        // Prepare DTO
        const extraSettings = {
            bildirimler: {
                farkUyarisi: this.settings.bildirimler.farkUyarisi,
                farkEsikTutari: this.settings.bildirimler.farkEsikTutari
            },
            gorunum: {
                satirSayisi: this.settings.gorunum.satirSayisi,
                varsayilanTarihAraligi: this.settings.gorunum.varsayilanTarihAraligi
            },
            sistem: this.settings.sistem
        };

        const dto: UserSettingsDto = {
            theme: this.settings.gorunum.tema,
            notificationsEnabled: this.settings.bildirimler.vardiyaOnayBildirimi,
            emailNotifications: false, // Not in UI yet
            language: 'tr', // Default
            extraSettingsJson: JSON.stringify(extraSettings)
        };

        this.settingsService.updateSettings(dto).subscribe({
            next: () => {
                // Also save to local storage for offline/fast access
                localStorage.setItem('appSettings', JSON.stringify(this.settings));

                // Update theme immediately
                this.layoutService.layoutConfig.update((config) => ({
                    ...config,
                    darkTheme: this.settings.gorunum.tema === 'dark'
                }));

                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Ayarlar sunucuya kaydedildi',
                    life: 3000
                });
            },
            error: (err: any) => {
                console.error('Error saving settings', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Ayarlar kaydedilirken bir hata oluştu',
                    life: 3000
                });
            }
        });
    }

    resetSettings() {
        this.settings = {
            bildirimler: {
                vardiyaOnayBildirimi: true,
                farkUyarisi: true,
                farkEsikTutari: 100
            },
            gorunum: {
                satirSayisi: 10,
                tema: 'light',
                varsayilanTarihAraligi: 'buAy'
            },
            sistem: {
                otomatikYedekleme: false,
                yedeklemeGunu: 1,
                varsayilanIstasyon: 1
            }
        };
        this.saveSettings();
    }
}
