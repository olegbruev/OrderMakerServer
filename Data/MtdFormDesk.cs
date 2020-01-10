﻿/*
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

namespace Mtd.OrderMaker.Server.Data
{
    public partial class MtdFormDesk
    {
        public string Id { get; set; }
        public byte[] Image { get; set; }
        public string ImageType { get; set; }
        public int ImageSize { get; set; }
        public string ColorFont { get; set; }
        public string ColorBack { get; set; }

        public virtual MtdForm IdNavigation { get; set; }
    }
}
