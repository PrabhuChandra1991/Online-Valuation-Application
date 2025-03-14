﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.QPSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.QPSettings
{
    public class QpTemplateService : IQpTemplateService
    {
        private readonly ExaminationDbContext _context;
        private readonly IMapper _mapper;

        public QpTemplateService(ExaminationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<QPTemplateVM?> GetQPTemplateByCourseIdAsync(long courseId)
        {
            var course = await _context.Courses.Where(cd => cd.CourseId == courseId)
                .Select(c => new QPTemplateVM
                {
                    CourseId = c.CourseId,
                    CourseCode = c.Code,
                    CourseName = c.Name,
                    QPTemplateId = 0,
                    QPTemplateName = "",
                    QPCode = "",
                    QPTemplateStatusTypeId = 1,
                    QPTemplateStatusTypeName = "QP Pending for Allocation",
                    UserId = 1,
                    Institutions = new List<InstitutionDepartmentVM>()
                }).FirstOrDefaultAsync();

            if (course == null) return null;

            var courseDepartments = _context.CourseDepartments.Where(cd => cd.CourseId == courseId).ToList();
            if (courseDepartments.Any())
            {
                course.RegulationYear = courseDepartments.First().RegulationYear;
                course.BatchYear = courseDepartments.First().BatchYear;
                course.ExamYear = courseDepartments.First().ExamYear;
                course.ExamMonth = courseDepartments.First().ExamMonth;
                course.ExamType = courseDepartments.First().ExamType;
                course.Semester = courseDepartments.First().Semester;
                course.DegreeTypeId = courseDepartments.First().DegreeTypeId;
                course.StudentCount = courseDepartments.Sum(cd => cd.StudentCount);
            }
            course.DegreeTypeName = _context.DegreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == course.DegreeTypeId)?.Name ?? string.Empty;

            AuditHelper.SetAuditPropertiesForInsert(course, 1);
            course.qpDocumentVMs = new List<QPDocumentVM>() { 
            // add QP Template document for Course Syllabus document
           new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentTypeId = 1, QPDocumentTypeName = "Course Syllabus document", QPTemplateId = course.QPTemplateId ,DocumentUrl=""},

            // add QP Template document for Expert Preview with QP and Answer Generation with  bookmark
            new QPDocumentVM() { QPDocumentId=0,DocumentId=0, QPDocumentTypeId=2,QPDocumentTypeName="Expert preview document",QPTemplateId=course.QPTemplateId,DocumentUrl=""},

            // add QP Template document for Expert use for QP and Answer Generation with  bookmark
            new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentTypeId = 3, QPDocumentTypeName = "Expert QP generation", QPTemplateId = course.QPTemplateId,DocumentUrl="" },
            };
            course.qpUserDocumentVMs = new List<QPDocumentVM>()
            {
                //// add QP Template document for download  for Expert for QP and Answer Generation with  bookmark
                new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentTypeId = 6, QPDocumentTypeName = "For download for Expert for QP Generation", QPTemplateId = course.QPTemplateId,DocumentUrl="" },

                //// add QP Template document uploaded by Expert for QP and Answer Generation with  bookmark
                new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentTypeId = 7, QPDocumentTypeName = "Expert QP uploaded", QPTemplateId = course.QPTemplateId,DocumentUrl="" }

            };
            foreach (var qpDocumentVM in course.qpDocumentVMs)
            {
                AuditHelper.SetAuditPropertiesForInsert(qpDocumentVM, 1);
            }
            List<long> institutionIds = courseDepartments.Select(cd => cd.InstitutionId).Distinct().ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();

            foreach (var institutionId in institutionIds)
            {
                var institution = institutions.FirstOrDefault(i => i.InstitutionId == institutionId);
                if (institution != null)
                {
                    var institutionVM = new InstitutionDepartmentVM
                    {
                        InstitutionId = institution.InstitutionId,
                        InstitutionName = institution.Name,
                        InstitutionCode = institution.Code,
                        StudentCount = courseDepartments.Where(i => i.InstitutionId == institution.InstitutionId).Sum(ci => ci.StudentCount),
                        Departments = new List<DepartmentVM>()
                    };
                    var departmentVMs = courseDepartments.Join(departments, cd => cd.DepartmentId, d => d.DepartmentId, (cd, d) => new { cd, d })
                        .Where(cd => cd.cd.InstitutionId == institution.InstitutionId)
                        .Select(cd => new DepartmentVM
                        {
                            DepartmentId = cd.d.DepartmentId,
                            DepartmentName = cd.d.Name,
                            DepartmentShortName = cd.d.ShortName,
                            StudentCount = cd.cd.StudentCount
                        }).ToList();
                    institutionVM.Departments = departmentVMs;

                    institutionVM.qpDocumentVMs = new List<QPDocumentVM>() { 
                    // add QP Template document for Print with QP With  Tags
                    new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0,DocumentUrl="", QPDocumentTypeId = 4, QPDocumentTypeName = "Print preview QP document", QPTemplateId = course.QPTemplateId },

                    // add QP Template document for Print with QP and Answer With Tags
                    new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0,DocumentUrl="", QPDocumentTypeId = 5, QPDocumentTypeName = "Print preview QP Answer document", QPTemplateId = course.QPTemplateId }
                    };
                    foreach (var qpDocumentVM in institutionVM.qpDocumentVMs)
                    {
                        AuditHelper.SetAuditPropertiesForInsert(qpDocumentVM, 1);
                    }
                    course.Institutions.Add(institutionVM);
                }
            }
            return course;
        }

        public async Task<QPTemplate> CreateQpTemplateAsync(QPTemplateVM viewModel)
        {
            var qPTemplate = _mapper.Map<QPTemplate>(viewModel);
            _context.QPTemplates.Add(qPTemplate);
            await _context.SaveChangesAsync();
            return qPTemplate;
        }

        public Task<List<QPTemplateVM>> GetQPTemplatesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<QPTemplateVM?> GetQPTemplateAsync(long qpTemplateId)
        {
            throw new NotImplementedException();
        }

        public Task<List<QPTemplateVM>> GetQPTemplatesByUserIdAsync(long userId)
        {
            throw new NotImplementedException();
        }
    }
}
