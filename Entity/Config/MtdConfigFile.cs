﻿/*
    MTD OrderMaker - http://ordermaker.org
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using System;
using System.Collections.Generic;

namespace Mtd.OrderMaker.Server.Entity
{
    public partial class MtdConfigFile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int FileSize { get; set; }
        public string FileType { get; set; }
        public byte[] FileData { get; set; }
    }
}
