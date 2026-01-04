using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IstasyonDemo.Api.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateMutabakatExcel(int vardiyaId);
        Task<byte[]> GenerateMutabakatPdf(int vardiyaId);
    }

    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly IVardiyaService _vardiyaService;

        public ReportService(AppDbContext context, IVardiyaService vardiyaService)
        {
            _context = context;
            _vardiyaService = vardiyaService;
            
            // QuestPDF License Configuration (Community)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateMutabakatExcel(int vardiyaId)
        {
            var data = await _vardiyaService.CalculateVardiyaFinancials(vardiyaId);
            var tankEnvanter = await _context.VardiyaTankEnvanterleri.Where(t => t.VardiyaId == vardiyaId).OrderBy(t => t.TankNo).ToListAsync();
            
            using var workbook = new XLWorkbook();
            
            // 1. ÖZET SAYFASI
            var wsSummary = workbook.Worksheets.Add("Özet");
            wsSummary.Cell("A1").Value = "Vardiya Hesap Özeti";
            wsSummary.Cell("A1").Style.Font.Bold = true;
            wsSummary.Cell("A1").Style.Font.FontSize = 16;

            wsSummary.Cell("A3").Value = "Toplam Ciro (Otomasyon):";
            wsSummary.Cell("B3").Value = data.GenelOzet.ToplamOtomasyon;
            wsSummary.Cell("A4").Value = "Toplam Nakit:";
            wsSummary.Cell("B4").Value = data.GenelOzet.ToplamNakit;
            wsSummary.Cell("A5").Value = "Toplam Kredi Kartı:";
            wsSummary.Cell("B5").Value = data.GenelOzet.ToplamKrediKarti;
            wsSummary.Cell("A6").Value = "Toplam Gider:";
            wsSummary.Cell("B6").Value = data.GenelOzet.ToplamGider;
            wsSummary.Cell("A7").Value = "TOPLAM PUSULA:";
            wsSummary.Cell("B7").Value = data.GenelOzet.ToplamPusula;
            
            wsSummary.Cell("A9").Value = "FARK:";
            wsSummary.Cell("B9").Value = data.GenelOzet.Fark;
            if (data.GenelOzet.Fark < 0) wsSummary.Cell("B9").Style.Font.FontColor = XLColor.Red;
            else wsSummary.Cell("B9").Style.Font.FontColor = XLColor.Green;
            
            // Format Currency
            wsSummary.Range("B3:B9").Style.NumberFormat.Format = "#,##0.00 ₺";

            // 2. PERSONEL DETAY
            var wsPersonel = workbook.Worksheets.Add("Personel Raporu");
            wsPersonel.Cell("A1").Value = "Personel";
            wsPersonel.Cell("B1").Value = "Otomasyon Tutar";
            wsPersonel.Cell("C1").Value = "Pusula Toplam";
            wsPersonel.Cell("D1").Value = "Fark (Açık/Fazla)";

            int row = 2;
            foreach (var p in data.PersonelOzetler)
            {
                var pusula = data.Pusulalar.FirstOrDefault(x => x.PersonelId == p.PersonelId) 
                             ?? data.Pusulalar.FirstOrDefault(x => x.PersonelAdi == p.PersonelAdi);
                
                decimal pusulaTutar = pusula != null ? pusula.Toplam : 0;
                decimal fark = pusulaTutar - p.ToplamTutar;

                wsPersonel.Cell(row, 1).Value = p.GercekPersonelAdi ?? p.PersonelAdi;
                wsPersonel.Cell(row, 2).Value = p.ToplamTutar;
                wsPersonel.Cell(row, 3).Value = pusulaTutar;
                wsPersonel.Cell(row, 4).Value = fark;
                row++;
            }
            wsPersonel.Range($"B2:B{row}").Style.NumberFormat.Format = "#,##0";
            wsPersonel.Range($"C2:F{row}").Style.NumberFormat.Format = "#,##0.00";
            wsPersonel.Columns().AdjustToContents();

            // 3. FİLO & DİĞER
            var wsDetails = workbook.Worksheets.Add("Detaylar");
            var allCC = data.Pusulalar.SelectMany(x => x.KrediKartiDetayList ?? new List<PusulaKrediKartiDetayDto>()).GroupBy(x => x.BankaAdi).Select(g => new { Banka = g.Key, Tutar = g.Sum(x => x.Tutar) }).ToList();

            int dRow = 1;

            // --- KREDI KARTI BANKA DÖKÜMÜ ---
            wsDetails.Cell(dRow, 1).Value = "KREDİ KARTI BANKA DÖKÜMÜ";
            wsDetails.Cell(dRow, 1).Style.Font.Bold = true;
            wsDetails.Cell(dRow, 1).Style.Font.FontColor = XLColor.Purple;
            dRow++;

            wsDetails.Cell(dRow, 1).Value = "Banka Adı";
            wsDetails.Cell(dRow, 2).Value = "Tutar";
            wsDetails.Range($"A{dRow}:B{dRow}").Style.Font.Bold = true;
            dRow++;

            if (allCC.Any()) {
                foreach(var cc in allCC) {
                    wsDetails.Cell(dRow, 1).Value = cc.Banka;
                    wsDetails.Cell(dRow, 2).Value = cc.Tutar;
                    dRow++;
                }
            } else { wsDetails.Cell(dRow, 1).Value = "Kayıt yok."; dRow++; }
            dRow++;

            // --- FİLO SATIŞLARI ---
            wsDetails.Cell(dRow, 1).Value = "FİLO VE TAŞIT TANIMA SATIŞLARI";
            wsDetails.Cell(dRow, 1).Style.Font.Bold = true;
            wsDetails.Cell(dRow, 1).Style.Font.FontColor = XLColor.Blue;
            dRow++;
            
            wsDetails.Cell(dRow, 1).Value = "Filo Şirketi";
            wsDetails.Cell(dRow, 2).Value = "Tutar";
            wsDetails.Range($"A{dRow}:B{dRow}").Style.Font.Bold = true;
            dRow++;

            if (data.FiloDetaylari != null && data.FiloDetaylari.Any())
            {
                foreach(var f in data.FiloDetaylari)
                {
                    wsDetails.Cell(dRow, 1).Value = f.FiloAdi;
                    wsDetails.Cell(dRow, 2).Value = f.Tutar;
                    dRow++;
                }
            }
            else
            {
                wsDetails.Cell(dRow, 1).Value = "Kayıt yok.";
                dRow++;
            }
            dRow++; // Spacer

            // --- VERESİYE (CARİ) ---
            wsDetails.Cell(dRow, 1).Value = "VERESİYE (CARİ) LİSTESİ";
            wsDetails.Cell(dRow, 1).Style.Font.Bold = true;
            wsDetails.Cell(dRow, 1).Style.Font.FontColor = XLColor.Red;
            dRow++;

            wsDetails.Cell(dRow, 1).Value = "Cari Adı / Plaka";
            wsDetails.Cell(dRow, 2).Value = "Tutar";
             wsDetails.Range($"A{dRow}:B{dRow}").Style.Font.Bold = true;
            dRow++;

            var veresiyeler = data.Pusulalar.SelectMany(p => p.Veresiyeler).ToList();
            if(veresiyeler.Any())
            {
                foreach(var v in veresiyeler)
                {
                    var label = !string.IsNullOrEmpty(v.CariAd) ? $"{v.CariAd} ({v.Plaka})" : v.Plaka;
                    wsDetails.Cell(dRow, 1).Value = label;
                    wsDetails.Cell(dRow, 2).Value = v.Tutar;
                    dRow++;
                }
            }
            else
            {
                wsDetails.Cell(dRow, 1).Value = "Kayıt yok.";
                dRow++;
            }
             dRow++; // Spacer

            // --- DİĞER ÖDEMELER ---
            wsDetails.Cell(dRow, 1).Value = "DİĞER TAHSİLATLAR & GİDERLER";
            wsDetails.Cell(dRow, 1).Style.Font.Bold = true;
             wsDetails.Cell(dRow, 1).Style.Font.FontColor = XLColor.Orange;
            dRow++;
             wsDetails.Cell(dRow, 1).Value = "Açıklama";
            wsDetails.Cell(dRow, 2).Value = "Tutar";
             wsDetails.Range($"A{dRow}:B{dRow}").Style.Font.Bold = true;
            dRow++;

            var diger = data.Pusulalar.SelectMany(p => p.DigerOdemeler)
                .GroupBy(d => d.TurAdi)
                .Select(g => new { TurAdi = g.Key, Tutar = g.Sum(x => x.Tutar) })
                .ToList();

             if(diger.Any())
            {
                foreach(var d in diger)
                {
                    wsDetails.Cell(dRow, 1).Value = d.TurAdi;
                    wsDetails.Cell(dRow, 2).Value = d.Tutar;
                    dRow++;
                }
            }
             else
            {
                wsDetails.Cell(dRow, 1).Value = "Kayıt yok.";
                dRow++;
            }

            wsDetails.Range($"B5:B{dRow}").Style.NumberFormat.Format = "#,##0.00";
            wsDetails.Columns().AdjustToContents();

            // 4. TANK RAPORU
            var wsTank = workbook.Worksheets.Add("Tank Raporu");
            wsTank.Cell("A1").Value = "Tank No";
            wsTank.Cell("B1").Value = "Yakıt Tipi";
            wsTank.Cell("C1").Value = "Başlangıç Stok";
            wsTank.Cell("D1").Value = "Dolum";
            wsTank.Cell("E1").Value = "Satış";
            wsTank.Cell("F1").Value = "Bitiş Stok";
            wsTank.Range("A1:F1").Style.Font.Bold = true;
            
            int tRow = 2;
            foreach(var t in tankEnvanter)
            {
                wsTank.Cell(tRow, 1).Value = t.TankNo;
                wsTank.Cell(tRow, 2).Value = t.YakitTipi;
                wsTank.Cell(tRow, 3).Value = t.BaslangicStok;
                wsTank.Cell(tRow, 4).Value = t.SevkiyatMiktar;
                wsTank.Cell(tRow, 5).Value = t.SatilanMiktar;
                wsTank.Cell(tRow, 6).Value = t.BitisStok;
                tRow++;
            }
            wsTank.Range($"C2:F{tRow}").Style.NumberFormat.Format = "#,##0";
            wsTank.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateMutabakatPdf(int vardiyaId)
        {
            var data = await _vardiyaService.CalculateVardiyaFinancials(vardiyaId);
            
            // --- DATA PREPARATION ---
            var tankEnvanter = await _context.VardiyaTankEnvanterleri.Where(t => t.VardiyaId == vardiyaId).ToListAsync();
            var yakitOzetleri = tankEnvanter.GroupBy(t => t.YakitTipi)
                .Select(g => new { 
                    YakitAdi = g.Key, 
                    Baslangic = g.Sum(x => x.BaslangicStok), 
                    Satis = g.Sum(x => x.SatilanMiktar), 
                    Dolum = g.Sum(x => x.SevkiyatMiktar), 
                    Kalan = g.Sum(x => x.BitisStok) 
                }).ToList();

            // --- FUEL SALES DATA ---
            // 1. Fetch Automation Sales
            var otomasyonSatislari = await _context.OtomasyonSatislar
                .Where(s => s.VardiyaId == vardiyaId)
                .Select(s => new {
                     Tur = s.YakitTuru, // Raw Type
                     Tutar = s.ToplamTutar,
                     Litre = s.Litre,
                     Adet = 1 // Count as 1 transaction per record? Or is it aggregated? Usually per nozzle.
                }).ToListAsync();

             // 2. Fetch Fleet Sales (Separate Table)
            var filoSatislar = await _context.FiloSatislar
                .Where(f => f.VardiyaId == vardiyaId && f.FiloAdi != "İSTASYON")
                .Select(f => new {
                    Tur = f.YakitTuru, // Use Product Name from Fleet Sales
                    Tutar = f.Tutar,
                    Litre = f.Litre,
                    Adet = 1
                }).ToListAsync();

            // 3. Merge and Correct Fuel Types
            // Correction Logic: Benzin -> LPG, LPG -> Benzin, Others -> Motorin
            var combinedSales = otomasyonSatislari.Concat(filoSatislar).Where(x => x.Litre > 0 || x.Tutar > 0).ToList();
            
            var yakitSatislari = combinedSales
                .GroupBy(x => {
                    var raw = x.Tur?.ToUpperInvariant() ?? "";
                    if (raw.Contains("BENZIN")) return "LPG"; // User confirmed this is correct
                    if (raw.Contains("LPG") || raw.Contains("OTOGAZ")) return "MOTORİN"; // Swapped based on user request
                    return "BENZIN"; // Default swapped to Benzin
                })
                .Select(g => new { 
                    YakitTuru = g.Key, 
                    ToplamTutar = g.Sum(s => s.Tutar),
                    ToplamLitre = g.Sum(s => s.Litre),
                    IslemAdedi = g.Count() // This is record count, might not be exact transaction count if data is aggregated, but best we have.
                })
                .OrderBy(x => x.YakitTuru)
                .ToList();

            var personelListesi = data.PersonelOzetler.Select(o => {
                var pusula = data.Pusulalar.FirstOrDefault(p => p.PersonelId == o.PersonelId) ?? data.Pusulalar.FirstOrDefault(p => p.PersonelAdi == o.PersonelAdi);
                decimal pusulaTutar = pusula != null ? pusula.Toplam : 0;
                decimal fark = pusulaTutar - o.ToplamTutar;
                return new { 
                    Kod = o.PersonelKeyId ?? o.PersonelId.ToString(), 
                    Ad = o.GercekPersonelAdi ?? o.PersonelAdi, 
                    Islem = o.IslemSayisi,
                    Litre = o.ToplamLitre,
                    SatisTutar = o.ToplamTutar,
                    BeyanTutar = pusulaTutar,
                    AcikFazla = fark, 
                    Durum = Math.Abs(fark) < 1 ? "TAM" : (fark < 0 ? "AÇIK" : "FAZLA") 
                };
            }).ToList();

            var krediKartiDokumu = new List<CreditCardJsonModel>();
            foreach (var p in data.Pusulalar)
            {
                if (p.KrediKartiDetayList != null && p.KrediKartiDetayList.Any())
                    krediKartiDokumu.AddRange(p.KrediKartiDetayList.Select(k => new CreditCardJsonModel { Banka = k.BankaAdi, Tutar = k.Tutar }));
            }
            var groupedKK = krediKartiDokumu.GroupBy(k => k.Banka).Select(g => new { Banka = g.Key, Tutar = g.Sum(x => x.Tutar) }).ToList();
            if (!groupedKK.Any() && data.GenelOzet.ToplamKrediKarti > 0) groupedKK.Add(new { Banka = "GENEL TOPLAM", Tutar = data.GenelOzet.ToplamKrediKarti });

            var digerList = data.Pusulalar.SelectMany(p => p.DigerOdemeler)
                .GroupBy(d => d.TurAdi)
                .Select(g => new { Tur = g.Key, Tutar = g.Sum(x => x.Tutar) });

            var veresiyeList = data.Pusulalar.SelectMany(p => p.Veresiyeler)
                .GroupBy(v => v.CariAd ?? "BİLİNMEYEN CARİ")
                .Select(g => new { Tur = $"Cari / {g.Key}", Tutar = g.Sum(x => x.Tutar) });

            var digerOdemelerDokumu = digerList.Concat(veresiyeList)
                .GroupBy(x => x.Tur)
                .Select(g => new { Tur = g.Key, Tutar = g.Sum(x => x.Tutar) })
                .OrderBy(x => x.Tur)
                .ToList();


            // --- DOCUMENT GENERATION ---
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                });
            });

            void ComposeHeader(IContainer container)
            {
                var stationName = !string.IsNullOrEmpty(data.Vardiya.IstasyonAdi) ? data.Vardiya.IstasyonAdi : "TİGİN PETROL";
                var baslangic = data.Vardiya.BaslangicTarihi;
                var bitis = data.Vardiya.BitisTarihi ?? baslangic.AddHours(8); // Fallback if null
                
                // Correct Timezone Offset (UTC -> TR +3)
                // The data is stored as UTC, but the report needs to display TR time.
                var trBaslangic = baslangic.AddHours(3);
                var trBitis = bitis.AddHours(3);

                // Parse Shift Number from Filename (Format: YYYYMMDDNN)
                int repShift = 1;
                if (!string.IsNullOrEmpty(data.Vardiya.DosyaAdi))
                {
                    try 
                    {
                        var name = System.IO.Path.GetFileNameWithoutExtension(data.Vardiya.DosyaAdi);
                        if (name.Length >= 2)
                        {
                            var shiftPart = name.Substring(name.Length - 2);
                            if (int.TryParse(shiftPart, out int parsedShift)) repShift = parsedShift;
                        }
                    }
                    catch { /* Fallback to 1 */ }
                }

                container.Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Background(Colors.Blue.Darken3).Padding(10).Row(headerRow => 
                        {
                            headerRow.RelativeItem().Column(c => {
                                c.Item().Text(stationName).FontSize(16).Bold().FontColor(Colors.White);
                                // Swap: Date Time here
                                c.Item().Text($"{trBaslangic:dd.MM.yyyy HH:mm} - {trBitis:HH:mm} ({repShift}. Vardiya)").FontSize(10).FontColor(Colors.White);
                            });
                            headerRow.RelativeItem().AlignRight().Column(c => {
                                // Swap: Vardiya ID here
                                c.Item().Text($"Vardiya Özeti: #{vardiyaId}").FontSize(10).FontColor(Colors.Grey.Lighten3);
                                // Status aligned right
                                c.Item().Text(data.GenelOzet.Fark >= 0 ? "MUTABIK" : "FARK VAR").Bold().FontColor(data.GenelOzet.Fark >= 0 ? Colors.Green.Accent3 : Colors.Red.Accent2);
                            });
                        });
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                container.Column(col =>
                {
                    col.Spacing(20);

                    // --- SUMMARY CARDS (Restored to Top) ---
                    col.Item().PaddingBottom(5).Text("Vardiya Raporu").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Row(row =>
                    {
                        row.Spacing(10);
                        // Sales Card
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Column(c => {
                            c.Item().Text("Akaryakıt Satış").FontSize(10).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(data.GenelOzet.ToplamOtomasyon.ToString("N2") + " TL").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        });
                        
                        // Collections Card
                        var totalCollectionSum = data.GenelOzet.ToplamPusula + (data.FiloOzet?.ToplamTutar ?? 0) + data.GenelOzet.ToplamGider;
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Column(c => {
                            c.Item().Text("Toplam Tahsilat").FontSize(10).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(totalCollectionSum.ToString("N2") + " TL").FontSize(14).Bold().FontColor(Colors.Green.Darken2);
                        });

                         // Diff Card
                        var isPositive = data.GenelOzet.Fark >= 0;
                        row.RelativeItem().Border(1).BorderColor(isPositive ? Colors.Green.Lighten2 : Colors.Red.Lighten2)
                           .Background(isPositive ? Colors.Green.Lighten5 : Colors.Red.Lighten5).Padding(10).Column(c => {
                            c.Item().Text("Vardiya Farkı").FontSize(10).FontColor(Colors.Grey.Darken1);
                             c.Item().Text((isPositive ? "+" : "") + data.GenelOzet.Fark.ToString("N2") + " TL").FontSize(14).Bold().FontColor(isPositive ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });


                    // --- FUEL STOCK TABLE ---
                    col.Item().Column(c => 
                    {
                        c.Item().PaddingBottom(5).Text("Akaryakıt Stok Durumu").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        c.Item().Table(table =>
                        {
                            // Removed 2 columns (Price, Amount), Added 1 (Diff) -> 6 Columns Total
                            // Removed Fark -> 5 Columns Total
                            table.ColumnsDefinition(cols => { cols.ConstantColumn(80); cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                            
                            // Header
                            table.Header(h => {
                                h.Cell().Element(CellStyle).Text("Ürün").Bold();
                                h.Cell().Element(CellStyle).AlignRight().Text("Devir");
                                h.Cell().Element(CellStyle).AlignRight().Text("Dolum");
                                h.Cell().Element(CellStyle).AlignRight().Text("Satış");
                                h.Cell().Element(CellStyle).AlignRight().Text("Kalan");
                                
                                IContainer CellStyle(IContainer i) => i.Background(Colors.Grey.Lighten3).Padding(5);
                            });

                            foreach(var item in yakitOzetleri) {
                                table.Cell().Element(Block).Text(item.YakitAdi);
                                table.Cell().Element(Block).AlignRight().Text(item.Baslangic.ToString("N0"));
                                table.Cell().Element(Block).AlignRight().Text(item.Dolum.ToString("N0"));
                                table.Cell().Element(Block).AlignRight().Text(item.Satis.ToString("N0"));
                                table.Cell().Element(Block).AlignRight().Text(item.Kalan.ToString("N0"));
                            }
                             IContainer Block(IContainer i) => i.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);
                        });
                    });

                    // --- PUMP SALES SUMMARY ---
                    col.Item().Column(c => 
                    {
                         c.Item().PaddingBottom(5).Text("Pompa Satış Özeti").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                         c.Item().Table(table => 
                         {
                             table.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                             
                             table.Header(h => {
                                 h.Cell().Element(CellStyle).Text("Ürün Cinsi").Bold();
                                 h.Cell().Element(CellStyle).AlignRight().Text("İşlem Adet");
                                 h.Cell().Element(CellStyle).AlignRight().Text("Satış (Lt)");
                                 h.Cell().Element(CellStyle).AlignRight().Text("Tutar (TL)");
                                 IContainer CellStyle(IContainer i) => i.Background(Colors.Grey.Lighten3).Padding(5);
                             });

                             foreach(var ys in yakitSatislari)
                             {
                                 table.Cell().Element(Block).Text(ys.YakitTuru);
                                 table.Cell().Element(Block).AlignRight().Text(ys.IslemAdedi.ToString("N0"));
                                 table.Cell().Element(Block).AlignRight().Text(ys.ToplamLitre.ToString("N2"));
                                 table.Cell().Element(Block).AlignRight().Text(ys.ToplamTutar.ToString("N2"));
                             }

                             table.Footer(f => {
                                  f.Cell().Element(FStyle).Text("TOPLAM").Bold();
                                  f.Cell().Element(FStyle).AlignRight().Text(yakitSatislari.Sum(x => x.IslemAdedi).ToString("N0")).Bold();
                                  f.Cell().Element(FStyle).AlignRight().Text(yakitSatislari.Sum(x => x.ToplamLitre).ToString("N2")).Bold();
                                  f.Cell().Element(FStyle).AlignRight().Text(yakitSatislari.Sum(x => x.ToplamTutar).ToString("N2")).Bold();
                             });
                             IContainer Block(IContainer i) => i.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);
                             IContainer FStyle(IContainer i) => i.Background(Colors.Grey.Lighten4).Padding(5);
                         });
                    });

                    // Summary Cards moved back to top
                    
                    // --- FINANCIAL RECONCILIATION & POS GRID ---
                    col.Item().Row(row => 
                    {
                        row.Spacing(20);
                        
                        // LEFT COLUMN: Sales Removed -> Collections Only
                        row.RelativeItem().Column(leftCol => 
                        {
                             // Collections (Tahsilatlar) - Now Primary in Left Column
                             leftCol.Item().Column(collections => {
                                 collections.Item().PaddingBottom(5).Text("Tahsilatlar").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                                 collections.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).Column(cc => {
                                     
                                     // Credit Card
                                     AddRow(cc, "Kredi Kartı", data.GenelOzet.ToplamKrediKarti);
                                     
                                     // Fleet Sales
                                     decimal totalFleet = data.FiloOzet?.ToplamTutar ?? 0;
                                     AddRow(cc, "Filo Satışları", totalFleet, true); 
                                     if (data.FiloDetaylari != null)
                                     {
                                         foreach(var f in data.FiloDetaylari)
                                         {
                                             AddDetailRow(cc, f.FiloAdi, f.Tutar);
                                         }
                                     }

                                     // Other Transactions
                                     foreach(var item in digerOdemelerDokumu)
                                     {
                                         AddRow(cc, item.Tur, item.Tutar);
                                     }

                                     // Cash
                                     AddRow(cc, "Nakit", data.GenelOzet.ToplamNakit);

                                     // Expenses
                                     AddRow(cc, "Giderler", data.GenelOzet.ToplamGider);

                                     cc.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                                     
                                     decimal totalCollection = data.GenelOzet.ToplamPusula + (data.FiloOzet?.ToplamTutar ?? 0) + data.GenelOzet.ToplamGider;
                                     AddSummaryRow(cc, "TOPLAM", totalCollection);
                                 });
                             });
                        });

                        // RIGHT COLUMN: Bank / POS Breakdown
                        row.RelativeItem().Column(rightCol => {
                             rightCol.Item().PaddingBottom(5).Text("Banka / POS Dökümü").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                             rightCol.Item().Table(table => {
                                 table.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(2); });
                                 table.Header(h => { h.Cell().Element(HStyle).Text("Banka"); h.Cell().Element(HStyle).AlignRight().Text("Tutar"); });
                                 foreach(var kk in groupedKK) {
                                     table.Cell().Element(CStyle).Text(kk.Banka); table.Cell().Element(CStyle).AlignRight().Text(kk.Tutar.ToString("N2"));
                                 }
                                 table.Footer(f => { f.Cell().Element(FStyle).Text("TOPLAM").Bold(); f.Cell().Element(FStyle).AlignRight().Text(groupedKK.Sum(x=>x.Tutar).ToString("N2")).Bold(); });
                             });
                        });
                    });

                    void AddRow(ColumnDescriptor c, string label, decimal val, bool bold = false) {
                        c.Item().PaddingBottom(2).Row(r => { 
                            var txt = r.RelativeItem().Text(label);
                            if(bold) txt.Bold();
                            r.RelativeItem().AlignRight().Text(val.ToString("N2")); 
                        });
                    }
                    void AddDetailRow(ColumnDescriptor c, string label, decimal val) {
                        c.Item().PaddingBottom(1).PaddingLeft(10).Row(r => { 
                            r.RelativeItem().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2).Italic();
                            r.RelativeItem().AlignRight().Text(val.ToString("N2")).FontSize(8).FontColor(Colors.Grey.Darken2); 
                        });
                    }
                    void AddSummaryRow(ColumnDescriptor c, string label, decimal val) {
                        c.Item().PaddingTop(5).Row(r => { r.RelativeItem().Text(label).Bold(); r.RelativeItem().AlignRight().Text(val.ToString("N2")).Bold(); });
                    }


                    // --- STAFF PERFORMANCE ---
                    col.Item().PageBreak();
                    col.Item().PaddingBottom(10).Text("Personel Raporu").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(table => {
                        table.ColumnsDefinition(cd => { cd.ConstantColumn(75); cd.RelativeColumn(1.8f); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        table.Header(h => { 
                            h.Cell().Element(HStyle).Text("Key ID"); 
                            h.Cell().Element(HStyle).Text("Personel Adı"); 
                            h.Cell().Element(HStyle).AlignRight().Text("Araç"); 
                            h.Cell().Element(HStyle).AlignRight().Text("Litre"); 
                            h.Cell().Element(HStyle).AlignRight().Text("Satış Tutarı"); 
                            h.Cell().Element(HStyle).AlignRight().Text("Teslim Edilen"); 
                            h.Cell().Element(HStyle).AlignRight().Text("Fark"); 
                            h.Cell().Element(HStyle).AlignRight().Text("Durum"); 
                        });
                        
                        foreach(var p in personelListesi) {
                            table.Cell().Element(CStyle).Text(p.Kod); 
                            table.Cell().Element(CStyle).Text(p.Ad); 
                            table.Cell().Element(CStyle).AlignRight().Text(p.Islem.ToString("N0"));
                            table.Cell().Element(CStyle).AlignRight().Text(p.Litre.ToString("N0") + " Lt");
                            table.Cell().Element(CStyle).AlignRight().Text(p.SatisTutar.ToString("N2"));
                            table.Cell().Element(CStyle).AlignRight().Text(p.BeyanTutar.ToString("N2"));
                            table.Cell().Element(CStyle).AlignRight().Text(p.AcikFazla.ToString("N2")).FontColor(p.AcikFazla < 0 ? Colors.Red.Medium : (p.AcikFazla > 0 ? Colors.Green.Medium : Colors.Black)); 
                            table.Cell().Element(CStyle).AlignRight().Text(p.Durum).Bold().FontColor(p.Durum == "AÇIK" ? Colors.Red.Darken1 : (p.Durum == "FAZLA" ? Colors.Green.Darken1 : Colors.Grey.Darken1));
                        }
                    });

                    // --- TANK VISUALS ---
                    if (tankEnvanter.Any())
                    {
                        col.Item().PaddingTop(20).PaddingBottom(5).Text("Tank Doluluk Seviyeleri").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Row(row =>
                        {
                            row.Spacing(10);
                            var maxVol = tankEnvanter.Max(t => t.BitisStok);
                            if (maxVol <= 0) maxVol = 1;
                            // Add 20% headroom
                            var visualMax = (float)maxVol * 1.2f;

                            foreach (var tank in tankEnvanter)
                            {
                                var percent = (float)tank.BitisStok / visualMax;
                                if (percent > 1) percent = 1;
                                
                                var color = Colors.Grey.Medium;
                                var rawType = tank.YakitTipi?.ToUpperInvariant() ?? "";
                                if (rawType.Contains("MOTORIN")) color = Colors.Green.Medium;
                                else if (rawType.Contains("BENZIN")) color = Colors.Red.Medium;
                                else if (rawType.Contains("LPG") || rawType.Contains("OTOGAZ")) color = Colors.Blue.Medium;

                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten3).Column(c =>
                                {
                                    // Header
                                    c.Item().Background(Colors.Grey.Lighten5).Padding(5).AlignCenter().Text($"Tank {tank.TankNo}").FontSize(9).Bold();
                                    
                                    // Visual Tank (Height 60)
                                    c.Item().Height(60).PaddingHorizontal(10).Column(tc => {
                                        // Empty space on top
                                        tc.Item().Height(60 * (1 - percent));
                                        // Liquid on bottom
                                        tc.Item().Height(60 * percent).Background(color);
                                    });
                                    c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                                    
                                    // Details
                                    c.Item().Padding(5).Column(info => {
                                        info.Item().AlignCenter().Text(tank.YakitTipi).FontSize(8).FontColor(Colors.Grey.Darken2);
                                        info.Item().AlignCenter().Text($"{tank.BitisStok:N0} Lt").FontSize(10).Bold();
                                    });
                                });
                            }
                        });
                    }

                    // --- SIGNATURE ---
                    col.Item().PaddingTop(20).Row(row => {
                         row.RelativeItem();
                         row.RelativeItem().AlignRight().Column(c => {
                             c.Item().AlignCenter().Text("Vardiya Sorumlusu").Bold();
                             var sorumlu = !string.IsNullOrEmpty(data.Vardiya.OlusturanKullaniciAdi) ? data.Vardiya.OlusturanKullaniciAdi : "................................................";
                             c.Item().PaddingTop(30).AlignCenter().Text(sorumlu).Bold();
                         });
                    });
                });
            }
            
            IContainer HStyle(IContainer c) => c.Background(Colors.Blue.Lighten5).BorderBottom(1).BorderColor(Colors.Blue.Lighten4).Padding(5);
            IContainer CStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);
            IContainer FStyle(IContainer c) => c.Background(Colors.Grey.Lighten4).Padding(5);

            return document.GeneratePdf();
        }
    }

    public class CreditCardJsonModel
    {
        public string Banka { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
    }
}
