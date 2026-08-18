using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.Random;



namespace ShowMissionInfoOnDetail
{
    [HarmonyPatch(typeof(PrepareRaidScreen), nameof(PrepareRaidScreen.Configure))]

    [HarmonyPatch(new Type[] { typeof(Mission), typeof(bool) })]
    
    
    public static class FixMissionDetail
    {

        public static void Postfix(PrepareRaidScreen __instance, Mission mission, bool isReversed)
        {
            //Plugin.Logger.Log("--- main menu awake");
            string temp_appending_text = "";

            string color_white_prefix = "<color=#FFFEC1>";
            string color_red_prefix = "<color=#f51b1b>";
            string color_postfix = "</color>";
            string newline = "<br>";

            var factions = Traverse.Create(__instance).Field("_factions").GetValue<Factions>();
            var objectivesText = Traverse.Create(__instance).Field("_objectivesText").GetValue<TextMeshProUGUI>();

            string temp_mission_type = "";
            string enemy_faction_name = Localization.Get("faction." + mission.VictimFactionId + ".name");
            string temp_tech_level = "";
            string temp_power_concentration = "";
            string temp_enemy_count = "";
            string temp_floor_count = "";
            string temp_bramfatura_name = "";
            string temp_station_type = "";

            Faction enemy_faction = factions.Get(mission.VictimFactionId, true);

            int bonus_tech_level = 0;
            string temp_tech_postfix = " (" + color_white_prefix + "+";
            //tooltip.MissionObjective

            int estimatedEnemyCountPerFloor = -1;
            int estimatedPowerConcentrationPerFloor = -1;
            int estimatedFloor = 0;
            float powerPerUnit = -1f;

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
                temp_enemy_count = "???";
                temp_floor_count = "???";
                temp_bramfatura_name = "???";
                temp_station_type = "???";
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
                    
                    temp_power_concentration = "(" + Localization.Get("faction.Unknown.name") + ")";
                    temp_enemy_count = "(" + Localization.Get("faction.Unknown.name") + ")";
                    temp_floor_count = "(" + Localization.Get("faction.Unknown.name") + ")";
                }
                else {
                    foreach (KeyValuePair<string, DungeonGenerationPlan> keyValuePair in mission.LocationPlans)
                    {
                        if (mission.WorldStructure.GetLocation(keyValuePair.Key).ID.Contains("stage"))
                        {
                            estimatedFloor++;
                        }
                    }

                    estimatedEnemyCountPerFloor = GetEstimatedEnemyCountPerFloor(mission);
                    estimatedPowerConcentrationPerFloor = GetMonsterPointsPerFloor(mission, false);

                    temp_power_concentration = estimatedPowerConcentrationPerFloor.ToString();

                    temp_enemy_count = estimatedEnemyCountPerFloor.ToString();
                    if (estimatedEnemyCountPerFloor > 0)
                    {
                        powerPerUnit = (float)(estimatedPowerConcentrationPerFloor / estimatedEnemyCountPerFloor);
                    }
                    else 
                    {
                        powerPerUnit = 0;
                    }
                    temp_floor_count = estimatedFloor.ToString();
                }
                //bramfatura name logic
                string temp_bram_id = mission.BramfaturaId;
                if (!mission.IsStoryMission && (mission.ProcMissionType == ProceduralMissionType.Defense ))
                {
                    temp_bram_id = Data.Global.DefenseMissionsBramfaturaId;
                }

                temp_bramfatura_name = Localization.Get(string.Format("bramfatura.{0}.name", temp_bram_id));
                if (temp_bramfatura_name.Contains("bramfatura.")|| temp_bram_id.Contains("Sleep")) {
                    temp_bramfatura_name = Localization.Get("ui.label.none");

                }

