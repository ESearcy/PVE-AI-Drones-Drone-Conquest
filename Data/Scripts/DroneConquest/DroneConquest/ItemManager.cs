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


        public static void ReloadGuns(List<IMySlimBlock> guns)
        {
            for (int i = 0; i < guns.Count; i++)
            {
                if (guns[i].FatBlock.IsFunctional)
                {
                    guns.RemoveAt(i--);
                    continue;
                }
                Reload((IMyInventoryOwner)guns[i].FatBlock, IsAGun((IMyUserControllableGun)guns[i].FatBlock) ? _gatlingAmmo : _launcherAmmo);
            }
        }

        private static void Reload(IMyInventoryOwner gun, SerializableDefinitionId ammo, bool reactor = false, int i1=1)
        {
            var cGun = gun;
            IMyInventory inv = (IMyInventory)cGun.GetInventory(0);
            MyFixedPoint point = inv.GetItemAmount(ammo, MyItemFlags.None | MyItemFlags.Damaged);

            if (point.RawValue > 10000 && !reactor)
                return;
            inv.Clear();

            MyFixedPoint amount = new MyFixedPoint { RawValue = 10000 };

            MyObjectBuilder_InventoryItem ii;
            if (reactor)
            {
                ii = new MyObjectBuilder_InventoryItem()
                {
                    Amount = i1,
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
        }

        private static bool IsAGun(IMyUserControllableGun gun)
        {
            return gun is IMySmallGatlingGun || gun is IMyLargeGatlingTurret || gun is IMyLargeInteriorTurret;
        }

        public static void ReloadReactors(List<IMySlimBlock> reactors, int i1 = 1)
        {
            for (int i = 0; i < reactors.Count; i++)
            {
                if (reactors[i].IsDestroyed || reactors[i].IsFullyDismounted)
                {
                    reactors.RemoveAt(i--);
                    continue;
                }
                Reload((IMyInventoryOwner)reactors[i].FatBlock, _uraniumFuel, true, i1);
            }
        }

    }
}

