using BaseLibrary.Entites;
using Infrastructure.Repositories.Implementations;
using Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Helpers;
using System.Diagnostics.Metrics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🟢 1️⃣ إضافة خدمات الـ Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🟢 2️⃣ تحميل إعدادات JWT بأمان
var jwtSection = builder.Configuration.GetSection("JwtSection").Get<JwtSection>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

builder.Services.Configure<JwtSection>(builder.Configuration.GetSection("JwtSection"));

// 🟢 3️⃣ إعداد قاعدة البيانات
builder.Services.AddDbContext<Infrastructure.Data.AppContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string is missing."));
});

// 🟢 4️⃣ إعداد المصادقة JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSection.Issuer,
        ValidAudience = jwtSection.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection.Key))
    };
});

builder.Services.AddScoped<IGenericRepositoryInterface<Doctor>, DoctorRepository>();
builder.Services.AddScoped<IGenericRepositoryInterface<Patient>, PatientRepository>();
builder.Services.AddScoped<IGenericRepositoryInterface<Appointment>, AppointmentRepository>();



// 🟢 6️⃣ تفعيل CORS بشكل صحيح
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
        policy.WithOrigins("http://localhost:5149", "https://localhost:7230")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

// 🟢 7️⃣ تهيئة الـ Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorWasm");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
