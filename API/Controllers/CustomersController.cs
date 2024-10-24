using AutoMapper;
using BusinessLayer.Abstract;
using DtoLayer.CustomerDtos;
using EntityLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;
        private readonly IMapper _mapper;

        public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger, IMapper mapper)
        {
            _customerService = customerService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResultCustomerDto>>> CustomerList()
        {
            try
            {
                var customers = await _customerService.TGetListAllAsync();
                var customerDtos = _mapper.Map<IEnumerable<ResultCustomerDto>>(customers);
                _logger.LogInformation("Toplam {Count} müşteri başarıyla getirildi", customers.Count);
                return Ok(customerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri listesi getirilirken hata oluştu");
                return StatusCode(StatusCodes.Status500InternalServerError, "Müşteri listesi alınamadı");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            try
            {
                var customer = await _customerService.TGetByIDAsync(id);
                if (customer == null)
                    return NotFound($"{id} ID numarasına sahip müşteri bulunamadı");

                var customerDto = _mapper.Map<ResultCustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri getirme hatası ID: {CustomerId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Müşteri bilgisi alınamadı");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(CreateCustomerDto createCustomerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var customer = _mapper.Map<Customer>(createCustomerDto);
                await _customerService.TAddAsync(customer);
                _logger.LogInformation("Yeni müşteri başarı ile oluşturuldu ID: {CustomerId}", customer.Id);

                var customerResultDto = _mapper.Map<ResultCustomerDto>(customer);
                return CreatedAtAction(nameof(GetCustomer), new { id = customerResultDto.Id }, customerResultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri oluşturma hatası");
                return StatusCode(StatusCodes.Status500InternalServerError, "Müşteri oluşturulamadı");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id,UpdateCustomerDto updateCustomerDto)
        {
            try
            {
                var existingCustomer = await _customerService.TGetByIDAsync(id);
                if (existingCustomer == null)
                    return NotFound("Girilen ID'ye uygun müşteri bulunamadı");

                _mapper.Map(updateCustomerDto, existingCustomer);
                await _customerService.TUpdateAsync(existingCustomer);
                _logger.LogInformation("Müşteri bilgileri başarı ile güncellendi ID: {CustomerId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri güncelleme hatası ID: {CustomerId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Müşteri bilgileri güncellenemedi");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _customerService.TGetByIDAsync(id);
                if (customer == null)
                    return NotFound($"{id} ID numarasına sahip müşteri bulunamadı");

                await _customerService.TDeleteAsync(customer);
                _logger.LogInformation("Müşteri sistemden başarıyla silindi ID: {CustomerId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri silme hatası ID: {CustomerId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Müşteri silinemedi");
            }
        }
    }
}
