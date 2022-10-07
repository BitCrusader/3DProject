﻿using System;
using Engine.GPU;
using Engine.GPU.Native;
using Engine.Rendering;
using Engine.Resources;

namespace Engine.World
{
	[Icon('\uE9FE')]
	public partial class ModelNode : Node
	{
		[Inspect] public Model Model { get; set; } = null;
		[Inspect] public bool IsVisible { get; set; } = true;

		/*[Inspect] */public Material[] Materials { get; set; } = null;
		/*[Inspect] */public List<Shader> Layers { get; set; } = new();
	
		// Mesh instances
		public bool IsInstanceDirty = true;
		public BufferAllocation<GPUInstance>[] InstanceHandles;

		// Material instances
		public MaterialInstance[] MaterialInstances { get; set; }
		
		public ModelNode()
		{
			(this as INotify).Subscribe(nameof(Model), () =>
			{
				IsInstanceDirty = true;
				Materials = Model.Parts.SelectMany(o => o.Meshes).Select(o => o.Material).ToArray();
			});

			(this as INotify).Subscribe(nameof(IsVisible), () => IsInstanceDirty = true);
		}

		public override void Dispose()
		{
			for (int i = 0; i < InstanceHandles?.Length; i++)
			{
				MaterialInstances[i].Dispose();
				InstanceHandles[i].Dispose();

				if (Scene != null)
				{
					Scene.InstanceCount--;
				}
			}

			base.Dispose();
		}

		public void UpdateInstances()
		{
			// Get rid of existing instances.
			if (Model == null || Model?.Parts == null || !Model.IsCommitted || !IsVisible || Scene == null)
			{
				for (int i = 0; i < InstanceHandles?.Length; i++)
				{
					MaterialInstances[i].Dispose();
					InstanceHandles[i].Dispose();

					if (Scene != null)
					{
						Scene.InstanceCount--;
					}
				}

				InstanceHandles = null;
				MaterialInstances = null;
				return;
			}

			// Count instances.
			uint instanceCount = 0;
			foreach (ModelPart part in Model.Parts)
			{
				foreach (Mesh mesh in part.Meshes)
				{
					instanceCount++;
				}
			}

			// (Re)build the array of instance handles.
			if (InstanceHandles == null || InstanceHandles.Length != instanceCount)
			{
				// Allocate a handful of new ones.
				MaterialInstances = new MaterialInstance[instanceCount];
				InstanceHandles = new BufferAllocation<GPUInstance>[instanceCount];
				for (int i = 0; i < instanceCount; i++)
				{
					InstanceHandles[i] = Scene.InstanceBuffer.Allocate(1);
					Scene.InstanceCount++;
				}
			}

			// A Model can contain multiple ModelParts, which in turn may contain multiple submeshes. Every submesh needs it's own instance.
			int instanceID = 0;
			foreach (ModelPart part in Model.Parts)
			{
				foreach (Mesh mesh in part.Meshes)
				{
					// Create material instance.
					MaterialInstances[instanceID] = new MaterialInstance(mesh.Material);

					// Make instance data.
					GPUInstance instanceData = new()
					{
						MeshID = (uint)mesh.MeshHandle.Start,
						TransformID = (uint)TransformHandle.Start,
						MaterialID = (uint)MaterialInstances[instanceID].MaterialHandle.Start,
					};

					// Upload instance to buffer.
					Renderer.DefaultCommandList.UploadBuffer(InstanceHandles[instanceID], instanceData);
					instanceID++;
				}
			}

			IsInstanceDirty = false;
		}
	}
}