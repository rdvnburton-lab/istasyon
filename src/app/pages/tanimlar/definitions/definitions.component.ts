import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { InputNumberModule } from 'primeng/inputnumber';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { TabViewModule } from 'primeng/tabview';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { MessageService, ConfirmationService } from 'primeng/api';
import { DefinitionsService, SystemDefinition, DefinitionType } from '../../../services/definitions.service';

@Component({
    selector: 'app-definitions',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        InputSwitchModule,
        InputNumberModule,
        ConfirmDialogModule,
        ToastModule,
        ToolbarModule,
        TabViewModule,
        TagModule,
        TooltipModule,
        IconFieldModule,
        InputIconModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './definitions.component.html',
    styles: [`
        :host ::ng-deep .custom-tabs .p-tabview-nav {
            background: transparent;
            border: none;
            margin-bottom: 2rem;
        }
        :host ::ng-deep .custom-tabs .p-tabview-nav li .p-tabview-nav-link {
            background: transparent;
            border: none;
            border-bottom: 2px solid transparent;
            font-weight: 600;
            transition: all 0.3s;
            border-radius: 0;
            padding: 1rem 1.5rem;
            color: var(--text-color-secondary);
        }
        :host ::ng-deep .custom-tabs .p-tabview-nav li.p-highlight .p-tabview-nav-link {
            border-bottom: 2px solid var(--primary-color);
            color: var(--primary-color);
            background: rgba(var(--primary-color-rgb), 0.05);
        }
        @keyframes slow-spin {
            from { transform: rotate(0deg); }
            to { transform: rotate(360deg); }
        }
        .animate-spin-slow {
            animation: slow-spin 8s linear infinite;
        }
        .rounded-dialog .p-dialog-content {
            border-radius: 1rem !important;
        }
    `]
})
export class DefinitionsComponent implements OnInit {
    definitions: SystemDefinition[] = [];
    definition: SystemDefinition = this.getEmptyDefinition();
    selectedDefinition: SystemDefinition | null = null;

    definitionDialog: boolean = false;
    submitted: boolean = false;

    // Tab State
    activeType: DefinitionType = DefinitionType.BANKA;

    tabs = [
        { label: 'Banka Tanımları', type: DefinitionType.BANKA, icon: 'pi pi-building' },
        { label: 'Gider Kalemleri', type: DefinitionType.GIDER, icon: 'pi pi-minus-circle' },
        { label: 'Gelir Kalemleri', type: DefinitionType.GELIR, icon: 'pi pi-plus-circle' },
        { label: 'Ödeme Yöntemleri', type: DefinitionType.ODEME, icon: 'pi pi-credit-card' },
        { label: 'Geliş Yöntemleri', type: DefinitionType.GELIS_YONTEMI, icon: 'pi pi-truck' },
        { label: 'Pompa Giderleri', type: DefinitionType.POMPA_GIDER, icon: 'pi pi-wrench' },
        { label: 'Pusula Türleri', type: DefinitionType.PUSULA_TURU, icon: 'pi pi-file-edit' }
    ];

    constructor(
        private definitionsService: DefinitionsService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.loadDefinitions();
    }

    onTabChange(event: any) {
        // Tab index to Type mapping
        this.activeType = this.tabs[event.index].type;
        this.loadDefinitions();
    }

    loadDefinitions() {
        this.definitionsService.getByType(this.activeType).subscribe(data => {
            this.definitions = data;
        });
    }

    openNew() {
        this.definition = this.getEmptyDefinition();
        this.definition.type = this.activeType;
        this.submitted = false;
        this.definitionDialog = true;
    }

    editDefinition(item: SystemDefinition) {
        this.definition = { ...item };
        this.definitionDialog = true;
    }

    deleteDefinition(item: SystemDefinition) {
        this.confirmationService.confirm({
            message: '"' + item.name + '" tanımını silmek istediğinize emin misiniz?',
            header: 'Onay',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.definitionsService.delete(item.id!).subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Tanım Silindi', life: 3000 });
                    this.loadDefinitions();
                });
            }
        });
    }

    hideDialog() {
        this.definitionDialog = false;
        this.submitted = false;
    }

    saveDefinition() {
        this.submitted = true;

        if (this.definition.name?.trim()) {
            if (this.definition.id) {
                this.definitionsService.update(this.definition.id, this.definition).subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Tanım Güncellendi', life: 3000 });
                    this.loadDefinitions();
                    this.definitionDialog = false;
                    this.definition = this.getEmptyDefinition();
                });
            } else {
                this.definitionsService.create(this.definition).subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Tanım Oluşturuldu', life: 3000 });
                    this.loadDefinitions();
                    this.definitionDialog = false;
                    this.definition = this.getEmptyDefinition();
                });
            }
        }
    }

    getEmptyDefinition(): SystemDefinition {
        return {
            type: this.activeType,
            name: '',
            description: '',
            isActive: true,
            sortOrder: 0,
            code: ''
        };
    }

    getCurrentTabLabel(): string {
        const activeTab = this.tabs.find(t => t.type === this.activeType);
        return activeTab ? activeTab.label : '';
    }
}
