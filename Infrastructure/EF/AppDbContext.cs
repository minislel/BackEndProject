using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.EF
{
     public class AppDbContext : IdentityDbContext<UserEntity>
    {
        protected override void OnModelCreating(ModelBuilder builder)
        { 
            base.OnModelCreating(builder);
            var adminId = "7abf1057-5d1e-4efd-8166-27e4f6712ead";
            var adminCreatedAt = new DateTime(2025, 04, 08);
            var adminUser = new UserEntity() 
            { 
                
                Id = adminId,
                Email = "admin@wsei.edu.pl",
                NormalizedEmail = "ADMIN@WSEI.EDU.PL",
                UserName = "Admin",
                NormalizedUserName = "ADMIN",
                ConcurrencyStamp = adminId,
                SecurityStamp = adminId,
                PasswordHash = "AQAAAAIAAYagAAAAENrUGpVMb8wzhY3UuvwWcNf3lOjlXx/7expp/8dhpQOjv0cnxuQKvx+hFtP96D+ceA=="
            };
            //PasswordHasher<UserEntity> passwordHasher = new PasswordHasher<UserEntity>();
            //var hash = passwordHasher.HashPassword(adminUser, "Admin123!");
            //Console.WriteLine(hash);
            builder.Entity<UserEntity>().HasData(adminUser);
            builder.Entity<UserEntity>().OwnsOne(x => x.Details).HasData(
            new 
            {
                UserEntityId = adminId,
                CreatedAt = adminCreatedAt,
            });
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        { 
            
            optionsBuilder.UseSqlite("Data Source=C:\\data\\app.db");
        }
    }

}
