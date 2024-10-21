using AutoMapper;
using BusinessLayer.Abstract;
using DtoLayer.UserDtos;
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
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, ILogger<UsersController> logger, IMapper mapper)
        {
            _userService = userService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ResultUserDto>> UserList()
        {
            try
            {
                var users = _userService.TGetListAll();
                var userDtos = _mapper.Map<IEnumerable<ResultUserDto>>(users);
                _logger.LogInformation("Toplam {Count} kullanıcı başarıyla getirildi", users.Count);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı listesi getirilirken hata oluştu");
                return StatusCode(StatusCodes.Status500InternalServerError, "Kullanıcı listesi alınamadı");
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            try
            {
                var user = _userService.TGetbyID(id);
                if (user == null)
                    return NotFound($"{id} ID numarasına sahip kullanıcı bulunamadı");

                var userDto = _mapper.Map<ResultUserDto>(user);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı getirme hatası ID: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Kullanıcı bilgisi alınamadı");
            }
        }


        [HttpPost]
        public IActionResult CreateUser(CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = _mapper.Map<User>(createUserDto); // DTO'yu Entity'e dönüştürme
                _userService.TAdd(user);
                _logger.LogInformation("Yeni kullanıcı başarı ile oluşturuldu ID: {UserId}", user.Id);

                var userResultDto = _mapper.Map<ResultUserDto>(user);
                return CreatedAtAction(nameof(GetUser), new { id = userResultDto.Id }, userResultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı oluşturma hatası");
                return StatusCode(StatusCodes.Status500InternalServerError, "Kullanıcı oluşturulamadı");
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = _mapper.Map<User>(updateUserDto); // DTO'yu Entity'e dönüştürme
                _userService.TUpdate(user);
                _logger.LogInformation("Kullanıcı bilgileri başarı ile güncellendi ID: {UserId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncelleme hatası ID: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Kullanıcı bilgileri güncellenemedi");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                var user = _userService.TGetbyID(id);
                if (user == null)
                    return NotFound($"{id} ID numarasına sahip kullanıcı bulunamadı");

                _userService.TDelete(user);
                _logger.LogInformation("Kullanıcı sistemden başarıyla silindi ID: {UserId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silme hatası ID: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Kullanıcı silinemedi");
            }
        }
    }
}
