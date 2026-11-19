using ArchGuard.Application.Interfaces;
using ArchGuard.Infrastructure.AI;
using ArchGuard.Infrastructure.ExternalServices;
using ArchGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
	options.AddPolicy("ArchGuardUIPolicy", policy =>
	{
		policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
					.AllowAnyHeader()
					.AllowAnyMethod();
	});
});

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
																						builder.Configuration.GetConnectionString("DefaultConnection"),
																						npgsqlOptions => npgsqlOptions.MigrationsAssembly("ArchGuard.Infrastructure")
																						));

builder.Services.AddHttpClient<IIfcProcessorClient, IfcProcessorClient>(client =>
{
	var baseUrl = builder.Configuration["ExternalServices:IfcProcessorBaseUrl"] ?? "http://localhost:8020";
	client.BaseAddress = new Uri(baseUrl);
	client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient<GeminiEmbeddingService>();

builder.Services.AddScoped<IBimProjectService, BimProjectService>();
builder.Services.AddScoped<IRegulationRagService, QdrantRegulationRagService>();
builder.Services.AddScoped<IDynamicKernelFactory, DynamicKernelFactory>();
builder.Services.AddScoped<IBimAuditService, BimAuditService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// 2. Uygulama ayaða kalkarken veritabanýný ve bekleyen migration'larý kontrol edip uygular
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<AppDbContext>();
		await context.Database.MigrateAsync();
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "Veritabaný migration iþlemi sýrasýnda bir hata oluþtu.");
		throw;
	}
}


if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference(options =>
	{
		options.WithTitle("ArchGuard AI - BIM & Compliance API")
					 .WithTheme(ScalarTheme.DeepSpace);
	});
}

app.UseHttpsRedirection();
app.UseCors("ArchGuardUIPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();