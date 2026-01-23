using Microsoft.AspNetCore.Mvc;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Shared.Contracts.Host;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers.Select(MapToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound();
        return Ok(MapToDto(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            CustomerNumber = request.CustomerNumber,
            Name = request.Name,
            Address = request.Address,
            PostalCode = request.PostalCode,
            City = request.City,
            Country = request.Country,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes
        };

        var created = await _customerService.CreateAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound();

        customer.CustomerNumber = request.CustomerNumber;
        customer.Name = request.Name;
        customer.Address = request.Address;
        customer.PostalCode = request.PostalCode;
        customer.City = request.City;
        customer.Country = request.Country;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Notes = request.Notes;

        await _customerService.UpdateAsync(customer);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return NoContent();
    }

    private CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            CustomerNumber = customer.CustomerNumber,
            Name = customer.Name,
            Address = customer.Address,
            PostalCode = customer.PostalCode,
            City = customer.City,
            Country = customer.Country,
            Email = customer.Email,
            Phone = customer.Phone,
            Notes = customer.Notes
        };
    }
}
