using System;
using Engine.Editor;
using Engine.GPU;
using Engine.Rendering;
using Engine.Resources;
using ReactiveUI;

namespace Engine.World
{
	[Icon('\uE9FE')]
	public partial class ModelNode : Node
	{
		[Inspect]
		public Model Model { get; set; } = null;

		[Inspect]
		public bool IsVisible { get; set; } = true;
	
		// Mesh instances
		public bool IsInstanceValid { get; private set; } = true;
		public bool IsTransformValid { get; private set; } = false;
		public BufferAllocation<GPUTransform> TransformHandle;
		public BufferAllocation<GPUInstance>[] InstanceHandles;

		// Material instances
		public MaterialInstance[] MaterialInstances { get; set; }
		
		public ModelNode(Scene scene) : base(scene)
		{
			// Track changes in model/visibility
			this.WhenAnyValue(o => o.Model, o => o.IsVisible)
				.Subscribe(o =>
				{
					IsInstanceValid = false;
				});

			// Track changes in display transform
			this.WhenAnyValue(o => o.Transform)
				.Subscribe(o => InvalidateTransformRecurse(this));
		}

		private void InvalidateTransformRecurse(Node root)
		{
			if (root is ModelNode model)
			{
				// Mark this node as needing it's GPU-side transform updated.
				model.IsTransformValid = false;
			}

			foreach (var node in root.Children)
			{
				InvalidateTransformRecurse(node);
			}
		}

		public override void Dispose()
		{
			TransformHandle?.Dispose();

			for (int i = 0; i < InstanceHandles?.Length; i++)
			{
				MaterialInstances[i].Dispose();

				// Zero out, then deallocate instance.
				Renderer.DefaultCommandList.UploadBuffer(InstanceHandles[i], default(GPUInstance));
				InstanceHandles[i].Dispose();
			}

			base.Dispose();
		}

		public void UpdateInstances(CommandList list)
		{
			// Get rid of existing instances.
			for (int i = 0; i < InstanceHandles?.Length; i++)
			{
				MaterialInstances[i].Dispose();

				// Zero out, then deallocate instance.
				Renderer.DefaultCommandList.UploadBuffer(InstanceHandles[i], default(GPUInstance));
				InstanceHandles[i].Dispose();
			}

			// If we don't have a fresh model to switch to, we're done here.
			if (Model == null || Model?.Parts == null || !Model.IsCommitted || !IsVisible || Scene == null)
			{
				InstanceHandles = null;
				MaterialInstances = null;
				return;
			}

			// Count instances.
			int instanceCount = Model.Parts.Sum(o => o.Meshes.Length);

			// (Re)build the array of instance handles.
			MaterialInstances = new MaterialInstance[instanceCount];
			InstanceHandles = new BufferAllocation<GPUInstance>[instanceCount];
			for (int i = 0; i < instanceCount; i++)
			{
				InstanceHandles[i] = Scene.InstanceBuffer.Allocate(1, true);
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
						MeshID = (int)mesh.MeshHandle.Offset,
						TransformID = (int)TransformHandle.Offset,
						MaterialID = (int)MaterialInstances[instanceID].MaterialHandle.Offset,
					};

					// Upload instance to buffer.
					list.UploadBuffer(InstanceHandles[instanceID], instanceData);
					instanceID++;
				}
			}

			IsInstanceValid = true;
		}

		public void UpdateTransform(CommandList list)
		{
			// Calculate transform.
			TransformHandle ??= Scene.TransformBuffer.Allocate(1);
			list.UploadBuffer(TransformHandle, new GPUTransform()
			{
				ObjectToWorld = Transform,
				WorldToObject = Transform.Inverse()
			});

			IsTransformValid = true;
		}

		public override void DrawGizmos(GizmosContext context)
		{
			if (Selection.Selected.Contains(this) && Model != null && IsVisible)
			{
				Box3D modelBounds = Box3D.Zero;
				foreach (var part in Model.Parts)
				{
					foreach (var mesh in part.Meshes)
					{
						modelBounds += mesh.Bounds;
					}
				}

				context.DrawBox(modelBounds, Color.White, Color.Invisible);
			}

			base.DrawGizmos(context);
		}
	}
}