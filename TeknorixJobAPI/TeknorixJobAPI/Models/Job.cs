namespace TeknorixJobAPI.Models
{
    public class Job
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public Location? Location { get; set; } // Navigation property
        public int DepartmentId { get; set; }
        public Department? Department { get; set; } // Navigation property
        public DateTime PostedDate { get; set; }
        public DateTime ClosingDate { get; set; }
    }

    public class Location
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
    }

    public class Department
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
