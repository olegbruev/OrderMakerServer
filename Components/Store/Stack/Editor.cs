﻿/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.EntityHandler.Approval;
using Mtd.OrderMaker.Server.Models.Controls.MTDSelectList;
using Mtd.OrderMaker.Server.Models.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Components.Store.Stack
{
    [ViewComponent(Name = "StoreStackEditor")]
    public class Editor : ViewComponent
    {
        private readonly OrderMakerContext _context;

        public Editor(OrderMakerContext orderMakerContext)
        {
            _context = orderMakerContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(MtdFormPartField field, Warehouse warehouse)
        {

            MtdStoreStack mtdStoreStack = warehouse.Stack.Where(x => x.MtdFormPartField == field.Id).FirstOrDefault();

            if (mtdStoreStack == null)
            {
                mtdStoreStack = new MtdStoreStack()
                {
                    MtdFormPartField = field.Id,
                    MtdFormPartFieldNavigation = field,
                    MtdStore = warehouse.Store.Id
                };
            }

            CheckStackForNull(mtdStoreStack);

            if (field.MtdSysType == 11)
            {
                var fieldForList = await _context.MtdFormPartField.Include(m => m.MtdFormPartNavigation)
                        .Where(x => x.MtdFormPartNavigation.MtdForm == field.MtdFormList.MtdForm & x.MtdSysType == 1)
                        .OrderBy(o => o.MtdFormPartNavigation.Sequence).ThenBy(o => o.Sequence).FirstOrDefaultAsync();

                IList<long> stackIds = await _context.MtdStoreStack.Where(x => x.MtdFormPartField == fieldForList.Id).Select(x => x.Id).ToListAsync();

                var dataList = await _context.MtdStoreStack
                    .Include(m => m.MtdStoreStackText)
                    .Where(x => stackIds.Contains(x.Id))
                    .Select(x => new { Id = x.MtdStore, Name = x.MtdStoreStackText.Register })
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                string idSelected = null;
                if (mtdStoreStack.MtdStoreLink != null) { idSelected = mtdStoreStack.MtdStoreLink.MtdStore; }

                var items = new List<MTDSelectListItem>();
                dataList.ToList().ForEach((item) =>
                {
                    items.Add(new MTDSelectListItem { Id = item.Id, Value = item.Name, Selectded = item.Id == idSelected });
                });

                // ViewData[field.Id] = new SelectList(dataList, "Id", "Name", idSelected);
                ViewData[field.Id] = items;
            }

            ViewData["TypeStyle"] = field.MtdFormPartNavigation.MtdSysStyle == 5 ? "Columns" : "Rows";

            string viewName = GetViewName(field.MtdSysType, warehouse.Parts.FirstOrDefault().MtdSysStyle, mtdStoreStack, field);

            return View(viewName, mtdStoreStack);
        }


        private string GetViewName(int type, int style, MtdStoreStack mtdStoreStack, MtdFormPartField field)
        {
            bool isOk;
            string viewName;

            switch (type)
            {
                case 2:
                    {
                        viewName = "Integer";
                        if (mtdStoreStack.MtdStoreStackInt.Register > 0) { break; }
                        isOk = int.TryParse(field.DefaultData, out int result);
                        if (isOk)
                        {
                            mtdStoreStack.MtdStoreStackInt.Register = result;
                        }
                        break;
                    }
                case 3:
                    {
                        viewName = "Decimal";
                        if (mtdStoreStack.MtdStoreStackDecimal.Register > 0) { break; }

                        isOk = decimal.TryParse(field.DefaultData, out decimal result);
                        if (isOk)
                        {
                            mtdStoreStack.MtdStoreStackDecimal.Register = result;
                        }
                        break;
                    }
                case 4:
                    {
                        viewName = "Memo";
                        if (mtdStoreStack.MtdStoreStackText.Register != null) { break; }
                        mtdStoreStack.MtdStoreStackText.Register = field.DefaultData;
                        break;
                    }
                case 5:
                    {
                        viewName = "Date";

                        isOk = DateTime.TryParse(field.DefaultData, out DateTime dateTime);
                        if (isOk)
                        {
                            mtdStoreStack.MtdStoreStackDate.Register = dateTime;
                        }
                        break;
                    }
                case 6:
                    {
                        viewName = "DateTime";

                        isOk = DateTime.TryParse(field.DefaultData, out DateTime dateTime);
                        if (isOk)
                        {
                            mtdStoreStack.MtdStoreStackDate.Register = dateTime;
                        }
                        break;
                    }
                case 7:
                case 8: { viewName = style == 5 ? "FileColumn" : "FileRow"; break; }
                case 11: { viewName = "ListForm"; break; }
                case 12:
                    {
                        viewName = "CheckBox";
                        if (mtdStoreStack.MtdStoreStackInt.Register > 0) { break; }
                        isOk = int.TryParse(field.DefaultData, out int result);
                        if (isOk)
                        {
                            mtdStoreStack.MtdStoreStackInt.Register = result;
                        }
                        break;
                    }
                case 13:
                    {
                        viewName = "Link";
                        if (mtdStoreStack.MtdStoreStackText.Register != null) { break; }
                        mtdStoreStack.MtdStoreStackText.Register = field.DefaultData;
                        break;                        
                    }
                default:
                    {
                        viewName = "Text";
                        if (mtdStoreStack.MtdStoreStackText.Register != null) { break; }
                        mtdStoreStack.MtdStoreStackText.Register = field.DefaultData;
                        break;
                    }
            };

            return viewName;
        }

        private void CheckStackForNull(MtdStoreStack mtdStoreStack)
        {

            if (mtdStoreStack.MtdStoreStackDate == null) { mtdStoreStack.MtdStoreStackDate = new MtdStoreStackDate(); }
            if (mtdStoreStack.MtdStoreStackDecimal == null) { mtdStoreStack.MtdStoreStackDecimal = new MtdStoreStackDecimal(); }
            if (mtdStoreStack.MtdStoreStackFile == null) { mtdStoreStack.MtdStoreStackFile = new MtdStoreStackFile(); }
            if (mtdStoreStack.MtdStoreStackInt == null) { mtdStoreStack.MtdStoreStackInt = new MtdStoreStackInt(); }
            if (mtdStoreStack.MtdStoreStackText == null) { mtdStoreStack.MtdStoreStackText = new MtdStoreStackText(); }
        }
    }
}
