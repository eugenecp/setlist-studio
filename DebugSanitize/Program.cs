using System;
using System.Threading.Tasks;
using SetlistStudio.Core.Validation;
using SetlistStudio.Web.Controllers;

namespace DebugApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var controller = new ArtistsController();

            // Test SearchArtists response format for "The"
            var searchResult = await controller.SearchArtists("The");
            Console.WriteLine($"SearchArtists result for 'The': {searchResult.GetType()}");
            
            if (searchResult is Microsoft.AspNetCore.Mvc.OkObjectResult okResult)
            {
                var value = okResult.Value;
                if (value != null)
                {
                    var type = value.GetType();
                    var properties = type.GetProperties();
                    foreach (var prop in properties)
                    {
                        var propValue = prop.GetValue(value);
                        Console.WriteLine($"Property: {prop.Name} = {propValue}");
                        
                        if (prop.Name == "artists" && propValue is System.Collections.IEnumerable enumerable)
                        {
                            Console.WriteLine("Artists found:");
                            foreach (var artist in enumerable)
                            {
                                Console.WriteLine($"  - {artist}");
                            }
                        }
                    }
                }
            }
        }
    }
}