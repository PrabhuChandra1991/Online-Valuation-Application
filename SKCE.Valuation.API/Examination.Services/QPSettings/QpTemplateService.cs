using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
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
        private static readonly Dictionary<long, string> QPDocumentTypeDictionary = new Dictionary<long, string>
        {
            { 1, "Course Syllabus document" },
            { 2, "Expert preview document" },
            { 3, "Expert QP generation" },
            { 4, "Print preview QP document" },
            { 5, "Print preview QP Answer document" },
            { 6, "For download for Expert for QP Generation" },
            { 7, "Expert QP uploaded" }
        };

        public QpTemplateService(ExaminationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<QPTemplateVM?> GetQPTemplateByCourseIdAsync(long courseId)
        {
            var qPTemplate = await _context.Courses.Where(cd => cd.CourseId == courseId)
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
                    Documents = new List<QPTemplateDocumentVM>(),
                    UserDocuments = new List<QPTemplateDocumentVM>(),
                    Institutions = new List<QPTemplateInstitutionVM>()
                }).FirstOrDefaultAsync();

            if (qPTemplate == null) return null;

            var courseDepartments = _context.CourseDepartments.Where(cd => cd.CourseId == courseId).ToList();
            if (courseDepartments.Any())
            {
                qPTemplate.RegulationYear = courseDepartments.First().RegulationYear;
                qPTemplate.BatchYear = courseDepartments.First().BatchYear;
                qPTemplate.ExamYear = courseDepartments.First().ExamYear;
                qPTemplate.ExamMonth = courseDepartments.First().ExamMonth;
                qPTemplate.ExamType = courseDepartments.First().ExamType;
                qPTemplate.Semester = courseDepartments.First().Semester;
                qPTemplate.DegreeTypeId = courseDepartments.First().DegreeTypeId;
                qPTemplate.StudentCount = courseDepartments.Sum(cd => cd.StudentCount);
            }
            qPTemplate.DegreeTypeName = _context.DegreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;

            AuditHelper.SetAuditPropertiesForInsert(qPTemplate, 1);
            qPTemplate.Documents = new List<QPTemplateDocumentVM>() { 
            // add QP Template document for Course Syllabus document
           new QPTemplateDocumentVM()
           {
               QPTemplateDocumentId = 0,
               DocumentId = 0,
               DocumentName="",
               QPDocumentTypeId = 1,
               QPDocumentTypeName = QPDocumentTypeDictionary[1],
               QPTemplateId = qPTemplate.QPTemplateId,
               DocumentUrl=""
           },


            // add QP Template document for Expert Preview with QP and Answer Generation with  bookmark
            new QPTemplateDocumentVM()
            {
                QPTemplateDocumentId=0,
                DocumentId=0,
                DocumentName="",
                QPDocumentTypeId=2,
                QPDocumentTypeName = QPDocumentTypeDictionary[2],
                QPTemplateId=qPTemplate.QPTemplateId,
                DocumentUrl=""
            },

            // add QP Template document for Expert use for QP and Answer Generation with  bookmark
            new QPTemplateDocumentVM()
            {
                QPTemplateDocumentId = 0,
                DocumentId = 0,
                DocumentName="",
                QPDocumentTypeId = 3,
                QPDocumentTypeName = QPDocumentTypeDictionary[3],
                QPTemplateId = qPTemplate.QPTemplateId,
                DocumentUrl=""
            }
            };
            qPTemplate.UserDocuments = new List<QPTemplateDocumentVM>()
            {
                //// add QP Template document for download  for Expert for QP and Answer Generation with  bookmark
                new QPTemplateDocumentVM()
                {
                    QPTemplateDocumentId = 0,
                    DocumentId = 0,
                    DocumentName="",
                    QPDocumentTypeId = 6,
                    QPDocumentTypeName = QPDocumentTypeDictionary[6],
                    QPTemplateId = qPTemplate.QPTemplateId,
                    DocumentUrl=""
                },

                //// add QP Template document uploaded by Expert for QP and Answer Generation with  bookmark
                new QPTemplateDocumentVM()
                {
                    QPTemplateDocumentId = 0,
                    DocumentId = 0,
                    DocumentName="",
                    QPDocumentTypeId = 7,
                    QPDocumentTypeName = QPDocumentTypeDictionary[7],
                    QPTemplateId = qPTemplate.QPTemplateId,
                    DocumentUrl=""
                }

            };
            foreach (var document in qPTemplate.Documents)
            {
                AuditHelper.SetAuditPropertiesForInsert(document, 1);
            }
            foreach (var userDocument in qPTemplate.UserDocuments)
            {
                AuditHelper.SetAuditPropertiesForInsert(userDocument, 1);
            }
            List<long> institutionIds = courseDepartments.Select(cd => cd.InstitutionId).Distinct().ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();

            foreach (var institutionId in institutionIds)
            {
                var institution = institutions.FirstOrDefault(i => i.InstitutionId == institutionId);
                if (institution != null)
                {
                    var institutionVM = new QPTemplateInstitutionVM
                    {
                        InstitutionId = institution.InstitutionId,
                        InstitutionName = institution.Name,
                        InstitutionCode = institution.Code,
                        StudentCount = courseDepartments.Where(i => i.InstitutionId == institution.InstitutionId).Sum(ci => ci.StudentCount),
                        Departments = new List<QPTemplateInstitutionDepartmentVM>()
                    };
                    var departmentVMs = courseDepartments.Join(departments, cd => cd.DepartmentId, d => d.DepartmentId, (cd, d) => new { cd, d })
                        .Where(cd => cd.cd.InstitutionId == institution.InstitutionId)
                        .Select(cd => new QPTemplateInstitutionDepartmentVM
                        {
                            DepartmentId = cd.d.DepartmentId,
                            DepartmentName = cd.d.Name,
                            DepartmentShortName = cd.d.ShortName,
                            StudentCount = cd.cd.StudentCount
                        }).ToList();

                    institutionVM.Departments = departmentVMs;
                    foreach (var department in institutionVM.Departments)
                    {
                        AuditHelper.SetAuditPropertiesForInsert(department, 1);
                    }

                    institutionVM.Documents = new List<QPTemplateInstitutionDocumentVM>() { 
                    // add QP Template document for Print with QP With  Tags
                    new QPTemplateInstitutionDocumentVM()
                    {
                        QPTemplateInstitutionDocumentId = 0,
                        QPTemplateInstitutionId= institutionVM.QPTemplateInstitutionId,
                        DocumentId = 0,
                        DocumentName="",
                        DocumentUrl="",
                        QPDocumentTypeId = 4,
                        QPDocumentTypeName = QPDocumentTypeDictionary[4]
                    },

                    // add QP Template document for Print with QP and Answer With Tags
                    new QPTemplateInstitutionDocumentVM()
                    {
                        QPTemplateInstitutionDocumentId = 0,
                        QPTemplateInstitutionId= institutionVM.QPTemplateInstitutionId,
                        DocumentId = 0,
                        DocumentName="",
                        DocumentUrl="",
                        QPDocumentTypeId = 5,
                        QPDocumentTypeName = QPDocumentTypeDictionary[5]
                    }
                    };
                    foreach (var qPDocument in institutionVM.Documents)
                    {
                        AuditHelper.SetAuditPropertiesForInsert(qPDocument, 1);
                    }
                    AuditHelper.SetAuditPropertiesForInsert(institutionVM, 1);
                    qPTemplate.Institutions.Add(institutionVM);
                }
            }

            return qPTemplate;
        }

        public async Task<QPTemplate> CreateQpTemplateAsync(QPTemplateVM qPTemplateVM)
        {
            //var qPTemplate = _mapper.Map<QPTemplate>(qPTemplateVM);
            var qPTemplate = new QPTemplate
            {
                QPTemplateName = qPTemplateVM.QPTemplateName,
                QPCode = qPTemplateVM.QPCode,
                QPTemplateStatusTypeId = qPTemplateVM.QPTemplateStatusTypeId,
                CourseId = qPTemplateVM.CourseId,
                RegulationYear = qPTemplateVM.RegulationYear,
                BatchYear = qPTemplateVM.BatchYear,
                DegreeTypeId = qPTemplateVM.DegreeTypeId,
                ExamYear = qPTemplateVM.ExamYear,
                ExamMonth = qPTemplateVM.ExamMonth,
                ExamType = qPTemplateVM.ExamType,
                Semester = qPTemplateVM.Semester,
                StudentCount = qPTemplateVM.StudentCount
            };
            AuditHelper.SetAuditPropertiesForInsert(qPTemplate, 1);
            qPTemplateVM.Documents.ForEach(d => qPTemplate.Documents.Add(new QPTemplateDocument
            {
                QPDocumentTypeId = d.QPDocumentTypeId,
                DocumentId = d.DocumentId,
                QPTemplateId = qPTemplate.QPTemplateId
            }));
            foreach (var document in qPTemplate.Documents)
            {
                AuditHelper.SetAuditPropertiesForInsert(document, 1);
            }

            qPTemplateVM.Institutions.ForEach(i =>
            {
                var institution = new QPTemplateInstitution
                {
                    InstitutionId = i.InstitutionId,
                    StudentCount = i.StudentCount,
                    QPTemplateId = qPTemplate.QPTemplateId
                };
                i.Departments.ForEach(d => institution.Departments.Add(new QPTemplateInstitutionDepartment
                {
                    DepartmentId = d.DepartmentId,
                    StudentCount = d.StudentCount,
                    QPTemplateInstitutionId = institution.QPTemplateInstitutionId
                }));
                i.Documents.ForEach(d => institution.Documents.Add(new QPTemplateInstitutionDocument
                {
                    QPDocumentTypeId = d.QPDocumentTypeId,
                    DocumentId = d.DocumentId,
                    QPTemplateInstitutionId = institution.QPTemplateInstitutionId
                }));
                foreach (var document in institution.Departments)
                {
                    AuditHelper.SetAuditPropertiesForInsert(document, 1);
                }
                foreach (var document in institution.Documents)
                {
                    AuditHelper.SetAuditPropertiesForInsert(document, 1);
                }
                AuditHelper.SetAuditPropertiesForInsert(institution, 1);

                qPTemplate.Institutions.Add(institution);
            });
            _context.QPTemplates.Add(qPTemplate);
            await _context.SaveChangesAsync();
            return qPTemplate;
        }

        public Task<List<QPTemplateVM>> GetQPTemplatesAsync()
        {
            //var degreeTypes = _context.DegreeTypes.ToList();
            //var institutions = _context.Institutions.ToList();
            //var departments = _context.Departments.ToList();
            //var courses = _context.Courses.ToList();
            //var documents = _context.Documents.ToList();
            //var qPTemplates = _context.QPTemplates.Select(qpt => new QPTemplateVM
            //{
            //    QPTemplateId = qpt.QPTemplateId,
            //    QPTemplateName = qpt.QPTemplateName,
            //    QPCode = qpt.QPCode,
            //    QPTemplateStatusTypeId = qpt.QPTemplateStatusTypeId,
            //    CourseId = qpt.CourseId,
            //    RegulationYear = qpt.RegulationYear,
            //    BatchYear = qpt.BatchYear,
            //    DegreeTypeId = qpt.DegreeTypeId,
            //    ExamYear = qpt.ExamYear,
            //    ExamMonth = qpt.ExamMonth,
            //    ExamType = qpt.ExamType,
            //    Semester = qpt.Semester,
            //    StudentCount = qpt.StudentCount,
            //    Documents = qpt.Documents.Select(d => new QPTemplateDocumentVM
            //    {
            //        QPTemplateDocumentId = d.QPTemplateDocumentId,
            //        QPTemplateId = d.QPTemplateId,
            //        DocumentId = d.DocumentId,
            //        QPDocumentTypeId = d.QPDocumentTypeId,
            //        QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId],
            //        DocumentName = documents.FirstOrDefault(di=>di.DocumentId == d.DocumentId)?.Name ??string.Empty,
            //        DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty,
            //    }).ToList(),
            //    Institutions = qpt.Institutions.Select(i => new QPTemplateInstitutionVM
            //    {
            //        QPTemplateInstitutionId = i.QPTemplateInstitutionId,
            //        QPTemplateId = i.QPTemplateId,
            //        InstitutionId = i.InstitutionId,
            //        InstitutionName = institutions.FirstOrDefault(inst =>inst.InstitutionId == i.InstitutionId)?.Name??string.Empty,
            //        InstitutionCode = institutions.FirstOrDefault(inst => inst.InstitutionId == i.InstitutionId)?.Code ?? string.Empty,
            //        StudentCount = i.StudentCount,
            //        Departments = i.Departments.Select(d => new QPTemplateInstitutionDepartmentVM
            //        {
            //            QPTemplateInstitutionDepartmentId = d.QPTemplateInstitutionDepartmentId,
            //            QPTemplateInstitutionId = d.QPTemplateInstitutionId,
            //            DepartmentId = d.DepartmentId,
            //            DepartmentName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.Name ?? string.Empty,
            //            DepartmentShortName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.ShortName ?? string.Empty,
            //            StudentCount = d.StudentCount
            //        }).ToList(),
            //        Documents = i.Documents.Select(d => new QPTemplateInstitutionDocumentVM
            //        {
            //            QPTemplateInstitutionDocumentId = d.QPTemplateInstitutionDocumentId,
            //            QPTemplateInstitutionId = d.QPTemplateInstitutionId,
            //            DocumentId = d.DocumentId,
            //            QPDocumentTypeId = d.QPDocumentTypeId,
            //            QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId],
            //            DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty,
            //            DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty,
            //        }).ToList()
            //    }).ToList()
            //}).ToList();
            //foreach (var qPTemplate in qPTemplates)
            //{
            //    qPTemplate.DegreeTypeName = degreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;

            //    foreach (var institution in qPTemplate.Institutions)
            //    {
            //        institution.InstitutionName = institutions.FirstOrDefault(i => i.InstitutionId == institution.InstitutionId)?.Name ?? string.Empty;
            //        institution.InstitutionCode = institutions.FirstOrDefault(i => i.InstitutionId == institution.InstitutionId)?.Code ?? string.Empty;
            //        institution.Departments.ForEach(d =>
            //        {
            //            d.DepartmentName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.Name ?? string.Empty;
            //            d.DepartmentShortName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.ShortName ?? string.Empty;
            //        });
            //    }
            //}
            //return qPTemplates;
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
