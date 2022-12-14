using System;

namespace Engine.Resources
{
	/// <summary>
	/// Describes a loader used when an unloaded asset is requested
	/// </summary>
	public abstract class ResourceLoader {}

	/// <inheritdoc/>
	public abstract class ResourceLoader<T> : ResourceLoader where T : Resource
	{
		public abstract Task<T> Load();
		public virtual void Unload() {}
	}
}