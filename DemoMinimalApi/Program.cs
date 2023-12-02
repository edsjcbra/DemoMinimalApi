using DemoMinimalApi.Models;
using DemoMinimalApi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MiniValidation;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MinimalContextDb>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("DemoMinimalApi")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirFornecedor",
        policy => policy.RequireClaim("ExcuirFornecedor"));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();
app.UseHttpsRedirection();

//METODOS AUTH
app.MapPost("/registro", [AllowAnonymous] async(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> appJwtSettings,
    RegisterUser registerUser) =>
    {
        if (registerUser == null)
            return Results.BadRequest("Usuario nao informado");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
        .WithUserManager(userManager)
        .WithJwtSettings(appJwtSettings.Value)
        .WithEmail(user.Email)
        .WithJwtClaims()
        .WithUserClaims()
        .WithUserRoles()
        .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("RegistroUsuario")
        .WithTags("Usuario");

app.MapPost("/login", [AllowAnonymous] async (
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> appJwtSettings,
    LoginUser loginUser) =>
{
    if (loginUser == null)
        return Results.BadRequest("Usuario nao informado");

    if (!MiniValidator.TryValidate(loginUser, out var errors))
        return Results.ValidationProblem(errors);

    var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, false);

    if (result.IsLockedOut)
        return Results.BadRequest("Usuario bloqueado");

    if (!result.Succeeded)
        return Results.BadRequest("Usuario ou Senha invalidos");

    var jwt = new JwtBuilder()
    .WithUserManager(userManager)
    .WithJwtSettings(appJwtSettings.Value)
    .WithEmail(loginUser.Email)
    .WithJwtClaims()
    .WithUserClaims()
    .WithUserRoles()
    .BuildUserResponse();

    return Results.Ok(jwt);

}).ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("LoginUsuario")
        .WithTags("Usuario");

//METDOS CRUD
app.MapGet("/fornecedor", [AllowAnonymous] async (
    MinimalContextDb context) =>

    await context.Fornecedores.ToListAsync())
    .WithName("GetFornecedores")
    .WithTags("Fornecedor");

app.MapGet("/fornecedor{id}", async (
    Guid id,
    MinimalContextDb context) =>

    await context.Fornecedores.FindAsync(id)
        is Fornecedor fornecedor
             ? Results.Ok(fornecedor)
             : Results.NotFound())
    .Produces<Fornecedor>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetFornecedorPorId")
    .WithTags("Fornecedor");

app.MapPost("/fornecedor", [Authorize] async (
    Fornecedor fornecedor,
    MinimalContextDb context) =>
{
    if (!MiniValidator.TryValidate(fornecedor, out var errors)) 
        return Results.ValidationProblem(errors);

    context.Fornecedores.Add(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
    // poder ser assim: ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
    ? Results.CreatedAtRoute("GetFornecedorPorId", new {id = fornecedor.Id}, fornecedor)
    : Results.BadRequest("Houve um problema ao salvar o Registro");
}).ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PostFornecedor")
    .WithTags("Fornecedor");

app.MapPut("/fornecedor/{id}", [Authorize] async (
    Guid id,
    MinimalContextDb context,
    Fornecedor fornecedor) =>
{
    var fornecedorBanco = await context.Fornecedores.AsNoTracking<Fornecedor>().FirstOrDefaultAsync(f=>f.Id == id);
    if (fornecedorBanco == null) return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Update(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
    // poder ser assim: ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
    ? Results.NoContent()
    : Results.BadRequest("Houve um problema ao atualizar o Registro");
}).ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PutFornecedor")
    .WithTags("Fornecedor");

app.MapDelete("/fornecedor/{id}", [Authorize] async (
    Guid id,
    MinimalContextDb context) =>
{
    var fornecedor = await context.Fornecedores.FindAsync(id);
    if (fornecedor == null) return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Remove(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
    // poder ser assim: ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
    ? Results.NoContent()
    : Results.BadRequest("Houve um problema ao deletar o Registro");
}).ProducesValidationProblem()
.Produces(StatusCodes.Status400BadRequest)
    .Produces<Fornecedor>(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("ExcluirFornecedor")
    .WithName("DeleteFornecedor")
    .WithTags("Fornecedor");
//commited to GitHub
app.Run();