using MeuBlog.Components;
using MeuBlog.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor in Static SSR mode: renders HTML on the server, no SignalR/WebAssembly
builder.Services.AddRazorComponents();

// Reads the .md files once and keeps them in memory (no database)
builder.Services.AddSingleton<PostService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>();

app.Run();
