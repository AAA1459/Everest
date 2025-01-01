using Celeste;
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

        [MonoModIgnore]
        [ForceNoInlining]
        public new extern static void ClearFramesMetadata();
#pragma warning restore CS0108

        private static void fillAnimForID(string id) {
            List<string> ids = Everest.Events.PlayerSprite.GetIdsUsedFillAnimFor(id);
            if (ids.Count == 0)
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

            //Explicitly instantiate the generic HashSet<T> type
            GenericInstanceType genericInst = (GenericInstanceType) context.Module.ImportReference(f_DoNotFillAnimFor.FieldType);

            MethodReference m_temp = context.Module.ImportReference(genericInst.Resolve().FindMethod("Contains"));
            MethodReference m_HashSet_string_Contains = context.Module.ImportReference(new MethodReference(m_temp.Name, m_temp.ReturnType, genericInst) {
                HasThis = m_temp.HasThis,
                ExplicitThis = m_temp.ExplicitThis,
                CallingConvention = m_temp.CallingConvention,
            });
            m_HashSet_string_Contains.Parameters.AddRange(m_temp.Parameters);

            ILCursor cursor = new ILCursor(context);

            // if (DoNotFillAnimFor.Contains(id)) {
            ILLabel If = cursor.DefineLabel();
            cursor.EmitLdsfld(f_DoNotFillAnimFor);
            cursor.EmitLdarg0();
            cursor.EmitCallvirt(m_HashSet_string_Contains);
            cursor.EmitBrtrue(If);

            //   fillAnimForID(id);
            cursor.EmitLdarg0();
            cursor.EmitCall(m_PlayerSprite_fillAnimForID);
            // }
            cursor.EmitBr(If);
            cursor.MarkLabel(If);
        }
    }
}