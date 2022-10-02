﻿using System;
using Engine.Frontend;
using Engine.GPU;
using Engine.World;
using Vortice.Direct3D12;

namespace Engine.Rendering
{
	public static class Renderer
	{
		/// <summary>
		/// Command list used for any command that needs to be done before the renderer does it's thing.
		/// </summary>
		public static CommandList DefaultCommandList { get; private set; } = new CommandList();

		private static List<RenderStep> globalStage= new();
		private static List<RenderStep> sceneStage= new();
		private static List<RenderStep> cameraStage= new();

		public static void AddStep(RenderStep step, RenderStage stage)
		{
			switch (stage)
			{
				case RenderStage.Global:
					Debug.Assert(!globalStage.Any(o => o.GetType() == step.GetType()), "Cannot add multiple render steps of the same type.");
					step.List = DefaultCommandList;
					step.Viewport = null;
					step.Scene = null;

					globalStage.Add(step);
					break;
				case RenderStage.Scene:
					Debug.Assert(!sceneStage.Any(o => o.GetType() == step.GetType()), "Cannot add multiple render steps of the same type.");
					step.List = DefaultCommandList;
					step.Viewport = null;

					sceneStage.Add(step);
					break;
				case RenderStage.Camera:
					Debug.Assert(!cameraStage.Any(o => o.GetType() == step.GetType()), "Cannot add multiple render steps of the same type.");

					cameraStage.Add(step);
					break;
			}

			step.Init();
		}

		public static T GetStep<T>() where T : RenderStep
		{
			foreach (var step in cameraStage)
			{
				if (step is T)
					return step as T;
			}
			foreach (var step in sceneStage)
			{
				if (step is T)
					return step as T;
			}
			foreach (var step in globalStage)
			{
				if (step is T)
					return step as T;
			}

			return null;
		}

		public static void Init()
		{
			GPUContext.Init(2);
			DefaultCommandList.Name = "Default List";

			AddStep(new SceneUpdateStep(), RenderStage.Scene);
			AddStep(new SkinningStep(), RenderStage.Scene);
			AddStep(new PrepassStep(), RenderStage.Camera);
			AddStep(new MaterialStep(), RenderStage.Camera);
			AddStep(new GizmosStep(), RenderStage.Camera);
			AddStep(new ResolveStep(), RenderStage.Camera);
		}

		public static void Render()
		{
			// Execute global render steps.
			foreach (RenderStep step in globalStage)
			{
				step.List.PushEvent($"{step.GetType().Name} (global)");
				step.Run();
				step.List.PopEvent();
			}

			// Loop through scenes.
			foreach (Scene scene in Scene.All)
			{
				// Execute per-scene render steps.
				foreach (RenderStep step in sceneStage)
				{
					step.Scene = scene;

					step.List.PushEvent($"{step.GetType().Name} (scene)");
					step.Run();
					step.List.PopEvent();
				}
			}

			// Execute default command list and wait for it on the GPU.
			DefaultCommandList.Execute();

			// Build and execute per-viewport commands. Consider doing this in parallel.
			foreach (var viewport in Viewport.All)
			{
				foreach (RenderStep step in cameraStage)
				{
					step.List = viewport.CommandList;
					step.Viewport = viewport;
					step.Scene = step.Viewport.Scene;

					step.List.PushEvent($"{step.GetType().Name} (camera)");
					step.Run();
					step.List.PopEvent();
				}

				// Make sure the viewport's backbuffer is in the right state for presentation.
				viewport.CommandList.RequestState(viewport.Host.Swapchain.RT, ResourceStates.Present);

				// Make sure this viewport's commands are executing while we submit the next.
				viewport.CommandList.Execute();
			}

			// Present swapchains.
			Viewport.All.ForEach(o => o.Host.Swapchain.Present());

			// Wait for completion.
			Graphics.WaitFrame();
		}

		public static void Cleanup()
		{
			Graphics.Flush();
		}
	}
}
