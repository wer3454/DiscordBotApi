﻿// <auto-generated />
using System;
using BotApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BotApi.Migrations
{
    [DbContext(typeof(BotDbContext))]
    [Migration("20240925125224_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("BotApi.Models.PlayHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("PlayedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("TrackId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TrackTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("PlayHistory");
                });
#pragma warning restore 612, 618
        }
    }
}