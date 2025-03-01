
using Microsoft.EntityFrameworkCore;
using Examination.Services.Common;
using Examination.Models.DBModels.Common;

var builder = WebApplication.CreateBuilder(args);

// Database Connection
builder.Services.AddDbContext<ExaminationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<LoginServices>();
builder.Services.AddScoped<EmailService>();

// Enable Controllers
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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