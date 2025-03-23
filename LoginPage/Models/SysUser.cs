using System;

namespace LoginPage.Server.Models
{
    public class SysUser
    {
        public int? Id { get; set; }
        public string? TitleBeforeName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? TitleAfterName { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? StatusEnumItemId { get; set; }
        public Guid? Guid { get; set; }
    }
}
