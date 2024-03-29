/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mtd.OrderMaker.Server.AppConfig;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.Extensions;
using Mtd.OrderMaker.Server.Models.Controls.MTDSelectList;
using Mtd.OrderMaker.Server.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Areas.Identity.Pages.Users.Accounts
{

    public class GroupModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
    }

    public partial class EditModel : PageModel
    {

        private readonly UserHandler _userManager;
        private readonly RoleManager<WebAppRole> _roleManager;
        private readonly OrderMakerContext _context;
        private readonly IOptions<ConfigSettings> options;

        public EditModel(
            IOptions<ConfigSettings> options,
            UserHandler userManager,
            RoleManager<WebAppRole> roleManager,
            OrderMakerContext context
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            this.options = options;
        }


        public string UserName { get; set; }
        //public string Role { get; set; }
        public IList<MtdGroup> MtdGroups { get; set; }
        public InputModel Input { get; set; }
        public List<string> GroupIds { get; set; }
        public bool CpqModule { get; set; }

        public List<GroupModel> SelectedGroups { get; set; }
        public string SelectedGroupIds { get; set; }
        public string SelectedChildIds { get; set; }

        public List<MTDSelectListItem> GroupList { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Full name")]
            public string Title { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Confirm")]
            public bool IsConfirm { get; set; }

            public string Role { get; set; }
            public string RoleCpq { get; set; }
            public string Policy { get; set; }
            public string TitleGroup { get; set; }
            public string CpqPolicyView { get; set; }
            public string CpqPolicyEdit { get; set; }
            /*PRINT GROSS PRICE*/
            public string CpqPolicyPGP { get; set; }
        }

        public List<MTDSelectListItem> Roles { get; set; }
        public List<MTDSelectListItem> RolesCpq { get; set; }
        public List<MTDSelectListItem> Policies { get; set; }
        public List<MTDSelectListItem> Users { get; set; }
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { return NotFound(); }
            UserName = user.UserName;
            IList<WebAppRole> roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRoleName = userRoles.Where(x => !x.ToUpper().Contains("CPQ-")).FirstOrDefault();
            var userRoleNameCpq = userRoles.Where(x => x.ToUpper().Contains("CPQ-")).FirstOrDefault();
            var userRole = await _roleManager.FindByNameAsync(userRoleName);
            var userRoleCpq = await _roleManager.FindByNameAsync(userRoleNameCpq ?? "cpq-guest");

            Input = new InputModel
            {
                Role = userRole.Id,
                RoleCpq = userRoleCpq.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Title = user.Title,
                IsConfirm = user.EmailConfirmed,
                TitleGroup = user.TitleGroup
            };

            Roles = new List<MTDSelectListItem>();
            roles.Where(x => !x.NormalizedName.Contains("CPQ-")).OrderBy(x => x.Seq).ToList().ForEach((item) =>
            {
                Roles.Add(new MTDSelectListItem { Id = item.Id, Value = item.Title });
            });

            RolesCpq = new List<MTDSelectListItem>();
            roles.Where(x => x.NormalizedName.Contains("CPQ-")).OrderBy(x => x.Seq).ToList().ForEach((item) =>
            {
                RolesCpq.Add(new MTDSelectListItem { Id = item.Id, Value = item.Title });
            });

            //ViewData["Roles"] = new SelectList(roles.Where(x=>!x.NormalizedName.Contains("CPQ-")).OrderBy(x=>x.Seq), "Id", "Title", Input.Role);
            //ViewData["RolesCPQ"] = new SelectList(roles.Where(x =>x.NormalizedName.Contains("CPQ-")).OrderBy(x => x.Seq), "Id", "Title", Input.RoleCpq);

            Users = new List<MTDSelectListItem>();
            var users = await _userManager.Users.OrderBy(x => x.Title).ToListAsync();
            users.ForEach((item) =>
            {
                Users.Add(new MTDSelectListItem { Id = item.Id, Value = item.GetFullName() });
            });
            //ViewData["Users"] = new SelectList(_userManager.Users.OrderBy(x => x.Title), "Id", "Title", Input.Role);

            string policyID = await _userManager.GetPolicyIdAsync(user);
            Input.Policy = policyID;
            IList<MtdPolicy> mtdPolicy = await _userManager.CacheGetOrCreateAsync();

            Policies = new List<MTDSelectListItem>();
            mtdPolicy.OrderBy(x => x.Name).ToList().ForEach((item) =>
            {
                Policies.Add(new MTDSelectListItem { Id = item.Id, Value = item.Name });
            });

            //ViewData["Policies"] = new SelectList(mtdPolicy.OrderBy(x=>x.Name), "Id", "Name", policyID);

            MtdGroups = await _context.MtdGroup.OrderBy(x => x.Name).ToListAsync();

            IList<Claim> claims = await _userManager.GetClaimsAsync(user);
            GroupIds = new List<string>();
            GroupIds = claims.Where(x => x.Type == "group").Select(x => x.Value).ToList();

            IList<MtdGroup> groups = await _context.MtdGroup.OrderBy(x => x.Name).ToListAsync();
            GroupList = new List<MTDSelectListItem>()
            {
                new MTDSelectListItem{ Id="firstitem", Value="No group selected", Selectded=true, Localized=true}
            };

            foreach (var group in groups)
            {
                GroupList.Add(new MTDSelectListItem { Id = group.Id, Value = group.Name });
            }

            IList<MtdGroup> selectedGroups = groups.Where(x => GroupIds.Contains(x.Id)).ToList();

            SelectedGroups = new List<GroupModel>();
            foreach (var group in selectedGroups)
            {
                GroupModel groupModel = new GroupModel { Id = group.Id, Name = group.Name, UserName = "No owner selected" };

                SelectedGroupIds += $"&{group.Id}";
                SelectedGroups.Add(groupModel);
            }



            Input.CpqPolicyView = "view-own";
            Input.CpqPolicyEdit = "edit-own";
            string cpqPolicy = claims.Where(x => x.Type == "cpq-policy").Select(x => x.Value).FirstOrDefault();
            if (cpqPolicy != null)
            {
                if (cpqPolicy.Contains("view-all")) { Input.CpqPolicyView = "view-all"; }
                if (cpqPolicy.Contains("view-group")) { Input.CpqPolicyView = "view-group"; }
                if (cpqPolicy.Contains("edit-all")) { Input.CpqPolicyEdit = "edit-all"; }
                if (cpqPolicy.Contains("edit-group")) { Input.CpqPolicyEdit = "edit-group"; }
                if (cpqPolicy.Contains("print-gross-price")) { Input.CpqPolicyPGP = "print-gross-price"; }
            }

            CpqModule = false;
            return Page();
        }

    }
}