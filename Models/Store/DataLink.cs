using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Models.Store
{
    public class DataLink
    {
        public string PartId { get; set; }
        public Warehouse Data { get; set; }
    }
}
