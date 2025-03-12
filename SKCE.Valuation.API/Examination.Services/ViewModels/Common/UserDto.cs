using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string? Salutation { get; set; }
        public required string MobileNumber { get; set; }
        public long? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public long? TotalExperience { get; set; }
        public string DepartmentName { get; set; }
        public string? CollegeName { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankBranchName { get; set; }
        public string? BankIFSCCode { get; set; }
        public bool IsEnabled { get; set; }
        public List<UserCourseDto> Courses { get; set; }
        public List<UserSpecializationDto> Specializations { get; set; }
        public List<UserDesignationDto> Designations { get; set; }
        public List<UserQualificationsDto> Qualifications { get; set; }
    }

    public class UserCourseDto
    {
        public long UserCourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public long NumberOfYearsHandled { get; set; }
        public bool IsHandledInLast2Semester { get; set; }
        public long DegreeTypeId { get; set; }
        public string DegreeTypeName { get; set; } = string.Empty;
    }

    public class UserSpecializationDto
    {
        public long UserAreaOfSpecializationId { get; set; }
        public string AreaOfSpecializationName { get; set; }
    }

    public class UserDesignationDto
    {
        public long UserDesignationId { get; set; }
        public long DesignationId { get; set; }
        public string DesignationName { get; set; }
        public long Experience { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class UserQualificationsDto
    {
        public long UserQualificationId { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public bool? IsCompleted { get; set; }
    }
}