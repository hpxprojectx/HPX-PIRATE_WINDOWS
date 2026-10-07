namespace HPXPirate.Services;

public readonly record struct DragSample(double RawDx, double RawDy, double OutDx, double OutDy, double RawMagnitude, double OutMagnitude, double Speed, double StabilityScore, double SmoothnessScore);
