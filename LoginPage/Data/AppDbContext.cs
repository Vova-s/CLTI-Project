using LoginPage.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders; // ← іноді потрібен
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LoginPage.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<SysUser> SysUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SysUser>(entity =>
            {
                entity.ToTable("sys_user");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TitleBeforeName).HasColumnName("title_before_name");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.TitleAfterName).HasColumnName("title_after_name");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.StatusEnumItemId).HasColumnName("status_enum_item_id");
                entity.Property(e => e.Guid).HasColumnName("guid");
            });
        }
    }
}
