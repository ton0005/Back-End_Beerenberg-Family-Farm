using System.Collections.Generic;

namespace FarmManagement.Core.Entities
{
    public class ContractType
    {
        public int ContractTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
    }
}
