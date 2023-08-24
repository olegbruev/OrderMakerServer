﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Components;
using Mtd.OrderMaker.Server.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Areas.Config.Pages.Approval.Resolutions
{
    public class EditModel : PageModel
    {
        private readonly OrderMakerContext _context;

        public EditModel(OrderMakerContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MtdApprovalResolution MtdApprovalResolution { get; set; }

        [BindProperty]
        public MtdApprovalStage MtdApprovalStage { get; set; }
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MtdApprovalResolution = await _context.MtdApprovalResolution
                .Include(m => m.MtdApprovalStage).FirstOrDefaultAsync(m => m.Id == id);

            if (MtdApprovalResolution == null)
            {
                return NotFound();
            }

            MtdApprovalStage = MtdApprovalResolution.MtdApprovalStage;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            _context.Attach(MtdApprovalResolution).State = EntityState.Modified;
            MTDImgSModify img = await MTDImgSelector.ImageModifyAsync("img", Request);
            MtdApprovalResolution.ImgData = img.Image;
            MtdApprovalResolution.ImgType = img.ImgType;
            _context.Entry(MtdApprovalResolution).Property(x => x.ImgType).IsModified = img.Modify;
            _context.Entry(MtdApprovalResolution).Property(x => x.ImgData).IsModified = img.Modify;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MtdApprovalResolutionExists(MtdApprovalResolution.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return new OkResult();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            _context.MtdApprovalResolution.Remove(MtdApprovalResolution);
            await _context.SaveChangesAsync();
            return new OkResult();
        }

        private bool MtdApprovalResolutionExists(string id)
        {
            return _context.MtdApprovalResolution.Any(e => e.Id == id);
        }
    }
}
