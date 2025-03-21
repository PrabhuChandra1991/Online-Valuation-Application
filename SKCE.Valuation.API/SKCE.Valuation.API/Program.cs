
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.AutoMapperProfiles;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Azure;
using SKCE.Examination.Services.MappingProfiles;
using SKCE.Examination.Services.QPSettings;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Database Connection
builder.Services.AddDbContext<ExaminationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddCors();

// Register Services
builder.Services.AddScoped<LoginServices>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<QpTemplateService>();
builder.Services.AddScoped<QPDataImportHelper>(); // Register helper
builder.Services.AddScoped<CourseService>();
builder.Services.AddAutoMapper(typeof(QPTemplateMappingProfile));
// Add services to the container
builder.Services.AddScoped<AzureBlobStorageHelper>();
builder.Services.AddScoped<BookmarkProcessor>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
    .AddNewtonsoftJson(x=>x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

//builder.Services.AddControllers().AddJsonOptions(x =>
//   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["StorageConnection:blobServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddQueueServiceClient(builder.Configuration["StorageConnection:queueServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddTableServiceClient(builder.Configuration["StorageConnection:tableServiceUri"]!).WithName("StorageConnection");
});

var app = builder.Build();

// Seed Default User in Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ExaminationDbContext>();
        context.Database.Migrate(); // Apply pending migrations
        SeedDegreeTypes(context);
        SeedDefaultUser(context);
        SeedRoles(context);
        SeedExamMonths(context);
        SeedQPTags(context);
        SeedQPTemplateStatusType(context);
        SeedDesignations(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
    }
}
    app.UseExceptionHandler("/Error");
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();
// Enable CORS Middleware
//app.UseCors("AllowSpecificOrigins");
app.UseCors(options => options.SetIsOriginAllowed(x => _ = true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()
               );
app.UseAuthorization();
app.MapControllers();
app.Run();

/// <summary>
/// Seeds a default user into the database if not exists.
/// </summary>
void SeedDefaultUser(ExaminationDbContext context)
{
    if (!context.Users.Any()) // Check if user table is empty
    {
        var defaultUser = new User
        {
            Salutation="Mr.",
            Gender="Male",
            Name = "Super Admin",
            Email = "superadmin@skitech.ai",
            MobileNumber = "8300034477",
            RoleId = 1,
            CollegeName = "SRI KRISHNA COLLEGE OF ENGINEERING TECHNOLOGY",
            DepartmentName ="",
            TotalExperience = 1,
            BankAccountName = "",
            BankAccountNumber = "",
            BankName = "",
            BankBranchName = "",
            BankIFSCCode = "",
            IsEnabled = true,
        };
        AuditHelper.SetAuditPropertiesForInsert(defaultUser, 1);
        context.Users.Add(defaultUser);
        context.SaveChanges(); // Commit to Database
    }
}

/// <summary>
/// Seeds a default roles into the database if not exists.
/// </summary>
void SeedRoles(ExaminationDbContext context)
{
    if (!context.Roles.Any()) // Check if role table is empty
    {
        var roles = new List<Role>
        {
            new Role { Name = "Super Admin" },
            new Role { Name = "Admin" },
            new Role { Name = "Expert" }
        };
        foreach (var role in roles)
            AuditHelper.SetAuditPropertiesForInsert(role, 1);

        context.Roles.AddRange(roles);
        context.SaveChanges(); // Commit to Database
    }
}

/// <summary>
/// Seeds a default ExamMonth into the database if not exists.
/// </summary>
void SeedExamMonths(ExaminationDbContext context)
{
    if (!context.ExamMonths.Any()) // Check if role table is empty
    {
        var examMonths = new List<ExamMonth>
        {
            new ExamMonth { Name = "NOV/DEC" ,Semester=1},
            new ExamMonth { Name = "APRIL/MAY" ,Semester=2},
            new ExamMonth { Name = "NOV/DEC" ,Semester=3},
            new ExamMonth { Name = "APRIL/MAY" ,Semester=4},
            new ExamMonth { Name = "NOV/DEC" ,Semester=5},
            new ExamMonth { Name = "APRIL/MAY" ,Semester=6},
            new ExamMonth { Name = "NOV/DEC" ,Semester=7},
            new ExamMonth { Name = "APRIL/MAY" ,Semester=8},
        };
        foreach (var examMonth in examMonths)
            AuditHelper.SetAuditPropertiesForInsert(examMonth, 1);

        context.ExamMonths.AddRange(examMonths);
        context.SaveChanges(); // Commit to Database
    }
}

/// <summary>
/// Seeds a default QP Tags into the database if not exists.
/// </summary>
void SeedQPTags(ExaminationDbContext context)
{
    if (!context.QPTags.Any()) 
    {
        //TagDataTypeId -1 for Rich text box, 2 for TextBox, 3 for Numeric Text box
        var qptags = new List<QPTag>
        {
            new QPTag { Name = "Question 1",Tag="<Question 1>",TagDataTypeId=1,IsQuestion=true,Description="Question 1 text"},
            new QPTag { Name = "Question 1 Answer",Tag="<Question 1 Answer>",TagDataTypeId=1,IsQuestion=false,Description="Question 1 Answer text"},
            new QPTag { Name = "Question 1 BT",Tag="<Question 1 BT>",TagDataTypeId=1,IsQuestion=true,Description="Question 1 BT text"},
            new QPTag { Name = "Question 1 CO",Tag="<Question 1 CO>",TagDataTypeId=1,IsQuestion=true,Description="Question 1 CO text"},
            new QPTag { Name = "Question 1 Marks",Tag="<Question 1 Marks>",TagDataTypeId=1,IsQuestion=true,Description="Question 1 Marks text"},
        };

        foreach (var qptag in qptags)
            AuditHelper.SetAuditPropertiesForInsert(qptag, 1);

        context.QPTags.AddRange(qptags);
        context.SaveChanges(); // Commit to Database
    }
}

/// <summary>
/// Seeds a default QP Template Status Type into the database if not exists.
/// </summary>
void SeedQPTemplateStatusType(ExaminationDbContext context)
{
    if (!context.QPTemplateStatusTypes.Any())
    {
        var qpTemplateStatusTypes = new List<QPTemplateStatusType>
        {
            new QPTemplateStatusType { Name = "QP Pending for Allocation"},
            new QPTemplateStatusType { Name = "QP Generation Allocated" },
            new QPTemplateStatusType { Name = "QP Pending for Scrutiny" },
            new QPTemplateStatusType { Name = "QP Scrutiny Allocated" },
            new QPTemplateStatusType { Name = "QP Pending for Selection"},
            new QPTemplateStatusType { Name = "QP Selected" },
            new QPTemplateStatusType { Name = "QP Printed" },
            new QPTemplateStatusType { Name = "Generation QP InProgress" },
            new QPTemplateStatusType { Name = "Generated QP Submitted"},
            new QPTemplateStatusType { Name = "Scrutinize QP InProgress" },
            new QPTemplateStatusType { Name = "Scrutinized QP Submitted"},
            new QPTemplateStatusType { Name = "Selection QP InProgress" },
            new QPTemplateStatusType { Name = "Selected QP Submitted"}
        };

        foreach (var qpemplateStatusTypes in qpTemplateStatusTypes)
            AuditHelper.SetAuditPropertiesForInsert(qpemplateStatusTypes, 1);

        context.QPTemplateStatusTypes.AddRange(qpTemplateStatusTypes);
        context.SaveChanges(); // Commit to Database
    }
}

/// <summary>
/// Seeds a default Designations into the database if not exists.
/// </summary>
void SeedDesignations(ExaminationDbContext context)
{
    if (!context.Designations.Any())
    {
        var designations = new List<Designation>
        {
           new Designation { Name = "Professor" },
           new Designation { Name = "Assistant Professor" },
           new Designation { Name = "Associate Professor" },
        };

        foreach (var designation in designations)
            AuditHelper.SetAuditPropertiesForInsert(designation, 1);

        context.Designations.AddRange(designations);
        context.SaveChanges(); // Commit to Database
    }
}

/// <summary>
/// Seeds a default Degree Types into the database if not exists.
/// </summary>
void SeedDegreeTypes(ExaminationDbContext context)
{
    if (!context.DegreeTypes.Any())
    {
        var degreeTypes = new List<DegreeType>
        {
           new DegreeType { Name = "UG",Code="UG" },
           new DegreeType { Name = "PG" ,Code="PG"},
           new DegreeType { Name = "Ph.D",Code="Ph.D"},
        };

        foreach (var degreeType in degreeTypes)
            AuditHelper.SetAuditPropertiesForInsert(degreeType, 1);

        context.DegreeTypes.AddRange(degreeTypes);
        context.SaveChanges(); // Commit to Database
    }
}