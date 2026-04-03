using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MsgBox.Data;
using MsgBox.Data.Migrations;
using MsgBox.Data.Repositories;
using MsgBox.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddSingleton<AppStoragePaths>();
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

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-msgbox-csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddRazorPages();
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

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

app.Use(async (context, next) =>
{
    var csp = app.Environment.IsDevelopment()
        ? "default-src 'self' data: blob: http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*; " +
          "script-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:* https://localhost:*; " +
          "style-src 'self' 'unsafe-inline'; " +
          "img-src 'self' data: blob: http://localhost:* https://localhost:*; " +
          "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*; " +
          "font-src 'self' data:; " +
          "object-src 'none'; " +
          "frame-ancestors 'none'; " +
          "base-uri 'self'; " +
          "form-action 'self'"
        : "default-src 'self'; " +
          "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
          "style-src 'self'; " +
          "img-src 'self' data: blob:; " +
          "connect-src 'self'; " +
          "font-src 'self'; " +
          "object-src 'none'; " +
          "frame-ancestors 'none'; " +
          "base-uri 'self'; " +
          "form-action 'self'";

    context.Response.Headers["Content-Security-Policy"] = csp;
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();
