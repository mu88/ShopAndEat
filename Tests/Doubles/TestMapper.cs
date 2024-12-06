using AutoMapper;
using DTO;

namespace Tests.Doubles;

public static class TestMapper
{
    public static IMapper Create()
    {
        var mapperConfiguration = new MapperConfiguration(config => config.AddProfile<AutoMapperProfile>());
        var mapper = mapperConfiguration.CreateMapper();
        return mapper;
    }
}