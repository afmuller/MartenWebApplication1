using Marten;
using System.Linq.Expressions;
using MartenWebApplication1;
using Weasel.Core;
using static Weasel.Postgresql.TokenParser;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("MartenConnection"));
    options.AutoCreateSchemaObjects = AutoCreate.All;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var session = services.GetRequiredService<IDocumentSession>();

        session.DeleteWhere<Post>(p => true);
        session.SaveChanges();

        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 1", Type = "Featured", Section = "Section1", Tags = new[] { "general" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 2", Type = "Featured", Section = "Section1", Tags = new[] { "music" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 3", Type = "General", Section = "Section1", Tags = new[] { "news" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 4", Type = "General", Section = "Section1", Tags = new[] { "news" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 5", Type = "General", Section = "Section2", Tags = new[] { "general" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 6", Type = "General", Section = "Section2", Tags = new[] { "general" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 7", Type = "General", Section = "Section2", Tags = new[] { "general" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 8", Type = "General", Section = "Section2", Tags = new[] { "sports" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 9", Type = "Featured", Section = "Section3", Tags = new[] { "music" } });
        session.Store<Post>(new Post() { Id = Guid.NewGuid(), Subject = "New 10", Type = "General", Section = "Section3", Tags = new[] { "music" } });

        session.SaveChanges();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogError(ex, "An error occurred while seeding the database.");

        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var group = app.MapGroup("/")
    .WithOpenApi();

group.MapGet("/getallposts", async (IQuerySession session) =>
{
    var result = await session.Query<Post>().ToListAsync();
    return result;
});

group.MapGet("/getallfeaturedformusic", async (IQuerySession session) =>
    {
        // This query works as expected
        IQueryable<Post> query = session.Query<Post>();

        query = query.Where(p => p.Type.Equals("Featured") && p.Tags.Contains("music"));

        var result = await query.ToListAsync();
        return result;
    })
    .WithName("GetAllFeaturedPostsForMusic");

group.MapGet("/getallfeaturedexceptmovies", async (IQuerySession session) =>
{
    // This query does NOT work as expected - the Type='Featured' filter seems to be ignored
    IQueryable<Post> query = session.Query<Post>();

    query = query.Where(p => !p.Tags.Contains("movies"));
    query = query.Where(p => p.Type.Equals("Featured"));

    var result = await query.ToListAsync();
    return result;
})
    .WithName("GetAllFeaturedPostsExceptMovies");

group.MapGet("/getallfeaturedexceptmusic", async (IQuerySession session) =>
    {
        // This query does NOT work as expected - the Type='Featured' filter seems to be ignored
        IQueryable<Post> query = session.Query<Post>();

        query = query.Where(p => p.Type.Equals("Featured") && !p.Tags.Contains("music"));

        var result = await query.ToListAsync();
        return result;
    })
    .WithName("GetAllFeaturedPostsExceptForMusic");

group.MapGet("/getallfeaturedexceptmusicinsection1a", async (IQuerySession session) =>
    {
        // This query works as expected.
        IQueryable<Post> query = session.Query<Post>();
        query = query.Where(p => p.Type.Equals("Featured") && !p.Tags.Contains("music") && p.Section.Equals("Section1"));
        var result = await query.ToListAsync();
        return result;
    })
    .WithName("GetAllFeaturedPostsExceptForMusicInSection1a");

group.MapGet("/getallfeaturedexceptmusicinsection1b", async (IQuerySession session) =>
    {
        // This query does NOT work as expected -  - the Type='Featured' & Section filter seems to be ignored - even though its basically exactly the same as 'getallfeaturedexceptmusicinsection1a' (only with the order switched around)
        IQueryable<Post> query = session.Query<Post>();
        query = query.Where(p => p.Type.Equals("Featured") && p.Section.Equals("Section1") && !p.Tags.Contains("music"));
        var result = await query.ToListAsync();
        return result;
    })
    .WithName("GetAllFeaturedPostsExceptForMusicInSection1b");

group.MapGet("/getallfeaturedexceptmusicinsection1expression", async (IQuerySession session) =>
    {
        // This query does NOT work as expected - the Type='Featured' filter seems to be ignored
        IQueryable<Post> query = session.Query<Post>();

        Expression<Func<Post, bool>>? filter = ExpressionHelpersExtensions.CreateCombinedAndLambda<Post>(
            p => p.Type.Equals("Featured"),
            p => p.Section.Equals("Section1"),
            p => !p.Tags.Contains("music")
        );

        if ( filter != null )
            query = query.Where(filter);

        var result = await query.ToListAsync();
        return result;
    })
    .WithName("GetAllFeaturedPostsExceptForMusicInSection1expression");

app.Run();


public class Post
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Section { get; set; } = "";
    public string Subject { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();
}