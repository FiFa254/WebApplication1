using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class AddStockWatchItemRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
}
