using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class RedirectTo : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity.IsAuthenticated)
        {
            var path = context.HttpContext.Request.Path.Value?.ToLower();

            if (path == "/home/privacy")
            {
                context.Result = new RedirectResult("/Identity/Account/Login");
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
