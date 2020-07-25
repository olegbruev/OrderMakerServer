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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.EntityHandler.Approval;
using Mtd.OrderMaker.Server.Models.Store;
using Mtd.OrderMaker.Server.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Components.Store
{
    public enum FormType { Create, Edit, Details, Print }

    [ViewComponent(Name = "StoreForm")]
    public class Form : ViewComponent
    {
        private readonly OrderMakerContext _context;
        private readonly UserHandler _userHandler;

        public Form(OrderMakerContext orderMakerContext, UserHandler userHandler)
        {
            _context = orderMakerContext;
            _userHandler = userHandler;
        }

        private async Task<IList<MtdFormPart>> GetPartsAsync(string formId)
        {
            IList<MtdFormPart> result = await _context.MtdFormPart
                .Include(m => m.MtdFormPartHeader)
                .Where(x => x.MtdForm == formId)
                .OrderBy(s => s.Sequence)
                .ToListAsync();

            return result;
        }

        private async Task<IList<MtdFormPartField>> GetFieldsAsync(IList<MtdFormPart> formParts)
        {
            IList<string> partIds = formParts.Select(x => x.Id).ToList();

            return await _context.MtdFormPartField
                .Include(x => x.MtdFormList)
                .Where(x => partIds.Contains(x.MtdFormPart))
                .OrderBy(s => s.Sequence)
                .ToListAsync();
        }

        private async Task<Warehouse> CreateWarehouseAsync(MtdStore store, FormType type = FormType.Details)
        {
            if (store == null) return null;
            WebAppUser webAppUser = await _userHandler.GetUserAsync(HttpContext.User);
            List<MtdFormPart> mtdFormParts = new List<MtdFormPart>();
            IList<MtdFormPart> parts = await GetPartsAsync(store.MtdForm);
            bool isReviewer = await _userHandler.IsReviewer(webAppUser, store.MtdForm);
            ApprovalHandler approvalHandler = new ApprovalHandler(_context, store.Id);
            List<string> blockedParts = new List<string>();
            if (!isReviewer)
            {
                blockedParts = await approvalHandler.GetBlockedPartsIds();
            }

            foreach (MtdFormPart formPart in parts)
            {
                if (type == FormType.Edit)
                {
                    bool isEditor = await _userHandler.IsEditorPartAsync(webAppUser, formPart.Id);
                    if (isEditor && !blockedParts.Contains(formPart.Id)) { mtdFormParts.Add(formPart); }
                }
                else
                {
                    bool isViewer = await _userHandler.IsViewerPartAsync(webAppUser, formPart.Id);
                    if (isViewer) { mtdFormParts.Add(formPart); }
                }
            }

            IList<MtdFormPartField> mtdFormPartFields = await GetFieldsAsync(mtdFormParts);

            var mtdStore = await _context.MtdStore
                .Include(m => m.ParentNavigation)
                .Include(m => m.MtdFormNavigation)
                .ThenInclude(m => m.MtdFormHeader)
                .Include(m => m.MtdFormNavigation)
                .ThenInclude(m => m.ParentNavigation)
                .FirstOrDefaultAsync(m => m.Id == store.Id);

            IList<long> ids = await _context.MtdStoreStack.Where(x => x.MtdStore == mtdStore.Id).Select(x => x.Id).ToListAsync();

            IList<MtdStoreStack> stack = await _context.MtdStoreStack
                .Include(m => m.MtdStoreStackText)
                .Include(m => m.MtdStoreStackDecimal)
                .Include(m => m.MtdStoreStackFile)
                .Include(m => m.MtdStoreStackDate)
                .Include(m => m.MtdStoreStackInt)
                .Include(m => m.MtdStoreLink)
               .Where(x => ids.Contains(x.Id))
               .ToListAsync();

            if (type == FormType.Details || type == FormType.Print)
            {
                var listIds = stack.Where(x => x.MtdFormPartFieldNavigation != null)
                    .Select(x => x.MtdFormPartFieldNavigation.MtdFormPart)
                    .GroupBy(x => x).Select(x => x.Key).ToList();

                mtdFormParts = mtdFormParts.Where(x => listIds.Contains(x.Id)).ToList();

                List<MtdFormPart> nullParts = new List<MtdFormPart>();
                List<MtdFormPartField> nullFields = new List<MtdFormPartField>();

                foreach (MtdFormPart part in mtdFormParts)
                {
                    var fields = mtdFormPartFields.Where(x => x.MtdFormPart == part.Id).ToList();
                    int counter = 0;
                    foreach (var field in fields)
                    {
                        var stackField = stack.Where(x => x.MtdFormPartField == field.Id).FirstOrDefault();
                        if (stackField == null || (stackField.MtdStoreLink == null
                             & stackField.MtdStoreStackDate == null
                             & stackField.MtdStoreStackDecimal == null
                             & stackField.MtdStoreStackFile == null
                             & stackField.MtdStoreStackInt == null
                             & stackField.MtdStoreStackText == null)) { counter++; nullFields.Add(field); }
                    }

                    if (counter == fields.Count)
                    {
                        nullParts.Add(part);                        
                    }
                }

                foreach (MtdFormPart nullPart in nullParts)
                {
                    mtdFormParts.Remove(nullPart);
                    foreach (var field in nullFields.Where(x => x.MtdFormPart == nullPart.Id))
                    {
                        mtdFormPartFields.Remove(field);
                    }
                }
            }
            
            Warehouse result = new Warehouse()
            {
                Store = mtdStore,
                Parts = mtdFormParts,
                Fields = mtdFormPartFields,
                Stack = stack,
                SetDate = await _userHandler.CheckUserPolicyAsync(webAppUser, store.MtdForm, RightsType.SetDate)                
            };

            return result;
        }

        public async Task<IViewComponentResult> InvokeAsync(MtdStore store, FormType type = FormType.Details)
        {
            if (store == null)
            {
                return View();
            }

            WebAppUser webAppUser = await _userHandler.GetUserAsync(HttpContext.User);

            if (type == FormType.Create)
            {
                store.MtdFormNavigation.MtdFormHeader = await _context.MtdFormHeader.FindAsync(store.MtdForm);
                store.MtdFormNavigation.ParentNavigation = await _context.MtdForm.FindAsync(store.MtdFormNavigation.Parent);

                List<MtdFormPart> mtdFormParts = new List<MtdFormPart>();
                IList<MtdFormPart> parts = await GetPartsAsync(store.MtdForm);

                foreach (MtdFormPart formPart in parts)
                {
                    bool isCreator = await _userHandler.IsCreatorPartAsync(webAppUser, formPart.Id);
                    if (isCreator) { mtdFormParts.Add(formPart); }
                }

                IList<MtdFormPartField> mtdFormPartFields = await GetFieldsAsync(mtdFormParts);


                Warehouse warehouse = new Warehouse()
                {
                    Store = store,
                    Parts = mtdFormParts,
                    Fields = mtdFormPartFields,
                    Stack = new List<MtdStoreStack>(),
                    SetDate = await _userHandler.CheckUserPolicyAsync(webAppUser, store.MtdForm, RightsType.SetDate)
                };

                return View("Create", warehouse);
            }

            
            Warehouse dataMain = await CreateWarehouseAsync(store, type);

            List<DataLink> dataLinks = new List<DataLink>();

            List<MtdFormPartField> fields = dataMain.Fields.Where(x => x.MtdSysType == 11).ToList();
            if (fields != null && fields.Count > 0)
            {         
                foreach (var field in fields)
                {
                    MtdStoreStack stack = dataMain.Stack.Where(z => z.MtdStore == store.Id && z.MtdFormPartField == field.Id).FirstOrDefault();
                    string storeId = stack.MtdStoreLink.MtdStore;
                    MtdStore storeLink = await _context.MtdStore.FindAsync(storeId);
                    Warehouse dataLink = await CreateWarehouseAsync(storeLink, type);
                    dataLinks.Add(new DataLink { PartId = field.MtdFormPartNavigation.Id, Data = dataLink });
                }
            }

            DataContainer dataContainer = new DataContainer
            {
                Owner = dataMain,
                Parent = await CreateWarehouseAsync(store.ParentNavigation),
                Links = dataLinks
            };

            return View(type.ToString(), dataContainer);
        }
    }
}