                //station name logic. need hard coding
                var stations = AccessTools.Field(typeof(PrepareRaidScreen), "_stations").GetValue(__instance) as Stations;
                Station _station = stations.Get(mission.StationId, true);
                //var stations = AccessTools.Field(typeof(Station), "_stations").GetValue(__instance) as Stations;
                //Plugin.Logger.Log(_station.Record.StationType);
                switch (_station.Record.StationType)
                {

                    case "Bramfaturian":
                        {
                            //quasi-realm
                            temp_station_type = Localization.Get("station.FeatheredTemple.type");
                            break;
                        }
                    case "Civilian":
                        {
                            //colony
                            temp_station_type = Localization.Get("station.Carcosa.type");
                            break;
                        }
                    case "Industrial":
                        {
                            //factory
                            temp_station_type = Localization.Get("station.SampoQuern.type");
                            break;
                        }
                    case "Lab":
                        {
                            //labs
                            temp_station_type = Localization.Get("station.Lomonosov.type");
                            break;
                        }
                    case "Military":
                        {
                            //military complex
                            temp_station_type = Localization.Get("station.Escher.type");
                            break;
                        }
                    case "Mine":
                        {
                            //mining complex
                            temp_station_type = Localization.Get("station.Flinthold.type");
                            break;
                        }
                    case "Prison":
                        {
                            //space prison
                            temp_station_type = Localization.Get("station.February.type");
                            break;
                        }
                    case "QuasimorphicTemple":
                        {
                            //factory
                            temp_station_type = Localization.Get("station.FeatheredTemple.type");
                            break;
                        }
                    case "SpaceStation":
                        {
                            //orbital station
                            temp_station_type = Localization.Get("station.Juphub.type");
                            break;
                        }
                    case "ColonyFarm":
                        {
                            //orbital station
                            temp_station_type = Localization.Get("station.NewKent.type");
                            break;
                        }
                    default:
                        {

                            temp_station_type = "Undefined";
                            break;
                        }
                }

            }

            
            temp_appending_text += color_white_prefix + Localization.Get("ui.label.enemy") + ":" + color_postfix + " " + color_faction_prefix + enemy_faction_name + color_postfix + newline;
            temp_appending_text += color_white_prefix + Localization.Get("ui.label.enemy") + " " + Localization.Get("tooltip.TechLevel") + ":" + color_postfix + " " + temp_tech_level + newline;
            temp_appending_text += color_white_prefix + Localization.Get("ui.label.mission") + ":" + color_postfix + " " + temp_mission_type + newline;
            temp_appending_text += color_white_prefix + Localization.Get("tooltip.PowerContentration") + " Per " + Localization.Get("ui.label.floor") + ":" + color_postfix + " " + temp_power_concentration;
            if (estimatedPowerConcentrationPerFloor > 0 && estimatedFloor > 1) 
            {
                temp_appending_text += " (" + color_white_prefix + "Total:" + color_postfix + " " + (estimatedPowerConcentrationPerFloor * estimatedFloor).ToString() + ")";
            }
            temp_appending_text += newline;
            String temp_enemy_count_string = Localization.Get("tooltip.EstimatedEnemies");
            string prefix = "/~{0}";
            if (temp_enemy_count_string.StartsWith(prefix))
            {
                // Remove "/~{0}" and trim remaining leading white spaces
                temp_enemy_count_string = temp_enemy_count_string.Substring(prefix.Length).TrimStart();
            }

            temp_enemy_count_string = StringExtensions.CapitalizeFirstNonAsian(temp_enemy_count_string);

            temp_appending_text += color_white_prefix + temp_enemy_count_string + " Per " + Localization.Get("ui.label.floor") + ":" + color_postfix + " " + temp_enemy_count;
            if (estimatedPowerConcentrationPerFloor > 0)
            {
                //temp_appending_text += " (" + color_white_prefix + "Total:" + color_postfix + " " + (estimatedEnemyCountPerFloor * estimatedFloor).ToString() + ", " + powerPerUnit.ToString("F2") + " " + color_white_prefix + Localization.Get("tooltip.PowerContentration") + "/" +temp_enemy_count_string + color_postfix + ")";
                temp_appending_text += " (" + powerPerUnit.ToString("F2") + " " + color_white_prefix + Localization.Get("tooltip.PowerContentration") + "/" + temp_enemy_count_string + color_postfix + ")";
            }
            temp_appending_text += newline;


            temp_appending_text += color_white_prefix + Localization.Get("tooltip.FloorsCount") + ":" + color_postfix + " " + temp_floor_count + newline;
            temp_appending_text += color_white_prefix + "Station Type" + ":" + color_postfix + " " + temp_station_type + newline;
            temp_appending_text += color_white_prefix + "Bramfatura" + ":" + color_postfix + " " + temp_bramfatura_name + newline;
            //Plugin.Logger.Log("guh");
            objectivesText.text = temp_appending_text.ConvertBrToNewLine() + objectivesText.text;

