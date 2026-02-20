using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Repositories;
using MyApp.Application.Services;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.Identity;
using MyApp.Infrastructure.Persistence;
using MyApp.WebApi.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cs = builder.Configuration.GetConnectionString("Default")
         ?? builder.Configuration.GetConnectionString("Postgres")
         ?? builder.Configuration.GetConnectionString("Connection")
         ?? "Host=127.0.0.1;Port=5432;Database=myapp;Username=postgres;Password=postgres";

builder.Services.AddDbContext<MyApp.Infrastructure.Persistence.AppDbContext>(o => o.UseNpgsql(cs));
builder.Services.AddDbContext<MyApp.Infrastructure.Data.AppDbContext>(o => o.UseNpgsql(cs));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequireDigit = true;
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<MyApp.Infrastructure.Persistence.AppDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-key-change-me-please-32-bytes-min";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MyApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MyApp";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddGrpc();

builder.Services.AddScoped<ImageService>(sp =>
{
    var repo = sp.GetRequiredService<IImageRepository>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var uploadsPath = Path.Combine(env.WebRootPath, "uploads");
    Directory.CreateDirectory(uploadsPath);
    return new ImageService(repo, uploadsPath);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var identityDb = services.GetRequiredService<MyApp.Infrastructure.Persistence.AppDbContext>();
    identityDb.Database.Migrate();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var roles = new[] { "admin", "user" };
    foreach (var r in roles)
    {
        if (!await roleManager.RoleExistsAsync(r))
            await roleManager.CreateAsync(new IdentityRole(r));
    }

    var adminEmail = app.Configuration["Seed:AdminEmail"] ?? "admin@local";
    var adminPassword = app.Configuration["Seed:AdminPassword"] ?? "Admin123$";

    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var created = await userManager.CreateAsync(admin, adminPassword);
        if (created.Succeeded)
            await userManager.AddToRoleAsync(admin, "admin");
    }

    var userEmail = app.Configuration["Seed:UserEmail"] ?? "user@local";
    var userPassword = app.Configuration["Seed:UserPassword"] ?? "User123$";

    var user = await userManager.FindByEmailAsync(userEmail);
    if (user is null)
    {
        user = new ApplicationUser { UserName = userEmail, Email = userEmail, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, userPassword);
        if (created.Succeeded)
            await userManager.AddToRoleAsync(user, "user");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<ImagesGrpcService>();

app.Run();
