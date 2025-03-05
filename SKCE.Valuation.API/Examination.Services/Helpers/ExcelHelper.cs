using SKCE.Examination.Models.DbModels.Common;
using OfficeOpenXml;
namespace SKCE.Examination.Services.Helpers
{
    public class ExcelHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public ExcelHelper(ExaminationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Reads an Excel file and imports data into the database.
        /// </summary>
        public async Task<List<User>> ImportCourseDetailsBySyllabusFromExcelAsync(Stream fileStream)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial; // Required for EPPlus

            var users = new List<User>();

            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Read first sheet
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip headers)
                {
                    var user = new User
                    {
                        Name = worksheet.Cells[row, 1].Text,
                        Email = worksheet.Cells[row, 2].Text,
                        MobileNumber = worksheet.Cells[row, 3].Text,
                        //RoleId = worksheet.Cells[row, 4].Text,
                        WorkExperience = int.TryParse(worksheet.Cells[row, 5].Text, out int exp) ? exp : 0,
                        //Department = worksheet.Cells[row, 6].Text,
                        //Designation = worksheet.Cells[row, 7].Text,
                        CollegeName = worksheet.Cells[row, 8].Text
                    };


                    users.Add(user);
                }

                // Save to the database
                await _dbContext.Users.AddRangeAsync(users);
                await _dbContext.SaveChangesAsync();
            }

            return users;
        }
    }
}
