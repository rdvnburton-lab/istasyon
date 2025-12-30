using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace IstasyonDemo.Api.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Content-Security-Policy
            // Temel bir CSP politikası. İhtiyaca göre 'unsafe-inline' vb. kaldırılabilir veya domainler eklenebilir.
            // Angular gibi SPA frameworkleri için genelde script-src ve style-src için 'unsafe-inline' gerekebilir (AOT kullanılmıyorsa).
            // frame-ancestors 'none' -> Bu sitenin iframe içinde çalışmasını engeller (Clickjacking koruması).
            if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                context.Response.Headers.Append("Content-Security-Policy", 
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https:; " +
                    "style-src 'self' 'unsafe-inline' https:; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self' data: https:; " +
                    "connect-src 'self' https:; " +
                    "frame-ancestors 'none'; " +
                    "object-src 'none'; " +
                    "base-uri 'self';");
            }

            // Strict-Transport-Security (HSTS)
            // app.UseHsts() middleware'i tarafından yönetiliyor, burada manuel eklemeye gerek yok.
            // Ancak app.UseHsts() sadece HTTPS isteklerinde çalışır.

            // X-Content-Type-Options
            // MIME-type sniffing'i engeller.
            if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            }

            // X-XSS-Protection
            // Eski tarayıcılar için XSS filtresini açar. Modern tarayıcılar CSP kullanır ama katmanlı güvenlik için iyidir.
            if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            {
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            }

            // X-Frame-Options
            // Siteyi iframe içine almayı engeller (Clickjacking koruması). CSP frame-ancestors bunu kapsar ama eski tarayıcılar için eklenir.
            if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
            }

            // Referrer-Policy
            // Başka bir siteye giderken ne kadar referrer bilgisi gönderileceğini belirler.
            if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            {
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            }

            // X-Permitted-Cross-Domain-Policies
            // Adobe Flash vb. çapraz domain politikalarını engeller.
            if (!context.Response.Headers.ContainsKey("X-Permitted-Cross-Domain-Policies"))
            {
                context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
            }

            // Permissions-Policy
            // Tarayıcı özelliklerine erişimi kısıtlar (Kamera, mikrofon vb. kullanılmıyorsa kapatılır).
            if (!context.Response.Headers.ContainsKey("Permissions-Policy"))
            {
                context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
            }

            await _next(context);
        }
    }
}
