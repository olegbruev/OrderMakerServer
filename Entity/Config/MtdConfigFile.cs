﻿/*
    MTD OrderMaker - http://mtdkey.com
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

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
