﻿using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.EntityHelpers;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class AnswersheetImportService
    {
        private readonly ExaminationDbContext _context;
        private readonly BlobStorageHelper _blobStorageHelper;

        public AnswersheetImportService(ExaminationDbContext context, BlobStorageHelper blobStorageHelper)
        {
            _context = context;
            _blobStorageHelper = blobStorageHelper;

        }

        public async Task<List<AnswersheetImportCourseDptDto>> GetExaminationInfoAsync(long institutionId, string examYear, string examMonth)
        {

            var result = await (from exam in this._context.Examinations
                                join course in this._context.Courses on exam.CourseId equals course.CourseId
                                join dept in this._context.Departments on exam.DepartmentId equals dept.DepartmentId
                                where exam.IsActive
                                && exam.InstitutionId == institutionId
                                && exam.ExamYear == examYear
                                && exam.ExamMonth == examMonth
                                select new AnswersheetImportCourseDptDto
                                {
                                    ExaminationId = exam.ExaminationId,
                                    CourseCode = course.Code,
                                    CourseName = course.Name,
                                    DepartmentCode = dept.ShortName,
                                    DepartmentName = dept.Name,
                                    StudentCount = (int)exam.StudentCount
                                }).ToListAsync();
            return result.OrderBy(x => x.CourseCode).ThenBy(x => x.DepartmentCode).ToList();
        }


        //POST
        public async Task<List<AnswersheetImportDetail>> ImportDummyNoFromExcelByCourse(Stream excelStream, long examinationId)
        {
            var helper = new AnswersheetImportHelper(this._context, this._blobStorageHelper);
            var result = await helper.ImportDummyNoFromExcelByCourse(excelStream, examinationId, "xlsx");
            return result;
        }


        public async Task<List<AnswersheetImport>> GetAnswersheetImports(long examinationId)
        {
            return await this._context.AnswersheetImports.Where(x => x.ExaminationId == examinationId).ToListAsync();
        }

        public async Task<List<AnswersheetImportDetail>> GetAnswersheetImportDetails(long answersheetImportId)
        {
            return await this._context.AnswersheetImportDetails.Where(x => x.AnswersheetImportId == answersheetImportId).ToListAsync();
        }



    }
}
