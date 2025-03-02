
using Microsoft.EntityFrameworkCore;
using Examination.Services.Common;
using Examination.Models.DBModels.Common;
using Examination.Services.ServiceContracts;
using System;
using Examination.Models.DbModels.Common;

var builder = WebApplication.CreateBuilder(args);

// Database Connection
builder.Services.AddDbContext<ExaminationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<LoginServices>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IUserService,UserService>();

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
            Experience = 1,
            DepartmentId = 1,
            DesignationId = 1,
            CollegeName = "SRI KRISHNA COLLEGE OF ENGINEERING TECHNOLOGY",
            BankAccountName = "",
            BankAccountNumber = "",
            BankName = "",
            BankBranchName = "",
            BankIFSCCode = "",
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            CreatedById = 1,
            ModifiedById = 1,
            IsActive = true,
            IsEnabled= true
        };

        context.Users.Add(defaultUser);
        context.SaveChanges(); // Commit to Database
    }
}