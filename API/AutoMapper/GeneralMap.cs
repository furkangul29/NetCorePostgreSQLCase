using AutoMapper;
using DtoLayer.CustomerDtos;
using DtoLayer.UserDtos;
using EntityLayer;

public class GeneralMap : Profile
{
    public GeneralMap()
    {
        //Customer sınıfı icin
        CreateMap<Customer, ResultCustomerDto>();
 
        CreateMap<CreateCustomerDto, Customer>();

        CreateMap<UpdateCustomerDto, Customer>();
        CreateMap<Customer, UpdateCustomerDto>(); // Burayı ekledik

        //User Sınıfı icin
        CreateMap<User, ResultUserDto>();

        CreateMap<CreateUserDto, User>();

        CreateMap<UpdateUserDto, User>();
     


    }
}
