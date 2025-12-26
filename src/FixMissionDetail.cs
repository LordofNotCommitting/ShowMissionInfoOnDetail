using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

namespace ShowMissionInfoOnDetail
{
    [HarmonyPatch(typeof(PrepareRaidScreen), nameof(PrepareRaidScreen.Configure))]

    [HarmonyPatch(new Type[] { typeof(Mission), typeof(bool) })]
    public static class FixMissionDetail
    {

        public static void Postfix(PrepareRaidScreen __instance, Mission mission, bool isReversed = false)
        {
            //Plugin.Logger.Log("--- main menu awake");
            string temp_appending_text = "";

            string color_white_prefix = "<color=#FFFEC1>";
            string color_red_prefix = "<color=#f51b1b>";
            string color_postfix = "</color>";
            string newline = "<br>";



            string temp_mission_type = "";
            string enemy_faction_name = Localization.Get("faction." + __instance._mission.VictimFactionId + ".name");
            string temp_tech_level = "";
            string temp_power_concentration = "";
            string temp_floor_count = "";
            string temp_bramfatura_name = "";

            Faction enemy_faction = __instance._factions.Get(mission.VictimFactionId, true);

            int bonus_tech_level = 0;
            string temp_tech_postfix = " (" + color_white_prefix + "+";
            //tooltip.MissionObjective
                


            switch (mission.ProcMissionType)
            {
                case ProceduralMissionType.Ritual:
                    {
                        bonus_tech_level = Data.Global.RitualMonsterGroupTechLevelBonus;
                        temp_tech_postfix += bonus_tech_level + color_postfix + " " + Localization.Get("ui.label.enemy") + " " + Localization.Get("missiontype.Counterattack.name") + " Squad)";
                        break;
                    }
                case ProceduralMissionType.Counterattack:
                    {
                        bonus_tech_level = Data.Global.CounterattackMonsterGroupTechLevelBonus;
                        temp_tech_postfix += bonus_tech_level + color_postfix + " " + Localization.Get("ui.label.enemy") + " " + Localization.Get("missiontype.Ritual.name") + " Squad)";
                        break;
                    }
                case ProceduralMissionType.Infiltration:
                    {
                        bonus_tech_level = Data.Global.InfiltrationMonsterGroupTechLevelBonus;
                        temp_tech_postfix += bonus_tech_level + color_postfix + " " + Localization.Get("ui.label.enemy") + " " + Localization.Get("missiontype.Infiltration.name") + " Squad)";
                        break;
                    }
                default:
                    {
                        break;
                    }
            }



            UnityEngine.Color relation_color = Colors.GetFactionColorByReputation(enemy_faction.PlayerReputation);

            string hex = ColorUtility.ToHtmlStringRGB(relation_color);
            //Plugin.Logger.Log("--- main menu awake" + hex);

            string color_faction_prefix = "<color=#" + hex + ">";

            if (mission.IsStoryMission)
            {
                enemy_faction_name += "(?)";
                temp_tech_level = "???";
                temp_mission_type = Localization.Get("missiontype.story.name");
                temp_power_concentration = "???";
                temp_floor_count = "???";
                temp_bramfatura_name = "???";
            }
            else {

                enemy_faction_name += " (" + enemy_faction.PlayerReputation + ")";
                temp_tech_level = enemy_faction.CurrentTechLevel.ToString();

                if (bonus_tech_level > 0) {
                    temp_tech_level += temp_tech_postfix;
                }

                temp_mission_type = Localization.Get(string.Format("missiontype.{0}.name", mission.ProcMissionType));

                //check if bramfatura
                if (mission.ProcMissionType == ProceduralMissionType.BramfaturaInvasion)
                {
                    
                    temp_power_concentration = "(" + Localization.Get("faction.Unknown.name") + ")" ;
                    temp_floor_count = "(" + Localization.Get("faction.Unknown.name") + ")";
                }
                else {
                    int num = 0;
                    int num2 = 0;
                    foreach (KeyValuePair<string, DungeonGenerationPlan> keyValuePair in mission.LocationPlans)
                    {
                        if (mission.WorldStructure.GetLocation(keyValuePair.Key).CustomID.Contains("stage"))
                        {
                            num += keyValuePair.Value.MonstersPointsLimit;
                            num2++;
                        }
                    }
                    temp_power_concentration = num.ToString();
                    temp_floor_count = num2.ToString();
                }
                //bramfatura name logic
                string temp_bram_id = mission.BramfaturaId;
                if (!mission.IsStoryMission && (mission.ProcMissionType == ProceduralMissionType.Defense ))
                {
                    temp_bram_id = Data.Global.DefenseMissionsBramfaturaId;
                }

                temp_bramfatura_name = Localization.Get(string.Format("faction.Ferals_{0}.name", temp_bram_id));
                if (temp_bramfatura_name.Contains("faction.Ferals_")) {
                    temp_bramfatura_name = Localization.Get("ui.label.none");
                }

            }

            
            temp_appending_text += color_white_prefix + Localization.Get("ui.label.enemy") + ":" + color_postfix + " " + color_faction_prefix + enemy_faction_name + color_postfix + newline;
            temp_appending_text += color_white_prefix + Localization.Get("ui.label.enemy") + " " + Localization.Get("tooltip.TechLevel") + ":" + color_postfix + " " + temp_tech_level + newline;
            temp_appending_text += color_white_prefix + Localization.Get("ui.label.mission") + ":" + color_postfix + " " + temp_mission_type + newline;
            temp_appending_text += color_white_prefix + Localization.Get("ui.property.PowerContentration") + ":" + color_postfix + " " + temp_power_concentration + newline;
            temp_appending_text += color_white_prefix + Localization.Get("ui.property.FloorsCount") + ":" + color_postfix + " " + temp_floor_count + newline;
            temp_appending_text += color_white_prefix + "Bramfatura" + ":" + color_postfix + " " + temp_bramfatura_name + newline;
            //Plugin.Logger.Log("guh");
            __instance._objectivesText.text = temp_appending_text.ConvertBrToNewLine() + __instance._objectivesText.text ;
        }
    }
}
