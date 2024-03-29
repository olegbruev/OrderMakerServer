﻿/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.EntityHandler.Approval;
using Mtd.OrderMaker.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.EntityHandler.Filter
{
    public partial class FilterHandler
    {
        public async Task<OutFlow> GetStackFlowAsync(Incomer incomer, TypeQuery typeQuery, IList<string> storeIds = null)
        {

            OutFlow outFlow = new OutFlow();

            if (incomer.WaitList == 1)
            {
                List<string> storesForUser = await ApprovalHandler.GetWaitStoreIds(_context, _user, incomer.FormId);
                queryMtdStore = queryMtdStore.Where(x => storesForUser.Contains(x.Id));
                outFlow = new OutFlow
                {
                    Count = queryMtdStore.Count(),
                    MtdStores = await queryMtdStore.OrderByDescending(x => x.Sequence).Skip((incomer.Page - 1) * incomer.PageSize).Take(incomer.PageSize).ToListAsync()
                };

                return outFlow;
            }


            IList<MtdFilterScript> scripts = await _userHandler.GetFilterScriptsAsync(_user, incomer.FormId, 1);
            if (scripts != null && scripts.Count > 0 && incomer.WaitList != 1)
            {
                foreach (var fs in scripts)
                {
                    List<string> scriptIds = await _context.MtdStore.FromSqlRaw(fs.Script).Select(x => x.Id).ToListAsync();
                    queryMtdStore = queryMtdStore.Where(x => scriptIds.Contains(x.Id));
                }
            }

            if (storeIds != null)
            {
                queryMtdStore = queryMtdStore.Where(x => storeIds.Contains(x.Id));
            }

            bool ownOnly = await _userHandler.CheckUserPolicyAsync(_user, incomer.FormId, RightsType.ViewOwn);
            if (ownOnly)
            {
                IList<string> ownStoreIds = await _context.MtdStoreOwner.Where(x => x.UserId == _user.Id).Select(x => x.Id).ToListAsync();
                queryMtdStore = queryMtdStore.Where(x => ownStoreIds.Contains(x.Id));
            }

            bool groupView = await _userHandler.CheckUserPolicyAsync(_user, FormId, RightsType.ViewGroup);
            if (groupView)
            {
                IList<WebAppUser> appUsers = await _userHandler.GetUsersInGroupsOutDenyAsync(_user, FormId);
                List<string> userIds = appUsers.Select(x => x.Id).ToList();

                IList<string> groupStoreIds = await _context.MtdStoreOwner.Where(x => userIds.Contains(x.UserId)).Select(x => x.Id).ToListAsync();
                queryMtdStore = queryMtdStore.Where(x => groupStoreIds.Contains(x.Id));
            }

            switch (typeQuery)
            {
                case TypeQuery.number:
                    {
                        /*ID search*/
                        outFlow = await GetDataForNumberAsync(incomer);
                        break;
                    }
                case TypeQuery.text:
                    {
                        /*Search line*/
                        outFlow = await GetDataForTextAsync(incomer);
                        break;
                    }
                case TypeQuery.field:
                case TypeQuery.textField:
                case TypeQuery.script:
                    {
                        /*Basic search - selection from the list*/
                        outFlow = await GetDataForFieldAsync(incomer);
                        break;
                    }
                default:
                    {
                        /*Without filters*/
                        outFlow = await GetDataForEmptyAsync(incomer);
                        break;
                    }
            }

            int pageCount = 0;
            decimal count = (decimal)outFlow.Count / incomer.PageSize;
            pageCount = Convert.ToInt32(Math.Ceiling(count));
            pageCount = pageCount == 0 ? 1 : pageCount;
            outFlow.PageCount = pageCount;

            return outFlow;
        }

    }
}
