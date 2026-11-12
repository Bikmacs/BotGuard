namespace BotGuard.AdminApi.Features.Blacklist.DTOs;

public record class AddBlackListDto(
    string IpAddress,
    double Minutes);