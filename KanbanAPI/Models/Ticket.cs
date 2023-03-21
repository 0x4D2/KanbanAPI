namespace KanbanAPI.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Modifier { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool isArchived { get; set; }
        public int StatusID { get; set; }
        public int Priority { get; set; }
        public string? Status { get; set; }
    }
}
