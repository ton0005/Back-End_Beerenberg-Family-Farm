namespace FarmManagement.Core.Entities
{
    public class ExceptionType
    {
        public int TypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
