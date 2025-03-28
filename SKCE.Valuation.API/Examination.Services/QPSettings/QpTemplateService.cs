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
using Document = Aspose.Words.Document;
using Microsoft.AspNetCore.Http;
using Shape = Aspose.Words.Drawing.Shape;
using System.Text.RegularExpressions;
using Aspose.Pdf.Operators;
using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
namespace SKCE.Examination.Services.QPSettings
{
    public class QpTemplateService 
    {
        private readonly ExaminationDbContext _context;
        private readonly IMapper _mapper;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly BookmarkProcessor _bookmarkProcessor;
        private readonly AzureBlobStorageHelper _azureBlobStorageHelper;
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
        public QpTemplateService(ExaminationDbContext context, IMapper mapper, IConfiguration configuration, BookmarkProcessor bookmarkProcessor, AzureBlobStorageHelper azureBlobStorageHelper, EmailService emailService)
        {
            _bookmarkProcessor = bookmarkProcessor;
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _azureBlobStorageHelper = azureBlobStorageHelper;
            _configuration = configuration;
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
            if (syllabusDocument == null) throw new Exception("Course syllabus Document is not uploaded.");
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
            var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplateVM.CourseId);
            var degreeType = _context.DegreeTypes.FirstOrDefault(c => c.DegreeTypeId == qPTemplateVM.DegreeTypeId);
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
                        if (u.UserId > 0)
                        {
                            AssignTemplateForQPGenerationAsync(u, institutionVM.QPTemplateInstitutionId, i);
                            var emailUser = _context.Users.FirstOrDefault(us => us.UserId == u.UserId);
                            //send email
                            if (emailUser != null)
                            {
                                _emailService.SendEmailAsync(emailUser.Email, "Question Paper Assignment Notification – Sri Krishna Institutions, Coimbatore",
                                  $"Dear {emailUser.Name}," +
                                  $"\n\nYou have been assigned to generate a Question Paper for the following course:" +
                                  $"\n\n Course:{courseDetails.Code} - {courseDetails.Name} \n Degree Type: {degreeType.Name}" +
                                  $"\n\nPlease review the assigned question paper and submit it before the due date." +
                                  $"\n To View Assignment please click here {_configuration["LoginUrl"]}" +
                                  $"\n\nContact Details:\nName:\nContact Number:\n\nThank you for your cooperation. We look forward to your valuable contribution to our institution.\n\nWarm regards,\nSri Krishna College of Engineering and Technology").Wait();
                            }
                        }
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
            var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplateVM.CourseId);
            var degreeType = _context.DegreeTypes.FirstOrDefault(c => c.DegreeTypeId == qPTemplateVM.DegreeTypeId);
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
                            var emailUser = _context.Users.FirstOrDefault(us => us.UserId == assignedUser.UserId);
                            //send email
                            if (emailUser != null)
                            {
                                _emailService.SendEmailAsync(emailUser.Email, "Question Paper Assignment Notification – Sri Krishna Institutions, Coimbatore",
                                  $"Dear {emailUser.Name}," +
                                  $"\n\nYou have been assigned to generate a Question Paper for the following course:" +
                                  $"\n\n Course:{courseDetails.Code} - {courseDetails.Name} \n Degree Type: {degreeType.Name} \n\n" +
                                  $"\n\nPlease review the assigned question paper and submit it before the due date." +
                                  $"\n To View Assignment please click here {_configuration["LoginUrl"]}" +
                                  $"\n\nContact Details:\nName:\nContact Number:\n\nThank you for your cooperation. We look forward to your valuable contribution to our institution.\n\nWarm regards,\nSri Krishna College of Engineering and Technology").Wait();
                            }

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

