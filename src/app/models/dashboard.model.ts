export interface SorumluDashboardDto {
    kullaniciAdi: string;
    adSoyad: string;
    rol: string;
    firmaAdi: string;
    istasyonAdi: string;
    aktifVardiyaId?: number;
    aktifMarketVardiyaId?: number;
    sonVardiyaTarihi?: Date;
    sonVardiyaTutar: number;
    sonMarketVardiyaTarihi?: Date;
    sonMarketVardiyaTutar: number;
    bekleyenOnaySayisi: number;
    bekleyenMarketOnaySayisi: number;
}
