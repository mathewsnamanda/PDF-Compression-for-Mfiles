using Mhasibu.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NTg0NUAzMjMwMkUzMzJFMzBkNm9FSEk1YjRsU203NnpOdzNjeXNVMGtHM2g3Y05QMlZkbWFWY1FpL3NjPQ==\r\n");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

 }
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>();


app.UseAuthorization();

app.MapControllers();

app.Run();
