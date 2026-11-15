namespace LmManager.Models;
using LmManager.ViewModels;

public record ModelParameters(
    ServerViewModel Server,
    int? ContextSize,
    double? Temperature,
    ReasoningProfile? Reasoning);
