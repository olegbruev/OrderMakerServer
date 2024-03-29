﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Controllers.Index.Filter
{
    [Route("api/filter/custom")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class CustomController : ControllerBase
    {
        private readonly OrderMakerContext context;
        private readonly UserHandler userHandler;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CustomController(OrderMakerContext context, UserHandler userHandler, IStringLocalizer<SharedResource> localizer)
        {
            this.context = context;
            this.userHandler = userHandler;
            this._localizer = localizer;
        }

        [HttpPost("get/list")]
        [Produces("application/json")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> GetFieldList()
        {
            var form = await Request.ReadFormAsync();
            string idField = form["id-field"];

            /*Check access for field*/
            WebAppUser user = await userHandler.GetUserAsync(HttpContext.User);
            MtdFormPartField field = await context.MtdFormPartField.FindAsync(idField);
            if (field == null || field.MtdSysType != 11) { return NotFound(); }
            MtdFormPart part = await context.MtdFormPart.FindAsync(field.MtdFormPart);
            bool isOk = await userHandler.IsViewerPartAsync(user, field.MtdFormPart);
            if (!isOk) { return NotFound(); }
            /*---------------------*/

            /*Get a link to the field*/
            string linkFormId = await context.MtdFormList.Where(x => x.Id == field.Id).Select(x => x.MtdForm).FirstOrDefaultAsync();
            if (linkFormId == null) { return NotFound(); }
            IList<string> linkPartIds = await context.MtdFormPart.Where(x => x.MtdForm == linkFormId).Select(x => x.Id).ToListAsync();
            string linkFieldId = await context.MtdFormPartField
                .Where(x => linkPartIds.Contains(x.MtdFormPart) && x.MtdSysType == 1).Select(x => x.Id).FirstOrDefaultAsync();
            if (linkFieldId == null) { return NotFound(); }
            /*---------------------*/

            IList<MtdStoreStack> stack = await context.MtdStoreStack
                .Where(x => x.MtdFormPartField == linkFieldId).ToListAsync();
            IList<long> stackIds = stack.Select(x => x.Id).ToList();
            IList<MtdStoreStackText> values = await context.MtdStoreStackText.Where(x => stackIds.Contains(x.Id)).ToListAsync();

            var data = stack.Join(values, x => x.Id, v => v.Id, (x, v) => new { Id = x.MtdStore, Value = v.Register }).OrderBy(x => x.Value).ToList();

            return Ok(new JsonResult(data));
        }

        [HttpPost("add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostFilterAddAsync()
        {
            var form = await Request.ReadFormAsync();

            string formId = form["form-id"];
            string fieldId = form["field-id"];
            string fieldType = form["field-type"];
            string fieldAction = form["field-action"];
            string fieldValue = form["field-value"];
            string fieldValueExt = form["field-value-ext"];
            string dateFormat = form["date-format"];

            MtdFilter filter = await userHandler.GetFilterAsync(User, formId);

            bool isOk = int.TryParse(fieldAction, out int term);
            if (!isOk) { return BadRequest(_localizer["Error: Bad request."]); }

            if (fieldType == "5" || fieldType == "6") { fieldValue = $"{fieldValue}***{fieldValueExt}"; } else { dateFormat = null; }

            MtdFilterField field = new MtdFilterField { MtdFilter = filter.Id, MtdFormPartField = fieldId, MtdTerm = term, Value = fieldValue, ValueExtra = dateFormat };
            try
            {
                await context.MtdFilterField.AddAsync(field);
                await context.SaveChangesAsync();
            }
            catch (Exception ex) { throw ex.InnerException; }



            return Ok();
        }

    }
}
