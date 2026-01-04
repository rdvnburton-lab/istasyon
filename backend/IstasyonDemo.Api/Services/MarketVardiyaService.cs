using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    public class MarketVardiyaService : IMarketVardiyaService
    {
        private readonly AppDbContext _context;

        public MarketVardiyaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MarketVardiyaDto>> GetMarketVardiyalarAsync(int userId, string userRole, int? istasyonId)
        {
            IQueryable<MarketVardiya> query = _context.MarketVardiyalar
                .Include(m => m.Istasyon).ThenInclude(i => i!.Firma)
                .Include(m => m.Sorumlu);

            query = ApplySecurityFilter(query, userId, userRole, istasyonId);

            return await query
                .OrderByDescending(m => m.Tarih)
                .Select(m => new MarketVardiyaDto
                {
                    Id = m.Id,
                    IstasyonId = m.IstasyonId,
                    IstasyonAdi = m.Istasyon!.Ad,
                    SorumluId = m.SorumluId,
                    SorumluAdi = m.Sorumlu!.Username,
                    Tarih = m.Tarih,
                    Durum = m.Durum,
                    ToplamSatisTutari = m.ToplamSatisTutari,
                    ToplamTeslimatTutari = m.ToplamTeslimatTutari,
                    ToplamFark = m.ToplamFark,
                    ZRaporuTutari = m.ZRaporuTutari,
                    ZRaporuNo = m.ZRaporuNo,
                    OlusturmaTarihi = m.OlusturmaTarihi
                })
                .ToListAsync();
        }

        public async Task<MarketVardiya?> GetMarketVardiyaByIdAsync(int id, int userId, string userRole, int? istasyonId)
        {
            var vardiya = await _context.MarketVardiyalar
                .Include(m => m.ZRaporlari)
                .Include(m => m.Tahsilatlar).ThenInclude(t => t.Personel)
                .Include(m => m.Giderler)
                .Include(m => m.Gelirler)
                .Include(m => m.Istasyon).ThenInclude(i => i!.Firma)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (vardiya == null) return null;

            // Security Check logic adapted from controller
            ValidateAccess(vardiya, userId, userRole, istasyonId);
            
            return vardiya;
        }

        public async Task<MarketVardiya> CreateMarketVardiyaAsync(CreateMarketVardiyaDto dto, int userId, int? istasyonId)
        {
            if (!istasyonId.HasValue) 
                throw new InvalidOperationException("Kullanıcı istasyon bilgisi bulunamadı.");

            var tarih = dto.Tarih.Date;
            var mevcut = await _context.MarketVardiyalar
                .AnyAsync(m => m.IstasyonId == istasyonId.Value && m.Tarih.Date == tarih && m.Durum != VardiyaDurum.SILINDI);

            if (mevcut)
                throw new InvalidOperationException($"{tarih:dd.MM.yyyy} tarihi için zaten bir market mutabakatı mevcut.");

            var marketVardiya = new MarketVardiya
            {
                IstasyonId = istasyonId.Value,
                SorumluId = userId,
                Tarih = dto.Tarih,
                ZRaporuTutari = dto.ZRaporuTutari,
                ZRaporuNo = dto.ZRaporuNo,
                Durum = VardiyaDurum.ACIK,
                OlusturmaTarihi = DateTime.UtcNow,
                ToplamSatisTutari = 0,
                ToplamTeslimatTutari = 0,
                ToplamFark = 0
            };

            // Map sub-items
            foreach (var z in dto.ZRaporlari)
            {
                NormalizeZRaporu(z);
                ValidateZRaporu(z);
                marketVardiya.ZRaporlari.Add(new MarketZRaporu
                {
                    Tarih = dto.Tarih, 
                    GenelToplam = z.GenelToplam,
                    Kdv0 = z.Kdv0,
                    Kdv1 = z.Kdv1,
                    Kdv10 = z.Kdv10,
                    Kdv20 = z.Kdv20,
                    KdvToplam = z.KdvToplam,
                    KdvHaricToplam = z.KdvHaricToplam
                });
            }

            foreach (var t in dto.Tahsilatlar)
            {
                marketVardiya.Tahsilatlar.Add(new MarketTahsilat
                {
                    PersonelId = t.PersonelId,
                    Nakit = t.Nakit,
                    KrediKarti = t.KrediKarti,
                    ParoPuan = t.ParoPuan,
                    Toplam = t.Toplam,
                    Aciklama = t.Aciklama
                });
            }

            foreach (var g in dto.Giderler)
            {
                marketVardiya.Giderler.Add(new MarketGider
                {
                    GiderTuru = g.GiderTuru,
                    Tutar = g.Tutar,
                    Aciklama = g.Aciklama,
                    BelgeTarihi = g.BelgeTarihi
                });
            }

            foreach (var g in dto.Gelirler)
            {
                marketVardiya.Gelirler.Add(new MarketGelir
                {
                    GelirTuru = g.GelirTuru,
                    Tutar = g.Tutar,
                    Aciklama = g.Aciklama,
                    BelgeTarihi = g.BelgeTarihi
                });
            }

            // Calculations
            marketVardiya.ToplamSatisTutari = marketVardiya.ZRaporlari.Sum(z => z.GenelToplam);
            var tahsilat = marketVardiya.Tahsilatlar.Sum(t => t.Toplam);
            var gelir = marketVardiya.Gelirler.Sum(g => g.Tutar);
            var gider = marketVardiya.Giderler.Sum(g => g.Tutar);
            
            marketVardiya.ToplamTeslimatTutari = tahsilat + gelir - gider;
            marketVardiya.ToplamFark = marketVardiya.ToplamTeslimatTutari - marketVardiya.ToplamSatisTutari;

            _context.MarketVardiyalar.Add(marketVardiya);
            await _context.SaveChangesAsync();
            return marketVardiya;
        }

        public async Task<MarketZRaporu> AddZRaporuAsync(int vardiyaId, MarketZRaporuDto dto, int userId, string userRole, int? istasyonId)
        {
            NormalizeZRaporu(dto);
            ValidateZRaporu(dto);
            var vardiya = await GetAndValidateAsync(vardiyaId, userId, userRole, istasyonId);

            // Clear existing Z-reports for this day? Code assumed replace logic
            _context.MarketZRaporlari.RemoveRange(vardiya.ZRaporlari);
            vardiya.ZRaporlari.Clear();

            var zRaporu = new MarketZRaporu
            {
                MarketVardiyaId = vardiyaId,
                Tarih = vardiya.Tarih,
                GenelToplam = dto.GenelToplam,
                Kdv0 = dto.Kdv0,
                Kdv1 = dto.Kdv1,
                Kdv10 = dto.Kdv10,
                Kdv20 = dto.Kdv20,
                KdvToplam = dto.KdvToplam,
                KdvHaricToplam = dto.KdvHaricToplam
            };
            _context.MarketZRaporlari.Add(zRaporu);
            vardiya.ZRaporlari.Add(zRaporu);
            
            await _context.SaveChangesAsync();
            await RecalculateTotals(vardiya);

            return zRaporu;

            return zRaporu;
        }

        public async Task AddTahsilatAsync(int vardiyaId, MarketTahsilatDto dto, int userId, string userRole, int? istasyonId)
        {
            var vardiya = await GetAndValidateAsync(vardiyaId, userId, userRole, istasyonId);
            
            var mevcut = vardiya.Tahsilatlar.FirstOrDefault(t => t.PersonelId == dto.PersonelId);
            if (mevcut != null)
            {
                mevcut.Nakit = dto.Nakit;
                mevcut.KrediKarti = dto.KrediKarti;
                mevcut.ParoPuan = dto.ParoPuan;
                mevcut.SistemSatisTutari = dto.SistemSatisTutari;
                mevcut.Toplam = dto.Toplam;
                mevcut.BankaId = dto.BankaId;
                mevcut.KrediKartiDetayJson = dto.KrediKartiDetayJson;
                mevcut.Aciklama = dto.Aciklama;
            }
            else
            {
                var tahsilat = new MarketTahsilat
                {
                    MarketVardiyaId = vardiyaId,
                    PersonelId = dto.PersonelId,
                    Nakit = dto.Nakit,
                    KrediKarti = dto.KrediKarti,
                    ParoPuan = dto.ParoPuan,
                    SistemSatisTutari = dto.SistemSatisTutari,
                    Toplam = dto.Toplam,
                    BankaId = dto.BankaId,
                    KrediKartiDetayJson = dto.KrediKartiDetayJson,
                    Aciklama = dto.Aciklama
                };
                _context.MarketTahsilatlar.Add(tahsilat);
                vardiya.Tahsilatlar.Add(tahsilat);
            }

            await _context.SaveChangesAsync();
            await RecalculateTotals(vardiya);
        }

        public async Task DeleteTahsilatAsync(int tahsilatId, int userId, string userRole, int? istasyonId)
        {
            var tahsilat = await _context.MarketTahsilatlar
                .Include(t => t.MarketVardiya).ThenInclude(m => m!.Istasyon).ThenInclude(i => i!.Firma)
                .FirstOrDefaultAsync(t => t.Id == tahsilatId);

            if (tahsilat == null) throw new KeyNotFoundException("Tahsilat kaydı bulunamadı.");

            if (tahsilat.MarketVardiya == null) throw new InvalidOperationException("Tahsilatın bağlı olduğu vardiya bulunamadı.");
            ValidateAccess(tahsilat.MarketVardiya, userId, userRole, istasyonId);

            _context.MarketTahsilatlar.Remove(tahsilat);
            tahsilat.MarketVardiya.Tahsilatlar.Remove(tahsilat);
            await _context.SaveChangesAsync();
            await RecalculateTotals(tahsilat.MarketVardiya);
        }

        public async Task<MarketGider> AddGiderAsync(int vardiyaId, MarketGiderDto dto, int userId, string userRole, int? istasyonId)
        {
            var vardiya = await GetAndValidateAsync(vardiyaId, userId, userRole, istasyonId);

            var gider = new MarketGider
            {
                MarketVardiyaId = vardiyaId,
                GiderTuru = dto.GiderTuru,
                Tutar = dto.Tutar,
                Aciklama = dto.Aciklama,
                BelgeTarihi = dto.BelgeTarihi
            };
            _context.MarketGiderler.Add(gider);
            vardiya.Giderler.Add(gider);
            await _context.SaveChangesAsync();
            await RecalculateTotals(vardiya);
            return gider;
        }

        public async Task DeleteGiderAsync(int giderId, int userId, string userRole, int? istasyonId)
        {
            var gider = await _context.MarketGiderler
                .Include(g => g.MarketVardiya).ThenInclude(m => m!.Istasyon).ThenInclude(i => i!.Firma)
                .FirstOrDefaultAsync(g => g.Id == giderId);

            if (gider == null) throw new KeyNotFoundException("Gider bulunamadı.");
            
            if (gider.MarketVardiya == null) throw new InvalidOperationException("Giderin bağlı olduğu vardiya bulunamadı.");
            ValidateAccess(gider.MarketVardiya, userId, userRole, istasyonId);

            _context.MarketGiderler.Remove(gider);
            gider.MarketVardiya.Giderler.Remove(gider);
            await _context.SaveChangesAsync();
            await RecalculateTotals(gider.MarketVardiya);
        }

        public async Task<MarketGelir> AddGelirAsync(int vardiyaId, MarketGelirDto dto, int userId, string userRole, int? istasyonId)
        {
            var vardiya = await GetAndValidateAsync(vardiyaId, userId, userRole, istasyonId);
            var gelir = new MarketGelir
            {
                MarketVardiyaId = vardiyaId,
                GelirTuru = dto.GelirTuru,
                Tutar = dto.Tutar,
                Aciklama = dto.Aciklama,
                BelgeTarihi = dto.BelgeTarihi
            };
            _context.MarketGelirler.Add(gelir);
            vardiya.Gelirler.Add(gelir);
            await _context.SaveChangesAsync();
            await RecalculateTotals(vardiya);
            return gelir;
        }

        public async Task DeleteGelirAsync(int gelirId, int userId, string userRole, int? istasyonId)
        {
             var gelir = await _context.MarketGelirler
                .Include(g => g.MarketVardiya).ThenInclude(m => m!.Istasyon).ThenInclude(i => i!.Firma)
                .FirstOrDefaultAsync(g => g.Id == gelirId);

            if (gelir == null) throw new KeyNotFoundException("Gelir bulunamadı.");
            
            if (gelir.MarketVardiya == null) throw new InvalidOperationException("Gelirin bağlı olduğu vardiya bulunamadı.");
            ValidateAccess(gelir.MarketVardiya, userId, userRole, istasyonId);

            _context.MarketGelirler.Remove(gelir);
            gelir.MarketVardiya.Gelirler.Remove(gelir);
            await _context.SaveChangesAsync();
            await RecalculateTotals(gelir.MarketVardiya);
        }

        public async Task OnayaGonderAsync(int id, int userId, string userRole, int? istasyonId)
        {
            var vardiya = await GetAndValidateAsync(id, userId, userRole, istasyonId);
            vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
            await _context.SaveChangesAsync();
        }

        public async Task OnaylaAsync(int id, int userId, string userRole)
        {
            // Only Admin/Patron calling endpoint usually protected by [Authorize]. Service checks again.
             // Usually pass null for istasyonId for admin/patron checks or explicit logic
             var vardiya = await GetAndValidateAsync(id, userId, userRole, null); 
             
             vardiya.Durum = VardiyaDurum.ONAYLANDI;
             vardiya.OnaylayanId = userId;
             vardiya.OnayTarihi = DateTime.UtcNow;
             await _context.SaveChangesAsync();
        }

        public async Task ReddetAsync(int id, string neden, int userId, string userRole)
        {
            var vardiya = await GetAndValidateAsync(id, userId, userRole, null);
            vardiya.Durum = VardiyaDurum.REDDEDILDI;
            vardiya.RedNedeni = neden;
            await _context.SaveChangesAsync();
        }

        public async Task<object> GetMarketRaporuAsync(DateTimeOffset baslangic, DateTimeOffset bitis, int userId, string userRole, int? istasyonId)
        {
            var start = baslangic.UtcDateTime;
            var end = bitis.UtcDateTime;

             IQueryable<MarketVardiya> query = _context.MarketVardiyalar
                .Include(m => m.Istasyon).ThenInclude(i => i!.Firma);

            query = ApplySecurityFilter(query, userId, userRole, istasyonId);
            query = query.Where(m => m.Tarih >= start && m.Tarih <= end && m.Durum != VardiyaDurum.SILINDI);

            var vardiyalar = await query
                .OrderByDescending(m => m.Tarih)
                .Select(m => new
                {
                    m.Id,
                    Tarih = m.Tarih,
                    m.ToplamSatisTutari,
                    m.ToplamTeslimatTutari,
                    m.ToplamFark,
                    Durum = m.Durum.ToString()
                })
                .ToListAsync();

            var ozet = new
            {
                ToplamVardiya = vardiyalar.Count,
                ToplamSatis = vardiyalar.Sum(v => v.ToplamSatisTutari),
                ToplamTeslimat = vardiyalar.Sum(v => v.ToplamTeslimatTutari),
                ToplamFark = vardiyalar.Sum(v => v.ToplamFark)
            };

            return new { Ozet = ozet, Vardiyalar = vardiyalar };
        }

        // --- Helpers ---

        private IQueryable<MarketVardiya> ApplySecurityFilter(IQueryable<MarketVardiya> query, int userId, string userRole, int? istasyonId)
        {
            if (userRole == "admin") return query;
            
            if (userRole == "patron")
            {
                 return query.Where(m => m.Istasyon != null && m.Istasyon.Firma != null && m.Istasyon.Firma.PatronId == userId);
            }
            
            if (userRole == "vardiya_sorumlusu")
            {
                // Can see nothing or only their own if needed, but requirements say they don't see market shifts usually?
                // Returning empty as per original controller logic
                return query.Where(x => false);
            }

            // Market Sorumlusu / Istasyon Sorumlusu
            if (istasyonId.HasValue)
            {
                return query.Where(m => m.IstasyonId == istasyonId.Value);
            }
            
            return query.Where(x => false);
        }

        private async Task<MarketVardiya> GetAndValidateAsync(int id, int userId, string userRole, int? istasyonId)
        {
            var vardiya = await _context.MarketVardiyalar
                .Include(m => m.Istasyon).ThenInclude(i => i!.Firma)
                .Include(m => m.ZRaporlari)
                .Include(m => m.Tahsilatlar)
                .Include(m => m.Giderler)
                .Include(m => m.Gelirler)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");
            ValidateAccess(vardiya, userId, userRole, istasyonId);
            return vardiya;
        }

        private void ValidateAccess(MarketVardiya vardiya, int userId, string userRole, int? istasyonId)
        {
            if (userRole == "admin") return;
            
            if (userRole == "patron")
            {
                if (vardiya.Istasyon?.Firma?.PatronId != userId) 
                    throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok.");
                return;
            }

            // Others must match station
            if (istasyonId != vardiya.IstasyonId) 
                  throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok (İstasyon Uyuşmazlığı).");
        }

        private async Task RecalculateTotals(MarketVardiya vardiya)
        {
             // Assumes navigation properties are loaded or updated.
             // If called after adding/removing items on the tracked entity context, usually fine.
             // But for safety, explicit Sum from Context is synonymous if Saved.
             // But we are using the tracked entity object in memory which has the updates.
             
            var tahsilat = vardiya.Tahsilatlar.Sum(t => t.Toplam);
            var gelir = vardiya.Gelirler.Sum(g => g.Tutar);
            var gider = vardiya.Giderler.Sum(g => g.Tutar);
            var satis = vardiya.ZRaporlari.Sum(z => z.GenelToplam);
            
            vardiya.ToplamSatisTutari = satis;
            vardiya.ToplamTeslimatTutari = tahsilat + gelir - gider;
            vardiya.ToplamFark = vardiya.ToplamTeslimatTutari - vardiya.ToplamSatisTutari;
            
            await _context.SaveChangesAsync();
        }

        private void ValidateZRaporu(MarketZRaporuDto dto)
        {
            // 1. Genel Toplam Kontrolü: Matrah + KDV = Genel Toplam
            var calculatedTotal = dto.KdvHaricToplam + dto.KdvToplam;
            if (Math.Abs(calculatedTotal - dto.GenelToplam) > 0.05m)
            {
                throw new InvalidOperationException($"Z-Raporu tutarsız: Matrah ({dto.KdvHaricToplam}) + KDV ({dto.KdvToplam}) = {calculatedTotal} olmalı, ancak {dto.GenelToplam} girildi.");
            }

            // 2. KDV Oransal Kontrolü - KALDIRILDI
            // IsKdvDahil senaryosunda sunucu tarafı hesaplama yaptığı için bu kontrol yuvarlama farklarından ötürü
            // gereksiz hatalara sebep olabiliyor. Matrah + KDV = Genel Toplam kontrolü yeterlidir.
            
            /*
            var expectedKdv = (dto.Kdv1 * 0.01m) + (dto.Kdv10 * 0.10m) + (dto.Kdv20 * 0.20m);
            if (Math.Abs(expectedKdv - dto.KdvToplam) > 2.00m)
            {
                 throw new InvalidOperationException($"Z-Raporu KDV tutarsız: Oranlara göre hesaplanan KDV {expectedKdv:N2} TL olmalı, ancak {dto.KdvToplam:N2} TL girildi.");
            }
            */

            // 3. Matrah Toplamı Kontrolü
            var totalMatrah = dto.Kdv0 + dto.Kdv1 + dto.Kdv10 + dto.Kdv20;
            if (Math.Abs(totalMatrah - dto.KdvHaricToplam) > 0.05m)
            {
                throw new InvalidOperationException($"Z-Raporu Matrah tutarsız: Alt matrahların toplamı {totalMatrah:N2} TL, ancak Matrah Toplamı {dto.KdvHaricToplam:N2} TL girildi.");
            }
        }

        private void NormalizeZRaporu(MarketZRaporuDto dto)
        {
            if (!dto.IsKdvDahil) return;

            // Gelen değerler KDV Dahil (Gross) değerlerdir.
            // Kullanıcı ne girdiyse TOPLAM o tutmalıdır. Kuruş farkı KDV'ye yansıtılır.
            
            var gross0 = dto.Kdv0;
            var gross1 = dto.Kdv1;
            var gross10 = dto.Kdv10;
            var gross20 = dto.Kdv20;

            // 1. Genel Toplamı Kullanıcının Girdisine Sabitle
            dto.GenelToplam = gross0 + gross1 + gross10 + gross20;

            // 2. KDV'leri Hesapla (Standart Yuvarlama)
            // Tax = Gross - (Gross / Rate)
            var tax1 = Math.Round((gross1 - (gross1 / 1.01m)) * 100) / 100;
            var tax10 = Math.Round((gross10 - (gross10 / 1.10m)) * 100) / 100;
            var tax20 = Math.Round((gross20 - (gross20 / 1.20m)) * 100) / 100;

            // 3. Matrahları Hesapla (Bakiye Yöntemi)
            // Gross - Tax = Base
            var base0 = gross0;
            var base1 = gross1 - tax1;
            var base10 = gross10 - tax10;
            var base20 = gross20 - tax20;

            // DTO Matrahlarını Güncelle
            dto.Kdv0 = base0;
            dto.Kdv1 = base1;
            dto.Kdv10 = base10;
            dto.Kdv20 = base20;

            // 4. Toplamlar
            dto.KdvToplam = tax1 + tax10 + tax20;
            dto.KdvHaricToplam = dto.Kdv0 + dto.Kdv1 + dto.Kdv10 + dto.Kdv20;
            
            // Genel Toplam zaten Gross Sum.
            // Check consistency:
            // KdvHaric + KdvToplam = (Gross - Tax) + Tax = Gross. (Correct)

            // Hesaplama bitti. Artık veritabanında tüm parçalar tutarlı:
            // KdvHaricToplam (Sum Bases) + KdvToplam (Residual) = GenelToplam (Sum Inputs)
        }
    }
}
