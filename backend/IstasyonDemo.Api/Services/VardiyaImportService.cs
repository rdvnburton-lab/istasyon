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

        public VardiyaImportService(AppDbContext context, ILogger<VardiyaImportService> logger)
        {
            _context = context;
            _logger = logger;
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
                            YakitTuru = MapFuelType(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value ?? ""),
                            Litre = amount, 
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
                            Litre = amount,
                            BirimFiyat = unitPrice / 100,
                            Tarih = satisTarihi,
                            PompaNo = pumpNr,
                            FisNo = receiptNr,
                            YakitTuru = MapFuelType(saleDetails.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value ?? ""),
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
                        YakitTipi = MapFuelType(tankAdi),
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
                        YakitTipi = MapFuelType(tankAdi),
                        BitisStok = currentVol
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

        public string MapFuelType(string fuelType)
        {
            if (string.IsNullOrEmpty(fuelType)) return "DIGER";
            
            fuelType = fuelType.ToUpperInvariant();
            
            // If it's a numeric code from XML FuelType tag
            if (fuelType == "4" || fuelType == "5") return "KURSUNSUZ_95";
            if (fuelType == "6" || fuelType == "7" || fuelType == "8") return "MOTORIN";
            if (fuelType == "9") return "LPG";

            if (fuelType.Contains("DIESEL") || fuelType.Contains("MOTORIN") || fuelType.Contains("V/MAX")) return "MOTORIN";
            if (fuelType.Contains("KURSUNSUZ") || fuelType.Contains("BENZIN")) return "KURSUNSUZ_95";
            if (fuelType.Contains("LPG") || fuelType.Contains("OTOGAZ")) return "LPG";
            
            return "DIGER";
        }

        public string NormalizeYakitTipi(string tankName)
        {
            return MapFuelType(tankName);
        }
    }
}
