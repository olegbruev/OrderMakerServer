/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mtd.OrderMaker.Server.Entity;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Areas.Identity.Pages.Users.Group
{
    public class EditModel : PageModel
    {
        private readonly OrderMakerContext _context;

        public EditModel(OrderMakerContext context)
        {
            _context = context;
        }

        public MtdGroup MtdGroup { get; set; }
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null) { return NotFound(); }
            MtdGroup = await _context.MtdGroup.FindAsync(id);
            if (MtdGroup == null) { return NotFound(); }

            return Page();
        }

    }
}