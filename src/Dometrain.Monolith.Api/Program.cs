using System.Text;
using Dometrain.Monolith.Api.Courses;
using Dometrain.Monolith.Api.Database;
using Dometrain.Monolith.Api.Enrollments;
using Dometrain.Monolith.Api.ErrorHandling;
using Dometrain.Monolith.Api.Identity;
using Dometrain.Monolith.Api.OpenApi;
using Dometrain.Monolith.Api.Orders;
using Dometrain.Monolith.Api.ShoppingCarts;
using Dometrain.Monolith.Api.Students;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(x => x.OperationFilter<SwaggerDefaultValues>());

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Identity:Key"]!)),
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = config["Identity:Issuer"],
        ValidAudience = config["Identity:Audience"],
        ValidateIssuer = true,
        ValidateAudience = true
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ApiAdmin", p => p.AddRequirements(new AdminAuthRequirement(config["Identity:AdminApiKey"]!)))
    .AddPolicy("Admin", p => p.RequireAssertion(c => 
            c.User.HasClaim(m => m is { Type: "is_admin", Value: "true" })));

builder.Services.AddScoped<ApiKeyAuthFilter>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

builder.Services.Configure<IdentitySettings>(builder.Configuration.GetSection(IdentitySettings.SettingsKey));

builder.Services.AddSingleton<DbInitializer>();
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.AddNpgsqlDataSource("dometrain");
builder.AddAzureCosmosClient("cartdb");
builder.AddRedisClient("redis");

builder.Services.AddMassTransit(s =>
{
    s.AddConsumers(typeof(Program).Assembly);
    s.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(config["ConnectionStrings:rabbitmq"]!));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSingleton<IPasswordHasher<Student>, PasswordHasher<Student>>();
builder.Services.AddSingleton<IIdentityService, IdentityService>();

builder.Services.AddSingleton<IStudentService, StudentService>();
builder.Services.AddSingleton<IStudentRepository, StudentRepository>();

builder.Services.AddSingleton<ICourseService, CourseService>();

builder.Services.AddSingleton<CourseRepository>();
builder.Services.AddSingleton<ICourseRepository>(x => 
    new CachedCourseRepository(x.GetRequiredService<CourseRepository>(),
        x.GetRequiredService<IConnectionMultiplexer>()));

builder.Services.AddSingleton<IShoppingCartService, ShoppingCartService>();

builder.Services.AddSingleton<ShoppingCartRepository>();
builder.Services.AddSingleton<IShoppingCartRepository>(x =>
    new CachedShoppingCartRepository(x.GetRequiredService<ShoppingCartRepository>(), x.GetRequiredService<IConnectionMultiplexer>()));


builder.Services.AddSingleton<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IOrderService, OrderService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();
app.MapStudentEndpoints();
app.MapCourseEndpoints();
app.MapShoppingCartEndpoints();
app.MapEnrollmentEndpoints();
app.MapOrderEndpoints();

var db = app.Services.GetRequiredService<DbInitializer>();
await db.InitializeAsync();


app.Run();
