using Amazon.S3;
using Amazon.S3.Model;
using congThongTinf.Domain;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Infrastructure.Interfaces;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Services;
using congThongTinf.Service.Services.Commons;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


namespace congThongTinf.API.Architecture;

public static class IocContainer
{
    public static IServiceCollection SetupIocContainer(this IServiceCollection services)
    {
        //Add Logger
        services.AddScoped<ILoggerService, LoggerService>();

        //Add Project Services
        services.SetupDbContext();
        //services.SetupSwagger();

        //Add generic repositories
        //services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        //Add business services
        services.SetupBusinessServicesLayer();

        services.SetupJwt();

        // services.SetupGraphQl();
        //services.SetupReSendService();

        services.SetupVnpay();
        return services;
    }


    //public static IServiceCollection SetupRedisService(this IServiceCollection services, IConfiguration configuration)
    //{
    //    var redisConnectionString = configuration.GetConnectionString("Redis");

    //    if (string.IsNullOrEmpty(redisConnectionString))
    //        throw new InvalidOperationException("Redis connection string is missing in environment variables.");

    //    services.AddSingleton<IConnectionMultiplexer>(
    //        ConnectionMultiplexer.Connect(redisConnectionString));

    //    services.AddScoped<ICacheService, RedisCacheService>();

    //    return services;
    //}


    // public static IServiceCollection SetupGraphQl(this IServiceCollection services)
    // {
    //     services
    //         .AddGraphQLServer()
    //         .AddErrorFilter<GraphQLErrorFilter>()
    //         .AddQueryType<Query>();
    //     
    //     return services;
    // }

    //public static IServiceCollection SetupReSendService(this IServiceCollection services)
    //{
    //    services.AddOptions();
    //    services.AddHttpClient<ResendClient>();
    //    services.Configure<ResendClientOptions>(o =>
    //    {
    //        o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN")!;
    //    });
    //    services.AddTransient<IResend, ResendClient>();

    //    return services;
    //}

    public static IServiceCollection SetupObjectStorage(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile("appsettings.Development.json", true, true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables()
            .Build();

        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));

        var storage = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>()
                 ?? throw new InvalidOperationException("ObjectStorage configuration section is missing.");

        if (string.IsNullOrWhiteSpace(storage.ServiceUrl) ||
            string.IsNullOrWhiteSpace(storage.BucketName) ||
            string.IsNullOrWhiteSpace(storage.AccessKey) ||
            string.IsNullOrWhiteSpace(storage.SecretKey))
        {
            throw new InvalidOperationException("ObjectStorage ServiceUrl, BucketName, AccessKey, and SecretKey are required.");
        }

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var region = string.IsNullOrWhiteSpace(storage.Region) ? "us-east-1" : storage.Region;
            var s3Config = new AmazonS3Config
            {
                ServiceURL = storage.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = region,
                UseHttp = storage.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            };

            var client = new AmazonS3Client(storage.AccessKey, storage.SecretKey, s3Config);
            EnsureBucketExists(client, storage.BucketName);
            return client;
        });

        return services;
    }

    private static void EnsureBucketExists(IAmazonS3 client, string bucketName)
    {
        const int maxAttempts = 10;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var buckets = client.ListBucketsAsync().GetAwaiter().GetResult();
                if (buckets.Buckets?.Any(b => string.Equals(b.BucketName, bucketName, StringComparison.OrdinalIgnoreCase)) == true)
                    return;

                client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                }).GetAwaiter().GetResult();
                return;
            }
            catch (AmazonS3Exception ex) when (
                string.Equals(ex.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ex.ErrorCode, "BucketAlreadyExists", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastError = ex;
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException($"Unable to ensure object storage bucket '{bucketName}'.", lastError);
    }

    public static IServiceCollection SetupVnpay(this IServiceCollection services)
    {
        // Xây dựng IConfiguration từ các nguồn cấu hình
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Lấy thư mục hiện tại
            .AddJsonFile("appsettings.json", true, true) // Đọc appsettings.json
            .AddEnvironmentVariables() // Đọc biến môi trường từ Docker
            .Build();

        return services;
    }

    private static IServiceCollection SetupDbContext(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<congThongTinfDbContext>(options =>
            options.UseNpgsql(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(congThongTinfDbContext).Assembly.FullName);
                sql.CommandTimeout(300); // Cấu hình thời gian timeout truy vấn (tính bằng giây)
                sql.EnableRetryOnFailure(
                    5,
                    TimeSpan.FromSeconds(10),
                    null
                );
            })
        );

        return services;
    }


    public static IServiceCollection SetupBusinessServicesLayer(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.SetupObjectStorage();
        services.AddScoped<IR2StorageAnalyticsService, R2StorageAnalyticsService>();
        services.AddScoped<IBlobService, BlobService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<ILeaderService, LeaderService>();
        services.AddScoped<ISharedFileService, SharedFileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISystemService, SystemService>();
        services.AddScoped<IClaimsService, ClaimsService>();

        services.AddHttpContextAccessor();

        return services;
    }


    //public static IServiceCollection SetupSwagger(this IServiceCollection services)
    //{
    //    services.AddSwaggerGen(c =>
    //    {
    //        c.SwaggerDoc("v1", new OpenApiInfo
    //        {
    //            Title = "BlindTreasure API",
    //            Version = "v1",
    //            Description = "API cho hệ thống thương mại điện tử BlindTreasure."
    //        });


    //        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    //        {
    //            Name = "Authorization",
    //            Type = SecuritySchemeType.Http,
    //            Scheme = "bearer",
    //            BearerFormat = "JWT",
    //            In = ParameterLocation.Header,
    //            Description = "Nhập token vào format: Bearer {your token}"
    //        });

    //        c.AddSecurityRequirement(new OpenApiSecurityRequirement
    //        {
    //            {
    //                new OpenApiSecurityScheme
    //                {
    //                    Reference = new OpenApiReference
    //                    {
    //                        Type = ReferenceType.SecurityScheme,
    //                        Id = "Bearer"
    //                    }
    //                },
    //                Array.Empty<string>()
    //            }
    //        });

    //        // Load XML comment
    //        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    //        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    //        c.IncludeXmlComments(xmlPath);
    //    });


    //    return services;
    //}

    private static IServiceCollection SetupJwt(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.SaveToken = true;
                x.MapInboundClaims = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"] ??
                                                                        throw new InvalidOperationException())),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs/notification") ||
                             path.StartsWithSegments("/hubs/chat") ||
                             path.StartsWithSegments("/api/shared-files")))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    },
                };
            });
        services.AddAuthorization(options =>
        {
            //options.AddPolicy("CustomerPolicy", policy =>
            //    policy.RequireRole("Customer"));

            //options.AddPolicy("AdminPolicy", policy =>
            //    policy.RequireRole("Admin"));

            //options.AddPolicy("StaffPolicy", policy =>
            //    policy.RequireRole("Staff"));

            //options.AddPolicy("SellerPolicy", policy =>
            //    policy.RequireRole("Seller"));
        });

        return services;
    }
}