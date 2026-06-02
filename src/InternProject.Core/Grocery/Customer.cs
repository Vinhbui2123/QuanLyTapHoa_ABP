using Abp.Domain.Entities.Auditing;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery
{
    public class Customer : FullAuditedEntity<Guid>
    {
        [StringLength(50)]
        public string? Code { get; set; }

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(32)]
        public string? Phone { get; set; }

        [StringLength(512)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
