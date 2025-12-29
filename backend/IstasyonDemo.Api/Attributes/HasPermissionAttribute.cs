using IstasyonDemo.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IstasyonDemo.Api.Attributes
{
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string resourceKey) : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { resourceKey };
        }
    }
}
