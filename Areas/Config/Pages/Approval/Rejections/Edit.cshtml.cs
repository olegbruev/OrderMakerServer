﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Components;
using Mtd.OrderMaker.Server.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Areas.Config.Pages.Approval.Rejections
{
    public class EditModel : PageModel
    {
        private readonly OrderMakerContext _context;

        public EditModel(OrderMakerContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MtdApprovalRejection MtdApprovalRejection { get; set; }

        [BindProperty]
        public MtdApprovalStage MtdApprovalStage { get; set; }
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MtdApprovalRejection = await _context.MtdApprovalRejection
                .Include(m => m.MtdApprovalStage).FirstOrDefaultAsync(m => m.Id == id);

            if (MtdApprovalRejection == null)
            {
                return NotFound();
            }

            MtdApprovalStage = MtdApprovalRejection.MtdApprovalStage;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            _context.Attach(MtdApprovalRejection).State = EntityState.Modified;
            MTDImgSModify img = await MTDImgSelector.ImageModifyAsync("img", Request);
            MtdApprovalRejection.ImgData = img.Image;
            MtdApprovalRejection.ImgType = img.ImgType;
            _context.Entry(MtdApprovalRejection).Property(x => x.ImgType).IsModified = img.Modify;
            _context.Entry(MtdApprovalRejection).Property(x => x.ImgData).IsModified = img.Modify;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MtdApprovalRejectionExists(MtdApprovalRejection.Id))
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
            _context.MtdApprovalRejection.Remove(MtdApprovalRejection);
            await _context.SaveChangesAsync();
            return new OkResult();
        }

        private bool MtdApprovalRejectionExists(string id)
        {
            return _context.MtdApprovalRejection.Any(e => e.Id == id);
        }
    }
}
