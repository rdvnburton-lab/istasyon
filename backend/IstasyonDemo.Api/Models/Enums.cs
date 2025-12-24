namespace IstasyonDemo.Api.Models
{
    public enum YakitTuru
    {
        BENZIN,
        MOTORIN,
        LPG,
        EURO_DIESEL
    }

    public enum VardiyaDurum
    {
        ACIK,
        ONAY_BEKLIYOR,
        ONAYLANDI,
        REDDEDILDI,
        SILINME_ONAYI_BEKLIYOR,
        SILINDI
    }

    public enum PersonelRol
    {
        POMPACI,
        MARKET_GOREVLISI,
        VARDIYA_SORUMLUSU,
        PATRON,
        MARKET_SORUMLUSU
    }

    public enum PusulaTuru
    {
        KASA_ACILIS,        // Kasa açılış pusulası
        KASA_DEVIR,         // Kasa devir pusulası
        NAKIT_TAHSILAT,     // Nakit tahsilat
        KREDI_KARTI,        // Kredi kartı tahsilat
        HAVALE_EFT,         // Havale/EFT
        FARK_FAZLA,         // Fazla para
        FARK_EKSIK,         // Eksik para
        MASRAF,             // Masraf pusulası
        DIGER               // Diğer
    }
    public enum GiderTuru
    {
        EKMEK,
        TEMIZLIK,
        PERSONEL,
        KIRTASIYE,
        DIGER
    }

    public enum GelirTuru
    {
        KOMISYON,
        PRIM,
        DIGER
    }
}
