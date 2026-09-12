using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace QuernRendererFix
{
    public class QuernRendererFixModSystem : ModSystem
    {
        private Harmony? harmony;
        public override void StartClientSide(ICoreClientAPI api)
        {
            harmony = new Harmony("colinswrath.quernrendererfix");
            harmony.PatchAll();
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("colinswrath.querntexturefix");
            harmony = null;

            base.Dispose();
        }
    }
}
