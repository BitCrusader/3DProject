using System;

namespace Engine.World
{
	[Icon('\uEA24')]
	public class PointLightNode : Node
	{
		/// <summary>
		/// The light's intensity in lumens
		/// </summary>
		[Inspect] public float Intensity { get; set; } = 100;

		/// <summary>
		/// The light's radius in meters
		/// </summary>
		[Inspect] public float Radius { get; set; } = 0.2f;

		public PointLightNode(Scene scene) : base(scene)
		{

		}
	}
}