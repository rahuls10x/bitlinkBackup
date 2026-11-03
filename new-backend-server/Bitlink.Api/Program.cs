using Amazon.Runtime;
using Amazon.S3;
using Bitlink.Api.Configuration;
using Bitlink.Api.Data;
using Bitlink.Api.Hubs;
using Bitlink.Api.Middleware;
using Bitlink.Api.Repositories;
using Bitlink.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ------------- Configuration Options ---------------
builder.Services.Configure<AuthServerOptions>(builder.Configuration.GetSection(AuthServerOptions.SectionName));
builder.Services.Configure<S3Options>(builder.Configuration.GetSection(S3Options.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

var corsOrigin = builder.Configuration[$"{CorsOptions.SectionName}:OriginUrl"] ?? "http://localhost:5174";
const string corsPolicyName = "BitlinkCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins(corsOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ------------- Database Connection (PostgreSQL) ---------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection string is not configured");

builder.Services.AddDbContext<BitlinkDbContext>(options =>
    options.UseNpgsql(connectionString));

// ------------- S3 / MinIO Client ---------------
var s3Section = builder.Configuration.GetSection(S3Options.SectionName).Get<S3Options>() ?? new S3Options();
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = s3Section.Endpoint,
        ForcePathStyle = true,
        UseHttp = s3Section.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
    };

    var credentials = new BasicAWSCredentials(s3Section.AccessKey, s3Section.SecretKey);
    return new AmazonS3Client(credentials, config);
});

// ------------- HTTP Client ---------------
builder.Services.AddHttpClient<IAuthService, AuthService>();

// ------------- Repositories ---------------
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IUploadRepository, UploadRepository>();
builder.Services.AddScoped<IBucketRepository, BucketRepository>();

// ------------- Services ---------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IUploadService, UploadService>();

// ------------- Real-Time Communication (SignalR) ---------------
builder.Services.AddSignalR();

// ------------- Controllers & OpenAPI ---------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ------------- Middleware Pipeline ---------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(corsPolicyName);

app.MapControllers();
app.MapHub<FileHub>("/hubs/files");

app.Run();


