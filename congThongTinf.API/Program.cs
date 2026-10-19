using congThongTinf.API.Architecture;
using congThongTinf.API.Middleware;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán JWT vào ô bên dưới. Swagger gửi header Authorization: Bearer {token}."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.SetupIocContainer();
builder.Services.AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseRouting();
app.UseRateLimiter();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "congThongTinf API v1"
        );

        c.RoutePrefix = string.Empty;

        c.ConfigObject.AdditionalItems.Add(
            "persistAuthorization",
            "true"
        );

        c.InjectJavascript("/custom-swagger.js");
    });
}


try
{
    app.ApplyMigrations(app.Logger);
}
catch (Exception e)
{
    app.Logger.LogError(e, "An problem occurred during migration!");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();