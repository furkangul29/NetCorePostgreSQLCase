using AutoMapper;
using BusinessLayer.Abstract;
using DtoLayer.CustomerDtos;
using EntityLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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
        public ActionResult<IEnumerable<ResultCustomerDto>> CustomerList()
        {
            try
            {
                var customers = _customerService.TGetListAll();
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
        public IActionResult GetCustomer(int id)
        {
            try
            {
                var customer = _customerService.TGetbyID(id);
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
        public IActionResult CreateCustomer(CreateCustomerDto createCustomerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var customer = _mapper.Map<Customer>(createCustomerDto); // DTO'yu Entity'e dönüştürme
                _customerService.TAdd(customer);
                _logger.LogInformation("Yeni müşteri başarı ile oluşturuldu ID: {CustomerId}", customer.Id);

                // Yeni müşteri oluştuğu için customer.Id burada veritabanı tarafından set ediliyor
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
        public IActionResult Update(int id, UpdateCustomerDto updateCustomerDto)
        {
            try
            {
                
                var existingCustomer = _customerService.TGetbyID(id);
                if (existingCustomer == null)
                    return NotFound("Girilen ID'ye uygun müşteri bulunamadı");

                // DTO'yu mevcut customer nesnesine map ediyoruz
                _mapper.Map(updateCustomerDto, existingCustomer); // Mevcut entity'yi güncelliyoruz

                _customerService.TUpdate(existingCustomer);
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
        public IActionResult DeleteCustomer(int id)
        {
            try
            {
                var customer = _customerService.TGetbyID(id);
                if (customer == null)
                    return NotFound($"{id} ID numarasına sahip müşteri bulunamadı");

                _customerService.TDelete(customer);
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
