using Asset_Management.Database;
using Asset_Management.Extensions;
using Asset_Management.Interfaces;
using Asset_Management.Middleware;
using Asset_Management.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;


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
                          policy.WithOrigins("http://localhost:3000",
                              "http://10.10.10.7:3000").AllowAnyHeader().AllowAnyMethod();
                      });
});

//register DbContext
builder.Services.AddDbContext<AssetDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



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


//JsonSerializerService 
//builder.Services.AddSingleton<IAssetStorageService, JsonAssetStorageService>();

//XmlSerializerService
//builder.Services.AddSingleton<IAssetStorageService, XmlAssetStorageService>();



// Add Storage service in DI based on "StorageFlag" in appsettings.json
//string FileType = builder.Configuration["StorageFlag"].ToLower();
//if (FileType == "xml")
//{
//    builder.Services.AddSingleton<IAssetStorageService, XmlAssetStorageService>();
//}
//else
//{
//    builder.Services.AddSingleton<IAssetStorageService, JsonAssetStorageService>();
//}

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);
//app.UseAuthorization();

//app.UseMiddleware<RateLimitingCustomMiddelware>();
app.UseMiddleware<NewAssetsLoggerMiddleware>();

app.MapControllers();

app.Run();
