using SKCE.Examination.Models.DbModels;

public static class AuditHelper
{
    /// <summary>
    /// Sets audit properties for insert operation.
    /// </summary>
    public static void SetAuditPropertiesForInsert<T>(T entity, long userId) where T : AuditModel
    {
        entity.CreatedById = userId;
        entity.CreatedDate = DateTime.UtcNow;
        entity.ModifiedById = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.IsActive = true;
    }
    

    /// <summary>
    /// Sets audit properties for update operation.
    /// </summary>
    public static void SetAuditPropertiesForUpdate<T>(T entity, long userId, bool isActive =true) where T : AuditModel
    {
        entity.ModifiedById = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.IsActive = isActive;
    }
}
