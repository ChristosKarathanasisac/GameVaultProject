using GameVault.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Catalog.Infrastructure.Persistence;

public static class CatalogDataSeeder
{
    public static async Task SeedAsync(CatalogDbContext db)
    {
        if (await db.Products.AnyAsync())
            return;

        var products = new[]
        {
            (Name: "The Witcher 3: Wild Hunt",      Description: "Open-world RPG set in a dark fantasy universe. Hunt monsters and make choices that shape the world.",        Price: 39.99m,  Stock: 150),
            (Name: "Cyberpunk 2077",                 Description: "Action RPG set in a dystopian open world. Play as V, a mercenary outlaw in Night City.",                    Price: 49.99m,  Stock: 120),
            (Name: "Red Dead Redemption 2",          Description: "Epic tale of life in America's unforgiving heartland. Open-world action-adventure set in 1899.",            Price: 59.99m,  Stock: 200),
            (Name: "Elden Ring",                     Description: "Action RPG set in the Lands Between, crafted by FromSoftware and George R.R. Martin.",                      Price: 59.99m,  Stock: 180),
            (Name: "Baldur's Gate 3",                Description: "RPG based on Dungeons & Dragons 5th Edition rules. Over 100 hours of branching narrative.",                 Price: 59.99m,  Stock: 160),
            (Name: "God of War: Ragnarök",           Description: "Kratos and Atreus journey through Norse realms to prevent the apocalypse.",                                  Price: 49.99m,  Stock: 140),
            (Name: "Hollow Knight",                  Description: "Classic action-adventure through a vast ruined kingdom of insects and heroes.",                              Price: 14.99m,  Stock: 300),
            (Name: "Hades",                          Description: "Rogue-like dungeon crawler where you defy the god of death. Exquisite combat and storytelling.",            Price: 24.99m,  Stock: 250),
            (Name: "Stardew Valley",                 Description: "Build the farm of your dreams in this relaxing open-ended farming RPG.",                                     Price: 14.99m,  Stock: 500),
            (Name: "Disco Elysium: The Final Cut",   Description: "Revolutionary RPG with unprecedented freedom of choice and zero combat.",                                   Price: 39.99m,  Stock: 100),
        };

        foreach (var (name, description, price, stock) in products)
        {
            var product = Product.Create(name, description, price);
            db.Products.Add(product);
            db.Stock.Add(Stock.Create(product.Id, stock));
        }

        await db.SaveChangesAsync();
    }
}
