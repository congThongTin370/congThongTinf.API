using congThongTinf.API.Architecture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.SetupIocContainer();

var app = builder.Build();

app.UseCors("AllowFrontend");

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


app.UseAuthorization();

app.MapControllers();

app.Run();