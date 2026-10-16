using GestaoEntregas.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuração JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "chave-padrao-seguranca-super-secreta-123456";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GestaoEntregasAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GestaoEntregasUsers",
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<NFeParserService>();
builder.Services.AddEndpointsApiExplorer();

// Configuração padrão do Swagger (sem conflitos de classes de segurança)
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// 1. Endpoint de Login (Gera o Token)
app.MapPost("/api/auth/login", (LoginRequest request, IConfiguration config) =>
{
    if (request.Username == "admin" && request.Password == "admin123")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = config["Jwt:Issuer"] ?? "GestaoEntregasAPI",
            Audience = config["Jwt:Audience"] ?? "GestaoEntregasUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
    }

    return Results.Unauthorized();
});

// 2. Endpoint Protegido para Importar o XML da Nota Fiscal
app.MapPost("/api/nfe/importar", async (IFormFile file, NFeParserService parser) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { message = "Selecione um arquivo XML válido." });

    try
    {
        using var stream = file.OpenReadStream();
        var dadosNota = parser.ParseXml(stream);

        return Results.Ok(new
        {
            message = "Nota Fiscal processada com sucesso!",
            dados = dadosNota
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = $"Erro ao processar o XML: {ex.Message}" });
    }
})
.DisableAntiforgery()
.RequireAuthorization();

app.Run();

record LoginRequest(string Username, string Password);