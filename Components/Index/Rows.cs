/*
    MTD OrderMaker - http://ordermaker.org
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.

    This file is part of MTD OrderMaker.
    MTD OrderMaker is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see  https://www.gnu.org/licenses/.
*/

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.EntityHandler.Approval;
using Mtd.OrderMaker.Server.EntityHandler.Filter;
using Mtd.OrderMaker.Server.EntityHandler.Stack;
using Mtd.OrderMaker.Server.Models.Index;
using Mtd.OrderMaker.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Components.Index
{
    [ViewComponent(Name = "IndexRows")]
    public class Rows : ViewComponent
    {
        private readonly OrderMakerContext _context;
        private readonly UserHandler _userHandler;
        public int pageCount = 0;


        public Rows(OrderMakerContext orderMakerContext, UserHandler userHandler)
        {
            _context = orderMakerContext;
            _userHandler = userHandler;
        }

        public async Task<IViewComponentResult> InvokeAsync(string formId)
        {
            var user = await _userHandler.GetUserAsync(HttpContext.User);
            IList<MtdFormPart> parts = await _userHandler.GetAllowPartsForView(user, formId);
            List<string> partIds = parts.Select(x => x.Id).ToList(); 

            FilterHandler handlerFilter = new FilterHandler(_context, formId, user, _userHandler);
            Incomer incomer = await handlerFilter.GetIncomerDataAsync();
            TypeQuery typeQuery = await handlerFilter.GetTypeQueryAsync(user);
            OutFlow outFlow = await handlerFilter.GetStackFlowAsync(incomer, typeQuery);
            IList<MtdStore> mtdStores = outFlow.MtdStores;

            decimal count = (decimal)outFlow.Count / incomer.PageSize;
            pageCount = Convert.ToInt32(Math.Ceiling(count));
            pageCount = pageCount == 0 ? 1 : pageCount;

            IList<string> storeIds = mtdStores.Select(s => s.Id).ToList();
            IList<string> fieldIds = fieldIds = incomer.FieldForColumn.Select(x => x.Id).ToList();

            IList<string> allowFiieldIds = await _context.MtdFormPartField.Where(x => partIds.Contains(x.MtdFormPart)).Select(x => x.Id).ToListAsync();
            fieldIds = allowFiieldIds.Where(x => fieldIds.Contains(x)).ToList();

            StackHandler handlerStack = new StackHandler(_context);
            IList<MtdStoreStack> mtdStoreStack = await handlerStack.GetStackAsync(storeIds, fieldIds);

            IList<MtdStoreApproval> mtdStoreApprovals = await _context.MtdStoreApproval.Where(x => storeIds.Contains(x.Id)).ToListAsync();
            List<ApprovalStore> approvalStores = await ApprovalHandler.GetStoreStatusAsync(_context, storeIds, user);

            MtdApproval mtdApproval = await _context.MtdApproval.Where(x => x.MtdForm == formId).FirstOrDefaultAsync();
            
            MtdFilter filter = await _context.MtdFilter.FirstOrDefaultAsync(x => x.IdUser == user.Id && x.MtdForm == formId);
            var mtdFormList = await ApprovalHandler.GetWaitStoreIds(_context, user, formId);
            int pending = mtdFormList.Count();

            List<MtdFormPartField> rowFields = incomer.FieldForColumn.Where(x => fieldIds.Contains(x.Id)).ToList();
         
            RowsModelView rowsModel = new RowsModelView
            {
                FormId = formId,
                SearchNumber = incomer.SearchNumber,
                PageCount = pageCount,
                MtdFormPartFields = rowFields,
                MtdStores = mtdStores,
                MtdStoreStack = mtdStoreStack,
                WaitList = incomer.WaitList == 1,
                ShowDate = await handlerFilter.IsShowDate(),
                ShowNumber = await handlerFilter.IsShowNumber(),
                ApprovalStores = approvalStores,
                MtdApproval = mtdApproval,
                StoreIds = string.Join("&", storeIds),
                SearchText = filter == null ? string.Empty : filter.SearchText,
                Pending = pending,
                IsCreator = await _userHandler.IsCreator(user, formId),
                PageSize = filter.PageSize,
                PageCurrent = filter.Page,                
            };

            return View("Default", rowsModel);
        }
    }
}
