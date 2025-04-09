namespace SKCE.Examination.Models.DbModels
{
    public abstract class AuditModel
    {
        public bool IsActive { get; set; }
        public long CreatedById { get; set; }
        public DateTime CreatedDate { get; set; }
        public long ModifiedById { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
