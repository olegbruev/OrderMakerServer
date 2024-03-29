﻿using Microsoft.EntityFrameworkCore;

namespace Mtd.OrderMaker.Server.Entity
{
    public partial class OrderMakerContext : DbContext
    {

        public virtual DbSet<MtdSysStyle> MtdSysStyle { get; set; }
        public virtual DbSet<MtdSysTerm> MtdSysTerm { get; set; }
        public virtual DbSet<MtdSysType> MtdSysType { get; set; }
        public virtual DbSet<MtdSysTrigger> MtdSysTrigger { get; set; }


        public void SystemModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<MtdSysStyle>(entity =>
            {
                entity.ToTable("mtd_sys_style");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("id_UNIQUE")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Active)
                    .HasColumnName("active")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnName("description")
                    .HasColumnType("varchar(512)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(120)");
            });

            modelBuilder.Entity<MtdSysTerm>(entity =>
            {
                entity.ToTable("mtd_sys_term");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("id_UNIQUE")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(45)");

                entity.Property(e => e.Sign)
                    .IsRequired()
                    .HasColumnName("sign")
                    .HasColumnType("varchar(45)");
            });

            modelBuilder.Entity<MtdSysType>(entity =>
            {
                entity.ToTable("mtd_sys_type");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("id_UNIQUE")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Active)
                    .HasColumnName("active")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnName("description")
                    .HasColumnType("varchar(512)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(120)");
            });

            modelBuilder.Entity<MtdSysTrigger>(entity =>
            {
                entity.ToTable("mtd_sys_trigger");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("id_UNIQUE")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasColumnName("id")
                    .HasColumnType("varchar(36)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(120)");

                entity.Property(e => e.Sequence)
                    .IsRequired()
                    .HasColumnName("sequence")
                    .HasColumnType("int");
            });
        }
    }
}
