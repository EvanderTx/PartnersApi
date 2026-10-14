using Microsoft.AspNetCore.Mvc;
using Partners.Application.Common;
using Partners.Application.Partners;
using Partners.Application.Partners.Dtos;

namespace Partners.Api.Controllers;

[ApiController]
[Route("api/partners")]
[Produces("application/json")]
public sealed class PartnersController(PartnerService partnerService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PartnerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PartnerResponse>> Create(
        CreatePartnerRequest request, CancellationToken cancellationToken)
    {
        var partner = await partnerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = partner.Id }, partner);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PartnerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartnerResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await partnerService.GetByIdAsync(id, cancellationToken));

    [HttpGet]
    [ProducesResponseType<PagedResult<PartnerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PartnerResponse>>> List(
        [FromQuery] PartnerListQuery query, CancellationToken cancellationToken) =>
        Ok(await partnerService.ListAsync(query, cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<PartnerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PartnerResponse>> Update(
        Guid id, UpdatePartnerRequest request, CancellationToken cancellationToken) =>
        Ok(await partnerService.UpdateAsync(id, request, cancellationToken));
}
