using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;
using DeviceManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagement.Api.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DeviceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DeviceResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var devices = await _deviceService.GetAllAsync(cancellationToken);
        return Ok(devices);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var device = await _deviceService.GetByIdAsync(id, cancellationToken);
        return Ok(device);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> CreateAsync(
        [FromBody] CreateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var createdDevice = await _deviceService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = createdDevice.Id },
            createdDevice);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> UpdateAsync(
        int id,
        [FromBody] UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var updatedDevice = await _deviceService.UpdateAsync(id, request, cancellationToken);
        return Ok(updatedDevice);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _deviceService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