        public async Task<(string message, bool inValidForSubmission)> ValidateGeneratedQPAndPreview(long userId, long QPTemplateInstitutionId, Document doc) {
            var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
            if (qpTemplateInstitution == null)
            {
                return ("User QP assignment is missing.",true);
            }
            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateInstitutionId == qpTemplateInstitution.QPTemplateInstitutionId);
            if (userQPTemplate == null)
            {
                return ("User QP assignment is missing.", true);
            }
            var qpDocument = await _context.QPDocuments.FirstOrDefaultAsync(qpd => qpd.QPDocumentId == userQPTemplate.QPDocumentId);
            if (qpDocument == null)
            {
                return ("User QP documemt is missing.",true);
            }
            if (qpDocument.DegreeTypeName == "UG")
            {
                var ugValidations = await UGQPValidationAsync(userQPTemplate, doc);
                  return (ugValidations.Item1, ugValidations.Item2) ;
            }
            else if (qpDocument.DegreeTypeName == "PG")
            {
                var pgValidations = await PGQPValidationAsync(userQPTemplate, doc);
                return (pgValidations.Item1, pgValidations.Item2);
            }
            return (string.Empty,true);
        }

        private async Task<(string message, bool inValidForSubmission)> PGQPValidationAsync(UserQPTemplate userQPTemplate, Document doc)
        {
            List<string> validationResults = new List<string>();
            int totalMarks = 0;
            int expectedMarks = 20; // Part A should have 20 marks
            return (string.Empty,true);
        }

        private async Task<(string,bool)> UGQPValidationAsync(UserQPTemplate userQPTemplate, Document doc)
        {
            List<string> validationResults = new List<string>();
            Dictionary<string, int> coMarks = new Dictionary<string, int>();
            Dictionary<string, int> btMarks = new Dictionary<string, int>();

            HashSet<string> allCOs = new HashSet<string> { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" }; // Predefined COs
            HashSet<string> allBTs = new HashSet<string> { "U", "AP", "AN" }; // Predefined BTs

            int totalMarks = 0;
            int expectedMarks = 20; // Expected marks for Part A

            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part A: Question Validation</h2>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            htmlTable.Append("<tr><th>Question</th><th>CO</th><th>BT</th><th>Marks</th></tr>");

            for (int qNo = 1; qNo <= 10; qNo++)
            {
                string questionBookmark = $"Q{qNo}";
                string coBookmark = $"Q{qNo}CO";
                string btBookmark = $"Q{qNo}BT";
                string marksBookmark = $"Q{qNo}Marks";

                string co = ExtractBookmarkText(doc, coBookmark);
                string bt = ExtractBookmarkText(doc, btBookmark);
                int marks = ExtractMarksFromBookmark(doc, marksBookmark);

                totalMarks += marks;

                // Add row to HTML table
                htmlTable.Append($"<tr><td>Q{qNo}</td><td>{co}</td><td>{bt}</td><td>{marks}</td></tr>");

                if (!string.IsNullOrEmpty(co) && allCOs.Contains(co))
                {
                    if (!coMarks.ContainsKey(co)) coMarks[co] = 0;
                    coMarks[co] += marks;
                }

                if (!string.IsNullOrEmpty(bt) && allBTs.Contains(bt))
                {
                    if (!btMarks.ContainsKey(bt)) btMarks[bt] = 0;
                    btMarks[bt] += marks;
                }
                if(qNo == 10)
                {
                    // Add Total row to HTML table
                    htmlTable.Append($"<tr><td></td><td></td><td>Total</td><td>{totalMarks}</td></tr>");
                }
            }

            htmlTable.Append("</table>");

            bool isMarksCorrect = totalMarks == expectedMarks;
            string marksValidationMessage = isMarksCorrect
                ? $"✅ Total marks for Part A is correct ({totalMarks}/{expectedMarks})."
                : $"❌ Incorrect marks for Part A ({totalMarks}/{expectedMarks}).";

            var missingCOs = allCOs.Except(coMarks.Keys).ToList();
            var missingBTs = allBTs.Except(btMarks.Keys).ToList();

            htmlTable.Append($"<h3>{marksValidationMessage}</h3>");

            // CO Marks Distribution
            htmlTable.Append("<h2>CO Marks Distribution</h2><table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'><tr><th>CO</th><th>Marks</th></tr>");
            foreach (var kvp in coMarks)
            {
                htmlTable.Append($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
            }
            htmlTable.Append("</table>");

            // BT Marks Distribution
            htmlTable.Append("<h2>BT Marks Distribution</h2><table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'><tr><th>BT</th><th>Marks</th></tr>");
            foreach (var kvp in btMarks)
            {
                htmlTable.Append($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
            }
            htmlTable.Append("</table>");

            // Missing COs & BTs
            if (missingCOs.Count > 0)
                htmlTable.Append($"<h3 style='color:red;'>Missing COs: {string.Join(", ", missingCOs)}</h3>");
            if (missingBTs.Count > 0)
                htmlTable.Append($"<h3 style='color:red;'>Missing BTs: {string.Join(", ", missingBTs)}</h3>");

            var partBResults = ValidatePartB(doc);
            return ($"{htmlTable.ToString()} +\n\n {partBResults.Item1}", partBResults.Item2);
        }

        public static (string,bool) ValidatePartB(Document doc)
        {
            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part B: Question Validation</h2>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            htmlTable.Append("<tr><th>Question</th><th>SubQ1 Text</th><th>SubQ1 CO</th><th>SubQ1 BT</th><th>SubQ1 Marks</th><th>SubQ2 Text</th><th>SubQ2 CO</th><th>SubQ2 BT</th><th>SubQ2 Marks</th><th>Total Marks</th></tr>");

            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 6, 8, 10 };

            int totalPartBMarks = 0;
            int expectedPartBMarks = 160;

            for (int qNo = 11; qNo <= 20; qNo++)
            {
                string subQ1Text = ExtractBookmarkText(doc, $"Q{qNo}I");
                string subQ1CO = ExtractBookmarkText(doc, $"Q{qNo}ICO");
                string subQ1BT = ExtractBookmarkText(doc, $"Q{qNo}IBT");
                int subQ1Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IMarks");

                string subQ2Text = ExtractBookmarkText(doc, $"Q{qNo}II");
                string subQ2CO = ExtractBookmarkText(doc, $"Q{qNo}IICO");
                string subQ2BT = ExtractBookmarkText(doc, $"Q{qNo}IIBT");
                int subQ2Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IIMarks");

                string qpakAssigned = ExtractBookmarkText(doc, $"Q{qNo}QPAK");
                string subQ2AnswerKey = ExtractBookmarkText(doc, $"Q{qNo}SubQ2AnswerKey");

                int totalMarks = subQ1Marks + subQ2Marks;
                totalPartBMarks += totalMarks;

                // Validations
                if (string.IsNullOrEmpty(subQ1Text)) errors.Add($"❌ Q{qNo} SubQ1 text is empty.");
                if (string.IsNullOrEmpty(subQ1CO) || !validCOs.Contains(subQ1CO)) errors.Add($"❌ Q{qNo} SubQ1 CO is invalid.");
                if (string.IsNullOrEmpty(subQ1BT) || !validBTs.Contains(subQ1BT)) errors.Add($"❌ Q{qNo} SubQ1 BT is invalid.");
                if (!validMarks.Contains(subQ1Marks)) errors.Add($"❌ Q{qNo} SubQ1 Marks should be 6, 8, or 10.");

                if (string.IsNullOrEmpty(subQ2Text)) errors.Add($"❌ Q{qNo} SubQ2 text is empty.");
                if (string.IsNullOrEmpty(subQ2CO) || !validCOs.Contains(subQ2CO)) errors.Add($"❌ Q{qNo} SubQ2 CO is invalid.");
                if (string.IsNullOrEmpty(subQ2BT) || !validBTs.Contains(subQ2BT)) errors.Add($"❌ Q{qNo} SubQ2 BT is invalid.");
                if (!validMarks.Contains(subQ2Marks)) errors.Add($"❌ Q{qNo} SubQ2 Marks should be 6, 8, or 10.");

                if (!string.IsNullOrEmpty(qpakAssigned) && string.IsNullOrEmpty(subQ2AnswerKey)) errors.Add($"❌ Q{qNo} Answer key is missing for SubQ2 (QPAK assigned).");

                if (totalMarks != 16) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 16).");

                // Add to table
                htmlTable.Append($"<tr><td>Q{qNo}</td><td>{subQ1Text}</td><td>{subQ1CO}</td><td>{subQ1BT}</td><td>{subQ1Marks}</td><td>{subQ2Text}</td><td>{subQ2CO}</td><td>{subQ2BT}</td><td>{subQ2Marks}</td><td>{totalMarks}</td></tr>");
            }

            htmlTable.Append("</table>");

            bool isPartBValid = totalPartBMarks == expectedPartBMarks;
            string marksValidationMessage = isPartBValid
                ? $"✅ Total Part B Marks are correct ({totalPartBMarks}/{expectedPartBMarks})."
                : $"❌ Total Part B Marks are incorrect ({totalPartBMarks}/{expectedPartBMarks}).";

            htmlTable.Append($"<h3>{marksValidationMessage}</h3>");

            if (errors.Count > 0)
            {
                htmlTable.Append("<h3 style='color:red;'>Validation Errors:</h3>");
                htmlTable.Append("<ul>");
                foreach (var error in errors)
                {
                    htmlTable.Append($"<li>{error}</li>");
                }
                htmlTable.Append("</ul>");
            }
            else
            {
                htmlTable.Append("<h3 style='color:green;'>✅ No validation errors found.</h3>");
            }

            return (htmlTable.ToString(), errors.Any());
        }
        private static string ExtractBookmarkText(Document doc, string bookmarkName)
        {
            Bookmark bookmark = doc.Range.Bookmarks[bookmarkName];
            if (bookmark != null)
            {
                return bookmark.Text.Trim();
            }
            return "Unknown";
        }

        private static int ExtractMarksFromBookmark(Document doc, string bookmarkName)
        {
            Bookmark bookmark = doc.Range.Bookmarks[bookmarkName];
            if (bookmark != null && int.TryParse(bookmark.Text.Trim(), out int marks))
            {
                return marks;
            }
            return 0; // Default to 0 if marks not found
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
