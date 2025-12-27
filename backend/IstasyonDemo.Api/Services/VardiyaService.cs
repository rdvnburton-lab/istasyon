using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace IstasyonDemo.Api.Services
{
    public class VardiyaService : IVardiyaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VardiyaService> _logger;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public VardiyaService(AppDbContext context, ILogger<VardiyaService> logger, IMapper mapper, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<Vardiya> CreateVardiyaAsync(CreateVardiyaDto dto, int userId, string? userRole, string? userName)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Vardiya oluştur
                var vardiya = _mapper.Map<Vardiya>(dto);
                
                _context.Vardiyalar.Add(vardiya);
                await _context.SaveChangesAsync();

                // 2. Otomasyon Satışlarını Ekle
                foreach (var satisDto in dto.OtomasyonSatislar)
                {
                    var personel = await _context.Personeller
                        .Where(p => p.IstasyonId == vardiya.IstasyonId)
                        .FirstOrDefaultAsync(p => 
                            (satisDto.PersonelKeyId != null && p.KeyId == satisDto.PersonelKeyId) || 
                            (p.AdSoyad == satisDto.PersonelAdi) || 
                            (p.OtomasyonAdi == satisDto.PersonelAdi));

                    if (personel == null && !string.IsNullOrWhiteSpace(satisDto.PersonelAdi))
                    {
                        personel = new Personel
                        {
                            OtomasyonAdi = satisDto.PersonelAdi.Trim(),
                            AdSoyad = satisDto.PersonelAdi.Trim(),
                            KeyId = satisDto.PersonelKeyId,
                            Rol = PersonelRol.POMPACI,
                            Aktif = true,
                            IstasyonId = vardiya.IstasyonId
                        };
                        _context.Personeller.Add(personel);
                        await _context.SaveChangesAsync();
                    }

                    var satis = _mapper.Map<OtomasyonSatis>(satisDto);
                    satis.VardiyaId = vardiya.Id;
                    satis.PersonelId = personel?.Id;
                    
                    _context.OtomasyonSatislar.Add(satis);
                }

                // 3. Filo Satışlarını Ekle
                foreach (var filoDto in dto.FiloSatislar)
                {
                    var filo = _mapper.Map<FiloSatis>(filoDto);
                    filo.VardiyaId = vardiya.Id;
                    _context.FiloSatislar.Add(filo);
                }

                await _context.SaveChangesAsync();
                
                var log = new VardiyaLog
                {
                    VardiyaId = vardiya.Id,
                    Islem = "OLUSTURULDU",
                    Aciklama = $"Vardiya dosyası yüklendi: {dto.DosyaAdi}",
                    KullaniciId = userId,
                    KullaniciAdi = userName ?? "",
                    KullaniciRol = userRole ?? "",
                    IslemTarihi = DateTime.UtcNow,
                    YeniDurum = VardiyaDurum.ACIK.ToString()
                };
                _context.VardiyaLoglari.Add(log);
                await _context.SaveChangesAsync();

                await _context.SaveChangesAsync();

                // Kullanıcıya bildirim gönder (Kendi işlemi)
                await _notificationService.NotifyUserAsync(
                    userId,
                    "Vardiya Oluşturuldu",
                    $"{dto.DosyaAdi} başarıyla yüklendi.",
                    "VARDIYA_OLUSTURULDU",
                    "success",
                    relatedVardiyaId: vardiya.Id
                );

                await transaction.CommitAsync();
                return vardiya;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating vardiya");
                throw;
            }
        }

        public async Task<Vardiya?> GetVardiyaByIdAsync(int id)
        {
             return await _context.Vardiyalar
                .AsNoTracking()
                .Include(v => v.OtomasyonSatislar)
                .Include(v => v.FiloSatislar)
                .Include(v => v.Pusulalar)
                    .ThenInclude(p => p.KrediKartiDetaylari)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<List<Vardiya>> GetOnayBekleyenlerAsync(int userId, string? userRole)
        {
            IQueryable<Vardiya> query = _context.Vardiyalar
                .Include(v => v.Istasyon)
                .Where(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR || v.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR);

            if (userRole == "patron")
            {
                query = query.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == userId);
            }

            return await query.OrderByDescending(v => v.BaslangicTarihi).ToListAsync();
        }

        public async Task OnayaGonderAsync(int id, int userId, string? userRole)
        {
            var vardiya = await _context.Vardiyalar.FindAsync(id);
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole != "admin")
            {
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
                if (user?.IstasyonId != vardiya.IstasyonId) throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok.");
            }

            if (vardiya.Durum != VardiyaDurum.ACIK && vardiya.Durum != VardiyaDurum.REDDEDILDI)
            {
                throw new InvalidOperationException("Sadece AÇIK veya REDDEDİLMİŞ vardiyalar onaya gönderilebilir.");
            }

            vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Kullanıcıya bildirim gönder (Kendi işlemi)
            await _notificationService.NotifyUserAsync(
                userId,
                "Onaya Gönderildi",
                $"{vardiya.DosyaAdi} onaya gönderildi.",
                "VARDIYA_ONAY_BEKLIYOR",
                "info",
                relatedVardiyaId: vardiya.Id
            );

            // Adminlere bildirim gönder
            await _notificationService.NotifyAdminsAsync(
                "Yeni Vardiya Onayı",
                $"{vardiya.DosyaAdi} için onay bekleniyor.",
                "VARDIYA_ONAY_BEKLIYOR",
                "info",
                relatedVardiyaId: vardiya.Id
            );
        }

        public async Task SilmeTalebiOlusturAsync(int id, SilmeTalebiDto dto, int userId, string? userRole, string? userName)
        {
            var vardiya = await _context.Vardiyalar.Include(v => v.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(v => v.Id == id);
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole == "patron" && (vardiya.Istasyon == null || vardiya.Istasyon.Firma == null || vardiya.Istasyon.Firma.PatronId != userId)) throw new UnauthorizedAccessException();
            if (userRole != "admin" && userRole != "patron")
            {
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
                if (user?.IstasyonId != vardiya.IstasyonId) throw new UnauthorizedAccessException();
            }

            if (vardiya.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR)
            {
                 throw new InvalidOperationException("Zaten silinme onayı bekliyor.");
            }

            if (userRole != "admin" && userRole != "patron" && vardiya.Durum == VardiyaDurum.ONAYLANDI)
            {
                throw new InvalidOperationException("Onaylanmış vardiyalar sorumlular tarafından silinemez.");
            }

            var eskiDurum = vardiya.Durum.ToString();
            
            vardiya.Durum = VardiyaDurum.SILINME_ONAYI_BEKLIYOR;
            vardiya.SilinmeTalebiNedeni = dto.Nedeni;
            vardiya.SilinmeTalebiOlusturanId = userId;
            vardiya.SilinmeTalebiOlusturanAdi = userName;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await LogVardiyaIslem(
                vardiya.Id,
                "SILME_TALEP_EDILDI",
                $"Silme nedeni: {dto.Nedeni}",
                userId,
                userName,
                userRole,
                eskiDurum,
                VardiyaDurum.SILINME_ONAYI_BEKLIYOR.ToString()
            );

            // Kullanıcıya bildirim gönder (Kendi işlemi)
            await _notificationService.NotifyUserAsync(
                userId,
                "Silme Talebi Oluşturuldu",
                $"{vardiya.DosyaAdi} için silme talebi oluşturuldu.",
                "VARDIYA_SILME_ONAYI_BEKLIYOR",
                "warn",
                relatedVardiyaId: vardiya.Id
            );

            // Adminlere bildirim gönder
            await _notificationService.NotifyAdminsAsync(
                "Vardiya Silme Talebi",
                $"{vardiya.DosyaAdi} için silme onayı bekleniyor. Neden: {dto.Nedeni}",
                "VARDIYA_SILME_ONAYI_BEKLIYOR",
                "warn",
                relatedVardiyaId: vardiya.Id
            );
        }

        public async Task OnaylaAsync(int id, OnayDto dto, int userId, string? userRole)
        {
            var vardiya = await _context.Vardiyalar.Include(v => v.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(v => v.Id == id);
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole == "patron" && (vardiya.Istasyon == null || vardiya.Istasyon.Firma == null || vardiya.Istasyon.Firma.PatronId != userId)) throw new UnauthorizedAccessException();

            if (vardiya.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR)
            {
                // Soft Delete
                vardiya.Durum = VardiyaDurum.SILINDI;
                vardiya.OnaylayanId = dto.OnaylayanId;
                vardiya.OnaylayanAdi = dto.OnaylayanAdi;
                vardiya.OnayTarihi = DateTime.UtcNow;
                vardiya.GuncellemeTarihi = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                await LogVardiyaIslem(
                    vardiya.Id,
                    "SILINDI",
                    $"Silme işlemi onaylandı. Onaylayan: {dto.OnaylayanAdi}",
                    userId,
                    dto.OnaylayanAdi,
                    userRole,
                    VardiyaDurum.SILINME_ONAYI_BEKLIYOR.ToString(),
                    VardiyaDurum.SILINDI.ToString()
                );

                // Silme talebini oluşturan kişiye bildirim gönder
                if (vardiya.SilinmeTalebiOlusturanId.HasValue)
                {
                    await _notificationService.NotifyUserAsync(
                        vardiya.SilinmeTalebiOlusturanId.Value,
                        "Vardiya Silindi",
                        $"{vardiya.DosyaAdi} silme talebiniz onaylandı.",
                        "VARDIYA_SILME_ONAYLANDI",
                        "success",
                        relatedVardiyaId: vardiya.Id
                    );
                }
                return;
            }

            if (vardiya.Durum != VardiyaDurum.ONAY_BEKLIYOR)
            {
                throw new InvalidOperationException("Sadece ONAY BEKLEYEN veya SILINME ONAYI BEKLEYEN vardiyalar onaylanabilir.");
            }

            vardiya.Durum = VardiyaDurum.ONAYLANDI;
            vardiya.OnaylayanId = dto.OnaylayanId;
            vardiya.OnaylayanAdi = dto.OnaylayanAdi;
            vardiya.OnayTarihi = DateTime.UtcNow;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            await LogVardiyaIslem(
                vardiya.Id,
                "ONAYLANDI",
                $"Vardiya onaylandı. Onaylayan: {dto.OnaylayanAdi}",
                userId,
                dto.OnaylayanAdi,
                userRole,
                VardiyaDurum.ONAY_BEKLIYOR.ToString(),
                VardiyaDurum.ONAYLANDI.ToString()
            );

            // Vardiyayı oluşturan kişiye bildirim gönder
            var olusturanLog = await _context.VardiyaLoglari
                .Where(l => l.VardiyaId == vardiya.Id && l.Islem == "OLUSTURULDU")
                .OrderByDescending(l => l.IslemTarihi)
                .FirstOrDefaultAsync();

            if (olusturanLog != null && olusturanLog.KullaniciId.HasValue)
            {
                await _notificationService.NotifyUserAsync(
                    olusturanLog.KullaniciId.Value,
                    "Vardiya Onaylandı",
                    $"{vardiya.DosyaAdi} onaylandı.",
                    "VARDIYA_ONAYLANDI",
                    "success",
                    relatedVardiyaId: vardiya.Id
                );
            }
        }

        public async Task ReddetAsync(int id, RedDto dto, int userId, string? userRole)
        {
            var vardiya = await _context.Vardiyalar.Include(v => v.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(v => v.Id == id);
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole == "patron" && (vardiya.Istasyon == null || vardiya.Istasyon.Firma == null || vardiya.Istasyon.Firma.PatronId != userId)) throw new UnauthorizedAccessException();

            if (vardiya.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR)
            {
                // Revert to ACIK so it's not deleted
                vardiya.Durum = VardiyaDurum.ACIK;
                vardiya.RedNedeni = dto.RedNedeni;
                vardiya.OnaylayanId = dto.OnaylayanId;
                vardiya.OnaylayanAdi = dto.OnaylayanAdi;
                vardiya.GuncellemeTarihi = DateTime.UtcNow;
                
                // Clear delete request info
                var silmeTalepEdenId = vardiya.SilinmeTalebiOlusturanId;
                vardiya.SilinmeTalebiNedeni = null;
                vardiya.SilinmeTalebiOlusturanId = null;
                vardiya.SilinmeTalebiOlusturanAdi = null;

                await _context.SaveChangesAsync();

                await LogVardiyaIslem(
                    vardiya.Id,
                    "SILME_REDDEDILDI",
                    $"Silme talebi reddedildi. Neden: {dto.RedNedeni}",
                    userId,
                    dto.OnaylayanAdi,
                    userRole,
                    VardiyaDurum.SILINME_ONAYI_BEKLIYOR.ToString(),
                    VardiyaDurum.ACIK.ToString()
                );

                if (silmeTalepEdenId.HasValue)
                {
                    await _notificationService.NotifyUserAsync(
                        silmeTalepEdenId.Value,
                        "Silme Talebi Reddedildi",
                        $"{vardiya.DosyaAdi} silme talebiniz reddedildi. Neden: {dto.RedNedeni}",
                        "VARDIYA_SILME_REDDEDILDI",
                        "error",
                        relatedVardiyaId: vardiya.Id
                    );
                }
                return;
            }

            if (vardiya.Durum != VardiyaDurum.ONAY_BEKLIYOR)
            {
                throw new InvalidOperationException("Sadece ONAY BEKLEYEN vardiyalar reddedilebilir.");
            }

            vardiya.Durum = VardiyaDurum.REDDEDILDI;
            vardiya.RedNedeni = dto.RedNedeni;
            vardiya.OnaylayanId = dto.OnaylayanId;
            vardiya.OnaylayanAdi = dto.OnaylayanAdi;
            vardiya.OnayTarihi = DateTime.UtcNow;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogVardiyaIslem(
                vardiya.Id,
                "REDDEDILDI",
                $"Vardiya reddedildi. Neden: {dto.RedNedeni}",
                userId,
                dto.OnaylayanAdi,
                userRole,
                VardiyaDurum.ONAY_BEKLIYOR.ToString(),
                VardiyaDurum.REDDEDILDI.ToString()
            );

            // Vardiyayı oluşturan kişiye bildirim gönder
            var olusturanLog = await _context.VardiyaLoglari
                .Where(l => l.VardiyaId == vardiya.Id && l.Islem == "OLUSTURULDU")
                .OrderByDescending(l => l.IslemTarihi)
                .FirstOrDefaultAsync();

            if (olusturanLog != null && olusturanLog.KullaniciId.HasValue)
            {
                await _notificationService.NotifyUserAsync(
                    olusturanLog.KullaniciId.Value,
                    "Vardiya Reddedildi",
                    $"{vardiya.DosyaAdi} reddedildi. Neden: {dto.RedNedeni}",
                    "VARDIYA_REDDEDILDI",
                    "error",
                    relatedVardiyaId: vardiya.Id
                );
            }
        }

        private async Task LogVardiyaIslem(int vardiyaId, string islem, string aciklama, int userId, string? userName, string? userRole, string? eskiDurum, string? yeniDurum)
        {
            var log = new VardiyaLog
            {
                VardiyaId = vardiyaId,
                Islem = islem,
                Aciklama = aciklama,
                KullaniciId = userId,
                KullaniciAdi = userName ?? "",
                KullaniciRol = userRole ?? "",
                IslemTarihi = DateTime.UtcNow,
                EskiDurum = eskiDurum,
                YeniDurum = yeniDurum
            };
            _context.VardiyaLoglari.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
