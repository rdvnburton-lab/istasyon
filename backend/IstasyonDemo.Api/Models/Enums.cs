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
