﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Database.Migrations.Migrations
{
    [DbContext(typeof(DiscordDBContext))]
    [Migration("20220403112216_LotteryUpdate")]
    partial class LotteryUpdate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.23")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Database.Models.LotteryTickets", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("DiscordId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("TicketNumber")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.ToTable("LotteryTickets");
                });

            modelBuilder.Entity("Database.Models.Users", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("BankBalance")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("CashBalance")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("CockFightWinStreak")
                        .HasColumnType("int");

                    b.Property<string>("DiscordId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastStimulus")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("LastWorked")
                        .HasColumnType("datetime2");

                    b.Property<int>("LotteryTicketCount")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
