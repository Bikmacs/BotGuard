using BotGuard.Gateway.Services;

namespace BotGuard.Tests;

public class UserAgentInspectorTests
{
    [Theory]
    [InlineData("curl/7.68", true)]            
    [InlineData("Mozilla/5.0", false)]        
    [InlineData("python-requests/2.31", true)] 
    [InlineData("", true)]                     
    [InlineData("Googlebot/2.1", false)]      
    [InlineData("YandexBot/3.0", false)]       

    public void IsSuspicious_ReturnsExpectedResult(string userAgent, bool expected)
    {
        var ua = new UserAgentInspector();
        Assert.Equal(expected, ua.IsSuspicious(userAgent, CancellationToken.None));
    }
}