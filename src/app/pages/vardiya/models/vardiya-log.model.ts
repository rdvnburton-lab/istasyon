export interface VardiyaLog {
    id: number;
    vardiyaId: number;
    vardiyaDosyaAdi: string;
    istasyonAdi: string;
    islem: string;
    aciklama?: string;
    kullaniciId?: number;
    kullaniciAdi?: string;
    kullaniciRol?: string;
    islemTarihi: Date;
    eskiDurum?: string;
    yeniDurum?: string;
}

export enum LogIslemTipi {
    OLUSTURULDU = 'OLUSTURULDU',
    ONAYA_GONDERILDI = 'ONAYA_GONDERILDI',
    ONAYLANDI = 'ONAYLANDI',
    REDDEDILDI = 'REDDEDILDI',
    SILME_TALEP_EDILDI = 'SILME_TALEP_EDILDI',
    SILINDI = 'SILINDI',
    SILME_REDDEDILDI = 'SILME_REDDEDILDI'
}
