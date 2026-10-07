using System;

namespace HPXPirate.Models;

public sealed class ControlMapping
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");


	public MappingInputType InputType { get; set; }

	public string Input { get; set; } = "W";


	public string Action { get; set; } = "Action";


	public double X { get; set; } = 0.5;


	public double Y { get; set; } = 0.5;


	public double Size { get; set; } = 60.0;


	public bool Enabled { get; set; } = true;


	public string Color { get; set; } = "#FF2BD6";

}
