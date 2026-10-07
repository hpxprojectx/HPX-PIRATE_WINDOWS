namespace HPXPirate.Models;

public sealed class ProcessInfoModel
{
	public int Pid { get; set; }

	public string Name { get; set; } = "";


	public string Description { get; set; } = "";


	public double CpuPercent { get; set; }

	public double RamMb { get; set; }

	public bool IsGame { get; set; }

	public bool IsProtected { get; set; }

	public bool IsSafeToOptimize
	{
		get
		{
			if (!IsProtected)
			{
				return !IsGame;
			}
			return false;
		}
	}

	public string Priority { get; set; } = "Normal";

}
