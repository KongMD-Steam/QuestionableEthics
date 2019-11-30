using Verse;
using RimWorld;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse.AI;

namespace QEthics
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("KongMD.QEE");
            //HarmonyInstance.DEBUG = true;

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(MedicalRecipesUtility))]
        [HarmonyPatch(nameof(MedicalRecipesUtility.SpawnNaturalPartIfClean))]
        static class SpawnNaturalPartIfClean_Patch
        {
            [HarmonyPostfix]
            static void SpawnNaturalPartIfCleanPostfix(ref Thing __result, Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
            {
                bool isOrganTransplant = false;
                bool shouldDropTransplantOrgan = false;
                int badHediffCount = 0;
                foreach (Hediff currHediff in pawn.health.hediffSet.hediffs)
                {
                    if (currHediff.Part == part)
                    {
                        QEEMod.TryLog("Hediff on this body part: " + currHediff.def.defName + " isBad: " + currHediff.def.isBad);
                        if (currHediff.def == QEHediffDefOf.QE_OrganRejection)
                        {
                            isOrganTransplant = true;
                        }
                        else if (currHediff.def.isBad)
                        {
                            badHediffCount++;
                        }
                    }
                }

                //if the only hediff on bodypart is organ rejection, that organ should spawn
                //vanilla game would not spawn it, because part hediffs > 0
                if (isOrganTransplant && badHediffCount == 0 && part.def.spawnThingOnRemoved != null)
                {
                    shouldDropTransplantOrgan = true;
                }

                QEEMod.TryLog("shouldDropTransplantOrgan: " + shouldDropTransplantOrgan + " [isOrganTransplant: " + 
                    isOrganTransplant + " " + part.LabelShort + " bad hediffs: " + badHediffCount + "]");

                //spawn a biological arm when a shoulder is removed with a healthy arm attached
                if (part.LabelShort == "shoulder")
                {
                    foreach (BodyPartRecord childPart in part.parts)
                    {
                        bool isHealthy = MedicalRecipesUtility.IsClean(pawn, childPart);
                        QEEMod.TryLog("body part: " + childPart.LabelShort + " defName: " + childPart.def.defName +
                            " healthy: " + isHealthy);

                        if (childPart.def == BodyPartDefOf.Arm && isHealthy && (shouldDropTransplantOrgan = true || 
                            isOrganTransplant == false))
                        {
                            QEEMod.TryLog("Spawn natural arm from shoulder replacement");
                            __result = GenSpawn.Spawn(QEThingDefOf.QE_Organ_Arm, pos, map);
                        }
                    }
                }
                else if (shouldDropTransplantOrgan)
                {
                    __result = GenSpawn.Spawn(part.def.spawnThingOnRemoved, pos, map);
                }

            }
        }

        /// <summary>
        /// Lightweight patch that adds any hediffs in the <addsHediff> element of the recipe
        /// after surgery involving natural body parts completes.
        /// </summary>
        [HarmonyPatch(typeof(Recipe_InstallNaturalBodyPart))]
        [HarmonyPatch(nameof(Recipe_InstallNaturalBodyPart.ApplyOnPawn))]
        class ApplyOnPawn_Patch
        {
            [HarmonyPostfix]
            static void ApplyOnPawnPostfix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
            {
                if (MedicalRecipesUtility.IsCleanAndDroppable(pawn, part) && bill.recipe.addsHediff != null)
                {
                    pawn.health.AddHediff(bill.recipe.addsHediff, part);
                }
            }
        }

        /// <summary>
        /// This patch allows GetWorkgiver() to return our custom workgiver, WorkGiver_DoBill_Grower. It will continue on to the vanilla function
        /// if no BillGivers are of this class. 
        /// </summary>
        [HarmonyPatch(typeof(BillUtility))]
        [HarmonyPatch(nameof(BillUtility.GetWorkgiver))]
        class GetWorkgiver_Patch
        {
            [HarmonyPrefix]
            static bool GetWorkgiver_Prefix(ref WorkGiverDef __result, IBillGiver billGiver) //pass the __result by ref to alter it.
            {
                Thing thing = billGiver as Thing;
                if (thing == null)
                {
                    Log.ErrorOnce($"Attempting to get the workgiver for a non-Thing IBillGiver {billGiver.ToString()}", 96810282);
                    __result = null;
                    return false;
                }
                List<WorkGiverDef> allDefsListForReading = DefDatabase<WorkGiverDef>.AllDefsListForReading;
                for (int i = 0; i < allDefsListForReading.Count; i++)
                {
                    WorkGiverDef workGiverDef = allDefsListForReading[i];
                    WorkGiver_DoBill_Grower workGiver_DoBill = workGiverDef.Worker as WorkGiver_DoBill_Grower;
                    if (workGiver_DoBill != null && workGiver_DoBill.ThingIsUsableBillGiver(thing))
                    {
                        __result = workGiverDef;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BillStack))]
        [HarmonyPatch(nameof(BillStack.AddBill))]
        class AddBill_Patch
        {
            [HarmonyPostfix]
            static void AddBillPostfix(BillStack __instance, Bill bill)
            {
                IBillGiverExtension extension = __instance.billGiver as IBillGiverExtension;
                if (extension != null)
                {
                    extension.Notify_BillAdded(bill);
                }
            }
        }

        [HarmonyPatch(typeof(BillStack))]
        [HarmonyPatch(nameof(BillStack.Delete))]
        class DeleteBill_Patch
        {
            [HarmonyPostfix]
            static void DeleteBillPostfix(BillStack __instance, Bill bill)
            {
                IBillGiverExtension extension = __instance.billGiver as IBillGiverExtension;
                if (extension != null)
                {
                    extension.Notify_BillDeleted(bill);
                }
            }
        }
    }
}
