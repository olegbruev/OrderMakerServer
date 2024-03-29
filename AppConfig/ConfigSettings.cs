﻿/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.

*/
namespace Mtd.OrderMaker.Server.AppConfig
{
    public class ConfigSettings
    {
        public string AppName { get; set; } = string.Empty;
        public string EmailSupport { get; set; }
        public string DefaultUSR { get; set; }
        public string DefaultPWD { get; set; }
        public string CultureInfo { get; set; }
    }
}
