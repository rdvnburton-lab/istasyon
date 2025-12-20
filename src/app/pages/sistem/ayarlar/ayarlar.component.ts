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

    constructor(private messageService: MessageService) { }

    ngOnInit() {
        this.loadSettings();
    }

    loadSettings() {
        const savedSettings = localStorage.getItem('appSettings');
        if (savedSettings) {
            this.settings = JSON.parse(savedSettings);
        }
    }

    saveSettings() {
        localStorage.setItem('appSettings', JSON.stringify(this.settings));
        this.messageService.add({
            severity: 'success',
            summary: 'Başarılı',
            detail: 'Ayarlar kaydedildi',
            life: 3000
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
        this.messageService.add({
            severity: 'info',
            summary: 'Sıfırlandı',
            detail: 'Ayarlar varsayılan değerlere döndürüldü',
            life: 3000
        });
    }
}
