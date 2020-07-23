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

using System;
using System.Collections.Generic;
using System.Composition;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Mtd.OrderMaker.Server;
using Mtd.OrderMaker.Server.AppConfig;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.EntityHandler.Filter;
using Mtd.OrderMaker.Server.EntityHandler.Stack;
using Mtd.OrderMaker.Server.Services;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Mtd.OrderMaker.Web.Controllers.Index
{

    [Route("api/action/index")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class ActionController : ControllerBase
    {

        private readonly OrderMakerContext _context;
        private readonly UserHandler _userHandler;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly LimitSettings limit;

        public ActionController(OrderMakerContext context, UserHandler userHandler, 
            IStringLocalizer<SharedResource> localizer, IOptions<LimitSettings> limit)
        {
            _context = context;
            _userHandler = userHandler;
            _localizer = localizer;
            this.limit = limit.Value;
        }

        [HttpPost("excel/export")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostExportAsync()
        {
            if (limit.ExportExcel == 0) { return NotFound(); }

            var form = await Request.ReadFormAsync();            
            string formId = form["form-id"];

            

            var user = await _userHandler.GetUserAsync(User);               
            List<MtdFormPart> partIds = await _userHandler.GetAllowPartsForView(user, formId);
            if (partIds == null) { return NotFound(); }

            FilterHandler handlerFilter = new FilterHandler(_context, formId, user, _userHandler);
            Incomer incomer = await handlerFilter.GetIncomerDataAsync();
          
            TypeQuery typeQuery = await handlerFilter.GetTypeQueryAsync(user);
            incomer.PageSize = limit.ExportSize;

            OutFlow outFlow = await handlerFilter.GetStackFlowAsync(incomer, typeQuery);

            if (outFlow.Count > limit.ExportSize) {

                string message = _localizer["Limit **** lines! Use a filter to shrink the list."];
                message = message.Replace("****", limit.ExportSize.ToString());
                return BadRequest(new JsonResult(message)); 
            }

            IList<MtdStore> mtdStore = outFlow.MtdStores;

            IList<string> storeIds = mtdStore.Select(s => s.Id).ToList();
            IList<string> fieldIds = incomer.FieldForColumn.Select(x => x.Id).ToList();

            IList<string> allowFiieldIds = await _context.MtdFormPartField.Where(x => partIds.Select(x => x.Id).Contains(x.MtdFormPart)).Select(x => x.Id).ToListAsync();
            fieldIds = allowFiieldIds.Where(x => fieldIds.Contains(x)).ToList();

            StackHandler handlerStack = new StackHandler(_context);
            IList<MtdStoreStack> mtdStoreStack = await handlerStack.GetStackAsync(storeIds, fieldIds);
            IList<MtdFormPartField> columns = incomer.FieldForColumn.Where(x => fieldIds.Contains(x.Id)).ToList();

            IWorkbook workbook = CreateWorkbook(mtdStore, columns, mtdStoreStack);
       
            var ms = new NpoiMemoryStream
            {
                AllowClose = false
            };
            workbook.Write(ms);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            ms.AllowClose = true;

            return new FileStreamResult(ms, new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
            {
                FileDownloadName = $"{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"
            };
        }

        private IWorkbook CreateWorkbook(IList<MtdStore> mtdStores, IList<MtdFormPartField> partFields, IList<MtdStoreStack> storeStack)
        {

            IWorkbook workbook = new XSSFWorkbook();

            ISheet sheet1 = workbook.CreateSheet("Sheet1");
            var rowIndex = 0;
            var colIndex = 0;
            IRow rowTitle = sheet1.CreateRow(rowIndex);
            rowTitle.CreateCell(0).SetCellValue("ID");
            rowTitle.CreateCell(1).SetCellValue(_localizer["Date"]);
            colIndex++;
            foreach (var field in partFields)
            {
                colIndex++;
                rowTitle.CreateCell(colIndex).SetCellValue(field.Name);
            }

            colIndex = 0;
            rowIndex++;
            foreach (var store in mtdStores)
            {
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(colIndex).SetCellValue(store.Sequence.ToString("D9"));
                colIndex++;
                row.CreateCell(colIndex).SetCellValue(store.Timecr.ToShortDateString());
                foreach (var field in partFields)
                {
                    colIndex++;
                    MtdStoreStack stack = storeStack.FirstOrDefault(x => x.MtdStore == store.Id && x.MtdFormPartField == field.Id);
                    ICell cell = row.CreateCell(colIndex);
                    SetValuefoCell(stack, field, cell);
                }
                colIndex = 0;
                rowIndex++;
            }

            sheet1.AutoSizeColumn(0);
            return workbook;
        }

        private void SetValuefoCell(MtdStoreStack stack, MtdFormPartField field, ICell cell)
        {

            switch (field.MtdSysType)
            {
                case 2:
                    {
                        int result = 0;
                        if (stack != null && stack.MtdStoreStackInt != null)
                        {
                            result = stack.MtdStoreStackInt.Register;
                        };

                        cell.SetCellType(CellType.Numeric);
                        cell.SetCellValue(result);
                        break;
                    }
                case 3:
                    {
                        double result = 0.00;
                        if (stack != null && stack.MtdStoreStackDecimal != null)
                        {
                            result = (double)stack.MtdStoreStackDecimal.Register;
                        }
                        cell.SetCellType(CellType.Numeric);
                        cell.SetCellValue(result);
                        break;
                    }
                case 5:
                    {

                        // cell.SetCellType(CellType.String);
                        bool check = false;
                        if (stack != null && stack.MtdStoreStackDate != null)
                        {
                            check = true;
                            cell.SetCellValue(stack.MtdStoreStackDate.Register.Date);
                        }
                        if (!check)
                        {
                            cell.SetCellValue(0);
                        }
                        break;
                    }
                case 6:
                    {
                        bool check = false;
                        if (stack != null && stack.MtdStoreStackDate != null)
                        {
                            check = true;
                            cell.SetCellValue(stack.MtdStoreStackDate.Register);
                        }
                        if (!check)
                        {
                            cell.SetCellValue(0);
                        }
                        break;
                    }
                case 10:
                    {
                        bool check = false;
                        if (stack != null && stack.MtdStoreStackDate != null)
                        {
                            check = true;
                            cell.SetCellValue(stack.MtdStoreStackDate.Register);
                        }
                        if (!check)
                        {
                            cell.SetCellValue(0);
                        }
                        break;
                    }
                case 11:
                    {
                        string result = "";
                        if (stack != null && stack.MtdStoreLink != null)
                        {
                            result = stack.MtdStoreLink.Register;
                        }
                        cell.SetCellType(CellType.String);
                        cell.SetCellValue(result);
                        break;
                    }
                case 12:
                    {
                        int result = 0;
                        if (stack != null && stack.MtdStoreStackInt != null)
                        {
                            result = stack.MtdStoreStackInt.Register;
                        }
                        cell.SetCellType(CellType.Boolean);
                        cell.SetCellValue(result);
                        break;
                    }
                default:
                    {
                        string result = "";
                        if (stack != null && stack.MtdStoreStackText != null)
                        {
                            result = stack.MtdStoreStackText.Register;
                        }
                        cell.SetCellValue(result);
                        break;
                    }
            }

        }

    }


    public class NpoiMemoryStream : MemoryStream
    {
        public NpoiMemoryStream()
        {
            // We always want to close streams by default to
            // force the developer to make the conscious decision
            // to disable it.  Then, they're more apt to remember
            // to re-enable it.  The last thing you want is to
            // enable memory leaks by default.  ;-)
            AllowClose = true;
        }

        public bool AllowClose { get; set; }

        public override void Close()
        {
            if (AllowClose)
                base.Close();
        }
    }
}