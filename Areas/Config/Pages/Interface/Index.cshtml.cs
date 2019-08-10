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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Mtd.OrderMaker.Web.Data;

namespace Mtd.OrderMaker.Web.Areas.Config.Pages.Interface
{

    public class IndexModel : PageModel
    {

        private readonly OrderMakerContext _context;


        public IndexModel(OrderMakerContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ImgMenu"] = await GetImageFromConfig(1);
            ViewData["ImgAppBar"] = await GetImageFromConfig(2);
            var barColor = await _context.MtdConfigParam.Where(x => x.Id == 1).Select(x => x.Value).FirstOrDefaultAsync();
            var iconColor = await _context.MtdConfigParam.Where(x => x.Id == 2).Select(x => x.Value).FirstOrDefaultAsync();
            ViewData["BarColor"] = barColor ?? "#00008a";
            ViewData["IconColor"] = iconColor ?? "#ffffff";
            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {

            await SaveImg(1);
            await SaveImg(2);

            string colorBar = Request.Form["color-bar"];
            string colorIcon = Request.Form["color-icon"];

            await SaveBarColor(colorBar);
            await SaveIconColor(colorIcon);

            await _context.SaveChangesAsync();



            return Page();
        }


        private async Task SaveImg(int code)
        {
            string idCheckBox = $"{code}-delete";
            if (Request.Form[idCheckBox].FirstOrDefault() == null || Request.Form[idCheckBox].FirstOrDefault() == "false")
            {
                string idInput = $"{code}-file-upload-input";
                IFormFile file = Request.Form.Files.FirstOrDefault(x => x.Name == idInput);
                if (file != null)
                {
                    byte[] streamArray = new byte[file.Length];
                    await file.OpenReadStream().ReadAsync(streamArray, 0, streamArray.Length);
                    MtdConfigFile imgConfig = new MtdConfigFile()
                    {
                        Id = code,
                        Name = code == 1 ? "Image for menu" : "Image for AppBar",
                        FileData = streamArray,
                        FileSize = streamArray.Length,
                        FileType = file.ContentType
                    };

                    bool exists = await _context.MtdConfigFiles.Where(x => x.Id == code).AnyAsync();
                    if (exists)
                        _context.Attach(imgConfig).State = EntityState.Modified;
                    else
                        _context.Attach(imgConfig).State = EntityState.Added;
                }
            }
            else
            {
                MtdConfigFile header = new MtdConfigFile() { Id = code };
                _context.Attach(header).State = EntityState.Deleted;
            }

        }

        private async Task SaveBarColor(string color)
        {

            MtdConfigParam mtdConfigParam = await _context.MtdConfigParam.FindAsync(1);
            if (mtdConfigParam == null)
            {
                mtdConfigParam = new MtdConfigParam
                {
                    Id = 1,
                    Name = "Bar Color",
                    Value = color
                };

                await _context.MtdConfigParam.AddAsync(mtdConfigParam);
                return;
            }

            mtdConfigParam.Value = color;
            _context.MtdConfigParam.Update(mtdConfigParam);
            return;
        }

        private async Task SaveIconColor(string color)
        {

            MtdConfigParam mtdConfigParam = await _context.MtdConfigParam.FindAsync(2);
            if (mtdConfigParam == null)
            {
                mtdConfigParam = new MtdConfigParam
                {
                    Id = 2,
                    Name = "Icon Color",
                    Value = color
                };

                await _context.MtdConfigParam.AddAsync(mtdConfigParam);
                return;
            }

            mtdConfigParam.Value = color;
            _context.MtdConfigParam.Update(mtdConfigParam);
            return;
        }

        private async Task<string> GetImageFromConfig(int code)
        {
            string result = string.Empty;
            MtdConfigFile configFile = await _context.MtdConfigFiles.FindAsync(code);
            if (configFile != null && configFile.FileData != null)
            {
                string base64 = Convert.ToBase64String(configFile.FileData);
                result = string.Format("data:{0};base64,{1}", configFile.FileType, base64);
            }

            return result;
        }

    }
}