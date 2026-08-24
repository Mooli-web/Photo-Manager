namespace PhotoManager.Application.Models;

public sealed record PhotoQuery(string Search = "", string Tag = "", int MinimumRating = 0, int Offset = 0, int Limit = 200);
