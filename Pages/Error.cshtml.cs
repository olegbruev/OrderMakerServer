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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Mtd.OrderMaker.Web.Areas.Identity.Data;
using Mtd.OrderMaker.Web.DataConfig;

namespace Mtd.OrderMaker.Web.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : PageModel
    {
        
        private readonly IEmailSender _emailSender;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ConfigSettings _emailSupport;

        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public ErrorModel(      
            IEmailSender emailSender, 
            IHostingEnvironment hostingEnvironment, 
            IOptions<ConfigSettings> emailSupport)
        {
            
            _emailSender = emailSender;
            _hostingEnvironment = hostingEnvironment;
            _emailSupport = emailSupport.Value;
        }


        public async void OnGetAsync()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature != null)
            {

                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    
                string webRootPath = _hostingEnvironment.WebRootPath;
                string contentRootPath = _hostingEnvironment.ContentRootPath;
                var file = Path.Combine(contentRootPath, "wwwroot", "lib", "mtd-ordermaker", "emailform", "error.html");
                var htmlArray = System.IO.File.ReadAllText(file);
                string htmlText = htmlArray.ToString();

                htmlText = htmlText.Replace("{RequestID}", RequestId);
                htmlText = htmlText.Replace("{Host}", HttpContext.Request.Host.Value);
                htmlText = htmlText.Replace("{Path}", exceptionFeature.Path);
                htmlText = htmlText.Replace("{Query}", HttpContext.Request.QueryString.Value);
                htmlText = htmlText.Replace("{Message}", exceptionFeature.Error.Message);
                htmlText = htmlText.Replace("{Sorce}", exceptionFeature.Error.Source);                
                htmlText = htmlText.Replace("{UserName}", User.Identity.Name);

                await _emailSender.SendEmailAsync(_emailSupport.EmailSupport, "Server Error", htmlText);                
            }

        }
    }
}
