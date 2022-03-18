﻿/*
    MTD OrderMaker - http://ordermaker.org
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.Models.Index;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Components.Index
{
    [ViewComponent(Name = "IndexPlace")]
    public class Place : ViewComponent
    {
        private readonly OrderMakerContext _context;
        private readonly UserManager<WebAppUser> _userManager;

        public Place(OrderMakerContext context, UserManager<WebAppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string formId)
        {

            var user = await _userManager.GetUserAsync(HttpContext.User);
            bool isExists = await _context.MtdFilter.Where(x => x.IdUser == user.Id).AnyAsync();

            if (!isExists)
            {
                MtdFilter mtdFilter = new MtdFilter
                {
                    IdUser = user.Id,
                    MtdForm = formId,
                    Page = 1,
                    PageSize = 10,
                    SearchNumber = "",
                    SearchText = ""
                };
                await _context.MtdFilter.AddAsync(mtdFilter);
                await _context.SaveChangesAsync();
            }

            PlaceModelView model = new PlaceModelView { FormId = formId };
            return View("Default", model);
        }
    }
}
