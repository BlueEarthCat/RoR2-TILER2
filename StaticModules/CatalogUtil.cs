﻿using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Linq.Expressions;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.ObjectModel;
using RoR2.Navigation;
using UnityEngine.AddressableAssets;
using R2API;

namespace TILER2 {
    /// <summary>
    /// Contains miscellaneous utilities pertaining to RoR2 catalogs, such as retrieving and converting values.
    /// </summary>
    public static class CatalogUtil {
        public static bool TryGetItemDef(PickupIndex pickupIndex, out ItemDef itemDef) {
            itemDef = null;
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if(pickupDef == null || pickupDef.itemIndex == ItemIndex.None) return false;
            itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
            return itemDef != null;
        }
        public static bool TryGetItemDef(PickupDef pickupDef, out ItemDef itemDef) {
            itemDef = null;
            if(pickupDef == null || pickupDef.itemIndex == ItemIndex.None) return false;
            itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
            return itemDef != null;
        }
        public static bool TryGetItemDef(ItemIndex itemIndex, out ItemDef itemDef) {
            itemDef = null;
            if(itemIndex == ItemIndex.None) return false;
            itemDef = ItemCatalog.GetItemDef(itemIndex);
            return itemDef != null;
        }

        public static bool TryGetEquipmentDef(PickupIndex pickupIndex, out EquipmentDef equipmentDef) {
            equipmentDef = null;
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if(pickupDef == null || pickupDef.equipmentIndex == EquipmentIndex.None) return false;
            equipmentDef = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
            return equipmentDef != null;
        }
        public static bool TryGetEquipmentDef(PickupDef pickupDef, out EquipmentDef equipmentDef) {
            equipmentDef = null;
            if(pickupDef == null || pickupDef.equipmentIndex == EquipmentIndex.None) return false;
            equipmentDef = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
            return equipmentDef != null;
        }
        public static bool TryGetEquipmentDef(EquipmentIndex equipmentIndex, out EquipmentDef equipmentDef) {
            equipmentDef = null;
            if(equipmentIndex == EquipmentIndex.None) return false;
            equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
            return equipmentDef != null;
        }
    }
}
