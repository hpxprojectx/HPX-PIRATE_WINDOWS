using System;
using System.Collections.Generic;

namespace HPXPirate.Models;

public sealed class GameProfile
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");


	public string Game { get; set; } = "New Game";


	public string ExecutablePath { get; set; } = "";


	public string Resolution { get; set; } = "1920x1080";


	public int FpsTarget { get; set; } = 120;


	public OptimizationMode PerformanceMode { get; set; }

	public double MouseSensitivity { get; set; } = 1.0;


	public double DragSensitivity { get; set; } = 1.0;


	public MouseSettings Mouse { get; set; } = new MouseSettings();


	public DragAssistSettings DragAssist { get; set; } = new DragAssistSettings();


	public EasyDragSettings EasyDrag { get; set; } = new EasyDragSettings();


	public CrosshairSettings Crosshair { get; set; } = new CrosshairSettings();


	public List<ControlMapping> KeyboardMapping { get; set; } = new List<ControlMapping>();


	public List<ControlMapping> MouseMapping { get; set; } = new List<ControlMapping>();


	public DateTime Created { get; set; } = DateTime.Now;


	public DateTime Modified { get; set; } = DateTime.Now;


	public static GameProfile CreateDefault(string name)
	{
		return new GameProfile
		{
			Game = name,
			KeyboardMapping = new List<ControlMapping>
			{
				new ControlMapping
				{
					Input = "W",
					Action = "Move Forward",
					X = 0.3,
					Y = 0.35
				},
				new ControlMapping
				{
					Input = "A",
					Action = "Move Left",
					X = 0.2,
					Y = 0.5
				},
				new ControlMapping
				{
					Input = "S",
					Action = "Move Back",
					X = 0.3,
					Y = 0.65
				},
				new ControlMapping
				{
					Input = "D",
					Action = "Move Right",
					X = 0.4,
					Y = 0.5
				},
				new ControlMapping
				{
					Input = "SPACE",
					Action = "Jump",
					X = 0.8,
					Y = 0.7
				}
			},
			MouseMapping = new List<ControlMapping>
			{
				new ControlMapping
				{
					InputType = MappingInputType.MouseButton,
					Input = "MouseLeft",
					Action = "Fire",
					X = 0.7,
					Y = 0.35
				},
				new ControlMapping
				{
					InputType = MappingInputType.MouseButton,
					Input = "MouseRight",
					Action = "Aim",
					X = 0.8,
					Y = 0.35
				}
			}
		};
	}
}
