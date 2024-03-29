﻿/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Mtd.Cpq.Manager.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.Extensions;
using Mtd.OrderMaker.Server.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Controllers.Users
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public partial class UsersController : ControllerBase
    {
        private readonly UserHandler _userManager;
        private readonly RoleManager<WebAppRole> _roleManager;
        private readonly IEmailSenderBlank _emailSender;
        private readonly OrderMakerContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IdentityDbContext identity;


        public UsersController(
            UserHandler userManager,
            RoleManager<WebAppRole> roleManager,
            IEmailSenderBlank emailSender,
            OrderMakerContext context,
            IStringLocalizer<SharedResource> localizer, IdentityDbContext identity
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _context = context;
            _localizer = localizer;
            this.identity = identity;
        }

        [HttpGet("admin/user/{userId}")]
        public async Task<IActionResult> OnPostGetUserAsync(string userId)
        {
            WebAppUser user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return BadRequest(_localizer["ERROR! User not found."]);
            }

            return new JsonResult(new { user.Id, titleName = user.Title, titleGroup = user.TitleGroup, fullName = user.GetFullName() });
        }

        [HttpPost("admin/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAdminDeleteAsync()
        {
            string userId = Request.Form["user-delete-id"];
            WebAppUser user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return BadRequest(_localizer["ERROR! User not found."]);
            }

            bool isApprover = await _context.MtdApprovalStage.Where(x => x.UserId == user.Id).AnyAsync();
            bool isOwner = await _context.MtdStoreOwner.Where(x => x.UserId == user.Id).AnyAsync();
            bool isCpqTitlesOwner = await identity.MtdCpqTitlesOwners.Where(x => x.UserId == user.Id).AnyAsync();
            bool isCpqQuoteOwner = await identity.MtdCpqProposalOwners.Where(x => x.UserId == user.Id).AnyAsync();

            if (isApprover || isOwner || isCpqQuoteOwner || isCpqTitlesOwner)
            {
                return BadRequest(_localizer["ERROR! There are documents owned by the user. Before deleting, transfer of documents to another user."]);
            }

            IList<MtdFilter> mtdFilters = await _context.MtdFilter.Where(x => x.IdUser == user.Id).ToListAsync();
            _context.MtdFilter.RemoveRange(mtdFilters);
            await _context.SaveChangesAsync();
            await _userManager.DeleteAsync(user);

            return Ok();
        }

        [HttpPost("admin/profile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAdminProfileAsync()
        {

            var form = await HttpContext.Request.ReadFormAsync();

            string username = form["UserName"];

            if (username == null)
            {
                return BadRequest(_localizer["Error. User name is null."]);
            }

            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return BadRequest(_localizer["Error. User not found."]);
            }

            string email = form["Input.Email"];
            string title = form["Input.Title"];
            string titleGroup = form["Input.TitleGroup"];
            string phone = form["Input.PhoneNumber"];
            string roleId = form["Input.Role"];
            string roleCpqId = form["Input.RoleCpq"];
            string policyId = form["Input.Policy"];
            string cpqViewAll = form["cpq-view-all"];
            string cpqViewGroup = form["cpq-view-group"];
            // string cpqViewOwn = form["cpq-view-own"];
            string cpqEditAll = form["cpq-edit-all"];
            string cpqEditGroup = form["cpq-edit-group"];
            string cpqPrintGrossPrice = form["cpq-print-gross-price"];

            string groupContainer = form["groupContainer"];

            WebAppRole roleUser = await _roleManager.FindByIdAsync(roleId);
            WebAppRole roleCpq = await _roleManager.FindByIdAsync(roleCpqId);

            string[] formConfirm = form["Input.IsConfirm"];
            bool isConfirm = false;
            if (formConfirm.FirstOrDefault() != null)
            {
                isConfirm = bool.Parse(formConfirm.FirstOrDefault());
            }

            if (user.Email != email)
            {
                user.Email = email;
                user.EmailConfirmed = false;
            }

            if (user.Title != title)
            {
                user.Title = title;
            }

            if (user.TitleGroup != titleGroup)
            {
                user.TitleGroup = titleGroup;
            }

            if (user.PhoneNumber != phone)
            {
                user.PhoneNumber = phone;
            }

            if (isConfirm)
            {
                user.EmailConfirmed = true;
            }

            await _userManager.UpdateAsync(user);
            IList<string> roles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, roles);
            await _userManager.AddToRoleAsync(user, roleUser.Name);
            await _userManager.AddToRoleAsync(user, roleCpq.Name);

            IEnumerable<Claim> claims = await _userManager.GetClaimsAsync(user);
            await _userManager.RemoveClaimsAsync(user, claims);

            Claim claim = new Claim("policy", policyId);
            await _userManager.AddClaimAsync(user, claim);

            //IList<MtdGroup> groups = await _context.MtdGroup.ToListAsync();
            //foreach (var group in groups)
            //{
            //    string value = form[$"{group.Id}-group"];
            //    if (value == "true")
            //    {
            //        Claim claimGroup = new Claim("group", group.Id);
            //        await _userManager.AddClaimAsync(user, claimGroup);
            //    }
            //}

            List<string> groupIds = groupContainer.Split("&").Where(x => x != "").ToList();
            foreach (var groupId in groupIds)
            {
                Claim claimGroup = new Claim("group", groupId);
                await _userManager.AddClaimAsync(user, claimGroup);
            }

            string cpqPolicy = "view-own";
            if (cpqViewAll == "true") { cpqPolicy = "view-all"; }
            if (cpqViewGroup == "true") { cpqPolicy = "view-group"; }
            string cpqPolicyEdit = "edit-own";
            if (cpqEditAll == "true") { cpqPolicyEdit = "edit-all"; }
            if (cpqEditGroup == "true") { cpqPolicyEdit = "edit-group"; }
            cpqPolicy += cpqPolicyEdit;
            string cpqPolicyPGP = "";
            if (cpqPrintGrossPrice == "true") { cpqPolicyPGP = "print-gross-price"; }
            cpqPolicy += cpqPolicyPGP;

            Claim cpqClaim = new Claim("cpq-policy", cpqPolicy);
            await _userManager.AddClaimAsync(user, cpqClaim);

            await _userManager.AddClaimAsync(user, new Claim("revoke", "false"));

            return Ok();
        }

        [HttpPost("admin/transfer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAdminTranferAsync()
        {
            string UserOwner = Request.Form["user-owner-id"];
            string UserRecipient = Request.Form["user-recipient-id"];

            WebAppUser userOwner = await _userManager.FindByIdAsync(UserOwner);
            WebAppUser userRecipient = await _userManager.FindByIdAsync(UserRecipient);

            if (userOwner == null || userRecipient == null)
            {
                return BadRequest(_localizer["ERROR! User not found."]);
            }

            IList<MtdStoreOwner> storeOwners = await _context.MtdStoreOwner.Where(x => x.UserId == userOwner.Id).ToListAsync();
            foreach (MtdStoreOwner owner in storeOwners)
            {
                owner.UserId = userRecipient.Id;
                owner.UserName = userRecipient.Title;
            }

            IList<MtdApprovalStage> stages = await _context.MtdApprovalStage.Where(x => x.UserId == userOwner.Id).ToListAsync();
            foreach (MtdApprovalStage stage in stages)
            {
                stage.UserId = userRecipient.Id;
            }

            IList<MtdCpqTitlesOwner> titlesOwners = await identity.MtdCpqTitlesOwners.Where(x => x.UserId == userOwner.Id).ToListAsync();
            foreach (MtdCpqTitlesOwner owner in titlesOwners)
            {
                owner.UserId = userRecipient.Id;
                owner.UserName = userRecipient.Title;
            }

            IList<MtdCpqProposalOwner> quoteOwners = await identity.MtdCpqProposalOwners.Where(x => x.UserId == userOwner.Id).ToListAsync();
            foreach (MtdCpqProposalOwner owner in quoteOwners)
            {
                owner.UserId = userRecipient.Id;
                owner.UserName = userRecipient.Title;
            }

            identity.MtdCpqProposalOwners.UpdateRange(quoteOwners);
            identity.MtdCpqTitlesOwners.UpdateRange(titlesOwners);
            _context.MtdStoreOwner.UpdateRange(storeOwners);
            _context.MtdApprovalStage.UpdateRange(stages);
            await _context.SaveChangesAsync();

            return Ok();
        }

    }
}