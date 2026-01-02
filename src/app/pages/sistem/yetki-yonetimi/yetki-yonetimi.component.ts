import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { PermissionService, Resource } from '../../../services/permission.service';
import { RoleService, Role } from '../../../services/role.service';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'app-yetki-yonetimi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        CheckboxModule,
        ToastModule,
        CardModule
    ],
    providers: [MessageService],
    templateUrl: './yetki-yonetimi.component.html',
    styleUrls: ['./yetki-yonetimi.component.scss']
})
export class YetkiYonetimiComponent implements OnInit {
    roles: Role[] = [];
    resources: Resource[] = [];
    groupedResources: { group: string, items: Resource[] }[] = [];

    // roleId -> [resourceKey1, resourceKey2]
    permissions: { [roleName: string]: string[] } = {};

    loading: boolean = false;

    constructor(
        private permissionService: PermissionService,
        private roleService: RoleService,
        private messageService: MessageService
    ) { }

    ngOnInit() {
        this.loading = true;

        forkJoin({
            roles: this.roleService.getAll(),
            // Resources are static in service
        }).subscribe({
            next: (data) => {
                // Admin rolünü filtrele (Admin her zaman tam yetkili, düzenlenemez)
                this.roles = data.roles.filter(r => r.ad.toLowerCase() !== 'admin');

                this.resources = this.permissionService.getResources();
                this.groupResources();
                this.loadCurrentPermissions();

                this.loading = false;
            },
            error: (err) => {
                console.error('Veri yükleme hatası:', err);
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Veriler yüklenemedi.' });
                this.loading = false;
            }
        });
    }

    groupResources() {
        const groups: { [key: string]: Resource[] } = {};
        this.resources.forEach(r => {
            if (!groups[r.group]) {
                groups[r.group] = [];
            }
            groups[r.group].push(r);
        });

        this.groupedResources = Object.keys(groups).map(groupName => ({
            group: groupName,
            items: groups[groupName]
        }));
    }

    loadCurrentPermissions() {
        this.roles.forEach(role => {
            this.permissions[role.ad.toLowerCase()] = [...this.permissionService.getPermissions(role.ad)];
        });
    }

    hasPermission(role: Role, resource: Resource): boolean {
        const rolePerms = this.permissions[role.ad.toLowerCase()] || [];
        return rolePerms.includes(resource.key);
    }

    togglePermission(role: Role, resource: Resource, event: any) {
        const roleName = role.ad.toLowerCase();
        if (!this.permissions[roleName]) {
            this.permissions[roleName] = [];
        }

        const isChecked = event.checked;
        if (isChecked) {
            if (!this.permissions[roleName].includes(resource.key)) {
                this.permissions[roleName].push(resource.key);
            }
        } else {
            this.permissions[roleName] = this.permissions[roleName].filter(k => k !== resource.key);
        }
    }

    save() {
        this.loading = true;
        const observables = Object.keys(this.permissions).map(roleName =>
            this.permissionService.updatePermissions(roleName, this.permissions[roleName])
        );

        if (observables.length === 0) {
            this.loading = false;
            this.messageService.add({ severity: 'info', summary: 'Bilgi', detail: 'Değişiklik yok.' });
            return;
        }

        forkJoin(observables).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Yetkiler güncellendi.' });
                this.loading = false;
            },
            error: (err) => {
                console.error('Kaydetme hatası:', err);
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kaydetme başarısız.' });
                this.loading = false;
            }
        });
    }

    reset() {
        if (confirm('Tüm yetkileri varsayılan ayarlara döndürmek istediğinize emin misiniz?')) {
            this.permissionService.resetToDefaults();
            this.loadCurrentPermissions();
            this.messageService.add({ severity: 'info', summary: 'Bilgi', detail: 'Varsayılan ayarlar yüklendi.' });
        }
    }
}
