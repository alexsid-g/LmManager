using LmManager.ViewModels;

namespace LmManager.Models;

public record ModelParameters(
    ServerViewModel Server,
    int? ContextSize,
    double? Temperature,
    ReasoningProfile? Reasoning);
