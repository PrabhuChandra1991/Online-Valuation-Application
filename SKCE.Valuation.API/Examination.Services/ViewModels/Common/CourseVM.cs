﻿namespace SKCE.Examination.Services.ViewModels.Common
{
    public class CourseVM
    {
        public long CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string RegulationYear { get; set; }
        public string BatchYear { get; set; }
        public string ExamYear { get; set; }
        public string ExamMonth { get; set; }
        public string ExamType { get; set; }
        public long Semester { get; set; }
        public long TotalStudentCount { get; set; }
        public List<InstitutionDepartmentVM> Institutions { get; set; }
    }
    public class InstitutionDepartmentVM
    {
        public long InstitutionId { get; set; }
        public string InstitutionName { get; set; }
        public string InstitutionCode { get; set; }
        public long TotalStudentCount { get; set; }
        public List<DepartmentVM> Departments { get; set; }
    }
    public class DepartmentVM
    {
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentShortName { get; set; }
        public long StudentCount { get; set; }
    }

}