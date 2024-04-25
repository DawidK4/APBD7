using System.ComponentModel.DataAnnotations;

namespace APBD7.Models;

public class Order
{
    [Required]
    public int IdOrder { get; set; }
    [Required]
    public int IdProduct { get; set; }
    [Required]
    public int Ammount { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public DateTime FulfilledAt { get; set; }
}