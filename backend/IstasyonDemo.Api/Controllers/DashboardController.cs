using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("sorumlu-summary")]
        [Authorize]
        public async Task<ActionResult<SorumluDashboardDto>> GetSorumluSummary()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.Istasyon)
                .ThenInclude(i => i!.Firma)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            var dto = new SorumluDashboardDto
            {
                KullaniciAdi = user.Username,
                AdSoyad = user.AdSoyad ?? user.Username,
                Rol = user.Role != null ? user.Role.Ad : "",
                IstasyonAdi = user.Istasyon?.Ad ?? "-",
                FirmaAdi = user.Istasyon?.Firma?.Ad ?? user.Istasyon?.Ad ?? "-"
            };

            if (user.IstasyonId.HasValue && user.Role != null)
            {
                bool isVardiyaSorumlusu = user.Role.Ad == "vardiya sorumlusu" || user.Role.Ad == "istasyon sorumlusu";
                bool isMarketSorumlusu = user.Role.Ad == "market sorumlusu" || user.Role.Ad == "istasyon sorumlusu";

                if (isVardiyaSorumlusu)
                {
                    // Aktif Pompa Vardiyası
                    var aktifPompaVardiya = await _context.Vardiyalar
                        .Where(v => v.IstasyonId == user.IstasyonId && v.Durum == VardiyaDurum.ACIK)
                        .OrderByDescending(v => v.BaslangicTarihi)
                        .FirstOrDefaultAsync();

                    dto.AktifVardiyaId = aktifPompaVardiya?.Id;

                    // Son Tamamlanan Pompa Vardiyası
                    var sonPompaVardiya = await _context.Vardiyalar
                        .Where(v => v.IstasyonId == user.IstasyonId && v.Durum == VardiyaDurum.ONAYLANDI)
                        .OrderByDescending(v => v.BitisTarihi)
                        .FirstOrDefaultAsync();

                    if (sonPompaVardiya != null)
                    {
                        dto.SonVardiyaTarihi = sonPompaVardiya.BitisTarihi;
                        dto.SonVardiyaTutar = sonPompaVardiya.GenelToplam;
                    }

                    dto.BekleyenOnaySayisi = await _context.Vardiyalar
                        .CountAsync(v => v.IstasyonId == user.IstasyonId && v.Durum == VardiyaDurum.ONAY_BEKLIYOR);
                }

                if (isMarketSorumlusu)
                {
                    // Aktif Market Vardiyası
                    var aktifMarketVardiya = await _context.MarketVardiyalar
                        .Where(m => m.IstasyonId == user.IstasyonId && m.Durum == VardiyaDurum.ACIK)
                        .OrderByDescending(m => m.Tarih)
                        .FirstOrDefaultAsync();

                    dto.AktifMarketVardiyaId = aktifMarketVardiya?.Id;

                    // Son Tamamlanan Market Vardiyası
                    var sonMarketVardiya = await _context.MarketVardiyalar
                        .Where(m => m.IstasyonId == user.IstasyonId && m.Durum == VardiyaDurum.ONAYLANDI)
                        .OrderByDescending(m => m.Tarih)
                        .FirstOrDefaultAsync();

                    if (sonMarketVardiya != null)
                    {
                        dto.SonMarketVardiyaTarihi = sonMarketVardiya.Tarih;
                        dto.SonMarketVardiyaTutar = sonMarketVardiya.ToplamTeslimatTutari;
                    }

                    dto.BekleyenMarketOnaySayisi = await _context.MarketVardiyalar
                        .CountAsync(m => m.IstasyonId == user.IstasyonId && m.Durum == VardiyaDurum.ONAY_BEKLIYOR);
                }
            }

            return Ok(dto);
        }

        [HttpGet("patron-dashboard")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> GetPatronDashboard()
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Kullanıcı ID bulunamadı.");
                }

                var userId = int.Parse(userIdClaim);
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Bugün ve bu ay için tarih aralıkları (PostgreSQL için UTC Kind zorunlu)
                var bugun = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                var ayBaslangic = DateTime.SpecifyKind(new DateTime(bugun.Year, bugun.Month, 1), DateTimeKind.Utc);
                var gecenAyBaslangic = ayBaslangic.AddMonths(-1);

                // Patron için istasyonları filtrele
                IQueryable<Vardiya> vardiyaQuery = _context.Vardiyalar.Include(v => v.Istasyon);
                if (userRole == "patron")
                {
                    vardiyaQuery = vardiyaQuery.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == userId);
                }

                // Silinmiş vardiyaları hariç tut
                vardiyaQuery = vardiyaQuery.Where(v => v.Durum != VardiyaDurum.SILINDI);

                // Verileri çek (Performans için bazılarını önceden çekiyoruz)
                var bugunVardiyalar = await vardiyaQuery
                    .Where(v => v.BaslangicTarihi >= bugun)
                    .ToListAsync();

                var buAyVardiyalar = await vardiyaQuery
                    .Where(v => v.BaslangicTarihi >= ayBaslangic)
                    .ToListAsync();

                var gecenAyVardiyalar = await vardiyaQuery
                    .Where(v => v.BaslangicTarihi >= gecenAyBaslangic && v.BaslangicTarihi < ayBaslangic)
                    .ToListAsync();

                var onayBekleyenler = await vardiyaQuery
                    .Where(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR || v.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR)
                    .ToListAsync();

                // İstasyon ve Personel sayıları
                int istasyonSayisi;
                int personelSayisi;

                if (userRole == "admin")
                {
                    istasyonSayisi = await _context.Istasyonlar.CountAsync(i => i.Aktif);
                    personelSayisi = await _context.Personeller.CountAsync(p => p.Aktif);
                }
                else
                {
                    istasyonSayisi = await _context.Istasyonlar.CountAsync(i => i.Firma != null && i.Firma.PatronId == userId && i.Aktif);
                    personelSayisi = await _context.Personeller.CountAsync(p => p.Istasyon != null && p.Istasyon.Firma != null && p.Istasyon.Firma.PatronId == userId && p.Aktif);
                }

                // Son 7 günlük trend
                var son7GunBaslangic = bugun.AddDays(-6);
                var son7GunVardiyalar = await vardiyaQuery
                    .Where(v => v.BaslangicTarihi >= son7GunBaslangic)
                    .ToListAsync();

                var son7Gun = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var gun = bugun.AddDays(-6 + i);
                        var gunSonu = gun.AddDays(1);
                        var gunVardiyalar = son7GunVardiyalar.Where(v => v.BaslangicTarihi >= gun && v.BaslangicTarihi < gunSonu).ToList();
                        return new
                        {
                            Tarih = gun,
                            VardiyaSayisi = gunVardiyalar.Count,
                            ToplamCiro = gunVardiyalar.Sum(v => v.GenelToplam)
                        };
                    })
                    .ToList();

                // En çok ciro yapan istasyonlar (top 5)
                var topIstasyonlar = buAyVardiyalar
                    .Where(v => v.Istasyon != null)
                    .GroupBy(v => new { v.IstasyonId, v.Istasyon!.Ad })
                    .Select(g => new
                    {
                        IstasyonAdi = g.Key.Ad,
                        ToplamCiro = g.Sum(v => v.GenelToplam),
                        VardiyaSayisi = g.Count()
                    })
                    .OrderByDescending(x => x.ToplamCiro)
                    .Take(5)
                    .ToList();

                // Aylık büyüme oranı
                var buAyCiro = buAyVardiyalar.Sum(v => v.GenelToplam);
                var gecenAyCiro = gecenAyVardiyalar.Sum(v => v.GenelToplam);
                decimal buyumeOrani = 0;
                if (gecenAyCiro > 0)
                {
                    buyumeOrani = ((buAyCiro - gecenAyCiro) / gecenAyCiro) * 100;
                }

                // Son işlemleri yükle
                var sonIslemler = new List<object>();
                try
                {
                    var logQuery = _context.VardiyaLoglari
                        .Include(vl => vl.Vardiya)
                            .ThenInclude(v => v!.Istasyon)
                        .AsQueryable();

                    if (userRole == "patron")
                    {
                        logQuery = logQuery.Where(vl => vl.Vardiya != null && vl.Vardiya.Istasyon != null && vl.Vardiya.Istasyon.Firma != null && vl.Vardiya.Istasyon.Firma.PatronId == userId);
                    }

                    var logs = await logQuery
                        .OrderByDescending(vl => vl.IslemTarihi)
                        .Take(10)
                        .ToListAsync();

                    sonIslemler = logs.Select(vl => new
                    {
                        vl.Id,
                        VardiyaId = vl.VardiyaId,
                        VardiyaDosyaAdi = vl.Vardiya?.DosyaAdi ?? "Bilinmeyen Vardiya",
                        IstasyonAdi = vl.Vardiya?.Istasyon?.Ad ?? "Bilinmeyen İstasyon",
                        vl.Islem,
                        vl.KullaniciAdi,
                        vl.IslemTarihi
                    } as object).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Log yüklenirken hata: {ex.Message}");
                }

                var dashboard = new
                {
                    Ozet = new
                    {
                        IstasyonSayisi = istasyonSayisi,
                        PersonelSayisi = personelSayisi,
                        BugunVardiyaSayisi = bugunVardiyalar.Count,
                        BugunToplamCiro = bugunVardiyalar.Sum(v => v.GenelToplam),
                        BuAyVardiyaSayisi = buAyVardiyalar.Count,
                        BuAyToplamCiro = buAyCiro,
                        GecenAyToplamCiro = gecenAyCiro,
                        BuyumeOrani = Math.Round((double)buyumeOrani, 2)
                    },
                    OnayBekleyenler = new
                    {
                        ToplamSayi = onayBekleyenler.Count,
                        VardiyaOnayi = onayBekleyenler.Count(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR),
                        SilmeOnayi = onayBekleyenler.Count(v => v.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR),
                        Liste = onayBekleyenler.OrderByDescending(v => v.OlusturmaTarihi).Take(5).Select(v => new
                        {
                            v.Id,
                            v.DosyaAdi,
                            IstasyonAdi = v.Istasyon?.Ad ?? "Bilinmeyen",
                            v.Durum,
                            v.GenelToplam,
                            v.OlusturmaTarihi
                        })
                    },
                    DurumDagilimi = new
                    {
                        Acik = buAyVardiyalar.Count(v => v.Durum == VardiyaDurum.ACIK),
                        OnayBekliyor = buAyVardiyalar.Count(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR),
                        Onaylandi = buAyVardiyalar.Count(v => v.Durum == VardiyaDurum.ONAYLANDI),
                        Reddedildi = buAyVardiyalar.Count(v => v.Durum == VardiyaDurum.REDDEDILDI)
                    },
                    Son7GunTrend = son7Gun,
                    TopIstasyonlar = topIstasyonlar,
                    SonIslemler = sonIslemler
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"İç hata: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { 
                    error = ex.Message, 
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }
        [HttpGet("admin-dashboard")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var usersByRole = await _context.Users
                    .Include(u => u.Role)
                    .GroupBy(u => u.Role != null ? u.Role.Ad : "Bilinmeyen")
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToListAsync();

                var totalIstasyonlar = await _context.Istasyonlar.CountAsync();
                var aktifIstasyonlar = await _context.Istasyonlar.CountAsync(i => i.Aktif);

                var totalRoles = await _context.Roles.CountAsync();

                // Online Users (Active in last 15 minutes)
                var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);
                var onlineUsers = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.LastActivity >= fifteenMinutesAgo)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.AdSoyad,
                        Role = u.Role != null ? u.Role.Ad : "Bilinmeyen",
                        u.LastActivity
                    })
                    .ToListAsync();

                // Database Metrics (PostgreSQL specific)
                string dbSize = "N/A";
                int tableCount = 0;
                try
                {
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "SELECT pg_size_pretty(pg_database_size(current_database()));";
                        _context.Database.OpenConnection();
                        dbSize = command.ExecuteScalar()?.ToString() ?? "N/A";

                        command.CommandText = "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';";
                        tableCount = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB Metrics Error: {ex.Message}");
                }

                var sonSistemLoglari = await _context.VardiyaLoglari
                    .OrderByDescending(l => l.IslemTarihi)
                    .Take(20)
                    .Select(l => new
                    {
                        l.Id,
                        l.Islem,
                        l.Aciklama,
                        l.KullaniciAdi,
                        l.KullaniciRol,
                        l.IslemTarihi
                    })
                    .ToListAsync();

                var tabloIstatistikleri = new
                {
                    Vardiyalar = await _context.Vardiyalar.CountAsync(),
                    MarketVardiyalar = await _context.MarketVardiyalar.CountAsync(),
                    OtomasyonSatislar = await _context.OtomasyonSatislar.CountAsync(),
                    Pusulalar = await _context.Pusulalar.CountAsync()
                };

                return Ok(new
                {
                    Ozet = new
                    {
                        TotalUsers = totalUsers,
                        TotalIstasyonlar = totalIstasyonlar,
                        AktifIstasyonlar = aktifIstasyonlar,
                        TotalRoles = totalRoles,
                        OnlineUserCount = onlineUsers.Count,
                        DbSize = dbSize,
                        TableCount = tableCount
                    },
                    UsersByRole = usersByRole,
                    OnlineUsers = onlineUsers,
                    SonSistemLoglari = sonSistemLoglari,
                    TabloIstatistikleri = tabloIstatistikleri
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
