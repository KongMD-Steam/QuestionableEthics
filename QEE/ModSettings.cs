using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace QEthics
{
    public class QEESettings : ModSettings
    {
        public static QEESettings instance; 
        public float maintRateFloat = 1.0f;
        public float organGrowthRateFloat = 1.0f;
        public float cloneGrowthRateFloat = 1.0f;
        public float organTotalResourcesFloat = 1.0f;
        public float cloneTotalResourcesFloat = 1.0f;
        public float maintWorkThresholdFloat = 0.40f;
        public bool debugLogging = false;
        public bool giveCloneNegativeThought = true;
        public int maxCloningTimeDays = 60;
        public int ingredientCheckIntervalSeconds = 3;

        public QEESettings()
        {
            instance = this;
        }

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref maintRateFloat, "maintRateFloat", 1.0f);
            Scribe_Values.Look(ref organGrowthRateFloat, "organGrowthRateFloat", 1.0f);
            Scribe_Values.Look(ref cloneGrowthRateFloat, "cloneGrowthRateFloat", 1.0f);
            Scribe_Values.Look(ref organTotalResourcesFloat, "organTotalResourcesFloat", 1.0f);
            Scribe_Values.Look(ref cloneTotalResourcesFloat, "cloneTotalResourcesFloat", 1.0f);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref maintWorkThresholdFloat, "maintWorkGiverThresholdFloat", 0.40f);
            Scribe_Values.Look(ref giveCloneNegativeThought, "giveCloneNegativeThought", true);
            Scribe_Values.Look(ref maxCloningTimeDays, "maxCloningTimeDays", 60);
            Scribe_Values.Look(ref ingredientCheckIntervalSeconds, "ingredientCheckIntervalSeconds", 3);
            base.ExposeData();
        }
    }

    public class QEEMod : Mod
    {
        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public QEEMod(ModContentPack content) : base(content)
        {
            QEESettings.instance = base.GetSettings<QEESettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.SliderLabeled("QE_OrganGrowthDuration".Translate(), ref QEESettings.instance.organGrowthRateFloat, QEESettings.instance.organGrowthRateFloat.ToString("0.00"), 0.00f, 4.0f, "QE_OrganGrowthDurationTooltip".Translate());
            listingStandard.SliderLabeled("QE_OrganIngredientMult".Translate(), ref QEESettings.instance.organTotalResourcesFloat, QEESettings.instance.organTotalResourcesFloat.ToString("0.00"), 0.00f, 4.0f, "QE_OrganIngredientMultTooltip".Translate());
            listingStandard.SliderLabeled("QE_CloneGrowthDuration".Translate(), ref QEESettings.instance.cloneGrowthRateFloat, QEESettings.instance.cloneGrowthRateFloat.ToString("0.00"), 0.00f, 4.0f, "QE_CloneGrowthDurationTooltip".Translate());
            listingStandard.SliderLabeled("QE_MaxCloningTime".Translate(), ref QEESettings.instance.maxCloningTimeDays, QEESettings.instance.maxCloningTimeDays.ToString(), 1, 300, "QE_MaxCloningTimeTooltip".Translate());
            listingStandard.SliderLabeled("QE_CloneIngredientMult".Translate(), ref QEESettings.instance.cloneTotalResourcesFloat, QEESettings.instance.cloneTotalResourcesFloat.ToString("0.00"), 0.00f, 4.0f, "QE_CloneIngredientMultTooltip".Translate());
            listingStandard.SliderLabeled("QE_IngredientCheckInterval".Translate(), ref QEESettings.instance.ingredientCheckIntervalSeconds, QEESettings.instance.ingredientCheckIntervalSeconds.ToString(), 1.00f, 20.0f, "QE_IngredientCheckIntervalDescription".Translate());
            listingStandard.SliderLabeled("QE_VatMaintTime".Translate(), ref QEESettings.instance.maintRateFloat, QEESettings.instance.maintRateFloat.ToString("0.00"), 0.01f, 4.0f, "QE_VatMaintTimeTooltip".Translate());
            listingStandard.SliderLabeled("QE_MaintenanceWorkThreshold".Translate(), ref QEESettings.instance.maintWorkThresholdFloat, QEESettings.instance.maintWorkThresholdFloat.ToStringPercent(), 0.00f, 1.0f, "QE_MaintenanceWorkThresholdTooltip".Translate());
            listingStandard.CheckboxLabeled("QE_GiveCloneNegativeThought".Translate(), ref QEESettings.instance.giveCloneNegativeThought, "QE_GiveCloneNegativeThoughtTooltip".Translate());
            listingStandard.CheckboxLabeled("QE_DebugLogging".Translate(), ref QEESettings.instance.debugLogging, "QE_DebugLoggingTooltip".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public static void TryLog(string message)
        {
            if (QEESettings.instance.debugLogging)
            {
                Log.Message("QEE: " + message);
            }
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory() => "Questionable Ethics Enhanced";
    }


}

