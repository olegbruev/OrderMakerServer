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

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Server.Areas.Identity.Data;
using Mtd.OrderMaker.Server.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.EntityHandler.Filter
{
    public partial class FilterHandler
    {


        public async Task<OutFlow> GetDataForEmptyAsync(Incomer incomer)
        {

            return new OutFlow
            {
                Count = await queryMtdStore.Where(x => x.MtdForm == incomer.FormId).CountAsync(),
                MtdStores = await queryMtdStore.Where(x => x.MtdForm == incomer.FormId).OrderByDescending(x => x.Sequence)
                .Skip((incomer.Page - 1) * incomer.PageSize)
                .Take(incomer.PageSize)
                .ToListAsync()
            };
        }

        public async Task<OutFlow> GetDataForNumberAsync(Incomer incomer)
        {
            MtdStore mtdStore = await queryMtdStore.Where(x => x.MtdForm == incomer.FormId && x.Sequence.ToString().Equals(incomer.SearchNumber)).FirstOrDefaultAsync();

            return new OutFlow
            {
                Count = mtdStore != null ? 1 : 0,
                MtdStores = mtdStore != null ? new List<MtdStore> { mtdStore } : new List<MtdStore>()
            };

        }

        public async Task<OutFlow> GetDataForTextAsync(Incomer incomer)
        {
            List<string> fieldsIds = incomer.FieldForColumn.Select(x => x.Id).ToList();
            IList<string> storeIds = new List<string>();
            bool plus = incomer.SearchText.Contains("+");

            if (plus)
            {
                List<string> words = incomer.SearchText.Split("+").Where(x => x != "").ToList();
                for (int i = 0; i < words.Count(); i++)
                {                                     
                   storeIds = i==0 ? await FindStoreIdsForText(fieldsIds, words[i], 4) : await FindStoreIdsForText(fieldsIds, words[i], 4, storeIds);                                       
                }
            }
            else {
                storeIds = await FindStoreIdsForText(fieldsIds, incomer.SearchText, 4);
            }
                   

            return new OutFlow
            {
                Count = storeIds.Count(),
                MtdStores = await queryMtdStore.Where(x => storeIds.Contains(x.Id))
                .OrderByDescending(x => x.Sequence).Skip((incomer.Page - 1) * incomer.PageSize).Take(incomer.PageSize).ToListAsync()
            };

        }

        public async Task<OutFlow> GetDataForFieldAsync(Incomer incomer)
        {

            IList<string> storeIds = await queryMtdStore.Select(x => x.Id).ToListAsync();

            if (incomer.SearchText.Length > 0)
            {
                List<string> fieldsIds = incomer.FieldForColumn.Select(x => x.Id).ToList();
                storeIds = await FindStoreIdsForText(fieldsIds, incomer.SearchText, 4, storeIds);
            }

            if (incomer.FieldForFilter != null && incomer.FieldForFilter.Count > 0)
            {
                foreach (var item in incomer.FieldForFilter)
                {
                    int fieldType = item.MtdFormPartFieldNavigation.MtdSysType;
                    List<string> field = new List<string> { item.MtdFormPartField };

                    switch (fieldType)
                    {
                        case 2:
                        case 12:
                            {
                                bool isOk = int.TryParse(item.Value, out int valueInt);
                                if (!isOk) { valueInt = 0; }
                                storeIds = await FindStoreIdsForInt(field, valueInt, item.MtdTerm, storeIds);
                                break;
                            }
                        case 3:
                            {
                                bool isOk = decimal.TryParse(item.Value, out decimal valueDecimal);
                                if (!isOk) { valueDecimal = 0; }
                                storeIds = await FindStoreIdsForDecimal(field, valueDecimal, item.MtdTerm, storeIds);
                                break;
                            }
                        case 5:
                            {
                                //bool ok = DateTime.TryParse(item.Value, out DateTime dateTime);
                                bool isOk = DateTime.TryParseExact(item.Value, item.ValueExtra, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime);
                                if (isOk)
                                {
                                    storeIds = await FindStoreIdsForDate(field, dateTime.Date, item.MtdTerm, storeIds);
                                }
                                break;
                            }
                        case 6:
                            {
                                bool ok = DateTime.TryParse(item.Value, out DateTime dateTime);                                
                                if (ok)
                                {
                                    storeIds = await FindStoreIdsForDateTime(field, dateTime, item.MtdTerm, storeIds);
                                }
                                break;
                            }
                        case 11:
                            {
                                storeIds = await FindStoreIdsForList(field, item.Value, item.MtdTerm, storeIds);
                                break;
                            }
                        default:
                            {
                                storeIds = await FindStoreIdsForText(field, item.Value, item.MtdTerm, storeIds);
                                break;
                            }
                    }
                };
            }

            OutFlow paramOut = new OutFlow
            {
                Count = storeIds.Count(),
                MtdStores = await queryMtdStore.Where(x => storeIds.Contains(x.Id))
                .OrderByDescending(x => x.Sequence).Skip((incomer.Page - 1) * incomer.PageSize).Take(incomer.PageSize).ToListAsync()
            };

            return paramOut;
        }
    }
}
