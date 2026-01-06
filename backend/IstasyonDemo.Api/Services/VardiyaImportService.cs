using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace IstasyonDemo.Api.Services
{
    public class VardiyaImportService : IVardiyaImportService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VardiyaImportService> _logger;
        private readonly IYakitService _yakitService;

        public VardiyaImportService(AppDbContext context, ILogger<VardiyaImportService> logger, IYakitService yakitService)
        {
            _context = context;
            _logger = logger;
            _yakitService = yakitService;
        }

        public async Task<VardiyaImportResult> ParseXmlZipAsync(Stream zipStream, string fileName, int userId)
        {
            try
            {
                var result = new VardiyaImportResult();
                // 1. ZIP'i Belleğe (byte[]) Al
                using var memoryStream = new MemoryStream();
                await zipStream.CopyToAsync(memoryStream);
                var zipBytes = memoryStream.ToArray();

                // 2. Hash Hesapla
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(zipBytes);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                // 3. ZIP Çıkart ve XML oku
                using var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
                var xmlEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

                if (xmlEntry == null)
                    throw new InvalidOperationException("ZIP dosyası içinde .xml uzantılı dosya bulunamadı.");

                string xmlContent;
                using (var stream = xmlEntry.Open())
                using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding("windows-1254")))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                var xdoc = XDocument.Parse(xmlContent);

                // 4. İstasyon Tanımlama
                var globalParams = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "GlobalParams");
                var stationCode = globalParams?.Elements().FirstOrDefault(x => x.Name.LocalName == "StationCode")?.Value;
                
                Istasyon? station = null;
                if (!string.IsNullOrEmpty(stationCode))
                {
                    station = await _context.Istasyonlar.FirstOrDefaultAsync(i => i.IstasyonKodu == stationCode);
                }

                if (station == null)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user?.IstasyonId != null)
                    {
                        station = await _context.Istasyonlar.FindAsync(user.IstasyonId);
                        
                        if (station != null && string.IsNullOrEmpty(station.IstasyonKodu) && !string.IsNullOrEmpty(stationCode))
                        {
                            station.IstasyonKodu = stationCode;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                if (station == null)
                    throw new InvalidOperationException($"İstasyon tanımlanamadı! XML StationCode: {stationCode}");

                // 5. Vardiya Dönem Bilgileri
                var shifts = xdoc.Descendants().Where(x => x.Name.LocalName == "Shift").ToList();
                var firstShift = shifts.FirstOrDefault();
                var shiftDateStr = firstShift?.Elements().FirstOrDefault(x => x.Name.LocalName == "ShiftDate")?.Value;
                
                DateTime vardiyaTarihi = DateTime.UtcNow.Date;
                if (DateTime.TryParseExact(shiftDateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pDate))
                {
                    vardiyaTarihi = DateTime.SpecifyKind(pDate, DateTimeKind.Utc);
                }

                // 6. DTO Oluştur
                var dto = new CreateVardiyaDto
                {
                    IstasyonId = station.Id,
                    BaslangicTarihi = vardiyaTarihi,
                    BitisTarihi = vardiyaTarihi.AddHours(23).AddMinutes(59),
                    DosyaAdi = fileName,
                    OtomasyonSatislar = new List<CreateOtomasyonSatisDto>(),
                    FiloSatislar = new List<CreateFiloSatisDto>(),
                    TankEnvanterleri = new List<CreateVardiyaTankEnvanteriDto>()
                };

                // 7. Satışları Parse Et (Txn Elements)
                var txns = xdoc.Descendants().Where(x => x.Name.LocalName == "Txn").ToList();
                _logger.LogInformation($"XML Parsing: {txns.Count} adet 'Txn' elementi bulundu.");

                foreach (var txn in txns)
                {
                    var tagDetails = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "TagDetails");
                    var saleDetails = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "SaleDetails");

                    if (saleDetails == null) continue;

                    string fleetCode = tagDetails?.Elements().FirstOrDefault(x => x.Name.LocalName == "FleetCode")?.Value ?? "C0000";
                    string plate = tagDetails?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "";
                    string tagNr = tagDetails?.Elements().FirstOrDefault(x => x.Name.LocalName == "TagNr")?.Value ?? "";
                    
                    int.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "TxnType")?.Value, out int txnType);
                    string dateTimeStr = saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "DateTime")?.Value ?? "";
                    int.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr")?.Value, out int receiptNr);
                    decimal.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "UnitPrice")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitPrice);
                    decimal.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "Amount")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);
                    decimal.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "Total")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal total);
                    int.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpNr")?.Value, out int pumpNr);
                    int.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "NozzleNr")?.Value, out int nozzleNr);
                    int.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "PaymentType")?.Value, out int paymentType);
                    string ecrPlate = saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value ?? "";
                    int.TryParse(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRReceiptNr")?.Value, out int ecrReceiptNr);
                    
                    DateTime satisTarihi = vardiyaTarihi;
                    if (DateTime.TryParseExact(dateTimeStr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sd))
                    {
                        satisTarihi = DateTime.SpecifyKind(sd, DateTimeKind.Utc);
                    }

                    // Otomasyon vs Filo ayrımı
                    bool isOtomasyon = fleetCode == "C0000" || string.IsNullOrEmpty(fleetCode) || fleetCode == "0";

                    if (isOtomasyon)
                    {
                        dto.OtomasyonSatislar.Add(new CreateOtomasyonSatisDto
                        {
                            PersonelAdi = plate, // Plate field is used for attendant name in C0000
                            PersonelKeyId = tagNr,
                            PompaNo = pumpNr,
                            TabancaNo = nozzleNr,
                            YakitTuru = (await _yakitService.IdentifyYakitAsync(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value ?? ""))?.Ad ?? "DIGER",
                            Litre = amount / 100, 
                            BirimFiyat = unitPrice / 100, // XML values are usually scaled (e.g. 4748 -> 47.48)
                            ToplamTutar = total / 100,
                            SatisTarihi = satisTarihi,
                            FisNo = receiptNr,
                            Plaka = ecrPlate,
                            SatisTuru = txnType,
                            OdemeTuru = paymentType,
                            YazarKasaPlaka = ecrPlate,
                            YazarKasaFisNo = ecrReceiptNr,
                            TagNr = tagNr
                        });
                    }
                    else
                    {
                        dto.FiloSatislar.Add(new CreateFiloSatisDto
                        {
                            FiloKodu = fleetCode,
                            FiloAdi = tagDetails?.Elements().FirstOrDefault(x => x.Name.LocalName == "FleetName")?.Value ?? "",
                            Plaka = !string.IsNullOrEmpty(plate) ? plate : ecrPlate,
                            Tutar = total / 100,
                            Litre = amount / 100,
                            BirimFiyat = unitPrice / 100,
                            Tarih = satisTarihi,
                            PompaNo = pumpNr,
                            FisNo = receiptNr,
                            YakitTuru = (await _yakitService.IdentifyYakitAsync(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value ?? ""))?.Ad ?? "DIGER",
                            TagNr = tagNr
                        });
                    }
                }

                // Update Vardiya Start/End dates based on transactions
                if (dto.OtomasyonSatislar.Any() || dto.FiloSatislar.Any())
                {
                    var allDates = dto.OtomasyonSatislar.Select(s => s.SatisTarihi)
                        .Concat(dto.FiloSatislar.Select(s => s.Tarih)).ToList();
                    dto.BaslangicTarihi = allDates.Min();
                    dto.BitisTarihi = allDates.Max();
                }

                // 7.1 Pompa Endekslerini Hesapla
                // Tüm satışları (Otomasyon + Filo) birleştirip pompa/tabanca bazında gruplayacağız
                var allSales = new List<dynamic>();
                
                foreach(var s in dto.OtomasyonSatislar)
                {
                    allSales.Add(new { s.PompaNo, s.TabancaNo, s.YakitTuru, s.SatisTarihi, s.Litre });
                }
                foreach(var s in dto.FiloSatislar)
                {
                    allSales.Add(new { s.PompaNo, s.TabancaNo, s.YakitTuru, Tarih = s.Tarih, s.Litre });
                }

                // Not: XML'de her işlemde TotalizerStart ve TotalizerEnd olmayabilir.
                // Ancak genellikle vardiya raporlarında bu bilgi "PumpReport" veya benzeri bir bölümde olur.
                // Eğer yoksa, elimizdeki verilerle sadece "Satılan Litre"yi biliyoruz, gerçek endeksi bilemeyiz.
                // Ancak kullanıcı "xml den pompa endeksleri geliyor" dediği için, Txn içinde TotalizerStart/End arayacağız.
                // Txn döngüsünde bu değerleri geçici bir listede tutalım.
                
                // Bu adım için Txn döngüsüne geri dönüp Totalizer değerlerini okumamız lazım.
                // Performans için Txn döngüsü içinde bir Dictionary doldurabiliriz.
                // Ancak şu an kod yapısı gereği, Txn döngüsü bitti. 
                // Txn listesi hala elimizde (var txns), tekrar dönebiliriz.

                // 7.1 Pompa Endekslerini Hesapla
                // XML'de <Pump><Nozzles><Nozzle> yapısı içinde Totalizer bilgisi var.
                // Bu genellikle vardiya sonu (Bitiş) endeksidir.
                // Başlangıç endeksini bulmak için Bitiş - Satılan Litre formülünü kullanabiliriz.

                var nozzleSales = new Dictionary<string, decimal>();
                foreach (var s in dto.OtomasyonSatislar)
                {
                    string key = $"{s.PompaNo}-{s.TabancaNo}";
                    if (!nozzleSales.ContainsKey(key)) nozzleSales[key] = 0;
                    nozzleSales[key] += s.Litre;
                }
                foreach (var s in dto.FiloSatislar)
                {
                    string key = $"{s.PompaNo}-{s.TabancaNo}";
                    if (!nozzleSales.ContainsKey(key)) nozzleSales[key] = 0;
                    nozzleSales[key] += s.Litre;
                }

                var pumpElements = xdoc.Descendants().Where(x => x.Name.LocalName == "Pump").ToList();
                foreach (var pumpElement in pumpElements)
                {
                    string pumpNameStr = pumpElement.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpName")?.Value ?? "0";
                    int.TryParse(pumpNameStr, out int pumpNr);

                    var nozzles = pumpElement.Descendants().Where(x => x.Name.LocalName == "Nozzle").ToList();
                    foreach (var nozzle in nozzles)
                    {
                        int.TryParse(nozzle.Elements().FirstOrDefault(x => x.Name.LocalName == "NozzleNr")?.Value, out int nozzleNr);
                        string fuelTypeStr = nozzle.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value ?? "";
                        decimal.TryParse(nozzle.Elements().FirstOrDefault(x => x.Name.LocalName == "Totalizer")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal bitisEndeks);

                        // XML'deki Totalizer genellikle 2 decimal basamaklı tam sayı olarak gelir (örn: 200093.76 -> 20009376)
                        // Eğer değer çok büyükse 100'e bölmeyi deneyebiliriz. 
                        // Ancak kullanıcıdan gelen örnekte 20009376 var. Bu 200.093,76 Lt olabilir.
                        if (bitisEndeks > 1000000) bitisEndeks /= 100;

                        string key = $"{pumpNr}-{nozzleNr}";
                        decimal satilanLitre = nozzleSales.ContainsKey(key) ? nozzleSales[key] : 0;
                        decimal baslangicEndeks = bitisEndeks - satilanLitre;

                        dto.PompaEndeksleri.Add(new CreateVardiyaPompaEndeksDto
                        {
                            PompaNo = pumpNr,
                            TabancaNo = nozzleNr,
                            YakitTuru = (await _yakitService.IdentifyYakitAsync(fuelTypeStr))?.Ad ?? "DIGER",
                            BaslangicEndeks = baslangicEndeks,
                            BitisEndeks = bitisEndeks
                        });
                    }
                }

                // 8. Tank Envanteri
                var tankElements = xdoc.Descendants().Where(x => x.Name.LocalName == "TankDetails").ToList();
                foreach (var tankElement in tankElements)
                {
                    int.TryParse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "TankNo")?.Value, out int tankNo);
                    string tankAdi = tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelName")?.Value 
                                   ?? tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "TankName")?.Value ?? "UNKNOWN";
                    
                    decimal.TryParse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "PreviousVolume")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal previousVol);
                    decimal.TryParse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "CurrentVolume")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal currentVol);
                    decimal.TryParse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "Delta")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal delta);
                    decimal.TryParse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "DeliveryVolume")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal delivery);

                    var beklenenTuketim = previousVol + delivery - currentVol;
                    var satilanMiktar = delta;
                    var fark = beklenenTuketim - satilanMiktar;

                    result.TankEnvanterleri.Add(new VardiyaTankEnvanteri
                    {
                        TankNo = tankNo,
                        TankAdi = tankAdi,
                        YakitTipi = (await _yakitService.IdentifyYakitAsync(tankAdi))?.Ad ?? "DIGER",
                        BaslangicStok = previousVol,
                        BitisStok = currentVol,
                        SatilanMiktar = satilanMiktar,
                        SevkiyatMiktar = delivery,
                        BeklenenTuketim = beklenenTuketim,
                        FarkMiktar = fark,
                        KayitTarihi = DateTime.UtcNow
                    });

                    dto.TankEnvanterleri.Add(new CreateVardiyaTankEnvanteriDto
                    {
                        TankNo = tankNo,
                        TankAdi = tankAdi,
                        YakitTipi = (await _yakitService.IdentifyYakitAsync(tankAdi))?.Ad ?? "DIGER",
                        BaslangicStok = previousVol,
                        BitisStok = currentVol,
                        SatilanMiktar = delta,
                        SevkiyatMiktar = delivery
                    });
                }

                _logger.LogInformation($"Vardiya Import: {dto.OtomasyonSatislar.Count} Otomasyon, {dto.FiloSatislar.Count} Filo satışı ayrıştırıldı.");

                result.CreateDto = dto;
                result.DosyaHash = hashString;
                result.Istasyon = station;
                result.RawXmlContent = xmlContent;
                result.ZipBytes = zipBytes;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML/ZIP işlenirken hata: {FileName}", fileName);
                throw;
            }
        }


    }
}
