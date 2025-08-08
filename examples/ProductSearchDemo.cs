using System;
using System.Collections.Generic;
using System.Linq;

// E-commerce product search - demonstrating the clustering problem
public class ProductSearchDemo
{
    public static void Main()
    {
        // Product catalog with similarity scores to query "wireless headphones"
        var products = new[]
        {
            new { Name = "Sony WH-1000XM4 Wireless Headphones", Similarity = 0.95, Category = "Audio" },
            new { Name = "Bose QuietComfort Wireless Headphones", Similarity = 0.93, Category = "Audio" },
            new { Name = "Apple AirPods Pro Wireless Earbuds", Similarity = 0.91, Category = "Audio" },
            new { Name = "Wireless Phone Charger", Similarity = 0.45, Category = "Accessories" },
            new { Name = "Bluetooth Speaker", Similarity = 0.42, Category = "Audio" },
            new { Name = "USB-C Cable", Similarity = 0.15, Category = "Accessories" }
        };

        Console.WriteLine("Query: 'wireless headphones'");
        Console.WriteLine("\nTraditional search (top 3 most similar):");
        
        var traditionalResults = products
            .OrderByDescending(p => p.Similarity)
            .Take(3);
            
        foreach (var product in traditionalResults)
        {
            Console.WriteLine($"• {product.Name} (similarity: {product.Similarity})");
        }
        
        var uniqueCategories = traditionalResults.Select(p => p.Category).Distinct().Count();
        Console.WriteLine($"\nProblem: Only {uniqueCategories} category represented - missing accessories!");
    }
}