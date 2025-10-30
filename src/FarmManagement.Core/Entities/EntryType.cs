namespace FarmManagement.Core.Entities
{
    public class EntryType
    {
        public int EntryTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty; // e.g. CLOCK_IN
    }
}
