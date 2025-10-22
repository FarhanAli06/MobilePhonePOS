using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Empire.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Empire.Web.Authorization;

/// <summary>
/// Custom authorization attribute that uses session-based authentication
/// </summary>
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var isAuthenticated = session.GetString("IsAuthenticated");
        var userId = session.GetString("UserId");

        if (string.IsNullOrEmpty(isAuthenticated) || isAuthenticated != "true" || string.IsNullOrEmpty(userId))
        {
            // User is not authenticated, redirect to login
            context.Result = new RedirectToActionResult("Login", "Home", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}

/// <summary>
/// Custom authorization attribute that requires specific shop access
/// </summary>
public class SessionAuthorizeWithShopAttribute : ActionFilterAttribute
{
    public override async void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var isAuthenticated = session.GetString("IsAuthenticated");
        var userId = session.GetString("UserId");
        var currentShopId = session.GetString("CurrentShopId");

        if (string.IsNullOrEmpty(isAuthenticated) || isAuthenticated != "true" || string.IsNullOrEmpty(userId))
        {
            // User is not authenticated, redirect to login
            context.Result = new RedirectToActionResult("Login", "Home", null);
            return;
        }

        if (string.IsNullOrEmpty(currentShopId) || currentShopId == "0")
        {
            try
            {
                // Check if user has any shops
                var shopService = context.HttpContext.RequestServices.GetRequiredService<IShopService>();
                var userIdInt = int.Parse(userId);
                var userShops = await shopService.GetShopsByUserAsync(userIdInt);

                if (!userShops.Any())
                {
                    // User has no shops, redirect to create shop
                    context.Result = new RedirectToActionResult("Create", "Shop", null);
                    return;
                }
                else
                {
                    // User has shops but none selected, auto-select the first one
                    var firstShop = userShops.First();
                    session.SetString("CurrentShopId", firstShop.Id.ToString());
                    session.SetString("CurrentShopName", firstShop.Name);
                    // Continue with the request
                }
            }
            catch (Exception ex)
            {
                // If shop service fails, let the controller handle it
                Console.WriteLine($"SessionAuthorizeWithShop error: {ex.Message}");
                // Don't block the request, let the controller handle shop selection
            }
        }

        base.OnActionExecuting(context);
    }
}

