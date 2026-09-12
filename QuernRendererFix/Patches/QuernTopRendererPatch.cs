using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace QuernRendererFix.Patches;

[HarmonyPatch]
public static class QuernRendererFixPatch
{
    private static readonly Dictionary<QuernTopRenderer, MultiTextureMeshRef> Meshes = new();

    [HarmonyPatch(typeof(QuernTopRenderer), MethodType.Constructor,
        new[]
        {
            typeof(ICoreClientAPI),
            typeof(BlockPos),
            typeof(MeshData)
        }
    )]
    [HarmonyPostfix]
    private static void ConstructorPostfix(QuernTopRenderer __instance, ICoreClientAPI ___api, MeshData mesh)
    {
        if (mesh == null)
        {
            return;
        }

        MultiTextureMeshRef multiMesh = ___api.Render.UploadMultiTextureMesh(mesh);

        Meshes[__instance] = multiMesh;
    }

    [HarmonyPatch(typeof(QuernTopRenderer),nameof(QuernTopRenderer.OnRenderFrame))]
    [HarmonyPrefix]
    private static bool OnRenderFrame(
        QuernTopRenderer __instance,
        float deltaTime,
        ICoreClientAPI ___api,
        BlockPos ___pos,
        bool ___ShouldRender,
        bool ___ShouldRotateManual,
        bool ___ShouldRotateAutomated)
    {
        if (!___ShouldRender) return false;

        IRenderAPI render = ___api.Render;
        Vec3d camPos = ___api.World.Player.Entity.CameraPos;

        render.GlDisableCullFace();
        render.GlToggleBlend(true);

        IStandardShaderProgram shader = render.PreparedStandardShader(___pos.X,___pos.Y,___pos.Z);

        shader.ModelMatrix = __instance.ModelMat
            .Identity()
            .Translate(___pos.X - camPos.X,___pos.Y - camPos.Y,___pos.Z - camPos.Z)
            .Translate(0.5f, 11f / 16f, 0.5f)
            .RotateY(__instance.AngleRad)
            .Translate(-0.5f, 0, -0.5f)
            .Values;

        shader.ViewMatrix = render.CameraMatrixOriginf;
        shader.ProjectionMatrix = render.CurrentProjectionMatrix;

        render.RenderMultiTextureMesh(Meshes[__instance],"tex");

        shader.Stop();

        if (___ShouldRotateManual)
        {
            __instance.AngleRad += deltaTime * 40 * GameMath.DEG2RAD;
        }

        if (___ShouldRotateAutomated)
        {
            __instance.AngleRad = __instance.mechPowerPart.AngleRad;
        }

        return false;
    }

    [HarmonyPatch(typeof(QuernTopRenderer),nameof(QuernTopRenderer.Dispose))]
    [HarmonyPostfix]
    private static void DisposePostfix(QuernTopRenderer __instance)
    {
        if (!Meshes.Remove(__instance, out MultiTextureMeshRef mesh))
        {
            return;
        }

        mesh.Dispose();
    }
}