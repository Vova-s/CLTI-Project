using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LoginPage.Server.Models
{

    public class SysUser
    {
        public int? Id { get; set; }

        [Column("title_before_name")]
        public string? TitleBeforeName { get; set; }

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Column("title_after_name")]
        public string? TitleAfterName { get; set; }

        public string? Password { get; set; }
        public string? Email { get; set; }

        [Column("status_enum_item_id")]
        public int? StatusEnumItemId { get; set; }

        [Column("guid")]
        public Guid? Guid { get; set; }

        // Якщо у вас є колонка created_at, додайте її теж:
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
   
}

