using PersonalisationEngine.Api.DTOs.Recommendations;
using PersonalisationEngine.Api.Models;

namespace PersonalisationEngine.Api.Services.Rules;

public interface IRulesEngine
{
    RulesResult Evaluate(Player player);
}
