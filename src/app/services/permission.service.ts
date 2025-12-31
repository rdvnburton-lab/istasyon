import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Resource {
    key: string;
    name: string;
    group: string;
}

export const RESOURCES: Resource[] = [
    // Vardiya Modülü
    { key: 'VARDIYA_LISTESI', name: 'Vardiya Listesi', group: 'Vardiya Yönetimi' },
    { key: 'VARDIYA_POMPA', name: 'Pompa Yönetimi', group: 'Vardiya Yönetimi' },
    { key: 'VARDIYA_MARKET', name: 'Market Yönetimi', group: 'Vardiya Yönetimi' },
    { key: 'VARDIYA_KARSILASTIRMA', name: 'Karşılaştırma Raporu', group: 'Vardiya Yönetimi' },
    { key: 'VARDIYA_ONAY_BEKLEYENLER', name: 'Onay Bekleyenler', group: 'Vardiya Yönetimi' },
    { key: 'VARDIYA_STOK', name: 'Yakıt Stok Girişi', group: 'Vardiya Yönetimi' },
    { key: 'VARDIYA_LOGLAR', name: 'İşlem Geçmişi', group: 'Vardiya Yönetimi' },

    // Tanımlamalar
    { key: 'TANIMLAMA_ISTASYON', name: 'İstasyon Tanımlama', group: 'Tanımlamalar' },
    { key: 'TANIMLAMA_PERSONEL', name: 'Personel Tanımlama', group: 'Tanımlamalar' },
    { key: 'TANIMLAMA_YAKIT', name: 'Yakıt Türü Tanımlama', group: 'Tanımlamalar' },

    // Raporlar
    { key: 'RAPOR_VARDIYA', name: 'Vardiya Raporu', group: 'Raporlar' },
    { key: 'RAPOR_PERSONEL', name: 'Personel Karnesi', group: 'Raporlar' },
    { key: 'RAPOR_FARK', name: 'Fark Raporu', group: 'Raporlar' },

    // Yönetim
    { key: 'YONETIM_ISTASYON', name: 'İstasyon Yönetimi', group: 'Yönetim' },
    { key: 'YONETIM_KULLANICI', name: 'Kullanıcı Yönetimi', group: 'Yönetim' },
    { key: 'YONETIM_YETKI', name: 'Yetki Yönetimi', group: 'Yönetim' },

    // Sistem
    { key: 'SISTEM_AYARLAR', name: 'Sistem Ayarları', group: 'Sistem' },
    { key: 'SISTEM_SAGLIK', name: 'Sistem Sağlığı', group: 'Sistem' },
    { key: 'SISTEM_ROLLER', name: 'Rol Yönetimi', group: 'Sistem' },
    { key: 'SISTEM_BILDIRIM', name: 'Bildirim Gönder', group: 'Sistem' }
];

// Varsayılan Yetkiler
const DEFAULT_PERMISSIONS: { [role: string]: string[] } = {
    'admin': RESOURCES.map(r => r.key), // Admin her şeye erişir
    'patron': [
        'VARDIYA_LISTESI', 'VARDIYA_POMPA', 'VARDIYA_MARKET', 'VARDIYA_KARSILASTIRMA',
        'VARDIYA_ONAY_BEKLEYENLER', 'VARDIYA_STOK', 'VARDIYA_LOGLAR',
        'TANIMLAMA_ISTASYON', 'TANIMLAMA_PERSONEL', 'TANIMLAMA_YAKIT',
        'RAPOR_VARDIYA', 'RAPOR_PERSONEL', 'RAPOR_FARK',
        'YONETIM_ISTASYON', 'YONETIM_KULLANICI',
        'SISTEM_AYARLAR'
    ],
    'istasyon sorumlusu': [
        'VARDIYA_LISTESI', 'VARDIYA_POMPA', 'VARDIYA_MARKET', 'VARDIYA_KARSILASTIRMA',
        'VARDIYA_STOK',
        'TANIMLAMA_PERSONEL', 'TANIMLAMA_YAKIT',
        'RAPOR_VARDIYA', 'RAPOR_PERSONEL', 'RAPOR_FARK'
    ],
    'vardiya sorumlusu': [
        'VARDIYA_LISTESI', 'VARDIYA_POMPA', 'VARDIYA_STOK',
        'RAPOR_VARDIYA'
    ],
    'market sorumlusu': [
        'VARDIYA_MARKET',
        'RAPOR_VARDIYA'
    ]
};

@Injectable({
    providedIn: 'root'
})
export class PermissionService {
    private permissionsSubject = new BehaviorSubject<{ [role: string]: string[] }>({});
    permissions$ = this.permissionsSubject.asObservable();
    private apiUrl = `${environment.apiUrl}/permission`;

    constructor(private http: HttpClient) {
        this.loadPermissions();
    }

    getResources(): Resource[] {
        return RESOURCES;
    }

    loadPermissions() {
        this.http.get<{ [role: string]: string[] }>(`${this.apiUrl}/all`).subscribe({
            next: (data) => {
                // Merge with defaults to ensure admin always has access even if DB is empty
                const merged = { ...DEFAULT_PERMISSIONS, ...data };
                this.permissionsSubject.next(merged);
            },
            error: (err) => {
                console.error('Yetkiler yüklenemedi, varsayılanlar kullanılıyor.', err);
                this.permissionsSubject.next(DEFAULT_PERMISSIONS);
            }
        });
    }

    getPermissions(role: string): string[] {
        const current = this.permissionsSubject.value;
        const normalizedRole = role.toLowerCase().trim();
        return current[normalizedRole] || DEFAULT_PERMISSIONS[normalizedRole] || [];
    }

    hasAccess(role: string, resourceKey: string): boolean {
        if (!role) return false;
        const normalizedRole = role.toLowerCase().trim();

        // Admin her zaman tam yetkili
        if (normalizedRole === 'admin') return true;

        const perms = this.getPermissions(normalizedRole);
        return perms.includes(resourceKey);
    }

    updatePermissions(role: string, resources: string[]): Observable<any> {
        return this.http.post(`${this.apiUrl}/${role}`, resources).pipe(
            tap(() => {
                // Local state'i güncelle
                const current = this.permissionsSubject.value;
                const normalizedRole = role.toLowerCase().trim();
                current[normalizedRole] = resources;
                this.permissionsSubject.next(current);
            })
        );
    }

    resetToDefaults() {
        // Backend'i sıfırlamak için her rol için tek tek update çağırmak gerekebilir
        // veya backend'e özel bir reset endpoint'i eklenebilir.
        // Şimdilik sadece local state'i ve UI'ı güncelliyoruz, kullanıcı kaydet dediğinde backend güncellenir.
        this.permissionsSubject.next(DEFAULT_PERMISSIONS);
    }
}
