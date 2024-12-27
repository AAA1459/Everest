using Celeste.Mod;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.InlineRT;
using System;
using System.Collections.Generic;
using MonoMod.Utils;
using System.Reflection.Emit;

namespace Celeste {
    class patch_PlayerSprite : PlayerSprite {
        public patch_PlayerSprite(PlayerSpriteMode mode) : base(mode) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

#pragma warning disable CS0108 // Hides inherited member
        [MonoModIgnore]
        [PatchPlayerSpriteCreateFramesMetadata]
        public static void CreateFramesMetadata(string id) {
        }
#pragma warning restore CS0108

        private static void fillAnimForID(string id) {
            List<string> ids = Everest.Events.PlayerSprite.GetIdsUsedFillAnimFor(id);
            if (ids == null)
                return;
            Dictionary<string, patch_Sprite.Animation> existingAnim = (GFX.SpriteBank.SpriteData[id].Sprite as patch_Sprite).Animations;
            foreach (string id2 in ids) {
                foreach (KeyValuePair<string, patch_Sprite.Animation> anim in (GFX.SpriteBank.SpriteData[id2].Sprite as patch_Sprite).Animations) {
                    if (!existingAnim.ContainsKey(anim.Key)) {
                        existingAnim[anim.Key] = anim.Value;
                    }
                }
            }
        }
    }
}

namespace MonoMod {
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchPlayerSpriteCreateFramesMetadata))]
    class PatchPlayerSpriteCreateFramesMetadata : Attribute {
    }

    static partial class MonoModRules {
        public static void PatchPlayerSpriteCreateFramesMetadata(ILContext context, CustomAttribute attrib) {
            MethodReference m_PlayerSprite_fillAnimForID = context.Method.DeclaringType.FindMethod("fillAnimForID");

            FieldReference f_DoNotFillAnimFor = MonoModRule.Modder.Module.GetType("Celeste.Mod.Everest/Events/PlayerSprite").FindField("DoNotFillAnimFor");

            MethodReference m_PlayerSprite_doNotFillAnimContains = context.Import(typeof(HashSet<string>).GetMethod("Contains"));

            ILCursor cursor = new ILCursor(context);

            // if (!Everest.Events.PlayerSprite.DoNotFillAnimFor.Contains(id)) {
            ILLabel If = cursor.DefineLabel();
            ILLabel done = cursor.DefineLabel();

            cursor.EmitLdsfld(f_DoNotFillAnimFor);
            cursor.EmitLdarg0();
            cursor.EmitCall(m_PlayerSprite_doNotFillAnimContains);
            cursor.EmitBrtrue(If);

            //   fillAnimForID(id);
            cursor.EmitLdarg0();
            cursor.EmitCall(m_PlayerSprite_fillAnimForID);
            // }
            cursor.EmitBr(done);
            cursor.MarkLabel(If);
            cursor.MarkLabel(done);
        }
    }
}
