using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mtd.OrderMaker.Server.Models.Controls.MTDSelectList
{
    public class MTDSelectListTagsModel : MTDSelectListTags
    {
       
        public string IdInput { get; set; }
        public string IdHelper { get; set; }
        public string IdLabel { get; set; }
        public string IdSelectedText { get; set; }
        public string IdDataLink { get; set; }

        public MTDSelectListTagsModel(MTDSelectListTags tags)
        {
            this.Id = tags.Id ?? Guid.NewGuid().ToString();
            this.Name = tags.Name;
            this.Label = tags.Label;
            this.LabelLocalized = tags.LabelLocalized;
            this.ValueId = tags.ValueId;           
            this.Items = tags.Items ?? new List<MTDSelectListItem>();
            this.MTDSelectListView = tags.MTDSelectListView;

            this.IdHelper = $"{Id}-helper";
            this.IdInput = $"{Id}-input";
            this.IdLabel = $"{Id}-label";
            this.IdSelectedText = $"{Id}-selected-text";
            this.IdDataLink = $"{Id}-datalink";

        }
    }
}
