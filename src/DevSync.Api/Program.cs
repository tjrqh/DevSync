using DevSync.Api.Data;
using DevSync.Api.Entities;
using DevSync.Api.Hubs;
using DevSync.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddScoped<RoomAccessService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5170",
                "https://localhost:5171",
                "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "MySql";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

if (databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
}
else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("DevSync"));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var resetDatabase = app.Configuration.GetValue("ResetDatabaseOnStartup", false);

    await PrepareDatabaseAsync(db, resetDatabase);
    await SeedDataAsync(db, passwordHasher);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BlazorClient");
app.MapControllers();
app.MapHub<ProjectHub>("/hubs/project");
app.MapGet("/", () => "DevSync API is running.");
app.Run();

static async Task PrepareDatabaseAsync(AppDbContext db, bool resetDatabase)
{
    for (var retry = 1; retry <= 10; retry++)
    {
        try
        {
            if (resetDatabase)
            {
                await db.Database.EnsureDeletedAsync();
            }

            await db.Database.EnsureCreatedAsync();
            return;
        }
        catch when (retry < 10)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

static async Task SeedDataAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
{
    if (await db.Users.AnyAsync())
    {
        await BackfillDemoLoginAsync(db, passwordHasher);
        return;
    }

    var owner = new User { DisplayName = "홍길동", Email = "hong@example.com" };
    owner.PasswordHash = passwordHasher.HashPassword(owner, "Password123!");

    var member = new User { DisplayName = "김철수", Email = "kim@example.com" };
    member.PasswordHash = passwordHasher.HashPassword(member, "Password123!");

    var room = new Room
    {
        Name = "학원 팀 프로젝트",
        OwnerUser = owner,
        InviteToken = "devsync-demo-token"
    };

    room.Members.Add(new RoomMember { Room = room, User = owner, IsApproved = true, ApprovedAt = DateTimeOffset.UtcNow });
    room.Members.Add(new RoomMember { Room = room, User = member, IsApproved = true, ApprovedAt = DateTimeOffset.UtcNow });
    room.Tasks.Add(new ProjectTask { Title = "로그인 기능", Description = "JWT 또는 세션 기반 로그인 구현", Priority = "High", DueDate = DateTime.Today.AddDays(7), AssigneeUser = owner });
    room.Tasks.Add(new ProjectTask { Title = "칸반 보드 UI", Description = "Todo/InProgress/Done 컬럼 구성", Priority = "Medium", DueDate = DateTime.Today.AddDays(10), AssigneeUser = member });

    db.Rooms.Add(room);
    await db.SaveChangesAsync();
}

static async Task BackfillDemoLoginAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
{
    var users = await db.Users.OrderBy(x => x.Id).ToListAsync();
    foreach (var user in users)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            user.Email = user.Id == 1 ? "hong@example.com" : user.Id == 2 ? "kim@example.com" : $"user{user.Id}@example.com";
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash == "created-without-login")
        {
            user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
        }
    }

    await db.SaveChangesAsync();
}
