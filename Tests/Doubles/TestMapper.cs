using AutoMapper;
using DTO;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Doubles;

public static class TestMapper
{
    public static IMapper Create()
    {
        var mapperConfiguration = new MapperConfiguration(config => config.AddProfile<AutoMapperProfile>(), new NullLoggerFactory());
        var mapper = mapperConfiguration.CreateMapper();
        return mapper;
    }
}