            //Localization.ActualizeFontAndSize(__instance._objectivesText, TextContext.LongText);
        }

        static private int GetEstimatedEnemyCountPerFloor(Mission mission)
        {
            int returnval = 0;

            MGSC.State _state = StateManager.ActiveState;
            if (_state != null) {

                Factions factions = _state.Get<Factions>();
                Statistics statistics = _state.Get<Statistics>();
                Difficulty difficulty = _state.Get<Difficulty>();
                int monsterPointsPerFloor = GetMonsterPointsPerFloor(mission, mission.IsStoryMission);
                List<UnitDropRecord> list = new List<UnitDropRecord>();
                foreach (KeyValuePair<string, DungeonGenerationPlan> keyValuePair in mission.LocationPlans)
                {
                    string text;
                    DungeonGenerationPlan dungeonGenerationPlan;
                    text = keyValuePair.Key;
                    dungeonGenerationPlan = keyValuePair.Value;
                    string text2 = text;
                    DungeonGenerationPlan dungeonGenerationPlan2 = dungeonGenerationPlan;
                    if (text2.Contains("stage"))
                    {
                        string text3;
                        int num;
                        MissionSystem.GetFactionEquipmentId(factions, text2, mission, out text3, out num);
                        num = Mathf.Clamp(num, 1, Data.Global.MaxTechLevel);
                        list.AddRange(UnitGenerationSystem.GetUnitVariants(dungeonGenerationPlan2.MonstersTableIds, mission.VictimFactionId, num, default(UnitGenerationConditions)));
                    }
                }
                float num2 = 0f;
                foreach (UnitDropRecord unitDropRecord in list)
                {
                    num2 += unitDropRecord.Weight;
                }
                float num3 = 0f;
                foreach (UnitDropRecord unitDropRecord2 in list)
                {
                    int num4 = (unitDropRecord2.UnitSize.Min + unitDropRecord2.UnitSize.Max) / 2;
                    if (!mission.IsStoryMission)
                    {
                        num4 = Data.ProgressionDifficulty.GetMaxCreaturesGroupSize(statistics.GetStatistic(StatisticType.TotalMissionsComplete), num4, difficulty);
                    }
                    if (unitDropRecord2.LeaderSpawn.Any((Tuple<float, string> l) => l.Item2 != "none"))
                    {
                        num4++;
                    }
                    float num5 = (float)num4 * unitDropRecord2.Weight / num2;
                    num3 += num5 * (float)monsterPointsPerFloor / unitDropRecord2.Points;
                }
                returnval = Mathf.RoundToInt(num3);
            }

            return returnval;
        }

        static private int GetMonsterPointsPerFloor(Mission mission, bool ignoreProgression)
        {
            int returnval = 0;

            MGSC.State _state = StateManager.ActiveState;

            if (_state != null)
            {
                Statistics statistics = _state.Get<Statistics>();
                Difficulty difficulty = _state.Get<Difficulty>();
                MagnumProgression magnumProgression = _state.Get<MagnumProgression>();
                int floorsCount = MissionSystem.GetFloorsCount(mission);
                int num = MissionSystem.GetTotalMonstersPoints(statistics, difficulty, mission, ignoreProgression) / MissionSystem.GetFloorsCount(mission);
                int num2 = Mathf.RoundToInt(Data.ProcMissions.Get(mission.ProcMissionType).AdditionalSpawnMultiplier * (float)num / (float)floorsCount);
                int num3 = Mathf.RoundToInt(Mathf.Clamp01((float)(magnumProgression.HyperWaveScanFloorBonus / floorsCount)) * (float)magnumProgression.HyperWaveEnemyFloorBonus);
                int b = num + num2 + num3;
                returnval = Mathf.Max(0, b);
            }

            return returnval;
        }

    }
}

public static class StringExtensions
{
    // A Regex pattern covering the main East Asian and South/Southeast Asian Unicode script blocks
    private static readonly Regex AsianScriptRegex = new Regex(
        @"^[\p{IsCJKUnifiedIdeographs}" +       // Chinese/Japanese/Korean common text
          @"\p{IsCJKUnifiedIdeographsExtensionA}" +
          @"\p{IsHiragana}\p{IsKatakana}" +    // Japanese scripts
          @"\p{IsHangulSyllables}" +            // Korean script
          @"\p{IsThai}\p{IsDevanagari}]",       // Thai and Indian/Hindi scripts
        RegexOptions.Compiled);

    public static string CapitalizeFirstNonAsian(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // If the first character matches an Asian script block, return the string unchanged
        if (AsianScriptRegex.IsMatch(input))
        {
            return input;
        }

        // Otherwise, capitalize only the first character and attach the rest
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}