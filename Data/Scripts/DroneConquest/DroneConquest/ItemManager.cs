using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRage.ObjectBuilders;
using IMyInventory = Sandbox.ModAPI.IMyInventory;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;

namespace DroneConquest
{
    class ItemManager
    {

        private static SerializableDefinitionId _gatlingAmmo = new SerializableDefinitionId(new MyObjectBuilderType(new MyObjectBuilder_AmmoMagazine().GetType()), "NATO_25x184mm");
        private static SerializableDefinitionId _launcherAmmo = new SerializableDefinitionId(new MyObjectBuilderType(new MyObjectBuilder_AmmoMagazine().GetType()), "Missile200mm");
        private static SerializableDefinitionId _uraniumFuel = new SerializableDefinitionId(new MyObjectBuilderType(new MyObjectBuilder_Ingot().GetType()), "Uranium");


        public static void Reload(List<Sandbox.ModAPI.IMySlimBlock> guns)
        {
            for (int i = 0; i < guns.Count; i++)
            {
                if (IsAGun((IMyUserControllableGun)guns[i].FatBlock))
                    Reload((IMyInventoryOwner)guns[i].FatBlock, _gatlingAmmo);
                else
                    Reload((IMyInventoryOwner)guns[i].FatBlock, _launcherAmmo);
            }
        }

        private static void Reload(IMyInventoryOwner gun, SerializableDefinitionId ammo, bool reactor = false)
        {
            var cGun = gun;
            Sandbox.ModAPI.IMyInventory inv = (Sandbox.ModAPI.IMyInventory)cGun.GetInventory(0);
            VRage.MyFixedPoint point = inv.GetItemAmount(ammo, MyItemFlags.None | MyItemFlags.Damaged);
            Util.GetInstance().Log(ammo.SubtypeName + " [ReloadGuns] Amount " + point.RawValue, "ItemManager.txt");
            if (point.RawValue > 1000000)
                return;
            //inv.Clear();
            VRage.MyFixedPoint amount = new VRage.MyFixedPoint();
            amount.RawValue = 2000000;

            MyObjectBuilder_InventoryItem ii;
            if (reactor)
            {
                ii = new MyObjectBuilder_InventoryItem()
                {
                    Amount = 10,
                    Content = new MyObjectBuilder_Ingot() { SubtypeName = ammo.SubtypeName }
                };
            }
            else
            {
                ii = new MyObjectBuilder_InventoryItem()
                {
                    Amount = 4,
                    Content = new MyObjectBuilder_AmmoMagazine() { SubtypeName = ammo.SubtypeName }
                };
            }
            inv.AddItems(amount, ii.PhysicalContent);

            point = inv.GetItemAmount(ammo, MyItemFlags.None | MyItemFlags.Damaged);
            Util.GetInstance().Log(ammo.SubtypeName + " [ReloadGuns] Amount " + point.RawValue, "ItemManager.txt");
        }

        private static bool IsAGun(IMyUserControllableGun gun)
        {
            return gun is IMySmallGatlingGun || gun is IMyLargeGatlingTurret || gun is IMyLargeInteriorTurret;
        }

        public static void ReloadReactors(List<Sandbox.ModAPI.IMySlimBlock> reactors)
        {
            for (int i = 0; i < reactors.Count; i++)
            {

                Reload((IMyInventoryOwner)reactors[i].FatBlock, _uraniumFuel, true);
            }
        }

    }
}

