using Asset_Management.Interfaces;
using Asset_Management.Services;
using Asset_Management.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddXmlSerializerFormatters();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<Asset_Management.Services.AssetHierarchyService>();

// Register Json and XML storage service using Extensions
builder.Services.AddStorageServices(builder.Configuration);

//JsonSerializerService 
//builder.Services.AddSingleton<IAssetStorageService, JsonAssetStorageService>();

//XmlSerializerService
//builder.Services.AddSingleton<IAssetStorageService, XmlAssetStorageService>();


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

app.UseAuthorization();

app.MapControllers();

app.Run();
