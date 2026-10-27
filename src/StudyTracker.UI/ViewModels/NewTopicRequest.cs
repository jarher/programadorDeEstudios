namespace StudyTracker.UI.ViewModels;

/// <summary>
/// Datos validados que el diálogo de alta entrega al dashboard para persistir una temática.
/// </summary>
public sealed record NewTopicRequest(string Title, string? Description, double TargetHours, string ColorHex);
