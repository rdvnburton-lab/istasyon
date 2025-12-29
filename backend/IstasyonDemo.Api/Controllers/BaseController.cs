using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IstasyonDemo.Api.Controllers
{
    public class BaseController : ControllerBase
    {
        protected int CurrentUserId => int.Parse(User.FindFirst("id")?.Value ?? "0");
        protected string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        
        protected int? CurrentIstasyonId 
        {
            get 
            {
                var claim = User.FindFirst("IstasyonId");
                return claim != null ? int.Parse(claim.Value) : null;
            }
        }

        protected int? CurrentFirmaId
        {
            get
            {
                var claim = User.FindFirst("FirmaId");
                return claim != null ? int.Parse(claim.Value) : null;
            }
        }

        protected bool IsAdmin => CurrentUserRole.ToLower() == "admin";
        protected bool IsPatron => CurrentUserRole.ToLower() == "patron";
    }
}
