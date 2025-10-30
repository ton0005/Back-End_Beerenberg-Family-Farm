using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FarmManagement.Core.Entities
{
    public class Department
    {
        public int DepartmentId { get; set; }           // PK
        public string DepartmentName { get; set; } = string.Empty; // UK
        public string? Description { get; set; }
        public int? ManagerId { get; set; }             // FK → Staff (department head)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [JsonIgnore]
    public Staff? Manager { get; set; }             // Department head
    [JsonIgnore]
    public ICollection<Staff> Staffs { get; set; } = new List<Staff>();
    }
}