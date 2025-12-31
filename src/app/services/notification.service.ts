import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { PushNotifications } from '@capacitor/push-notifications';
import { FirebaseMessaging } from '@capacitor-firebase/messaging';
import { Capacitor } from '@capacitor/core';

export interface Notification {
    id: number;
    title: string;
    message: string;
    time: string;
    read: boolean;
    icon: string;
    severity: 'info' | 'success' | 'warning' | 'danger';
    relatedId?: number;
    relatedType?: string;
}

export interface NotificationSummary {
    unreadCount: number;
    notifications: Notification[];
}

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private apiUrl = `${environment.apiUrl}/notification`;
    private notificationsSubject = new BehaviorSubject<NotificationSummary>({ unreadCount: 0, notifications: [] });
    public notifications$ = this.notificationsSubject.asObservable();
    public fcmToken = new BehaviorSubject<string | null>(null);

    private pollingInterval: any;

    constructor(private http: HttpClient) {
        // Otomatik başlatma yerine manuel başlatma tercih ediyoruz
        // AppLayout veya AuthService üzerinden tetiklenecek
    }

    private isListenersAdded = false;

    async initPush() {
        console.log('🔔 initPush: Başlatılıyor...');
        if (!Capacitor.isNativePlatform()) {
            console.log('🔔 initPush: Sadece mobil cihazlarda çalışır.');
            return;
        }
        try {
            const permStatus = await PushNotifications.checkPermissions();
            console.log('🔔 initPush: Mevcut izin durumu:', JSON.stringify(permStatus));

            if (permStatus.receive === 'prompt') {
                console.log('🔔 initPush: İzin isteniyor...');
                const newPerm = await PushNotifications.requestPermissions();
                console.log('🔔 initPush: Yeni izin sonucu:', JSON.stringify(newPerm));
                if (newPerm.receive !== 'granted') {
                    console.warn('🔔 initPush: İzin verilmedi.');
                    return;
                }
            } else if (permStatus.receive !== 'granted') {
                console.warn('🔔 initPush: İzin daha önce reddedilmiş.');
                return;
            }

            // Listeners must be added BEFORE register()
            this.addListeners();

            console.log('🔔 initPush: PushNotifications.register() çağrılıyor...');
            await PushNotifications.register();
            console.log('🔔 initPush: PushNotifications.register() başarılı.');
        } catch (error) {
            console.error('🔔 initPush: HATA:', error);
        }
    }

    private addListeners() {
        if (this.isListenersAdded) {
            console.log('🔔 addListeners: Listenerlar zaten eklenmiş, atlanıyor.');
            return;
        }
        console.log('🔔 addListeners: Dinleyiciler ekleniyor...');
        this.isListenersAdded = true;
        PushNotifications.addListener('registration', async token => {
            console.log('Push Registration Token (APNs): ', token.value);

            let fcmToken = token.value;
            if (Capacitor.getPlatform() === 'ios') {
                try {
                    const res = await FirebaseMessaging.getToken();
                    fcmToken = res.token;
                    console.log('FCM Token (iOS): ', fcmToken);
                } catch (e) {
                    console.error('FCM Token alma hatası:', e);
                }
            }

            this.fcmToken.next(fcmToken);
            this.saveTokenToBackend(fcmToken);
        });

        PushNotifications.addListener('registrationError', error => {
            console.error('Push kayıt hatası: ', error);
        });

        PushNotifications.addListener('pushNotificationReceived', notification => {
            console.log('Bildirim alındı: ', notification);
            // Polling'i tetikle ki yeni bildirim listeye düşsün
            this.loadNotifications().subscribe();
        });

        PushNotifications.addListener('pushNotificationActionPerformed', notification => {
            console.log('Bildirime tıklandı: ', notification);
            // Yönlendirme mantığı buraya eklenecek
        });
    }

    private saveTokenToBackend(token: string) {
        // Token'ı backend'e gönder
        // Backend endpoint'i: POST /api/notification/register-token
        this.http.post(`${this.apiUrl}/register-token`, { token }).subscribe({
            next: () => console.log('Token backend\'e kaydedildi.'),
            error: (err) => console.error('Token kaydetme hatası:', err)
        });
    }

    startPolling() {
        if (this.pollingInterval) return;

        // İlk yükleme
        this.loadNotifications().subscribe();

        // 30 saniyede bir kontrol
        this.pollingInterval = setInterval(() => {
            this.loadNotifications().subscribe();
        }, 30000);
    }

    stopPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = null;
        }
    }

    loadNotifications(): Observable<NotificationSummary> {
        return this.http.get<NotificationSummary>(this.apiUrl).pipe(
            tap(data => this.notificationsSubject.next(data)),
            catchError(error => {
                console.error('Bildirimler yüklenirken hata:', error);
                return of({ unreadCount: 0, notifications: [] });
            })
        );
    }

    markAsRead(notificationId: number): Observable<any> {
        return this.http.post(`${this.apiUrl}/mark-read/${notificationId}`, {}).pipe(
            tap(() => {
                const current = this.notificationsSubject.value;
                const updated = {
                    ...current,
                    notifications: current.notifications.map(n =>
                        n.id === notificationId ? { ...n, read: true } : n
                    ),
                    unreadCount: Math.max(0, current.unreadCount - 1)
                };
                this.notificationsSubject.next(updated);
            }),
            catchError(error => {
                console.error('Bildirim okundu işaretlenirken hata:', error);
                return of(null);
            })
        );
    }

    markAllAsRead(): Observable<any> {
        return this.http.post(`${this.apiUrl}/mark-all-read`, {}).pipe(
            tap(() => {
                const current = this.notificationsSubject.value;
                const updated = {
                    ...current,
                    notifications: current.notifications.map(n => ({ ...n, read: true })),
                    unreadCount: 0
                };
                this.notificationsSubject.next(updated);
            }),
            catchError(error => {
                console.error('Tüm bildirimler okundu işaretlenirken hata:', error);
                return of(null);
            })
        );
    }

    syncLogs(): Observable<any> {
        return this.http.post(`${this.apiUrl}/sync-logs`, {}).pipe(
            tap(() => this.loadNotifications().subscribe())
        );
    }

    getUnreadCount(): number {
        return this.notificationsSubject.value.unreadCount;
    }

    getNotifications(): Notification[] {
        return this.notificationsSubject.value.notifications;
    }

    sendTestNotification(data: { userId?: number, userIds?: number[], title: string, message: string }): Observable<any> {
        return this.http.post(`${this.apiUrl}/send-test`, data);
    }
}
