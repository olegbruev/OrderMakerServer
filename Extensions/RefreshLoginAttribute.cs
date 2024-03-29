﻿using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Extensions
{
    public class RefreshLoginAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await context.HttpContext.RefreshLoginAsync();
            await next();
        }
    }
}
