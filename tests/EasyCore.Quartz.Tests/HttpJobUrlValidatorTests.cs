using EasyCore.Quartz;
using EasyCore.Quartz.Jobs;

namespace EasyCoreQuartz.Tests;

public class HttpJobUrlValidatorTests
{
    [Theory]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://localhost/")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://10.0.0.1/")]
    [InlineData("http://192.168.1.1/")]
    [InlineData("file:///etc/passwd")]
    public void Blocks_Private_Or_Invalid_Urls(string url)
    {
        var options = new EasyCoreQuartzOptions();
        var error = HttpJobUrlValidator.Validate(url, options);
        Assert.NotNull(error);
    }

    [Fact]
    public void Allows_Public_Https_Url()
    {
        var options = new EasyCoreQuartzOptions();
        var error = HttpJobUrlValidator.Validate("https://example.com/api", options);
        Assert.Null(error);
    }

    [Fact]
    public void AllowedHosts_Bypasses_Private_Block()
    {
        var options = new EasyCoreQuartzOptions();
        options.HttpJobAllowedHosts.Add("localhost");
        options.HttpJobAllowedHosts.Add("127.0.0.1");

        Assert.Null(HttpJobUrlValidator.Validate("http://localhost/demo/ping", options));
        Assert.Null(HttpJobUrlValidator.Validate("http://127.0.0.1/demo/ping", options));
    }

    [Fact]
    public void Can_Disable_Private_Network_Blocking()
    {
        var options = new EasyCoreQuartzOptions { HttpJobBlockPrivateNetworks = false };
        Assert.Null(HttpJobUrlValidator.Validate("http://127.0.0.1/", options));
    }
}
