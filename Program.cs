using System.Text;
using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MetrixVox.Api.Middleware;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// Configurar licencia de QuestPDF (Community para uso personal/pequeña empresa)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// CORS: abierto solo en desarrollo; en producción únicamente la App web oficial
builder.Services.AddCors(options =>
{
    options.AddPolicy("MetrixVoxCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
		policy.WithOrigins(
		 "https://app.metrixvox.com",
		 "https://admin.metrixvox.com",
		 "http://localhost:8080",
		 "http://localhost:8081",   // ← NUEVO: kiosco local
		 "http://localhost:5000",
		 "http://localhost:5188")
	      .AllowAnyHeader()
	      .AllowAnyMethod();
        }
    });
});

// Tenant de la petición actual (una instancia por request)
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

builder.Services.AddScoped<ISurveyDefinitionService, SurveyDefinitionService>();
builder.Services.AddScoped<ISurveyManagementService, SurveyManagementService>();
builder.Services.AddScoped<ISurveyIngestionService, SurveyIngestionService>();
builder.Services.AddScoped<AdminReportsService>();
builder.Services.AddHttpClient<IAiAnalyticsService, AiAnalyticsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddHttpClient<IAiReportAnalysisService, AiReportAnalysisService>();

// Base única: todos los clientes comparten tablas y se separan por TenantId (filtro global en TenantDbContext)
builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// ============================================================
// JWT Authentication
// ============================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("Falta Jwt:SecretKey en appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // 🆕 Evita que el framework reescriba los claims cortos a URIs largas
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"] ?? "api.metrixvox.com",
        ValidAudience = jwtSection["Audience"] ?? "metrixvox",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(2),
    };
});

builder.Services.AddAuthorization();

// Servicios de autenticación
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAuthorization();
// Servicios de kioscos
builder.Services.AddScoped<IKioskService, KioskService>();   // 🆕

// builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger solo en desarrollo, o si se activa explícitamente con Swagger:Enabled=true
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MetrixVox API v1");
        c.RoutePrefix = "swagger";
    });
}

// En desarrollo se usa http para que la App web local pueda llamar sin problemas de certificado
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("MetrixVoxCorsPolicy");
app.UseAuthentication();           // 🆕 AGREGAR
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();            // ya estaba
app.MapControllers();

app.Run();
