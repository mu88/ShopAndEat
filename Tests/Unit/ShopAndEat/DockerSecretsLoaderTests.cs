using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using ShopAndEat;

namespace Tests.Unit.ShopAndEat;

[TestFixture]
[Category("Unit")]
public class DockerSecretsLoaderTests
{
    [Test]
    public void ApplyLlmApiKeySecret_ShouldSetConfigValue_WhenSecretFileExists()
    {
        // Arrange
        var secretFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        File.WriteAllText(secretFilePath, "my-secret-api-key\n");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        try
        {
            // Act
            DockerSecretsLoader.ApplyLlmApiKeySecret(configuration, secretFilePath);

            // Assert
            configuration["LlmClient:ApiKey"].Should().Be("my-secret-api-key");
        }
        finally
        {
            File.Delete(secretFilePath);
        }
    }

    [Test]
    public void ApplyLlmApiKeySecret_ShouldNotSetConfigValue_WhenSecretFileDoesNotExist()
    {
        // Arrange
        var secretFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        File.Exists(secretFilePath).Should().BeFalse();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        // Act
        DockerSecretsLoader.ApplyLlmApiKeySecret(configuration, secretFilePath);

        // Assert
        configuration["LlmClient:ApiKey"].Should().BeNull();
    }
}
