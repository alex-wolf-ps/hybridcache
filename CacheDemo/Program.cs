using CachingDemo;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

builder.Services.AddSingleton<SlowDateService>();

// Simple in-memory cache
builder.Services.AddMemoryCache();

// Both for distributed
//builder.Services.AddDistributedMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration.GetConnectionString("RedisConnectionString");
});

// New in-memory and distributed cache
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(10),
        LocalCacheExpiration = TimeSpan.FromSeconds(10)
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseHttpsRedirection();

// Memory Cache
app.MapGet("/memorycache", async (SlowDateService slowService, IMemoryCache cache) =>
{
    var key = $"current-time";

    var currentTime = await cache.GetOrCreateAsync(
        key,
        async cacheEntry => { return await slowService.GetSlowDateAsync(); },
        new MemoryCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(10) });

    return currentTime;
})
.WithName("GetMemoryCache");




// Distributed cache
app.MapGet("/distributedCache", async (SlowDateService slowService, IDistributedCache cache) =>
{
    var key = $"current-time";

    // Try to get from cache.
    var bytes = await cache.GetAsync(key);

    var currentTime = "";
    if (bytes is null)
    {
        // Not cached, get data from source
        currentTime = await slowService.GetSlowDateAsync();

        // Serialize and cache
        bytes = Encoding.ASCII.GetBytes(currentTime);
        await cache.SetAsync(key, bytes, 
            new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(10) });
    }
    else
    {
        // Deserialize and return cache item
        currentTime = Encoding.Default.GetString(bytes);
    }

    return currentTime;
})
.WithName("GetDistributedCache");




// Hybrid Cache
app.MapGet("/hybridcache", async (SlowDateService slowService, HybridCache cache) =>
{
    var key = $"current-time";

    return await cache.GetOrCreateAsync(
        key, // Unique key for this combination.
        async cancel => await slowService.GetSlowDateAsync()
    );
})
.WithName("GetHybridCache");

app.Run();
