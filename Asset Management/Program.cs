using Asset_Management.Interfaces;
using Asset_Management.Services;
using Asset_Management.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using Asset_Management.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddXmlSerializerFormatters();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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
builder.Services.AddSingleton<Asset_Management.Services.AssetHierarchyService>();

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

//app.UseAuthorization();

app.UseMiddleware<RateLimitingCustomMiddelware>();


app.MapControllers();

app.Run();
