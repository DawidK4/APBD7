using APBD7.Models;
using APBD7.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace APBD7.Controllers;

[ApiController]
[Route("api/warehouse")]
public class WarehouseController : ControllerBase
{
    private readonly DbService _dbService;

    public WarehouseController(DbService dbService)
    {
        _dbService = dbService;
    }
    
    [HttpPost("addProductToWarehouse")]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] WarehouseRequest warehouseRequest)
    {
        var (rowNumber, errorMessage) = await _dbService.CheckDataValidity(warehouseRequest.IdProduct, warehouseRequest.IdWarehouse, warehouseRequest.Amount);

        if (rowNumber != -1)
        {
            return Ok($"Data is correct. Row number: {rowNumber}");
        }
        else
        {
            return BadRequest(errorMessage);
        }
    }
}