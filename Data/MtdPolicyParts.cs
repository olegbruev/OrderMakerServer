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

using System;
using System.Collections.Generic;

namespace Mtd.OrderMaker.Server.Data
{
    public partial class MtdPolicyParts
    {
        public int Id { get; set; }
        public string MtdPolicy { get; set; }
        public string MtdFormPart { get; set; }
        public sbyte Create { get; set; }
        public sbyte Edit { get; set; }
        public sbyte View { get; set; }

        public virtual MtdFormPart MtdFormPartNavigation { get; set; }
        public virtual MtdPolicy MtdPolicyNavigation { get; set; }
    }
}
