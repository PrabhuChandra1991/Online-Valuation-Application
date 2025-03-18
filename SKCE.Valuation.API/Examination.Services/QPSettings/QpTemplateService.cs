﻿using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.QPSettings;
using AutoMapper;
using Aspose.Words.Drawing;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Aspose.Words;
namespace SKCE.Examination.Services.QPSettings
{
    public class QpTemplateService 
    {
        private readonly ExaminationDbContext _context;
        private readonly IMapper _mapper;
        private static readonly Dictionary<long, string> QPDocumentTypeDictionary = new Dictionary<long, string>
        {
            { 1, "Course syllabus document" },
            { 2, "Preview QP Answer document for expert" },
            { 3, "QP generation dcoument for expert" },
            { 4, "Preview QP document for print" },
            { 5, "Preview QP Answer document for print" },
            { 6, "For QP Generation" },
            { 7, "Generated QP" },
            { 8, "For QP Scrutiny" },
            { 9, "Scrutinized QP" },
            {10, "For QP Selection" },
            {11, "Selected QP" }
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
                    Documents = new List<QPTemplateDocumentVM>(),
                    Institutions = new List<QPTemplateInstitutionVM>()
                }).FirstOrDefaultAsync();

            if (qPTemplate == null) return null;
            qPTemplate.QPTemplateStatusTypeName = _context.QPTemplateStatusTypes.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == qPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
            
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
            foreach (var document in qPTemplate.Documents)
            {
                AuditHelper.SetAuditPropertiesForInsert(document, 1);
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
            // Create template
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

        public async Task<QPTemplate?> UpdateQpTemplateAsync(QPTemplateVM qPTemplateVM)
        {
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(q => q.QPTemplateId == qPTemplateVM.QPTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateName = qPTemplateVM.QPTemplateName;
            qpTemplate.QPCode = qPTemplateVM.QPCode;
            qpTemplate.QPTemplateStatusTypeId = qPTemplateVM.QPTemplateStatusTypeId;
            qpTemplate.CourseId = qPTemplateVM.CourseId;
            qpTemplate.RegulationYear = qPTemplateVM.RegulationYear;
            qpTemplate.BatchYear = qPTemplateVM.BatchYear;
            qpTemplate.DegreeTypeId = qPTemplateVM.DegreeTypeId;
            qpTemplate.ExamYear = qPTemplateVM.ExamYear;
            qpTemplate.ExamMonth = qPTemplateVM.ExamMonth;
            qpTemplate.ExamType = qPTemplateVM.ExamType;
            qpTemplate.Semester = qPTemplateVM.Semester;
            qpTemplate.StudentCount = qPTemplateVM.StudentCount;
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);
            qPTemplateVM.Documents.ForEach(d =>
            {
                var document = qpTemplate.Documents.FirstOrDefault(qptd => qptd.QPTemplateDocumentId == d.QPTemplateDocumentId);
                if (document != null)
                {
                    document.DocumentId = d.DocumentId;
                    AuditHelper.SetAuditPropertiesForUpdate(document, 1);
                }
            });
            qPTemplateVM.Institutions.ForEach(i =>
            {
                var institution = qpTemplate.Institutions.FirstOrDefault(qpti => qpti.QPTemplateInstitutionId == i.QPTemplateInstitutionId);
                if (institution != null)
                {
                    i.Documents.ForEach(d =>
                    {
                        var document = institution.Documents.FirstOrDefault(qptd => qptd.QPTemplateInstitutionDocumentId == d.QPTemplateInstitutionDocumentId);
                        if (document != null)
                        {
                            document.DocumentId = d.DocumentId;
                            AuditHelper.SetAuditPropertiesForUpdate(document, 1);
                        }
                    });
                    AuditHelper.SetAuditPropertiesForUpdate(institution, 1);
                }
            });
            if(qPTemplateVM.UserQPSelectionTemplate.UserQPTemplateId > 0)
            {
                var finalSelectedDocument = qPTemplateVM.UserQPSelectionTemplate.UserDocuments.FirstOrDefault(d=>d.QPDocumentTypeId ==10 && d.DocumentId > 0);
                if(finalSelectedDocument != null && qpTemplate.QPTemplateStatusTypeId == 5)
                {
                   await SubmitSelectedQPAsync(1, qpTemplate.QPTemplateId, finalSelectedDocument.DocumentId);
                }
            }
            await _context.SaveChangesAsync();
            return qpTemplate;
        }
        public async Task<List<QPTemplateVM>> GetQPTemplatesAsync()
        {
            var degreeTypes = _context.DegreeTypes.ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qPTemplates = new List<QPTemplateVM>();
            var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
            qPTemplates = await _context.QPTemplates.Select(qpt => new QPTemplateVM
            {
                QPTemplateId = qpt.QPTemplateId,
                QPTemplateName = qpt.QPTemplateName,
                QPCode = qpt.QPCode,
                QPTemplateStatusTypeId = qpt.QPTemplateStatusTypeId,
                CourseId = qpt.CourseId,
                RegulationYear = qpt.RegulationYear,
                BatchYear = qpt.BatchYear,
                DegreeTypeId = qpt.DegreeTypeId,
                ExamYear = qpt.ExamYear,
                ExamMonth = qpt.ExamMonth,
                ExamType = qpt.ExamType,
                Semester = qpt.Semester,
                StudentCount = qpt.StudentCount,
                Documents = qpt.Documents.Select(d => new QPTemplateDocumentVM
                {
                    QPTemplateDocumentId = d.QPTemplateDocumentId,
                    QPTemplateId = d.QPTemplateId,
                    DocumentId = d.DocumentId,
                    QPDocumentTypeId = d.QPDocumentTypeId,
                    //QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId]
                }).ToList(),

                Institutions = qpt.Institutions.Select(i => new QPTemplateInstitutionVM
                {
                    QPTemplateInstitutionId = i.QPTemplateInstitutionId,
                    QPTemplateId = i.QPTemplateId,
                    InstitutionId = i.InstitutionId,
                    StudentCount = i.StudentCount,
                    Departments = i.Departments.Select(d => new QPTemplateInstitutionDepartmentVM
                    {
                        QPTemplateInstitutionDepartmentId = d.QPTemplateInstitutionDepartmentId,
                        QPTemplateInstitutionId = d.QPTemplateInstitutionId,
                        DepartmentId = d.DepartmentId,
                        StudentCount = d.StudentCount
                    }).ToList(),
                    Documents = i.Documents.Select(d => new QPTemplateInstitutionDocumentVM
                    {
                        QPTemplateInstitutionDocumentId = d.QPTemplateInstitutionDocumentId,
                        QPTemplateInstitutionId = d.QPTemplateInstitutionId,
                        DocumentId = d.DocumentId,
                        QPDocumentTypeId = d.QPDocumentTypeId,
                    }).ToList()
                }).ToList()
            }).ToListAsync();

            foreach (var qPTemplate in qPTemplates)
            {
                qPTemplate.DegreeTypeName = degreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;
                qPTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == qPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                qPTemplate.CourseCode = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Code ?? string.Empty;
                qPTemplate.CourseName = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
                
                    qPTemplate.Documents.ForEach(d => {
                    d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                    d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                    d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                    });
                foreach (var institution in qPTemplate.Institutions)
                {
                    institution.InstitutionName = institutions.FirstOrDefault(i => i.InstitutionId == institution.InstitutionId)?.Name ?? string.Empty;
                    institution.InstitutionCode = institutions.FirstOrDefault(i => i.InstitutionId == institution.InstitutionId)?.Code ?? string.Empty;
                    institution.Departments.ForEach(d =>
                    {
                        d.DepartmentName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.Name ?? string.Empty;
                        d.DepartmentShortName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.ShortName ?? string.Empty;
                    });
                    institution.Documents.ForEach(d => {
                        d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                        d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                        d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                    });
                }
            }
            return qPTemplates;
        }

        public async Task<QPTemplateVM?> GetQPTemplateAsync(long qpTemplateId)
        {
            var degreeTypes = _context.DegreeTypes.ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
            var users = _context.Users.ToList();
            var qPTemplates = new List<QPTemplateVM>();
            qPTemplates = await _context.QPTemplates.Where(q=>q.QPTemplateId==qpTemplateId).Select(qpt => new QPTemplateVM
            {
                QPTemplateId = qpt.QPTemplateId,
                QPTemplateName = qpt.QPTemplateName,
                QPCode = qpt.QPCode,
                QPTemplateStatusTypeId = qpt.QPTemplateStatusTypeId,
                CourseId = qpt.CourseId,
                RegulationYear = qpt.RegulationYear,
                BatchYear = qpt.BatchYear,
                DegreeTypeId = qpt.DegreeTypeId,
                ExamYear = qpt.ExamYear,
                ExamMonth = qpt.ExamMonth,
                ExamType = qpt.ExamType,
                Semester = qpt.Semester,
                StudentCount = qpt.StudentCount,
                Documents = qpt.Documents.Select(d => new QPTemplateDocumentVM
                {
                    QPTemplateDocumentId = d.QPTemplateDocumentId,
                    QPTemplateId = d.QPTemplateId,
                    DocumentId = d.DocumentId,
                    QPDocumentTypeId = d.QPDocumentTypeId,
                }).ToList(),

                Institutions = qpt.Institutions.Select(i => new QPTemplateInstitutionVM
                {
                    QPTemplateInstitutionId = i.QPTemplateInstitutionId,
                    QPTemplateId = i.QPTemplateId,
                    InstitutionId = i.InstitutionId,
                    StudentCount = i.StudentCount,
                    Departments = i.Departments.Select(d => new QPTemplateInstitutionDepartmentVM
                    {
                        QPTemplateInstitutionDepartmentId = d.QPTemplateInstitutionDepartmentId,
                        QPTemplateInstitutionId = d.QPTemplateInstitutionId,
                        DepartmentId = d.DepartmentId,
                        StudentCount = d.StudentCount
                    }).ToList(),
                    Documents = i.Documents.Select(d => new QPTemplateInstitutionDocumentVM
                    {
                        QPTemplateInstitutionDocumentId = d.QPTemplateInstitutionDocumentId,
                        QPTemplateInstitutionId = d.QPTemplateInstitutionId,
                        DocumentId = d.DocumentId,
                        QPDocumentTypeId = d.QPDocumentTypeId,
                    }).ToList()
                }).ToList()
            }).ToListAsync();

            foreach (var qPTemplate in qPTemplates)
            {
                qPTemplate.DegreeTypeName = degreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;
                qPTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == qPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                qPTemplate.Documents.ForEach(d => {
                    d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                    d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;

                });
                foreach (var document in qPTemplate.Documents)
                {
                    document.QPDocumentTypeName = QPDocumentTypeDictionary[document.QPDocumentTypeId];
                }
                foreach (var institution in qPTemplate.Institutions)
                {
                    institution.InstitutionName = institutions.FirstOrDefault(i => i.InstitutionId == institution.InstitutionId)?.Name ?? string.Empty;
                    institution.InstitutionCode = institutions.FirstOrDefault(i => i.InstitutionId == institution.InstitutionId)?.Code ?? string.Empty;
                    institution.Departments.ForEach(d =>
                    {
                        d.DepartmentName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.Name ?? string.Empty;
                        d.DepartmentShortName = departments.FirstOrDefault(dept => dept.DepartmentId == d.DepartmentId)?.ShortName ?? string.Empty;
                    });
                    institution.Documents.ForEach(d => {
                        d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                        d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                    });
                    foreach (var document in institution.Documents)
                    {
                        document.QPDocumentTypeName = QPDocumentTypeDictionary[document.QPDocumentTypeId];
                    }
                }

                //get User template details
                // QP Generation template details
                var generationUserTemplateDetail = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateStatusTypeId == 8 || qp.QPTemplateStatusTypeId == 9);
                if (generationUserTemplateDetail != null) {
                    qPTemplate.UserQPGenerateTemplate = new UserQPTemplateVM() {
                        QPTemplateCurrentStateHeader = "QP Generation Assigned Details",
                        UserQPTemplateId = generationUserTemplateDetail?.UserQPTemplateId ?? 0,
                        UserId = generationUserTemplateDetail?.UserId ?? 0,
                        UserName = users.FirstOrDefault(u => u.UserId == generationUserTemplateDetail?.UserId)?.Name ?? string.Empty,
                        QPTemplateId = qPTemplate.QPTemplateId,
                        QPTemplateCourseCode = qPTemplate.CourseCode,
                        QPTemplateCourseName = qPTemplate.CourseName,
                        QPTemplateExamMonth = qPTemplate.ExamMonth,
                        QPTemplateExamYear = qPTemplate.ExamYear,
                        QPTemplateStatusTypeId = generationUserTemplateDetail?.QPTemplateStatusTypeId ?? 0,
                        QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == generationUserTemplateDetail?.QPTemplateStatusTypeId)?.Name ?? string.Empty,
                        QPTemplateName = qPTemplate.QPTemplateName,
                    };
                    qPTemplate.UserQPGenerateTemplate.UserDocuments = _context.UserQPTemplateDocuments
                        .Where(qpd => qpd.UserQPTemplateId == (generationUserTemplateDetail != null ? generationUserTemplateDetail.UserQPTemplateId : 0))
                        .Select(qpd => new QPTemplateDocumentVM
                        {
                            QPTemplateDocumentId = qpd.UserQPTemplateDocumentId,
                            DocumentId = qpd.DocumentId,
                            QPDocumentTypeId = qpd.QPDocumentTypeId,
                        }).ToList();
                    foreach (var userDocument in qPTemplate.UserQPGenerateTemplate.UserDocuments)
                    {
                        userDocument.QPDocumentTypeName = QPDocumentTypeDictionary[userDocument.QPDocumentTypeId];
                    }
                }

                // QP Scrutinity template details
                var ScrutinityUserTemplateDetail = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateStatusTypeId == 10 || qp.QPTemplateStatusTypeId == 11);
                if (ScrutinityUserTemplateDetail != null)
                {
                    qPTemplate.UserQPScrutinyTemplate = new UserQPTemplateVM()
                    {
                        QPTemplateCurrentStateHeader = "QP Scrutinity Assigned Details",
                        UserQPTemplateId = ScrutinityUserTemplateDetail?.UserQPTemplateId ?? 0,
                        UserId = ScrutinityUserTemplateDetail?.UserId ?? 0,
                        UserName = users.FirstOrDefault(u => u.UserId == ScrutinityUserTemplateDetail?.UserId)?.Name ?? string.Empty,
                        QPTemplateId = qPTemplate.QPTemplateId,
                        QPTemplateCourseCode = qPTemplate.CourseCode,
                        QPTemplateCourseName = qPTemplate.CourseName,
                        QPTemplateExamMonth = qPTemplate.ExamMonth,
                        QPTemplateExamYear = qPTemplate.ExamYear,
                        QPTemplateStatusTypeId = ScrutinityUserTemplateDetail?.QPTemplateStatusTypeId ?? 0,
                        QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == ScrutinityUserTemplateDetail?.QPTemplateStatusTypeId)?.Name ?? string.Empty,
                        QPTemplateName = qPTemplate.QPTemplateName,
                    };
                    qPTemplate.UserQPScrutinyTemplate.UserDocuments = _context.UserQPTemplateDocuments
                        .Where(qpd => qpd.UserQPTemplateId == (ScrutinityUserTemplateDetail != null ? ScrutinityUserTemplateDetail.UserQPTemplateId : 0))
                        .Select(qpd => new QPTemplateDocumentVM
                        {
                            QPTemplateDocumentId = qpd.UserQPTemplateDocumentId,
                            DocumentId = qpd.DocumentId,
                            QPDocumentTypeId = qpd.QPDocumentTypeId,
                        }).ToList();
                    foreach (var userDocument in qPTemplate.UserQPScrutinyTemplate.UserDocuments)
                    {
                        userDocument.QPDocumentTypeName = QPDocumentTypeDictionary[userDocument.QPDocumentTypeId];
                    }
                }

                // QP Selection template details
                var SelectionUserTemplateDetail = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateStatusTypeId == 12 || qp.QPTemplateStatusTypeId == 13);
                if (SelectionUserTemplateDetail != null)
                {
                    qPTemplate.UserQPSelectionTemplate = new UserQPTemplateVM()
                    {
                        QPTemplateCurrentStateHeader = "QP Selection Assigned Details",
                        UserQPTemplateId = SelectionUserTemplateDetail?.UserQPTemplateId ?? 0,
                        UserId = SelectionUserTemplateDetail?.UserId ?? 0,
                        UserName = users.FirstOrDefault(u => u.UserId == SelectionUserTemplateDetail?.UserId)?.Name ?? string.Empty,
                        QPTemplateId = qPTemplate.QPTemplateId,
                        QPTemplateCourseCode = qPTemplate.CourseCode,
                        QPTemplateCourseName = qPTemplate.CourseName,
                        QPTemplateExamMonth = qPTemplate.ExamMonth,
                        QPTemplateExamYear = qPTemplate.ExamYear,
                        QPTemplateStatusTypeId = SelectionUserTemplateDetail?.QPTemplateStatusTypeId ?? 0,
                        QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == SelectionUserTemplateDetail?.QPTemplateStatusTypeId)?.Name ?? string.Empty,
                        QPTemplateName = qPTemplate.QPTemplateName,
                    };
                    qPTemplate.UserQPSelectionTemplate.UserDocuments = _context.UserQPTemplateDocuments
                        .Where(qpd => qpd.UserQPTemplateId == (SelectionUserTemplateDetail != null ? SelectionUserTemplateDetail.UserQPTemplateId : 0))
                        .Select(qpd => new QPTemplateDocumentVM
                        {
                            QPTemplateDocumentId = qpd.UserQPTemplateDocumentId,
                            DocumentId = qpd.DocumentId,
                            QPDocumentTypeId = qpd.QPDocumentTypeId,
                        }).ToList();
                    foreach (var userDocument in qPTemplate.UserQPSelectionTemplate.UserDocuments)
                    {
                        userDocument.QPDocumentTypeName = QPDocumentTypeDictionary[userDocument.QPDocumentTypeId];
                    }
                }
            }
            return qPTemplates.FirstOrDefault();
        }

        public async Task<List<UserQPTemplateVM>> GetQPTemplatesByUserIdAsync(long userId)
        {
            return await GetUserQPTemplatesAsync(userId);

        }

        public async Task<bool?> AssignTemplateForQPGenerationAsync(long userId, long qpTemplateId)
        {
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 2; //QP Generation Allocated

            var userQPTemplate = new UserQPTemplate()
            {
                QPTemplateId = qpTemplateId,
                UserId = userId,
                QPTemplateStatusTypeId = 8,//Generation QP InProgress
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
            _context.UserQPTemplates.Add(userQPTemplate);
            await _context.SaveChangesAsync();
            var qpSyllabusDocument = await _context.QPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 1 && qptd.QPTemplateId == qpTemplateId);
            var userQPSyllabusDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 1,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = qpSyllabusDocument?.DocumentId ?? 0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPSyllabusDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPSyllabusDocument);


            var qpGenerationDocument = await _context.QPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId ==3 && qptd.QPTemplateId == qpTemplateId);
            var userQPDocument = new UserQPTemplateDocument() { 
                QPDocumentTypeId = 6,
                UserQPTemplateId =userQPTemplate.UserQPTemplateId,
                DocumentId = qpGenerationDocument?.DocumentId??0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPDocument);

            var generationQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 7,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = 0
            };
            AuditHelper.SetAuditPropertiesForInsert(generationQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(generationQPDocument);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool?> SubmitGeneratedQPAsync(long userId, long qpTemplateId, long documentId)
        {
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateId);
            
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 3; //QP Pending for Scrutiny
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateId ==qpTemplateId);
            
            if (userQPTemplate == null) return null;
            userQPTemplate.QPTemplateStatusTypeId = 9;

            var generatedQpDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 7 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
            if (generatedQpDocument != null)
            {
                generatedQpDocument.DocumentId = documentId;
                AuditHelper.SetAuditPropertiesForUpdate(generatedQpDocument, 1);
            }

            AuditHelper.SetAuditPropertiesForUpdate(userQPTemplate, 1);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool?> AssignTemplateForQPScrutinyAsync(long userId, long qpTemplateId)
        {
            var qpUserTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateId && qp.QPTemplateStatusTypeId==9);
            
            if (qpUserTemplate == null) return null;

            var userQPTemplate = new UserQPTemplate()
            {
                QPTemplateId = qpUserTemplate.QPTemplateId,
                UserId = userId,
                QPTemplateStatusTypeId = 10,//Scrutinize QP InProgress
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
            _context.UserQPTemplates.Add(userQPTemplate);
            await _context.SaveChangesAsync();

            var qpSyllabusDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 1 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
            var userQPSyllabusDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 1,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = qpSyllabusDocument?.DocumentId ?? 0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPSyllabusDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPSyllabusDocument);

            var qpGenerationDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 7 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
            var userQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 8,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = qpGenerationDocument?.DocumentId ?? 0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPDocument);

            var scrutinizedQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 9,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = 0
            };
            AuditHelper.SetAuditPropertiesForInsert(scrutinizedQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(scrutinizedQPDocument);

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool?> SubmitScrutinizedQPAsync(long userId, long qpTemplateId, long documentId)
        {
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateId);

            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 5; //QP Pending for Selection
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateId == qpTemplateId);

            if (userQPTemplate == null) return null;
            userQPTemplate.QPTemplateStatusTypeId = 11;//Scrutinized QP Submitted
            AuditHelper.SetAuditPropertiesForUpdate(userQPTemplate, 1);
            await _context.SaveChangesAsync();

            var qpScrutinizedUserTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateId && qp.QPTemplateStatusTypeId == 11);

            if (qpScrutinizedUserTemplate == null) return null;

            var userQPTemplateForSelection = new UserQPTemplate()
            {
                QPTemplateId = qpScrutinizedUserTemplate.QPTemplateId,
                UserId = 1,
                QPTemplateStatusTypeId = 12,//Selection QP InProgress
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplateForSelection, 1);
            _context.UserQPTemplates.Add(userQPTemplateForSelection);

            var scrutinizedQPDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 9 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
            if(scrutinizedQPDocument != null)
            {
                scrutinizedQPDocument.DocumentId = documentId;
                AuditHelper.SetAuditPropertiesForUpdate(scrutinizedQPDocument, 1);
            }

            await _context.SaveChangesAsync();

            var userQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 10,//For QP Selection
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = documentId
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPDocument);

            var selectedQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 11,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = 0
            };
            AuditHelper.SetAuditPropertiesForInsert(selectedQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(selectedQPDocument);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool?> SubmitSelectedQPAsync(long userId, long qpTemplateId, long documentId)
        {
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateId);

            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 6; //QP Selected
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateId == qpTemplateId && uqp.QPTemplateStatusTypeId ==12);

            if (userQPTemplate == null) return null;
            userQPTemplate.QPTemplateStatusTypeId = 13;//Selected QP Submitted
            AuditHelper.SetAuditPropertiesForUpdate(userQPTemplate, 1);
            await _context.SaveChangesAsync();


            var selectedQpDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 11 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
            if(selectedQpDocument != null)
            {
                selectedQpDocument.DocumentId = documentId;
                AuditHelper.SetAuditPropertiesForUpdate(selectedQpDocument, 1);
            }
            await _context.SaveChangesAsync();
            return true;
        }
        private async Task<List<UserQPTemplateVM>> GetUserQPTemplatesAsync(long userId)
        {
            try
            {
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qpTemplateStatuss =_context.QPTemplateStatusTypes.ToList();
            var qPTemplates = new List<UserQPTemplateVM>();
            var users = _context.Users.ToList();
            var userQPTemplates = _context.UserQPTemplates.Where(uqp => uqp.UserId == userId).ToList();
            var qpTemplateIds = userQPTemplates.Select(uqp => uqp.QPTemplateId).ToList();

            var qpTemplates = await _context.QPTemplates.Where(qp => qpTemplateIds.Contains(qp.QPTemplateId)).ToListAsync();
            userQPTemplates.ForEach(userQPTemplate =>
            {
                qPTemplates.Add(new UserQPTemplateVM() {
                    UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                    QPTemplateId = userQPTemplate.QPTemplateId,
                    QPTemplateName = qpTemplates.ToList().FirstOrDefault(q => q.QPTemplateId == userQPTemplate.QPTemplateId)?.QPTemplateName ?? string.Empty,
                    UserId = userQPTemplate.UserId,
                    UserName = users.FirstOrDefault(u => u.UserId == userQPTemplate.UserId)?.Name ?? string.Empty,
                    QPTemplateStatusTypeId = userQPTemplate.QPTemplateStatusTypeId, 
                    QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userQPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty,
                    
                });
            });
                foreach (var qPTemplate in qPTemplates)
                {
                    qPTemplate.UserDocuments = _context.UserQPTemplateDocuments
                        .Where(qpd => qpd.UserQPTemplateId == (qPTemplate != null ? qPTemplate.UserQPTemplateId : 0))
                        .Select(qpd => new QPTemplateDocumentVM
                        {
                            QPTemplateId = qpd.UserQPTemplateId,
                            QPTemplateDocumentId = qpd.UserQPTemplateDocumentId,
                            DocumentId = qpd.DocumentId,
                            QPDocumentTypeId = qpd.QPDocumentTypeId,
                        }).ToList();
                    foreach (var userDocument in qPTemplate.UserDocuments)
                    {
                        userDocument.QPDocumentTypeName = QPDocumentTypeDictionary[userDocument.QPDocumentTypeId];
                        userDocument.DocumentName = documents.FirstOrDefault(d => d.DocumentId == userDocument.DocumentId)?.Name ?? string.Empty;
                        userDocument.DocumentUrl = documents.FirstOrDefault(d => d.DocumentId == userDocument.DocumentId)?.Url ?? string.Empty;
                    }
                }
                return qPTemplates;
            }
            catch (Exception ex)
            {
                throw ;
            }
        }
    }
}
