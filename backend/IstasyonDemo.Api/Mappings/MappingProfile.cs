using AutoMapper;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Vardiya Mappings
            CreateMap<CreateVardiyaDto, Vardiya>()
                .ForMember(dest => dest.BaslangicTarihi, opt => opt.MapFrom(src => src.BaslangicTarihi.ToUniversalTime()))
                .ForMember(dest => dest.BitisTarihi, opt => opt.MapFrom(src => src.BitisTarihi.HasValue ? src.BitisTarihi.Value.ToUniversalTime() : (DateTime?)null))
                .ForMember(dest => dest.DosyaIcerik, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.DosyaIcerik) ? Convert.FromBase64String(src.DosyaIcerik) : null))
                .ForMember(dest => dest.Durum, opt => opt.MapFrom(src => VardiyaDurum.ACIK))
                .ForMember(dest => dest.OlusturmaTarihi, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.PompaToplam, opt => opt.MapFrom(src => src.OtomasyonSatislar.Sum(s => s.ToplamTutar) + src.FiloSatislar.Sum(s => s.Tutar)))
                .ForMember(dest => dest.GenelToplam, opt => opt.MapFrom(src => src.GenelToplam))
                .ForMember(dest => dest.OtomasyonSatislar, opt => opt.Ignore()) // Handled manually in service due to logic
                .ForMember(dest => dest.FiloSatislar, opt => opt.Ignore()); // Handled manually or mapped separately

            // OtomasyonSatis Mappings
            CreateMap<CreateOtomasyonSatisDto, OtomasyonSatis>()
                .ForMember(dest => dest.SatisTarihi, opt => opt.MapFrom(src => src.SatisTarihi.ToUniversalTime()))
                .ForMember(dest => dest.PersonelAdi, opt => opt.MapFrom(src => src.PersonelAdi ?? ""))
                .ForMember(dest => dest.PersonelKeyId, opt => opt.MapFrom(src => src.PersonelKeyId ?? ""));

            // FiloSatis Mappings
            CreateMap<CreateFiloSatisDto, FiloSatis>()
                .ForMember(dest => dest.Tarih, opt => opt.MapFrom(src => src.Tarih.ToUniversalTime()));

            // Role Mappings
            // Role Mappings
            CreateMap<Role, RoleDto>();
            CreateMap<CreateRoleDto, Role>();
            CreateMap<UpdateRoleDto, Role>();

            // Stok Mappings
            CreateMap<CreateTankGirisDto, TankGiris>()
               .ForMember(dest => dest.Tarih, opt => opt.MapFrom(src => src.Tarih.ToUniversalTime()))
               .ForMember(dest => dest.Id, opt => opt.Ignore())
               .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<TankGiris, TankGirisDto>()
               .ForMember(dest => dest.YakitAd, opt => opt.MapFrom(src => src.Yakit != null ? src.Yakit.Ad : ""))
               .ForMember(dest => dest.YakitRenk, opt => opt.MapFrom(src => src.Yakit != null ? src.Yakit.Renk : ""));

            // Yakit Mappings
            CreateMap<Yakit, YakitDto>();
            CreateMap<CreateYakitDto, Yakit>();
            CreateMap<UpdateYakitDto, Yakit>();
        }
    }
}
