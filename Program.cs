using Microsoft.Extensions.Options;
using MsgBox.Data;
using MsgBox.Data.Migrations;
using MsgBox.Data.Repositories;
using MsgBox.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<LiteDbOptions>(
    builder.Configuration.GetSection(LiteDbOptions.SectionName));
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddSingleton<LiteDbContext>();
builder.Services.AddScoped<PersonRepository>();
builder.Services.AddScoped<ChatRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<SettingsRepository>();
builder.Services.AddSingleton<UploadStorage>();
builder.Services.AddScoped<MessageDtoMapper>();
builder.Services.AddScoped<MessageBulkImportService>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IEnumerable<IMigration>>(sp =>
{
    var dbOpts = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (!dbOpts.RunSeedDemoData)
        return Array.Empty<IMigration>();
    return new IMigration[] { new SeedDemoDataMigration() };
});
builder.Services.AddSingleton<MigrationRunner>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 104_857_600;
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    init.EnsureFilesystem();
    init.EnsureIndexesAndMigrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();
