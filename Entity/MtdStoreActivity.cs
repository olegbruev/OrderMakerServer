﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Entity
{
    public class MtdStoreActivity
    {
        public string Id { get; set; }
        public string MtdStoreId { get; set; }
        public string MtdFormActivityId { get; set; }
        public string Comment { get; set; }
        public DateTime TimeCr { get; set; }

        public virtual MtdStore MtdStore { get; set; }
        public virtual MtdFormActivity  MtdFormActitvity {get; set;}
    }
}
