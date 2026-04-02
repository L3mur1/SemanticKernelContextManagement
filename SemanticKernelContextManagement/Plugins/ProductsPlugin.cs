using Microsoft.SemanticKernel;
using SemanticKernelContextManagement.Models;
using System.ComponentModel;
using System.Text.Json;

namespace SemanticKernelContextManagement.Plugins
{
    public class ProductsPlugin
    {
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
        private readonly List<Product> products;

        public ProductsPlugin()
        {
            products = LoadProducts();
        }

        [KernelFunction]
        [Description("Get full detailed description of a product by its name.")]
        public string GetProductDetails([Description("Product name, e.g. Laptop Pro 14")] string productName)
        {
            var product = products.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));
            if (product == null)
            {
                return $"Product not found: {productName}";
            }

            return JsonSerializer.Serialize(product, jsonSerializerOptions);
        }

        [KernelFunction]
        [Description("Get all products from shop. Returns only product names and short summaries.")]
        public string GetProducts()
        {
            var productSummaries = products.Select(p => new
            {
                p.Name,
                p.ShortSummary,
            });

            return JsonSerializer.Serialize(productSummaries, jsonSerializerOptions);
        }

        private static List<Product> LoadProducts()
        {
            var productsDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Products");
            if (!Directory.Exists(productsDir))
            {
                throw new InvalidOperationException($"Products directory not found: {productsDir}");
            }

            var files = Directory.GetFiles(productsDir, "*.json");
            var products = new List<Product>();
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);

                var product = JsonSerializer.Deserialize<Product>(json)
                    ?? throw new InvalidOperationException($"Failed to deserialize product from file: {file}");

                products.Add(product);
            }

            return products;
        }
    }
}