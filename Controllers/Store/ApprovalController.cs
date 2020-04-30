﻿/*
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Data;
using Mtd.OrderMaker.Server.DataHandler.Approval;
using Mtd.OrderMaker.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Mtd.OrderMaker.Server;

namespace Mtd.OrderMaker.Server.Controllers.Store
{

    [Route("api/store/approval")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class ApprovalController : ControllerBase
    {
        private readonly OrderMakerContext _context;
        private readonly UserHandler _userHandler;
        private readonly IEmailSenderBlank _emailSender;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ApprovalController(OrderMakerContext context, UserHandler userHandler, IEmailSenderBlank emailSender, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _context = context;
            _userHandler = userHandler;
            _emailSender = emailSender;
            _localizer = sharedLocalizer;
        }

        [HttpPost("start")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostStartAsync()
        {
            string storeId = Request.Form["id-store"];
            string resolutionId = Request.Form["id-resolution"];
            string comment = Request.Form["comment-start-id"];
            if (comment.Length == 0) { comment = null; }
            WebAppUser webAppUser = await _userHandler.GetUserAsync(HttpContext.User);
            ApprovalHandler approvalHandler = new ApprovalHandler(_context, storeId);
            bool isApprover = await approvalHandler.IsApproverAsync(webAppUser);
            bool isFirstStage = await approvalHandler.IsFirstStageAsync();
            if (!isApprover || !isFirstStage) { return NotFound(); }
            
            MtdApprovalStage stageNext = await approvalHandler.GetNextStageAsync();            
            bool sendEmail = stageNext.UserId != webAppUser.Id ? true : false;

            bool isOk = await approvalHandler.ActionApprove(webAppUser, resolutionId, comment);
            
            if (isOk && sendEmail)
            {
                await SendEmailStart(approvalHandler, comment);
            }
            return Ok();
        }

        [HttpPost("approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostApproveAsync()
        {
            string storeId = Request.Form["id-store"];
            string resolutionId = Request.Form["id-resolution"];
            string comment = Request.Form["comment-confirm-id"];
            if (comment.Length == 0) { comment = null; }

            WebAppUser webAppUser = await _userHandler.GetUserAsync(HttpContext.User);
            ApprovalHandler approvalHandler = new ApprovalHandler(_context, storeId);
            bool isApprover = await approvalHandler.IsApproverAsync(webAppUser);
            bool isSign = await approvalHandler.IsSignAsync();

            if (!isApprover || isSign) { return NotFound(); }

            bool isOk = await approvalHandler.ActionApprove(webAppUser, resolutionId, comment);
            if (isOk)
            {
                await SendEmailApprove(approvalHandler, comment);
            }
            return Ok();
        }

        [HttpPost("reject/{rtype}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRejectAsync(int rtype = 0)
        {
            string storeId = Request.Form["id-store"];
            string rejectionId = Request.Form["id-rejection"];
            string comment = Request.Form["comment-reject-id"];
            if (comment.Length == 0) { comment = null; }
            if (rtype == 1) { rejectionId = null; }

            bool completeOk = bool.TryParse(Request.Form["checkbox-complete"], out bool completeCheck);
            bool stageOk = int.TryParse(Request.Form["next-stage"], out int stageId);
            if (!stageOk || !completeOk) { return NotFound(); }

            WebAppUser webAppUser = await _userHandler.GetUserAsync(HttpContext.User);
            ApprovalHandler approvalHandler = new ApprovalHandler(_context, storeId);

            bool isFirstStage = await approvalHandler.IsFirstStageAsync();
            bool isApprover = await approvalHandler.IsApproverAsync(webAppUser);
            if (!isApprover || isFirstStage) { return NotFound(); }

            bool isOk = await approvalHandler.ActionReject(completeCheck, stageId, webAppUser, rejectionId, comment);
            if (isOk)
            {
                await SendEmailReject(approvalHandler, comment);
            }
            return Ok();
        }

        [HttpPost("restart")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostNewAsync()
        {
            string storeId = Request.Form["id-store"];
            string formId = Request.Form["id-form"];

            WebAppUser webAppUser = await _userHandler.GetUserAsync(HttpContext.User);
            ApprovalHandler approvalHandler = new ApprovalHandler(_context, storeId);
            bool isReviewer = await _userHandler.IsReviewer(webAppUser, formId);

            if (!isReviewer) { return NotFound(); }

            bool isOk = await approvalHandler.ActionApproveReset(webAppUser);
            if (isOk)
            {
                await SendEmailReStart(approvalHandler);
            }
            return Ok();
        }

        [HttpPost("request")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRequestAsync()
        {
            string storeId = Request.Form["id-store"];
            string formId = Request.Form["id-form"];
            string recipient = Request.Form["id-user-recipient"];
            string comment = Request.Form["comment-request-id"];

            WebAppUser userSender = await _userHandler.GetUserAsync(HttpContext.User);
            WebAppUser userRecepient = await _userHandler.FindByIdAsync(recipient);
            
            ApprovalHandler approvalHandler = new ApprovalHandler(_context, storeId);
            bool isApprover = await approvalHandler.IsApproverAsync(userSender);
            bool isViewer = await _userHandler.IsViewer(userRecepient, formId, storeId);

            bool SendEmail = false;
            if(isViewer && isApprover)
            {
                SendEmail = await approvalHandler.ActionRequest(userSender, userRecepient, comment);                
            }
            
            if (SendEmail)
            {
                await SendEmailRequest(approvalHandler, userRecepient, comment);
            }

            return Ok();
        }

        [HttpPost("response/{signtype}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostApproveSignAsync(int signtype = 0)
        {
            string storeId = Request.Form["id-store"];
            string comment = Request.Form["comment-request-id"];
            if (comment.Length == 0) { comment = null; }

            WebAppUser webAppUser = await _userHandler.GetUserAsync(HttpContext.User);
            ApprovalHandler approvalHandler = new ApprovalHandler(_context, storeId);
            bool isApprover = await approvalHandler.IsApproverAsync(webAppUser);
            bool isSign = await approvalHandler.IsSignAsync();

            if (!isApprover || !isSign) { return NotFound(); }

            bool isOk = await approvalHandler.ActionResponceSignAsync(webAppUser, comment, signtype);
            string recepientId = await approvalHandler.GetCurrentUserIdAsync();
            WebAppUser userRecipient = await _userHandler.FindByIdAsync(recepientId);


            if (isOk && userRecipient != null)
            {
                await SendEmailSignAsync(approvalHandler, userRecipient, comment);
            }

            return Ok();
        }

        private async Task<bool> SendEmailStart(ApprovalHandler approvalHandler, string comment = null)
        {

            string ownerId = await approvalHandler.GetOwnerID();
            WebAppUser userCurrent = await _userHandler.GetUserAsync(HttpContext.User);
            WebAppUser userOwner = _userHandler.Users.Where(x => x.Id == ownerId).FirstOrDefault();
            string storeId = await approvalHandler.GetStoreID();
            MtdForm mtdForm = await approvalHandler.GetFormAsync();

            MtdApprovalStage stageNext = await approvalHandler.GetNextStageAsync();

            if (await approvalHandler.IsFirstStageAsync())
            {
                WebAppUser userNext = _userHandler.Users.Where(x => x.Id == stageNext.UserId).FirstOrDefault();
                BlankEmail blankEmail = new BlankEmail
                {
                    Subject = _localizer["Approval event"],
                    Email = userNext.Email,
                    Header = _localizer["Approval required"],
                    Content = new List<string> {
                        $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                        $"{_localizer["User"]} {userCurrent.Title} {_localizer["started a new approval at"]} {DateTime.Now}"
                        }
                };

                if (comment != null)
                {
                    blankEmail.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
                }

                blankEmail.Content.Add($"{_localizer["Click on the link to view the document that required to approve."]}");
                blankEmail.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");

                await _emailSender.SendEmailBlankAsync(blankEmail);
            }

            return true;
        }

        private async Task<bool> SendEmailReStart(ApprovalHandler approvalHandler, string comment = null)
        {

            string ownerId = await approvalHandler.GetOwnerID();
            WebAppUser userCurrent = await _userHandler.GetUserAsync(HttpContext.User);
            WebAppUser userOwner = _userHandler.Users.Where(x => x.Id == ownerId).FirstOrDefault();
            string storeId = await approvalHandler.GetStoreID();
            MtdForm mtdForm = await approvalHandler.GetFormAsync();

            MtdApprovalStage stageNext = await approvalHandler.GetNextStageAsync();

            BlankEmail blankEmail = new BlankEmail
            {
                Subject = _localizer["Approval event"],
                Email = userOwner.Email,
                Header = _localizer["Approval process event"],
                Content = new List<string> {
                              $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                              $"{_localizer["User"]} {userCurrent.Title} {_localizer["restarted approval workflow at"]} {DateTime.Now}"
                              }
            };

            if (comment != null)
            {
                blankEmail.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
            }

            blankEmail.Content.Add($"{_localizer["Click on the link to view the document and start new approval."]}");
            blankEmail.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");
            await _emailSender.SendEmailBlankAsync(blankEmail);

            return true;
        }

        private async Task<bool> SendEmailApprove(ApprovalHandler approvalHandler, string comment = null)
        {

            string ownerId = await approvalHandler.GetOwnerID();
            WebAppUser userCurrent = await _userHandler.GetUserAsync(HttpContext.User);
            WebAppUser userOwner = _userHandler.Users.Where(x => x.Id == ownerId).FirstOrDefault();
            string storeId = await approvalHandler.GetStoreID();
            MtdForm mtdForm = await approvalHandler.GetFormAsync();

            MtdApprovalStage stageNext = await approvalHandler.GetNextStageAsync();

            if (stageNext != null)
            {
                WebAppUser userNext = _userHandler.Users.Where(x => x.Id == stageNext.UserId).FirstOrDefault();
                BlankEmail blankEmail = new BlankEmail
                {
                    Subject = _localizer["Approval event"],
                    Email = userNext.Email,
                    Header = _localizer["Approval required"],
                    Content = new List<string> {
                        $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                        $"{_localizer["User"]} {userCurrent.Title} {_localizer["approved the document at"]} {DateTime.Now}"
                        }
                };

                if (comment != null)
                {
                    blankEmail.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
                }

                blankEmail.Content.Add($"{_localizer["Click on the link to view the document that required to approve."]}");
                blankEmail.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");

                await _emailSender.SendEmailBlankAsync(blankEmail);
            }
            else
            {
                BlankEmail blankEmail = new BlankEmail
                {
                    Subject = _localizer["Approval event"],
                    Email = userOwner.Email,
                    Header = _localizer["Approval process event"],
                    Content = new List<string> {
                              $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                              $"{_localizer["User"]} {userCurrent.Title} {_localizer["approved the document at"]} {DateTime.Now}"
                              }
                };

                if (comment != null)
                {
                    blankEmail.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
                }

                blankEmail.Content.Add($"{_localizer["Approval process is complete. Click on the link to view the document."]}");
                blankEmail.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");

                await _emailSender.SendEmailBlankAsync(blankEmail);
            }

            return true;
        }

        private async Task<bool> SendEmailReject(ApprovalHandler approvalHandler, string comment = null)
        {
            string ownerId = await approvalHandler.GetOwnerID();
            WebAppUser userCurrent = await _userHandler.GetUserAsync(HttpContext.User);
            WebAppUser userOwner = _userHandler.Users.Where(x => x.Id == ownerId).FirstOrDefault();
            MtdForm mtdForm = await approvalHandler.GetFormAsync();
            string storeId = await approvalHandler.GetStoreID();

            MtdApprovalStage stagePrev = await approvalHandler.GetPrevStage();
            MtdApprovalStage stageFirst = await approvalHandler.GetFirstStageAsync();

            bool cacheReload = false;

            if (stagePrev != null)
            {
                WebAppUser user = userOwner;
                if (stagePrev.UserId != "owner")
                {
                    user = _userHandler.Users.Where(x => x.Id == stagePrev.UserId).FirstOrDefault();
                }

                BlankEmail blankEmail = new BlankEmail
                {
                    Subject = _localizer["Approval event"],
                    Email = user.Email,
                    Header = _localizer["Approval required"],
                    Content = new List<string> {
                        $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                        $"{_localizer["User"]} {userCurrent.Title} {_localizer["rejected the document at"]} {DateTime.Now}"
                        }
                };

                if (comment != null)
                {
                    blankEmail.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
                }

                blankEmail.Content.Add($"{_localizer["Click on the link to view the document that required to approve."]}");
                blankEmail.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");

                approvalHandler.ClearCache();
                cacheReload = true;

                if (!await approvalHandler.IsComplete())
                {
                    await _emailSender.SendEmailBlankAsync(blankEmail);
                }

            }

            if (!cacheReload)
            {
                approvalHandler.ClearCache();
            }

            if (await approvalHandler.IsComplete())
            {
                BlankEmail blankOwner = new BlankEmail
                {
                    Subject = _localizer["Approval event"],
                    Email = userOwner.Email,
                    Header = _localizer["Approval process event"],
                    Content = new List<string> {
                        $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                        $"{_localizer["User"]} {userCurrent.Title} {_localizer["rejected the document at"]} {DateTime.Now}"
                        }
                };

                if (comment != null)
                {
                    blankOwner.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
                }

                blankOwner.Content.Add($"{_localizer["Approval process is complete. Click on the link to view the document."]}");
                blankOwner.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");

                await _emailSender.SendEmailBlankAsync(blankOwner);
            }

            return true;
        }

        private async Task<bool> SendEmailRequest(ApprovalHandler approvalHandler, WebAppUser userRecipient, string comment = null)
        {

            WebAppUser userCurrent = await _userHandler.GetUserAsync(HttpContext.User);
            string storeId = await approvalHandler.GetStoreID();
            MtdForm mtdForm = await approvalHandler.GetFormAsync();

            BlankEmail blankEmail = new BlankEmail
            {
                Subject = _localizer["Approval event"],
                Email = userRecipient.Email,
                Header = _localizer["Approval process event - signature request"],
                Content = new List<string> {
                              $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                              $"{_localizer["User"]} {userCurrent.Title} {_localizer["requested a signature to approve the document at"]} {DateTime.Now}"
                              }
            };

            if (comment != null)
            {
                blankEmail.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
            }

            blankEmail.Content.Add($"{_localizer["Click on the link to view the document."]}");
            blankEmail.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");
            await _emailSender.SendEmailBlankAsync(blankEmail);

            return true;
        }

        private async Task<bool> SendEmailSignAsync(ApprovalHandler approvalHandler, WebAppUser userRecipient, string comment = null)
        {

            WebAppUser userCurrent = await _userHandler.GetUserAsync(HttpContext.User);
            string storeId = await approvalHandler.GetStoreID();
            MtdForm mtdForm = await approvalHandler.GetFormAsync();

            BlankEmail blankEmail = new BlankEmail
            {
                Subject = _localizer["Approval event"],
                Email = userRecipient.Email,
                Header = _localizer["Approval process event - answer to request"],
                Content = new List<string> {
                              $"<strong>{_localizer["Document"]} - {mtdForm.Name}</strong>",
                              $"{_localizer["User"]} {userCurrent.Title} {_localizer["answered to request signature document at"]} {DateTime.Now}"
                              }
            };

            if (comment != null)
            {
                blankEmail.Content.Add($"{ _localizer["User's comment"]}: <em>{comment}</em>");
            }

            blankEmail.Content.Add($"{_localizer["Click on the link to view the document."]}");
            blankEmail.Content.Add($"<a href='{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/workplace/store/details?id={storeId}'>{_localizer["Document link"]}</a>");

            await _emailSender.SendEmailBlankAsync(blankEmail);

            return true;
        }

    }
}
