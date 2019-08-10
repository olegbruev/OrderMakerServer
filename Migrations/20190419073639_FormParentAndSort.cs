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

using Microsoft.EntityFrameworkCore.Migrations;

namespace Mtd.OrderMaker.Web.Migrations
{
    public partial class FormParentAndSort : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<sbyte>(
                name: "child",
                table: "mtd_form_part",
                type: "tinyint(4)",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.AddColumn<string>(
                name: "Parent",
                table: "mtd_form",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sequence",
                table: "mtd_form",
                type: "int(11)",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.CreateIndex(
                name: "IX_mtd_form_Parent",
                table: "mtd_form",
                column: "Parent");

            migrationBuilder.AddForeignKey(
                name: "fk_form_parent",
                table: "mtd_form",
                column: "Parent",
                principalTable: "mtd_form",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_form_parent",
                table: "mtd_form");

            migrationBuilder.DropIndex(
                name: "IX_mtd_form_Parent",
                table: "mtd_form");

            migrationBuilder.DropColumn(
                name: "child",
                table: "mtd_form_part");

            migrationBuilder.DropColumn(
                name: "Parent",
                table: "mtd_form");

            migrationBuilder.DropColumn(
                name: "sequence",
                table: "mtd_form");
        }
    }
}
