using System.Collections.Generic;
using System.Threading.Tasks;
using Base.API.Data.Repositories;
using Base.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers
{
    [Route("api/v1/customers")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly CustomersRepository _customersRepository;

        public CustomerController(CustomersRepository customersRepository)
        {
            _customersRepository = customersRepository;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var customers = await _customersRepository.GetAllCustomersAsync();
            return Ok(customers);
        }

        [HttpGet("/api/v1/customer/{email}")]
        public async Task<ActionResult> Get(string email)
        {
            var customer = await _customersRepository.GetCustomerAsync(email);

            if (customer == null)
                return NotFound(new {message = "Cliente não encontrado"});

            return Ok(customer);
        }

        [HttpPost("/api/v1/customer/register")]
        public async Task<ActionResult<dynamic>> CreateCustomer([FromBody] Customer model)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();
            if(model.Name.Length < 3)
                errors.Add("nome", "Seu nome deve conter pelo menos 3 caracteres");
            if(model.Email.Length < 3)
                errors.Add("email", "E-mail inválido");
            if (errors.Count > 0)
                return BadRequest(new { error = errors });
            
            var response = await _customersRepository.CreateCustomerAsync(model.Name, model.Email);
            if (!response.Success)
                return BadRequest(new { error = response.ErrorMessage });
            return Ok(response.Customer);
        }

        [HttpPut("/api/vi/customer/update")]
        public async Task<ActionResult> UpdateCustomer(string email, [FromBody] Customer model)
        {
            var customer = await _customersRepository.GetCustomerAsync(email);

            if (customer == null)
                return NotFound(new {message = "Cliente não encontrado"});
            
            customer.UpdateCustomer(model.Name, model.Email, model.IsActive);

            await _customersRepository.UpdateCustomerAsync(email, customer);
            return Ok(customer);
        }
        
        [HttpDelete("/api/v1/customer/delete")]
        public async Task<ActionResult> Delete(string email)
        {
            var customer = await _customersRepository.GetCustomerAsync(email);

            if (customer == null)
                return NotFound(new {message = "Cliente não  encontrado"});

            return Ok(await _customersRepository.DeleteCustomerAsync(email));
        }
    }
}
