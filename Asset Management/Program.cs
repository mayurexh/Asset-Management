using Asset_Management.Database;
using Asset_Management.Extensions;
using Asset_Management.Hubs;
using Asset_Management.Interfaces;
using Asset_Management.Middleware;
using Asset_Management.Models;
using Asset_Management.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// Serilog config
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/newly_merged_assets-.log", rollingInterval: RollingInterval.Day,
    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
    buffered: false)
    .CreateLogger();


builder.Host.UseSerilog();


// Add services to the container.

builder.Services.AddControllers().AddXmlSerializerFormatters();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

// Add this line
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Asset Hierarchy Management API",
        Version = "v1",
        Description = "An API for managing Hierarchy of Assets",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Mayuresh",
            Email = "mayuresh.pisat@wonderbiz.in",
            Url = new Uri("https://github.com/mayurexh/Asset-Management")
        },
    });
});


//add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                          
                      });
});

//register DbContext
builder.Services.AddDbContext<AssetDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//register Signals Service
builder.Services.AddScoped<ISignalsService, SignalsService>();

//Adding (built-in) Middleware for RateLimiter 
//builder.Services.AddRateLimiter(options =>
//{
//    options.AddFixedWindowLimiter("fixed",opt =>
//    {
//        opt.PermitLimit = 10;
//        opt.Window = TimeSpan.FromSeconds(20);
//        opt.AutoReplenishment = true;
//        opt.QueueLimit = 0;
//    });
//});

//Hierarchy Management Service
//builder.Services.AddTransient<IAssetHierarchyService,AssetHierarchyService>();
builder.Services.AddAssetHierarchyService(builder.Configuration);


//Import Log service
builder.Services.AddSingleton<IUploadLogService, UploadLogService>();

// Register Json and XML storage service using Extensions
builder.Services.AddStorageServices(builder.Configuration);

// Add a password hasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

//Notification service (singleton/ is stateless)
builder.Services.AddSingleton<INotificationService, NotificationService>();

//configure jwt settings
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        //ValidIssuer = jwtSettings["Issuer"],
        //ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // tell authroization middleware to look for token in cookies
    //options.Events = new JwtBearerEvents
    //{
    //    OnMessageReceived = context =>
    //    {
    //        var accessToken = context.Request.Query["access_token"];
    //        var path = context.HttpContext.Request.Path;

    //        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/Notification"))
    //        {
    //            context.Token = accessToken;
    //        }
    //        return Task.CompletedTask;
    //    }
    //};

    //Read token from cookie instead of Authorization header
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            
            // Then, check for token in cookies (for regular API calls)
            if(context.Request.Cookies.ContainsKey("token"))
            {
                context.Token = context.Request.Cookies["token"];
                Console.WriteLine("Token set from cookie");
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();


//supress model state default behaviour 
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddScoped<IAssetLogService, AssetLogService>();


//signal R DI
builder.Services.AddSignalR();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

//app.UseMiddleware<RateLimitingCustomMiddelware>();
app.UseMiddleware<NewAssetsLoggerMiddleware>();

app.MapControllers();

app.MapHub<NotificationHub>("/Notification");


app.Run();
