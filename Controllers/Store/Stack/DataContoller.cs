﻿/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Controllers.Store.Stack
{
    [Route("api/store/stack")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class DataContoller : ControllerBase
    {
        private readonly UserHandler _userHandler;
        private readonly OrderMakerContext _context;
        private readonly ILogger<DataContoller> _logger;

        public DataContoller(OrderMakerContext context, UserHandler userHandler, ILogger<DataContoller> logger)
        {
            _context = context;
            _userHandler = userHandler;
            _logger = logger;
        }

        // GET: api/MtdStoreStack/5/file/
        [HttpGet("{id}/file")]
        public async Task<ActionResult> GetStackFile(int id)
        {

            var mtdStoreStack = await _context.MtdStoreStack
                .Include(m => m.MtdStoreStackFile)
                .FirstOrDefaultAsync(x => x.Id == id);
            string message;

            var user = await _userHandler.GetUserAsync(HttpContext.User);

            if (mtdStoreStack == null || mtdStoreStack.MtdStoreStackFile == null)
            {
                message = $"Error request ID for file at {DateTime.Now.ToLongTimeString()}";
                _logger.LogWarning("Access file Control: User {0} {1} {2}", user.Title, id, message);
                return NotFound();
            }


            var store = await _context.MtdStore.FindAsync(mtdStoreStack.MtdStore);
            string partId = await _context.MtdFormPartField
                .Where(x => x.Id == mtdStoreStack.MtdFormPartField)
                .Select(x => x.MtdFormPart)
                .FirstOrDefaultAsync();

            bool isViewer = await _userHandler.IsViewer(user, store.MtdForm, store.Id);
            bool isViewerPart = await _userHandler.IsViewerPartAsync(user, partId);

            if (!isViewer || !isViewerPart)
            {
                message = $"Reject access for file at {DateTime.Now.ToLongTimeString()}";
                _logger.LogWarning("Access file Control: User {0} File {1} {2}", user.Title, id, message);
                return NotFound();
            }

            message = $"Successful access to file at {DateTime.Now.ToLongTimeString()}";
            _logger.LogWarning("Access file Control: User {0} File {1} {2}", user.Title, id, message);

            return new FileStreamResult(new MemoryStream(mtdStoreStack.MtdStoreStackFile.Register), mtdStoreStack.MtdStoreStackFile.FileType)
            {
                FileDownloadName = mtdStoreStack.MtdStoreStackFile.FileName
            };
        }
    }
}
