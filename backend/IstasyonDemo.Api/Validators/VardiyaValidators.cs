using FluentValidation;
using IstasyonDemo.Api.Dtos;

namespace IstasyonDemo.Api.Validators
{
    public class CreateVardiyaDtoValidator : AbstractValidator<CreateVardiyaDto>
    {
        public CreateVardiyaDtoValidator()
        {
            RuleFor(x => x.BaslangicTarihi).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
            RuleFor(x => x.DosyaAdi).NotEmpty().WithMessage("Dosya adı zorunludur.");
            RuleFor(x => x.DosyaIcerik).NotEmpty().WithMessage("Dosya içeriği zorunludur.");
            
            RuleForEach(x => x.OtomasyonSatislar).SetValidator(new CreateOtomasyonSatisDtoValidator());
            RuleForEach(x => x.FiloSatislar).SetValidator(new CreateFiloSatisDtoValidator());
        }
    }

    public class CreateOtomasyonSatisDtoValidator : AbstractValidator<CreateOtomasyonSatisDto>
    {
        public CreateOtomasyonSatisDtoValidator()
        {
            RuleFor(x => x.PompaNo).GreaterThan(0).WithMessage("Pompa numarası geçersiz.");
            RuleFor(x => x.Litre).GreaterThan(0).WithMessage("Litre 0'dan büyük olmalıdır.");
            RuleFor(x => x.BirimFiyat).GreaterThan(0).WithMessage("Birim fiyat 0'dan büyük olmalıdır.");
            RuleFor(x => x.ToplamTutar).GreaterThan(0).WithMessage("Toplam tutar 0'dan büyük olmalıdır.");
        }
    }

    public class CreateFiloSatisDtoValidator : AbstractValidator<CreateFiloSatisDto>
    {
        public CreateFiloSatisDtoValidator()
        {
            RuleFor(x => x.FiloKodu).NotEmpty().WithMessage("Filo kodu zorunludur.");
            RuleFor(x => x.Plaka).NotEmpty().WithMessage("Plaka zorunludur.");
            RuleFor(x => x.Litre).GreaterThan(0).WithMessage("Litre 0'dan büyük olmalıdır.");
            RuleFor(x => x.Tutar).GreaterThan(0).WithMessage("Tutar 0'dan büyük olmalıdır.");
        }
    }

    public class OnayDtoValidator : AbstractValidator<OnayDto>
    {
        public OnayDtoValidator()
        {
            RuleFor(x => x.OnaylayanId).GreaterThan(0).WithMessage("Onaylayan ID zorunludur.");
            RuleFor(x => x.OnaylayanAdi).NotEmpty().WithMessage("Onaylayan adı zorunludur.");
        }
    }

    public class RedDtoValidator : AbstractValidator<RedDto>
    {
        public RedDtoValidator()
        {
            RuleFor(x => x.OnaylayanId).GreaterThan(0).WithMessage("Onaylayan ID zorunludur.");
            RuleFor(x => x.OnaylayanAdi).NotEmpty().WithMessage("Onaylayan adı zorunludur.");
            RuleFor(x => x.RedNedeni).NotEmpty().WithMessage("Red nedeni zorunludur.");
        }
    }

    public class SilmeTalebiDtoValidator : AbstractValidator<SilmeTalebiDto>
    {
        public SilmeTalebiDtoValidator()
        {
            RuleFor(x => x.Nedeni).NotEmpty().WithMessage("Silme nedeni zorunludur.");
        }
    }
}
