﻿/*
    MTD OrderMaker - http://ordermaker.org
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.
*/

using System;
using System.Collections.Generic;

namespace Mtd.OrderMaker.Server.Entity
{
    public partial class MtdLogDocument
    {
        public int Id { get; set; }
        public string MtdStore { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeCh { get; set; }

        public virtual MtdStore MtdStoreNavigation { get; set; }
    }
}
