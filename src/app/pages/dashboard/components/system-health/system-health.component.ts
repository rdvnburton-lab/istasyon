import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { ProgressBarModule } from 'primeng/progressbar';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { TimelineModule } from 'primeng/timeline';
import { TabsModule } from 'primeng/tabs';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PaginatorModule } from 'primeng/paginator';
import { FormsModule } from '@angular/forms';

interface EndpointHealth {
    name: string;
    path: string;
    status: string;
    responseTimeMs: number;
    lastChecked: string;
}

interface SystemHealth {
    status: {
        databaseStatus: string;
        storageStatus: string;
        storageUsage: string;
        availableFreeSpace: string;
        uptimeDays: number;
        serverTime: string;
    };
    anomalies: any[];
    recentErrors: any[];
    metrics: {
        totalSize: string;
        totalSizeRaw: number;
        freeSpaceRaw: number;
        connectionCount: number;
        totalRows: number;
    };
    tableStats: any[];
    endpoints: EndpointHealth[];
    environment: {
        osVersion: string;
        runtimeVersion: string;
        machineName: string;
        processorCount: number;
        deploymentMode: string;
    };
    resources: {
        memoryUsage: string;
        memoryUsageRaw: number;
        cpuUsage: string;
        cpuUsageRaw: number;
    };
    security: {
        failedLoginsLast24h: number;
        recentEvents: any[];
    };
    activeSessions: any[];
    backgroundTasks: any[];
}

@Component({
    selector: 'app-system-health',
    standalone: true,
    imports: [
        CommonModule,
        TableModule,
        TagModule,
        CardModule,
        ProgressBarModule,
        ButtonModule,
        TooltipModule,
        ToastModule,
        TimelineModule,
        TabsModule,
        InputTextModule,
        SelectModule,
        PaginatorModule,
        FormsModule
    ],
    providers: [MessageService],
    templateUrl: './system-health.component.html',
    styleUrls: ['./system-health.component.css']
})
export class SystemHealthComponent implements OnInit, OnDestroy {
    health: SystemHealth | null = null;
    backups: any[] = [];
    advancedLogs: any[] = [];
    totalLogs = 0;
    logQuery = {
        level: '',
        searchTerm: '',
        page: 1,
        pageSize: 20
    };
    loading = false;
    loadingBackups = false;
    loadingLogs = false;
    private refreshInterval: any;

    constructor(private http: HttpClient, private messageService: MessageService) { }

    ngOnInit(): void {
        this.loadHealth();
        this.refreshInterval = setInterval(() => {
            this.loadHealth(true);
        }, 30000);
    }

    ngOnDestroy(): void {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }
    }

    loadHealth(silent = false): void {
        if (!silent) this.loading = true;
        this.http.get<SystemHealth>(`${environment.apiUrl}/systemhealth`).subscribe({
            next: (data) => {
                console.log('System Health Data:', data);
                this.health = data;
                this.loading = false;
                if (!silent) {
                    this.messageService.add({ severity: 'success', summary: 'Güncellendi', detail: 'Sistem verileri başarıyla yüklendi.' });
                }
            },
            error: (err) => {
                console.error('Health data error:', err);
                this.loading = false;
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Sistem verileri alınamadı.' });
            }
        });
    }

    loadBackups(): void {
        this.loadingBackups = true;
        this.http.get<any[]>(`${environment.apiUrl}/maintenance/backups`).subscribe({
            next: (data) => {
                this.backups = data;
                this.loadingBackups = false;
            },
            error: () => {
                this.loadingBackups = false;
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Yedek listesi alınamadı.' });
            }
        });
    }

    createBackup(): void {
        this.loadingBackups = true;
        this.http.post(`${environment.apiUrl}/maintenance/backup`, {}).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Yedekleme başlatıldı.' });
                this.loadBackups();
            },
            error: (err) => {
                this.loadingBackups = false;
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Yedekleme başarısız: ' + (err.error || 'Bilinmeyen hata') });
            }
        });
    }

    downloadBackup(fileName: string): void {
        this.http.get(`${environment.apiUrl}/maintenance/backups/download/${fileName}`, {
            responseType: 'blob'
        }).subscribe({
            next: (blob) => {
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = fileName;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Yedek indirilemedi.' });
            }
        });
    }

    deleteBackup(fileName: string): void {
        if (!confirm('Bu yedeği silmek istediğinize emin misiniz?')) return;
        this.http.delete(`${environment.apiUrl}/maintenance/backups/${fileName}`).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Silindi', detail: 'Yedek başarıyla silindi.' });
                this.loadBackups();
            }
        });
    }

    loadAdvancedLogs(): void {
        this.loadingLogs = true;
        const params = {
            level: this.logQuery.level,
            searchTerm: this.logQuery.searchTerm,
            page: this.logQuery.page,
            pageSize: this.logQuery.pageSize
        };
        this.http.get<any>(`${environment.apiUrl}/maintenance/logs`, { params }).subscribe({
            next: (data) => {
                this.advancedLogs = data.logs;
                this.totalLogs = data.total;
                this.loadingLogs = false;
            },
            error: () => {
                this.loadingLogs = false;
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Loglar alınamadı.' });
            }
        });
    }

    onLogPageChange(event: any): void {
        this.logQuery.page = (event.first / event.rows) + 1;
        this.logQuery.pageSize = event.rows;
        this.loadAdvancedLogs();
    }

    getSeverity(severity: string): 'success' | 'info' | 'warn' | 'danger' {
        switch (severity.toLowerCase()) {
            case 'critical': return 'danger';
            case 'warning': return 'warn';
            case 'info': return 'info';
            default: return 'success';
        }
    }

    calculateRatio(rowCount: number): number {
        if (!this.health?.metrics?.totalRows) return 0;
        return Math.min(100, Math.round((rowCount / this.health.metrics.totalRows) * 100));
    }

    parsePercent(val: string | undefined | null): number {
        if (!val) return 0;
        return parseFloat(val.replace('%', '')) || 0;
    }

    getServerTimeOnly(): string {
        if (!this.health?.status?.serverTime) return '...';
        const parts = this.health.status.serverTime.split(' ');
        return parts.length > 1 ? parts[1] : parts[0];
    }

    getDbUsagePercent(): number {
        if (!this.health?.metrics?.totalSizeRaw || !this.health?.metrics?.freeSpaceRaw) return 0;
        const total = this.health.metrics.totalSizeRaw + this.health.metrics.freeSpaceRaw;
        return Math.round((this.health.metrics.totalSizeRaw / total) * 100) || 0;
    }

    getDbTotalCapacity(): string {
        if (!this.health?.metrics?.totalSizeRaw || !this.health?.metrics?.freeSpaceRaw) return '...';
        const totalBytes = this.health.metrics.totalSizeRaw + this.health.metrics.freeSpaceRaw;
        const gb = totalBytes / (1024 * 1024 * 1024);
        return gb.toFixed(1) + ' GB';
    }
}
