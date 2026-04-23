using GestionaleRistorante.Contracts;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[Route("api/v1/tables")]
public sealed class TablesController(ITableService tableService) : ApiControllerBase
{
    [Authorize(Roles = "1,2,3,99")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TableResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tables = await tableService.GetAllAsync(cancellationToken);
        return Success(tables.Select(ToResponse).ToList());
    }

    [Authorize(Roles = "3,99")]
    [HttpPost]
    [ProducesResponseType(typeof(TableResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTableRequest request, CancellationToken cancellationToken)
    {
        var table = new RestaurantTable
        {
            Name = request.Name,
            Capacity = request.Capacity,
            Status = request.Status
        };

        var created = await tableService.CreateAsync(table, cancellationToken);
        return Success(ToResponse(created), StatusCodes.Status201Created);
    }

    [Authorize(Roles = "3,99")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TableResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTableRequest request, CancellationToken cancellationToken)
    {
        var table = new RestaurantTable
        {
            Name = request.Name,
            Capacity = request.Capacity,
            Status = request.Status
        };

        var updated = await tableService.UpdateAsync(id, table, cancellationToken);
        return Success(ToResponse(updated));
    }

    [Authorize(Roles = "3,99")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await tableService.DeleteAsync(id, cancellationToken);
        return SuccessNoData();
    }

    private static TableResponse ToResponse(RestaurantTable table)
    {
        return new TableResponse
        {
            Id = table.Id,
            Name = table.Name,
            Capacity = table.Capacity,
            Status = table.Status
        };
    }
}
