
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Database Connection
builder.Services.AddDbContext<ExaminationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors();

// Add CORS Policy
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowSpecificOrigins", policy =>
//    {
//        policy.WithOrigins("http://localhost:5088")
//              .AllowAnyMethod()
//              .AllowAnyHeader();
//    });
//});


// Register Services
builder.Services.AddScoped<LoginServices>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ExcelHelper>(); // Register helper
builder.Services.AddScoped<S3Helper>(); // Register helper

builder.Services.AddHttpContextAccessor();
// Enable Controllers
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed Default User in Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ExaminationDbContext>();
        context.Database.Migrate(); // Apply pending migrations
        SeedDefaultUser(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseAuthorization();

app.MapControllers();

app.UseRouting();
// Enable CORS Middleware
//app.UseCors("AllowSpecificOrigins");
app.UseCors(
               options => options.SetIsOriginAllowed(x => _ = true)
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
            Name = "Super Admin",
            Email = "superadmin@skcet.ac.in",
            MobileNumber = "8300034477",
            RoleId = 1,
            WorkExperience = 1,
            DepartmentId = 1,
            DesignationId = 1,
            CollegeName = "SRI KRISHNA COLLEGE OF ENGINEERING TECHNOLOGY",
            BankAccountName = "",
            BankAccountNumber = "",
            BankName = "",
            BankBranchName = "",
            BankIFSCCode = "",
            IsEnabled = true,
            CourseId = 1,
            Qualification = "M.E",
            AreaOfSpecialization = "Computer Science and Engineering"
        };
        AuditHelper.SetAuditPropertiesForInsert(defaultUser, 1);

        context.Users.Add(defaultUser);
        context.SaveChanges(); // Commit to Database
    }
}