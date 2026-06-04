using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MP.GameEngine.Abstractions;
using MP.GameEngine.Services;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Extensions;

public static class ServiceRegistration
{
    public static IServiceCollection AddGameEngine(this IServiceCollection services)
    {
        //NOTE: Framework services are not DI, they are modelled on the GameEngine class
        
        //SubSystem Services
        services.TryAddScoped<BoardService>();
        services.TryAddScoped<DiceService>();
        services.TryAddScoped<MovementService>();
        services.TryAddScoped<PlayerService>();
        services.TryAddScoped<PropertyService>();
        services.TryAddScoped<GoService>();
        services.TryAddScoped<JailService>();
        services.TryAddScoped<FreeParkingService>();
        services.TryAddScoped<AuctionService>();
        services.TryAddScoped<LoanService>();
        
        //Main Services and Orchestrators
        services.TryAddScoped<GameEngineSetupService>();
        services.TryAddScoped<PlayerTurnOrchestrator>();
        services.TryAddScoped<TransactionService>();
        services.TryAddScoped<IShortfallService, ShortfallService>();

        return services;
    }
}