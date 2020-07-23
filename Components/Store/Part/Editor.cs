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
using Mtd.OrderMaker.Server.Entity;
using Mtd.OrderMaker.Server.Models.Store;
using System.Linq;


namespace Mtd.OrderMaker.Server.Components.Store.Part
{
    [ViewComponent (Name = "StorePartEditor")]
    public class Editor : ViewComponent
    {
        public IViewComponentResult Invoke(MtdFormPart part, Warehouse model)
        {
            Warehouse warehouse = new Warehouse()
            {               
                Store = model.Store,
                Parts = model.Parts.Where(x => x.Id == part.Id).ToList(),
                Stack = model.Stack,
                Fields = model.Fields.Where(x => x.MtdFormPart == part.Id && x.ReadOnly == 0).OrderBy(x => x.Sequence).ToList()
            };

            //string viewName = part.MtdSysStyle == 5 ? "Columns" : "Rows";
            
            return View("Rows",warehouse);
        }
    }
}
