using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class DepartmentService
    {
        private readonly ExaminationDbContext _context;

        public DepartmentService(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            return await _context.Departments.FindAsync(id);
        }

        public async Task<Department> AddDepartmentAsync(Department department)
        {
            AuditHelper.SetAuditPropertiesForInsert(department, department.CreatedById);
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<bool> UpdateDepartmentAsync(int id, Department updatedDepartment)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return false;
            AuditHelper.SetAuditPropertiesForUpdate(department, department.ModifiedById);
            department.Name = updatedDepartment.Name;
            department.ShortName = updatedDepartment.ShortName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return false;

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
