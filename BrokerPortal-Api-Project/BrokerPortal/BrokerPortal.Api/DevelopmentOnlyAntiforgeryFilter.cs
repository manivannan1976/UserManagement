using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrokerPortal.Api;

public sealed class DevelopmentOnlyAntiforgeryFilter(
    IAntiforgery antiforgery,
    IHostEnvironment environment) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            context.Result = new BadRequestResult();
        }
    }
}
