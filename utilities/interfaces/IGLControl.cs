using System;

namespace utilities
{
	public interface IGLControl
	{
		System.Drawing.Point Location { get; }
		int Width { get; }
		int Height { get; }
	}	
}
