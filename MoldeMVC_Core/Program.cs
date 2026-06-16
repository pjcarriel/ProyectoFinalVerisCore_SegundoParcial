using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;
using Microsoft.AspNetCore.Authentication.Cookies;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(w => w.Ignore(
               Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
// .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddSingleton<MoldeMVC_Core.Data.MongoDbContext>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RedirectTo>();
});

builder.Services.AddAuthentication()
      .AddMicrosoftAccount(opciones =>
      {
          opciones.ClientId = builder.Configuration["MicrosoftClientId"]!;
          opciones.ClientSecret = builder.Configuration["MicrosoftSecretId"]!;
      })
      .AddFacebook(opciones =>
      {
        /*
            AppId corresponde al Identificador de la app
            obtenido desde Meta for Developers.
        */
        opciones.AppId = builder.Configuration["FacebookAppId"]!;

        /*
            AppSecret corresponde a la Clave secreta de la app
            obtenida desde Meta for Developers.
        */
        opciones.AppSecret = builder.Configuration["FacebookAppSecret"]!;

        /*
            Se eliminan permisos adicionales para evitar el error:
            Invalid Scopes: email
        */
        opciones.Scope.Clear();

        /*
            Se solicita únicamente el perfil público del usuario.
            Esto es suficiente para una práctica básica de login.
        */
        opciones.Scope.Add("public_profile");
    });


/*
    Se configuran opciones adicionales para la cookie de autenticación.
    Identity usa cookies para mantener la sesión iniciada del usuario.
*/
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    IdentityConstants.ApplicationScheme,
    opciones =>
    {
        /*
            Ruta a la que será enviado el usuario cuando necesite iniciar sesión.
        */
        opciones.LoginPath = "/Identity/Account/Login";

        /*
            Ruta a la que será enviado el usuario si intenta entrar
            a una página sin permisos suficientes.
        */
        opciones.AccessDeniedPath = "/Home/AccesoDenegado";
    });


// Configurar la sesion para mantener el estado del usuario
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccesoDenegado";
});

var app = builder.Build();
app.UseSession();

// **Aplicar Migraciones Autom�ticamente**
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // ── Seed Roles y Usuarios ────────────────────────────────────────────
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        var seedData = new[]
        {
            new { Rol = "Administrador", Usuario = "ADM"       },
            new { Rol = "Medico",        Usuario = "Medico"    },
            new { Rol = "Paciente",      Usuario = "Paciente"  },
            new { Rol = "SuperAdmin",    Usuario = "SuperAdmin" },
        };

        foreach (var item in seedData)
        {
            if (!await roleManager.RoleExistsAsync(item.Rol))
                await roleManager.CreateAsync(new IdentityRole(item.Rol));

            var email = $"{item.Usuario}@hotmail.com";
            var user  = await userManager.FindByNameAsync(item.Usuario);

            if (user == null)
            {
                user = new IdentityUser { UserName = item.Usuario, Email = email, EmailConfirmed = true };
                await userManager.CreateAsync(user, "123");
            }

            if (!await userManager.IsInRoleAsync(user, item.Rol))
                await userManager.AddToRoleAsync(user, item.Rol);
        }
        // ────────────────────────────────────────────────────────────────────
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
