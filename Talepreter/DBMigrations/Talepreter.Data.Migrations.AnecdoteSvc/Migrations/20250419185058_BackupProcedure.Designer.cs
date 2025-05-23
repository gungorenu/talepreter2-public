﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Talepreter.Data.DbContext.AnecdoteSvc;

#nullable disable

namespace Talepreter.Data.Migrations.AnecdoteSvc.Migrations
{
    [DbContext(typeof(TaskDbContext))]
    [Migration("20250419185058_BackupProcedure")]
    partial class BackupProcedure
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.HasSequence("SubIndexSequence", "shared");

            modelBuilder.Entity("Talepreter.Data.BaseTypes.Command", b =>
                {
                    b.Property<Guid>("TaleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("TaleVersionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ChapterId")
                        .HasColumnType("int");

                    b.Property<int>("PageId")
                        .HasColumnType("int");

                    b.Property<int>("Index")
                        .HasColumnType("int");

                    b.Property<int>("Phase")
                        .HasColumnType("int");

                    b.Property<long>("SubIndex")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasDefaultValueSql("NEXT VALUE FOR shared.SubIndexSequence");

                    b.Property<long>("Duration")
                        .HasColumnType("bigint");

                    b.Property<string>("Error")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("OperationTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("RawData")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Result")
                        .HasColumnType("int");

                    b.Property<string>("Tag")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Target")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("TaleId", "TaleVersionId", "ChapterId", "PageId", "Index", "Phase", "SubIndex");

                    b.HasIndex("TaleId", "TaleVersionId", "ChapterId", "PageId", "Phase");

                    b.ToTable("Commands");
                });

            modelBuilder.Entity("Talepreter.Data.BaseTypes.Trigger", b =>
                {
                    b.Property<Guid>("TaleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("TaleVersionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("GrainId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GrainType")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Parameter")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.Property<string>("Target")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("TriggerAt")
                        .HasColumnType("bigint");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("TaleId", "TaleVersionId", "Id");

                    b.HasIndex("TaleId");

                    b.HasIndex("TaleId", "TaleVersionId", "Id", "Type");

                    b.HasIndex("TaleId", "TaleVersionId", "State", "TriggerAt");

                    b.HasIndex("TaleId", "TaleVersionId", "Id", "Type", "GrainType");

                    b.ToTable("Triggers");
                });
#pragma warning restore 612, 618
        }
    }
}
