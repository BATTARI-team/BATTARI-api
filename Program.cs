using System.Reflection;
using System.Text;
using BATTARI_api.Data;
using BATTARI_api.Interfaces;
using BATTARI_api.Repository;
using BATTARI_api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var options = new WebApplicationOptions()
{
    Args = args,
    ContentRootPath =
                                                Directory.GetCurrentDirectory(),
    WebRootPath = "wwwroot"
};
var builder = WebApplication.CreateBuilder(options);

// Add services to the container.
// jwtの設定はここでやる
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(configureOptions =>
    {
        configureOptions.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                                         throw new ArgumentNullException(
                                             $"appsettingsのJwt:Keyがnullです"))),
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidateAudience = false,
            };
    });
builder.Services.AddDbContext<UserContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IUserRepository, UserDatabase>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshTokensRepository, RefreshTokenDatabase>();
builder.Services.AddScoped<IFriendRepository, FriendDatabase>();

builder.Services.AddSwaggerGen(c =>
{
    // ここを追加
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("token認証", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "トークンをセットします(先頭の 'Bearer' + space は不要)。",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    { new OpenApiSecurityScheme {
       Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme,
                                          Id = "token認証" }
     },
      // Scope は必要に応じて入力する
      new string[] {} }
  });
});

builder.WebHost.UseUrls("http://*:" + args[0]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for
    // production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 順番結構大事なので注意
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseRouting();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromHours(1),
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default",
                       pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
