using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.QPSettings;
using AutoMapper;
using Aspose.Words.Drawing;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Aspose.Words;
using SKCE.Examination.Services.Helpers;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml.Office2010.Word;
namespace SKCE.Examination.Services.QPSettings
{
    public class QpTemplateService 
    {
        private readonly ExaminationDbContext _context;
        private readonly IMapper _mapper;
        private readonly BookmarkProcessor _bookmarkProcessor;
        private static readonly Dictionary<long, string> QPDocumentTypeDictionary = new Dictionary<long, string>
        {
            { 1, "Course syllabus document" },
            { 2, "Preview QP document for print" },
            { 3, "Preview QP Answer document for print" },
            { 4, "For QP Generation" },
            { 5, "Generated QP" },
            { 6, "For QP Scrutiny" },
            { 7, "Scrutinized QP" },
            { 8, "For QP Selection" },
            { 9, "Selected QP" }
        };
        public QpTemplateService(ExaminationDbContext context, IMapper mapper, BookmarkProcessor bookmarkProcessor)
        {
            _bookmarkProcessor = bookmarkProcessor;
            _context = context;
            _mapper = mapper;
        }
        public async Task<QPTemplateVM?> GetQPTemplateByCourseIdAsync(long courseId)
        {
            

            var documents = _context.Documents.ToList();
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
            qPTemplate.QPDocuments = new List<QPDocumentVM>();
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
            qPTemplate.QPTemplateName = $"{qPTemplate.CourseCode} for {qPTemplate.RegulationYear}-{qPTemplate.ExamYear}-{qPTemplate.ExamMonth}-{qPTemplate.DegreeTypeName}-{qPTemplate.ExamType}";

            AuditHelper.SetAuditPropertiesForInsert(qPTemplate, 1);

            var qpTemplateId = _context.QPTemplates.FirstOrDefault(qp => qp.CourseId == courseId 
                            && qp.RegulationYear == qPTemplate.RegulationYear
                            && qp.BatchYear == qPTemplate.BatchYear
                            && qp.ExamYear == qPTemplate.ExamYear
                            && qp.ExamMonth == qPTemplate.ExamMonth
                            && qp.ExamType == qPTemplate.ExamType
                            && qp.DegreeTypeId == qPTemplate.DegreeTypeId)?.QPTemplateId ?? 0;
            if (qpTemplateId > 0)
                return await GetQPTemplateAsync(qpTemplateId);

            var syllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(d => d.CourseId == qPTemplate.CourseId);
            if (syllabusDocument != null)
            {
                qPTemplate.Documents.Add(new QPTemplateDocumentVM
                {
                    QPTemplateDocumentId = 0,
                    QPTemplateId = qPTemplate.QPTemplateId,
                    DocumentId = syllabusDocument.DocumentId,
                    QPDocumentTypeId = 1,
                    QPDocumentTypeName = QPDocumentTypeDictionary[1]
                });
            }
            
            foreach (var document in qPTemplate.Documents)
            {
                document.DocumentName = documents.FirstOrDefault(di => di.DocumentId == document.DocumentId)?.Name ?? string.Empty;
                document.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == document.DocumentId)?.Url ?? string.Empty;
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
                  
                    var qpDocument = _context.QPDocuments.FirstOrDefault(d => d.InstitutionId == institution.InstitutionId && d.RegulationYear== qPTemplate.RegulationYear && d.DegreeTypeName == qPTemplate.DegreeTypeName && d.DocumentTypeId ==2 && d.ExamType.ToLower().Contains(qPTemplate.ExamType.ToLower()));
                    institutionVM.Documents = new List<QPTemplateInstitutionDocumentVM>();
                    if (qpDocument != null)
                    {
                        institutionVM.Documents.Add(new QPTemplateInstitutionDocumentVM
                        {
                            QPTemplateInstitutionDocumentId = 0,
                            QPTemplateInstitutionId = institutionVM.QPTemplateInstitutionId,
                            DocumentId = qpDocument.DocumentId,
                            QPDocumentTypeId = 2,
                            QPDocumentTypeName = QPDocumentTypeDictionary[2]
                        });
                    }
                    var qpAkDocument = _context.QPDocuments.FirstOrDefault(d => d.InstitutionId == institution.InstitutionId && d.RegulationYear == qPTemplate.RegulationYear && d.DegreeTypeName == qPTemplate.DegreeTypeName && d.DocumentTypeId == 3 && d.ExamType.ToLower().Contains(qPTemplate.ExamType.ToLower()));
                    
                    if(qpAkDocument != null)
                    {
                        institutionVM.Documents.Add(new QPTemplateInstitutionDocumentVM
                        {
                            QPTemplateInstitutionDocumentId = 0,
                            QPTemplateInstitutionId = institutionVM.QPTemplateInstitutionId,
                            DocumentId = qpAkDocument.DocumentId,
                            QPDocumentTypeId = 3,
                            QPDocumentTypeName = QPDocumentTypeDictionary[3],
                        });
                        var qpDocumentForGeneration = new QPDocumentVM
                        {
                            QPDocumentId = qpAkDocument.QPDocumentId,
                            InstitutionId = qpAkDocument.InstitutionId,
                            QPDocumentName = qpAkDocument.QPDocumentName,
                            QPOnlyDocumentId = qpDocument?.QPDocumentId??0,
                        };
                        qpDocumentForGeneration.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId=0,IsQPOnly=false,StatusTypeId=8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                        qpDocumentForGeneration.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName="", UserId = 0, UserName = string.Empty });
                        qPTemplate.QPDocuments.Add(qpDocumentForGeneration);
                    }
                    foreach (var qPDocument in institutionVM.Documents)
                    {
                        qPDocument.DocumentName = documents.FirstOrDefault(di => di.DocumentId == qPDocument.DocumentId)?.Name ?? string.Empty;
                        qPDocument.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == qPDocument.DocumentId)?.Url ?? string.Empty;
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
            if (qPTemplateVM.QPTemplateId > 0) return await UpdateQpTemplateAsync(qPTemplateVM);
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
            qPTemplateVM.QPDocuments.ForEach(i =>
            {
                var institutionVM = qPTemplate.Institutions.FirstOrDefault(qpti => qpti.InstitutionId == i.InstitutionId);
                if(institutionVM != null)
                {
                    i.QPAssignedUsers.ForEach(u =>
                    {
                        if(u.UserId > 0)
                        AssignTemplateForQPGenerationAsync(u, institutionVM.QPTemplateInstitutionId,i);
                    });
                }
            });
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
                foreach (var qpDocument in qPTemplateVM.QPDocuments)
                {
                    foreach (var assignedUser in qpDocument.QPAssignedUsers)
                    {
                        var userQPTemplate = _context.UserQPTemplates.FirstOrDefault(uqp => uqp.UserQPTemplateId == assignedUser.UserQPTemplateId && uqp.IsActive);
                        if (userQPTemplate == null)
                        {
                            var newAssignedUser = new UserQPTemplate
                            {
                                IsQPOnly = assignedUser.IsQPOnly,
                                QPTemplateInstitutionId = i.QPTemplateInstitutionId,
                                UserId = assignedUser.UserId,
                                QPTemplateStatusTypeId = 8,
                                QPDocumentId = assignedUser.IsQPOnly ? qpDocument.QPOnlyDocumentId : qpDocument.QPDocumentId
                            };
                            AuditHelper.SetAuditPropertiesForInsert(newAssignedUser, 1);
                            _context.UserQPTemplates.Add(newAssignedUser);
                             _context.SaveChanges();

                            var qpSyllabusDocument = _context.QPTemplateDocuments.FirstOrDefault(qptd => qptd.QPDocumentTypeId == 1 && qptd.QPTemplateId == qpTemplate.QPTemplateId);
                            var userQPSyllabusDocument = new UserQPTemplateDocument()
                            {
                                QPDocumentTypeId = 1,
                                UserQPTemplateId = newAssignedUser.UserQPTemplateId,
                                DocumentId = qpSyllabusDocument?.DocumentId ?? 0
                            };
                            AuditHelper.SetAuditPropertiesForInsert(userQPSyllabusDocument, 1);
                            _context.UserQPTemplateDocuments.Add(userQPSyllabusDocument);


                            var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qptd => qptd.QPDocumentId == newAssignedUser.QPDocumentId);
                            var userQPDocument = new UserQPTemplateDocument()
                            {
                                QPDocumentTypeId = 4,
                                UserQPTemplateId = newAssignedUser.UserQPTemplateId,
                                DocumentId = qpGenerationDocument?.DocumentId ?? 0
                            };
                            AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
                            _context.UserQPTemplateDocuments.Add(userQPDocument);

                            var generationQPDocument = new UserQPTemplateDocument()
                            {
                                QPDocumentTypeId = 5,
                                UserQPTemplateId = newAssignedUser.UserQPTemplateId,
                                DocumentId = 0
                            };
                            AuditHelper.SetAuditPropertiesForInsert(generationQPDocument, 1);
                            _context.UserQPTemplateDocuments.Add(generationQPDocument);

                        }

                    }
                }
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
                  
                    i.UserQPSelectionTemplates.ForEach(async u =>
                    {
                        if (u.UserQPTemplateId > 0)
                        {
                            var finalSelectedDocument = u.UserDocuments.FirstOrDefault(d => d.QPDocumentTypeId == 10 && d.DocumentId > 0);
                            if (finalSelectedDocument != null && qpTemplate.QPTemplateStatusTypeId == 5)
                            {
                                await SubmitSelectedQPAsync(1, qpTemplate.QPTemplateId, finalSelectedDocument.DocumentId);
                            }
                        }
                    });
                }
            });
            
            await _context.SaveChangesAsync();
            return qpTemplate;
        }
        public async Task<List<QPTemplateVM>> GetQPTemplatesAsync(long institutionId)
        {
            var degreeTypes = _context.DegreeTypes.ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qPTemplates = new List<QPTemplateVM>();
            var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
            var users = _context.Users.ToList();
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

                Institutions = qpt.Institutions.Where(i=>i.InstitutionId== institutionId).Select(i => new QPTemplateInstitutionVM
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
                    institution.UserQPGenerateTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 9) && u.IsActive)
                        .Select(u => new UserQPTemplateVM
                        {
                            UserQPTemplateId = u.UserQPTemplateId,
                            UserId = u.UserId,
                            QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                            QPTemplateCourseCode = qPTemplate.CourseCode,
                            QPTemplateCourseName = qPTemplate.CourseName,
                            QPTemplateExamMonth = qPTemplate.ExamMonth,
                            QPTemplateExamYear = qPTemplate.ExamYear,
                            QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                            QPTemplateName = qPTemplate.QPTemplateName,
                            QPDocumentId= u.QPDocumentId,
                            UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                .Select(ud => new QPTemplateDocumentVM
                                {
                                    QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                    DocumentId = ud.DocumentId,
                                    QPDocumentTypeId = ud.QPDocumentTypeId
                                }).ToList()
                        }).ToList();
                    foreach (var userTemplate in institution.UserQPGenerateTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        userTemplate.UserDocuments.ForEach(d =>
                        {
                            d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                            d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                            d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                        });
                        if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                        {
                            var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                            if (qpGenerationDocument != null)
                            {
                                qPTemplate.QPDocuments.Add(new QPDocumentVM
                                {
                                    QPDocumentId = qpGenerationDocument.QPDocumentId,
                                    InstitutionId = qpGenerationDocument.InstitutionId,
                                    QPDocumentName = qpGenerationDocument.QPDocumentName,
                                });
                            }
                        }
                        qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPAssignedUsers.Add(
                            new QPDocumentUserVM
                            {
                                UserId = userTemplate.UserId,
                                UserName = userTemplate.UserName,
                                StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                                StatusTypeName = userTemplate.QPTemplateStatusTypeName
                            });

                    }
                    institution.UserQPScrutinyTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 10 || u.QPTemplateStatusTypeId == 11) && u.IsActive)
                        .Select(u => new UserQPTemplateVM
                        {
                            UserQPTemplateId = u.UserQPTemplateId,
                            UserId = u.UserId,
                            QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                            QPTemplateCourseCode = qPTemplate.CourseCode,
                            QPTemplateCourseName = qPTemplate.CourseName,
                            QPTemplateExamMonth = qPTemplate.ExamMonth,
                            QPTemplateExamYear = qPTemplate.ExamYear,
                            QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                            QPTemplateName = qPTemplate.QPTemplateName,
                            QPDocumentId = u.QPDocumentId,
                            UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                .Select(ud => new QPTemplateDocumentVM
                                {
                                    QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                    DocumentId = ud.DocumentId,
                                    QPDocumentTypeId = ud.QPDocumentTypeId
                                }).ToList()
                        }).ToList();
                    foreach (var userTemplate in institution.UserQPScrutinyTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        userTemplate.UserDocuments.ForEach(d =>
                        {
                            d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                            d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                            d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                        });
                        if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                        {
                            var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                            if (qpGenerationDocument != null)
                            {
                                qPTemplate.QPDocuments.Add(new QPDocumentVM
                                {
                                    QPDocumentId = qpGenerationDocument.QPDocumentId,
                                    InstitutionId = qpGenerationDocument.InstitutionId,
                                    QPDocumentName = qpGenerationDocument.QPDocumentName,
                                });
                            }
                        }
                        qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPScrutinityUsers.Add(
                            new QPDocumentUserVM
                            {
                                UserId = userTemplate.UserId,
                                UserName = userTemplate.UserName,
                                StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                                StatusTypeName = userTemplate.QPTemplateStatusTypeName
                            });
                    }
                    ;
                    institution.UserQPSelectionTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 12 || u.QPTemplateStatusTypeId == 13) && u.IsActive)
                        .Select(u => new UserQPTemplateVM
                        {
                            UserQPTemplateId = u.UserQPTemplateId,
                            UserId = u.UserId,
                            QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                            QPTemplateCourseCode = qPTemplate.CourseCode,
                            QPTemplateCourseName = qPTemplate.CourseName,
                            QPTemplateExamMonth = qPTemplate.ExamMonth,
                            QPTemplateExamYear = qPTemplate.ExamYear,
                            QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                            QPTemplateName = qPTemplate.QPTemplateName,
                            QPDocumentId = u.QPDocumentId,
                            UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                .Select(ud => new QPTemplateDocumentVM
                                {
                                    QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                    DocumentId = ud.DocumentId,
                                    QPDocumentTypeId = ud.QPDocumentTypeId
                                }).ToList()
                        }).ToList();
                    foreach (var userTemplate in institution.UserQPSelectionTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        userTemplate.UserDocuments.ForEach(d =>
                        {
                            d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                            d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                            d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                        });
                        if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                        {
                            var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                            if (qpGenerationDocument != null)
                            {
                                qPTemplate.QPDocuments.Add(new QPDocumentVM
                                {
                                    QPDocumentId = qpGenerationDocument.QPDocumentId,
                                    InstitutionId = qpGenerationDocument.InstitutionId,
                                    QPDocumentName = qpGenerationDocument.QPDocumentName,
                                });
                            }
                        }
                        qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPSelectedUsers.Add(
                            new QPDocumentUserVM
                            {
                                UserId = userTemplate.UserId,
                                UserName = userTemplate.UserName,
                                StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                                StatusTypeName = userTemplate.QPTemplateStatusTypeName
                            });
                    }
                }
                foreach (var qPDocument in qPTemplate.QPDocuments)
                {
                    if (qPDocument.QPAssignedUsers.Count < 2)
                        qPDocument.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                }
            }
            return qPTemplates;
        }
        public async Task<QPTemplateVM?> GetQPTemplateAsync(long qpTemplateId)
        {
            try
            {
                var degreeTypes = _context.DegreeTypes.ToList();
                var institutions = _context.Institutions.ToList();
                var departments = _context.Departments.ToList();
                var courses = _context.Courses.ToList();
                var documents = _context.Documents.ToList();
                var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
                var users = _context.Users.ToList();
                var qPTemplates = new List<QPTemplateVM>();
                qPTemplates = await _context.QPTemplates.Where(q => q.QPTemplateId == qpTemplateId).Select(qpt => new QPTemplateVM
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
                    qPTemplate.CourseCode = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Code ?? string.Empty;
                    qPTemplate.CourseName = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
                    foreach (var document in qPTemplate.Documents)
                    {
                        document.QPDocumentTypeName = QPDocumentTypeDictionary[document.QPDocumentTypeId];
                        document.DocumentName = documents.FirstOrDefault(di => di.DocumentId == document.DocumentId)?.Name ?? string.Empty;
                        document.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == document.DocumentId)?.Url ?? string.Empty;
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
                        //get User template details
                        // QP Generation template details
                        institution.UserQPGenerateTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 9) && u.IsActive)
                            .Select(u => new UserQPTemplateVM
                            {
                                UserQPTemplateId = u.UserQPTemplateId,
                                UserId = u.UserId,
                                QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                                QPTemplateCourseCode = qPTemplate.CourseCode,
                                QPTemplateCourseName = qPTemplate.CourseName,
                                QPTemplateExamMonth = qPTemplate.ExamMonth,
                                QPTemplateExamYear = qPTemplate.ExamYear,
                                QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                                QPTemplateName = qPTemplate.QPTemplateName,
                                QPDocumentId= u.QPDocumentId,
                                IsQPOnly = u.IsQPOnly,
                                UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                .Select(ud => new QPTemplateDocumentVM
                                {
                                    QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                    DocumentId = ud.DocumentId,
                                    QPDocumentTypeId = ud.QPDocumentTypeId
                                }).ToList()
                            }).ToList();
                        foreach (var userTemplate in institution.UserQPGenerateTemplates)
                        {
                            userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                            userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                            userTemplate.UserDocuments.ForEach(d =>
                            {
                                d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                                d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                                d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                            });
                            if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                            {
                                var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                                if (qpGenerationDocument != null)
                                {
                                    var qpOnlyGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd =>
                                                                      qpd.InstitutionId == qpGenerationDocument.InstitutionId
                                                                   && qpd.RegulationYear == qpGenerationDocument.RegulationYear
                                                                   && qpd.DegreeTypeName == qpGenerationDocument.DegreeTypeName
                                                                   && qpd.ExamType == qpGenerationDocument.ExamType
                                                                   && qpd.DocumentTypeId == 2);
                                    qPTemplate.QPDocuments.Add(new QPDocumentVM
                                    {
                                        QPDocumentId = qpGenerationDocument.QPDocumentId,
                                        InstitutionId = qpGenerationDocument.InstitutionId,
                                        QPDocumentName = qpGenerationDocument.QPDocumentName,
                                        QPOnlyDocumentId = qpOnlyGenerationDocument?.QPDocumentId??0
                                    });
                                }
                            }
                            qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPAssignedUsers.Add(
                                new QPDocumentUserVM {
                                    UserQPTemplateId= userTemplate.UserQPTemplateId,
                                    IsQPOnly=userTemplate.IsQPOnly,
                                    UserId = userTemplate.UserId,
                                    UserName = userTemplate.UserName,
                                    StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                                    StatusTypeName = userTemplate.QPTemplateStatusTypeName
                                });
                        }
                        
                        // QP Scrutiny template details
                        institution.UserQPScrutinyTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 10 || u.QPTemplateStatusTypeId == 11) && u.IsActive)
                            .Select(u => new UserQPTemplateVM
                            {
                                UserQPTemplateId = u.UserQPTemplateId,
                                UserId = u.UserId,
                                QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                                QPTemplateCourseCode = qPTemplate.CourseCode,
                                QPTemplateCourseName = qPTemplate.CourseName,
                                QPTemplateExamMonth = qPTemplate.ExamMonth,
                                QPTemplateExamYear = qPTemplate.ExamYear,
                                QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                                QPTemplateName = qPTemplate.QPTemplateName,
                                QPDocumentId = u.QPDocumentId,
                                IsQPOnly = u.IsQPOnly,
                                UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                    .Select(ud => new QPTemplateDocumentVM
                                    {
                                        QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                        DocumentId = ud.DocumentId,
                                        QPDocumentTypeId = ud.QPDocumentTypeId
                                    }).ToList()
                            }).ToList();
                        foreach (var userTemplate in institution.UserQPScrutinyTemplates)
                        {
                            userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                            userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                            userTemplate.UserDocuments.ForEach(d =>
                            {
                                d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                                d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                                d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                            });
                            if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                            {
                                var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                                if (qpGenerationDocument != null)
                                {
                                    qPTemplate.QPDocuments.Add(new QPDocumentVM
                                    {
                                        QPDocumentId = qpGenerationDocument.QPDocumentId,
                                        InstitutionId = qpGenerationDocument.InstitutionId,
                                        QPDocumentName = qpGenerationDocument.QPDocumentName,
                                    });
                                }
                            }
                            qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPScrutinityUsers.Add(
                                new QPDocumentUserVM
                                {
                                    UserQPTemplateId = userTemplate.UserQPTemplateId,
                                    IsQPOnly = userTemplate.IsQPOnly,
                                    UserId = userTemplate.UserId,
                                    UserName = userTemplate.UserName,
                                    StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                                    StatusTypeName = userTemplate.QPTemplateStatusTypeName
                                });
                        }
                        // QP Selection template details
                        institution.UserQPSelectionTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 12 || u.QPTemplateStatusTypeId == 13) && u.IsActive)
                            .Select(u => new UserQPTemplateVM
                            {
                                UserQPTemplateId = u.UserQPTemplateId,
                                UserId = u.UserId,
                                QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                                QPTemplateCourseCode = qPTemplate.CourseCode,
                                QPTemplateCourseName = qPTemplate.CourseName,
                                QPTemplateExamMonth = qPTemplate.ExamMonth,
                                QPTemplateExamYear = qPTemplate.ExamYear,
                                QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                                QPTemplateName = qPTemplate.QPTemplateName,
                                QPDocumentId = u.QPDocumentId,
                                IsQPOnly = u.IsQPOnly,
                                UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                    .Select(ud => new QPTemplateDocumentVM
                                    {
                                        QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                        DocumentId = ud.DocumentId,
                                        QPDocumentTypeId = ud.QPDocumentTypeId
                                    }).ToList()
                            }).ToList();
                        foreach (var userTemplate in institution.UserQPSelectionTemplates)
                        {
                            userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                            userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                            userTemplate.UserDocuments.ForEach(d =>
                            {
                                d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                                d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                                d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                            });
                            if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                            {
                                var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                                if (qpGenerationDocument != null)
                                {
                                    qPTemplate.QPDocuments.Add(new QPDocumentVM
                                    {
                                        QPDocumentId = qpGenerationDocument.QPDocumentId,
                                        InstitutionId = qpGenerationDocument.InstitutionId,
                                        QPDocumentName = qpGenerationDocument.QPDocumentName,
                                    });
                                }
                            }
                            qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPSelectedUsers.Add(
                                new QPDocumentUserVM
                                {
                                    UserQPTemplateId = userTemplate.UserQPTemplateId,
                                    IsQPOnly = userTemplate.IsQPOnly,
                                    UserId = userTemplate.UserId,
                                    UserName = userTemplate.UserName,
                                    StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                                    StatusTypeName = userTemplate.QPTemplateStatusTypeName
                                });
                        }

                        // add default record if we have only one qp assigned user
                    }
                    foreach (var qPDocument  in qPTemplate.QPDocuments)
                    {
                        if(qPDocument.QPAssignedUsers.Count <2)
                            qPDocument.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                    }
                }
                return qPTemplates.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<List<QPTemplateVM>> GetQPTemplateByStatusIdAsync(long statusId)
        {
            var degreeTypes = _context.DegreeTypes.ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
            var users = _context.Users.ToList();
            var qPTemplates = new List<QPTemplateVM>();
            qPTemplates = await _context.QPTemplates.Where(q => q.QPTemplateStatusTypeId == statusId).Select(qpt => new QPTemplateVM
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
                qPTemplate.CourseCode = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Code ?? string.Empty;
                qPTemplate.CourseName = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
                foreach (var document in qPTemplate.Documents)
                {
                    document.QPDocumentTypeName = QPDocumentTypeDictionary[document.QPDocumentTypeId];
                    document.DocumentName = documents.FirstOrDefault(di => di.DocumentId == document.DocumentId)?.Name ?? string.Empty;
                    document.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == document.DocumentId)?.Url ?? string.Empty;
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

                    //get User template details
                    // QP Generation template details
                    institution.UserQPGenerateTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 9))
                        .Select(u => new UserQPTemplateVM
                        {
                            UserQPTemplateId = u.UserQPTemplateId,
                            UserId = u.UserId,
                            QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                            QPTemplateCourseCode = qPTemplate.CourseCode,
                            QPTemplateCourseName = qPTemplate.CourseName,
                            QPTemplateExamMonth = qPTemplate.ExamMonth,
                            QPTemplateExamYear = qPTemplate.ExamYear,
                            QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                            QPTemplateName = qPTemplate.QPTemplateName,
                            UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                .Select(ud => new QPTemplateDocumentVM
                                {
                                    QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                    DocumentId = ud.DocumentId,
                                    QPDocumentTypeId = ud.QPDocumentTypeId
                                }).ToList()
                        }).ToList();
                    foreach (var userTemplate in institution.UserQPGenerateTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        userTemplate.UserDocuments.ForEach(d =>
                        {
                            d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                            d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                            d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                        });
                    }
                        
                    // QP Scrutinity template details
                    institution.UserQPScrutinyTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 10 || u.QPTemplateStatusTypeId == 11))
                        .Select(u => new UserQPTemplateVM
                        {
                            UserQPTemplateId = u.UserQPTemplateId,
                            UserId = u.UserId,
                            QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                            QPTemplateCourseCode = qPTemplate.CourseCode,
                            QPTemplateCourseName = qPTemplate.CourseName,
                            QPTemplateExamMonth = qPTemplate.ExamMonth,
                            QPTemplateExamYear = qPTemplate.ExamYear,
                            QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                            QPTemplateName = qPTemplate.QPTemplateName,
                            UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                .Select(ud => new QPTemplateDocumentVM
                                {
                                    QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                    DocumentId = ud.DocumentId,
                                    QPDocumentTypeId = ud.QPDocumentTypeId
                                }).ToList()
                        }).ToList();
                    foreach (var userTemplate in institution.UserQPScrutinyTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        userTemplate.UserDocuments.ForEach(d =>
                        {
                            d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                            d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                            d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                        });
                    }

                    // QP Selection template details
                    institution.UserQPSelectionTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateInstitutionId == institution.QPTemplateInstitutionId && (u.QPTemplateStatusTypeId == 12 || u.QPTemplateStatusTypeId == 13))
                        .Select(u => new UserQPTemplateVM
                        {
                            UserQPTemplateId = u.UserQPTemplateId,
                            UserId = u.UserId,
                            QPTemplateInstitutionId = u.QPTemplateInstitutionId,
                            QPTemplateCourseCode = qPTemplate.CourseCode,
                            QPTemplateCourseName = qPTemplate.CourseName,
                            QPTemplateExamMonth = qPTemplate.ExamMonth,
                            QPTemplateExamYear = qPTemplate.ExamYear,
                            QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                            QPTemplateName = qPTemplate.QPTemplateName,
                            UserDocuments = _context.UserQPTemplateDocuments.Where(ud => ud.UserQPTemplateId == u.UserQPTemplateId)
                                .Select(ud => new QPTemplateDocumentVM
                                {
                                    QPTemplateDocumentId = ud.UserQPTemplateDocumentId,
                                    DocumentId = ud.DocumentId,
                                    QPDocumentTypeId = ud.QPDocumentTypeId
                                }).ToList()
                        }).ToList();
                    foreach (var userTemplate in institution.UserQPSelectionTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        userTemplate.UserDocuments.ForEach(d =>
                        {
                            d.DocumentName = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Name ?? string.Empty;
                            d.DocumentUrl = documents.FirstOrDefault(di => di.DocumentId == d.DocumentId)?.Url ?? string.Empty;
                            d.QPDocumentTypeName = QPDocumentTypeDictionary[d.QPDocumentTypeId];
                        });
                    }
                }
                foreach (var qPDocument in qPTemplate.QPDocuments)
                {
                    if (qPDocument.QPAssignedUsers.Count < 2)
                        qPDocument.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                }
            }
            return qPTemplates;
        }
        public async Task<List<UserQPTemplateVM>> GetQPTemplatesByUserIdAsync(long userId)
        {
            return await GetUserQPTemplatesAsync(userId);
        }
        public bool? AssignTemplateForQPGenerationAsync(QPDocumentUserVM qPDocumentUserVM, long QPTemplateInstitutionId,QPDocumentVM qPDocument)
        {
            var qpTemplateInstitution =  _context.QPTemplateInstitutions.FirstOrDefault(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
            if (qpTemplateInstitution == null) return null;
            var qpTemplate =  _context.QPTemplates.FirstOrDefault(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 2; //QP Generation Allocated

            var userQPTemplate = new UserQPTemplate()
            {
                QPTemplateInstitutionId = qpTemplateInstitution.QPTemplateInstitutionId,
                UserId = qPDocumentUserVM.UserId,
                QPTemplateStatusTypeId = 8,//Generation QP InProgress
                QPDocumentId = qPDocumentUserVM.IsQPOnly? qPDocument.QPOnlyDocumentId: qPDocument.QPDocumentId,
                IsQPOnly = qPDocumentUserVM.IsQPOnly
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
            _context.UserQPTemplates.Add(userQPTemplate);
             _context.SaveChanges();
            var qpSyllabusDocument = _context.QPTemplateDocuments.FirstOrDefault(qptd => qptd.QPDocumentTypeId == 1 && qptd.QPTemplateId == qpTemplate.QPTemplateId);
            var userQPSyllabusDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 1,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = qpSyllabusDocument?.DocumentId ?? 0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPSyllabusDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPSyllabusDocument);


            var qpGenerationDocument = _context.QPTemplateInstitutionDocuments.FirstOrDefault(qptd => qptd.QPDocumentTypeId ==3 && qptd.QPTemplateInstitutionId == QPTemplateInstitutionId);
            var userQPDocument = new UserQPTemplateDocument() { 
                QPDocumentTypeId = 4,
                UserQPTemplateId =userQPTemplate.UserQPTemplateId,
                DocumentId = qpGenerationDocument?.DocumentId??0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPDocument);

            var generationQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 5,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = 0
            };
            AuditHelper.SetAuditPropertiesForInsert(generationQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(generationQPDocument);

             _context.SaveChanges();
            return true;
        }
        public async Task<bool?> SubmitGeneratedQPAsync(long userId, long QPTemplateInstitutionId, long documentId)
        {
            var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
            if (qpTemplateInstitution == null) return null;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);
            
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 3; //QP Pending for Scrutiny
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateInstitutionId == qpTemplateInstitution.QPTemplateInstitutionId);
            
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
        public async Task<bool?> AssignTemplateForQPScrutinyAsync(long userId, long QPTemplateInstitutionId)
        {
            var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
            if (qpTemplateInstitution == null) return null;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 4; //QP Scrutiny Allocated

            var qpUserTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateInstitutionId == QPTemplateInstitutionId && qp.QPTemplateStatusTypeId==9);
            
            if (qpUserTemplate == null) return null;

            var userQPTemplate = new UserQPTemplate()
            {
                QPTemplateInstitutionId = qpUserTemplate.QPTemplateInstitutionId,
                UserId = userId,
                QPTemplateStatusTypeId = 10,//Scrutinize QP InProgress
                QPDocumentId = qpUserTemplate.QPDocumentId
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
            _context.UserQPTemplates.Add(userQPTemplate);
            await _context.SaveChangesAsync();

            var qpSyllabusDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 1 && qptd.UserQPTemplateId == qpUserTemplate.UserQPTemplateId);
            var userQPSyllabusDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 1,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = qpSyllabusDocument?.DocumentId ?? 0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPSyllabusDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPSyllabusDocument);

            var qpGenerationDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 7 && qptd.UserQPTemplateId == qpUserTemplate.UserQPTemplateId);
            var userQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 6,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = qpGenerationDocument?.DocumentId ?? 0
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPDocument);

            var scrutinizedQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 7,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                DocumentId = 0
            };
            AuditHelper.SetAuditPropertiesForInsert(scrutinizedQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(scrutinizedQPDocument);

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool?> SubmitScrutinizedQPAsync(long userId, long QPTemplateInstitutionId, long documentId)
        {
            var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
            if (qpTemplateInstitution == null) return null;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);

            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 5; //QP Pending for Selection
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

            var qpScrutinizedUserTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateInstitutionId == QPTemplateInstitutionId && uqp.QPTemplateStatusTypeId == 10);

            if (qpScrutinizedUserTemplate == null) return null;
            qpScrutinizedUserTemplate.QPTemplateStatusTypeId = 11;//Scrutinized QP Submitted
            AuditHelper.SetAuditPropertiesForUpdate(qpScrutinizedUserTemplate, 1);
           

            var userQPTemplateForSelection = new UserQPTemplate()
            {
                QPTemplateInstitutionId = qpScrutinizedUserTemplate.QPTemplateInstitutionId,
                UserId = 1,
                QPTemplateStatusTypeId = 12,//Selection QP InProgress
                QPDocumentId = qpScrutinizedUserTemplate.QPDocumentId
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplateForSelection, 1);
            _context.UserQPTemplates.Add(userQPTemplateForSelection);
            await _context.SaveChangesAsync();

            var scrutinizedQPDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 9 && qptd.UserQPTemplateId == qpScrutinizedUserTemplate.UserQPTemplateId);
            if(scrutinizedQPDocument != null)
            {
                scrutinizedQPDocument.DocumentId = documentId;
                AuditHelper.SetAuditPropertiesForUpdate(scrutinizedQPDocument, 1);
            }
            await _context.SaveChangesAsync();

            var userQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 8,//For QP Selection
                UserQPTemplateId = userQPTemplateForSelection.UserQPTemplateId,
                DocumentId = documentId
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(userQPDocument);

            var selectedQPDocument = new UserQPTemplateDocument()
            {
                QPDocumentTypeId = 9,
                UserQPTemplateId = userQPTemplateForSelection.UserQPTemplateId,
                DocumentId = documentId
            };
            AuditHelper.SetAuditPropertiesForInsert(selectedQPDocument, 1);
            _context.UserQPTemplateDocuments.Add(selectedQPDocument);

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool?> SubmitSelectedQPAsync(long userId, long QPTemplateInstitutionId, long documentId)
        {
            var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
            if (qpTemplateInstitution == null) return null;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);

            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 6; //QP Selected
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateInstitutionId == QPTemplateInstitutionId && uqp.QPTemplateStatusTypeId ==12);

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
            var userQPTemplateVMs = new List<UserQPTemplateVM>();
            var users = _context.Users.ToList();
            var userQPTemplates = _context.UserQPTemplates.Where(uqp => uqp.UserId == userId && uqp.IsActive).ToList();
            var QPTemplateInstitutionIds = userQPTemplates.Select(uqp => uqp.QPTemplateInstitutionId).ToList();
            var qpTemplateinstitutions = _context.QPTemplateInstitutions.Where(qpti => QPTemplateInstitutionIds.Contains(qpti.QPTemplateInstitutionId)).ToList();
                var qpTemplateIds = _context.QPTemplateInstitutions.Where(qpti => QPTemplateInstitutionIds.Contains(qpti.QPTemplateInstitutionId)).Select(qpti => qpti.QPTemplateId).ToList();
            var qpTemplates = await _context.QPTemplates.Where(qp => qpTemplateIds.Contains(qp.QPTemplateId)).ToListAsync();
            userQPTemplates.ForEach(userQPTemplate =>
            {
                userQPTemplateVMs.Add(new UserQPTemplateVM() {
                    UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                    QPTemplateInstitutionId = userQPTemplate.QPTemplateInstitutionId,
                    UserId = userQPTemplate.UserId,
                    UserName = users.FirstOrDefault(u => u.UserId == userQPTemplate.UserId)?.Name ?? string.Empty,
                    QPTemplateStatusTypeId = userQPTemplate.QPTemplateStatusTypeId, 
                    QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userQPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty,
                    QPDocumentId = userQPTemplate.QPDocumentId
                });
            });
                foreach (var qPTemplate in userQPTemplateVMs)
                {
                    var qpTemplate = qpTemplateinstitutions.FirstOrDefault(q => q.QPTemplateInstitutionId == qPTemplate.QPTemplateInstitutionId);
                    if (qpTemplate != null) 
                        qPTemplate.QPTemplateName = qpTemplates.FirstOrDefault(q => q.QPTemplateId == qpTemplate.QPTemplateId)?.QPTemplateName ?? string.Empty;
                    
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
                return userQPTemplateVMs;
            }
            catch (Exception ex)
            {
                throw ;
            }
        }
        public async Task<bool?> PrintSelectedQPAsync(long qpTemplateInstitutionId, string qpCode, bool isForPrint)
        {
            var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == qpTemplateInstitutionId);
            if (qpTemplateInstitution == null) return null;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);

            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 7; //QP Selected
            qpTemplate.QPCode = qpCode;
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);
            await _context.SaveChangesAsync();

            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.QPTemplateInstitutionId == qpTemplateInstitutionId && uqp.QPTemplateStatusTypeId == 12);
            if (userQPTemplate != null)
            {
                var selectedQpDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 9 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
                var institutaions = await _context.QPTemplateInstitutions.Where(qpti => qpti.QPTemplateId == qpTemplate.QPTemplateId).ToListAsync();
                foreach (var institution in institutaions)
                {
                    var qpDocumentToPrint = await _context.QPTemplateInstitutionDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 2 && qptd.QPTemplateInstitutionId == institution.QPTemplateInstitutionId);
                    if(selectedQpDocument != null && qpDocumentToPrint != null)
                    await PrintQPDocumentByInstitutionAsync(qpTemplate, institution, selectedQpDocument, qpDocumentToPrint, isForPrint);

                    var qpDocumentWithAnswerToPrint = await _context.QPTemplateInstitutionDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 3 && qptd.QPTemplateInstitutionId == institution.QPTemplateInstitutionId);
                    if (selectedQpDocument != null && qpDocumentWithAnswerToPrint != null)
                        await PrintQPAnswerDocumentByInstitutionAsync(qpTemplate, institution, selectedQpDocument, qpDocumentWithAnswerToPrint);
                }
            }
            return true;
        }
        private async Task<bool> PrintQPDocumentByInstitutionAsync(QPTemplate qPTemplate, QPTemplateInstitution qPTemplateInstitution, UserQPTemplateDocument selectedQPDocument, QPTemplateInstitutionDocument documentToPrint, bool isForPrint)
        {
            var qpSelectedDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == selectedQPDocument.DocumentId);
            var qpToPrintDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentToPrint.DocumentId);
            if (qpSelectedDocument == null || qpToPrintDocument == null) return false;
            await _bookmarkProcessor.ProcessBookmarksAndPrint(qPTemplate, qPTemplateInstitution, qpSelectedDocument.Name, qpToPrintDocument.Name,isForPrint);
            return true;
        }
        private async Task<bool> PrintQPAnswerDocumentByInstitutionAsync(QPTemplate qPTemplate, QPTemplateInstitution qPTemplateInstitution, UserQPTemplateDocument selectedQPDocument, QPTemplateInstitutionDocument documentToPrint)
        {
            return true;
        }

        public async Task<IEnumerable<QPAssignmentExpertVM>> GetExpertsForQPAssignmentAsync()
        {
            var users = await _context.Users.Where(u => u.IsEnabled && u.UserId != 1).ToListAsync();
            var userQPTemplates = await _context.UserQPTemplates.Where(uqp => uqp.QPTemplateStatusTypeId == 8 || uqp.QPTemplateStatusTypeId == 10).ToListAsync();
            var qpAssignmentExperts = new List<QPAssignmentExpertVM>();
            foreach (var user in users)
            {
                qpAssignmentExperts.Add(new QPAssignmentExpertVM
                {
                    UserId = user.UserId,
                    UserName =$"{user.Name}-{user.CollegeName}-{user.DepartmentName}",
                    IsAvailableForQPAssignment = (userQPTemplates.Where(uqp => uqp.UserId == user.UserId).ToList().Count < 3)
                });
            }
            return qpAssignmentExperts;
        }
    }
}
