using System;
using System.IO;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using SPT.Reflection.Patching;
using ChouUn.Iof.Reflection;
using static ChouUn.Iof.UI.UserInterfaceElements;

namespace ChouUn.Iof.Patches
{
    internal class PostMenuScreenInit : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuScreen), "Init");
        }

        [PatchPostfix]
        private static void PatchPostfix(ref DefaultUIButton ____hideoutButton)
        {
            try
            {
                if (OrganizeSprite != null) return;
                OrganizeSprite = ____hideoutButton.GetFieldValue<Sprite>("_iconSprite");
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
        }

        private static Sprite LoadSprite(string path)
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}