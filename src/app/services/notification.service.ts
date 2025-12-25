import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

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

    private pollingInterval: any;

    constructor(private http: HttpClient) {
        // Otomatik başlatma yerine manuel başlatma tercih ediyoruz
        // AppLayout veya AuthService üzerinden tetiklenecek
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
}
