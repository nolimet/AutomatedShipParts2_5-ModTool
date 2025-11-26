namespace GodModeEdicts;

public static class ModifierNamesData
{
    //Ship and station classes
    public static readonly string[] ShipSizes =
    [
        "abandoned_ship", "alien_racing_ship", "ancient_corvette", "ancient_destroyer", "ancient_drone_station",
        "ancient_mining_drone", "ark", "asteroid", "battleship", "blue_core_ai", "blue_military_station_large_ai", "blue_military_station_small_ai",
        "caravaneer_cargoship_01", "caravaneer_cruiser_01", "caravaneer_destroyer_01", "caravaneer_station_01", "civilian_freighter", "civilian_tanker",
        "colonizer", "colony_ship", "colony_ship_ai", "colossus", "construction_ship_ai", "construction_ship_ed", "construction_ship_swarm", "constructor",
        "core_ai", "corrupted_avatar", "corvette", "cruiser", "crystal_ship_large_blue", "crystal_ship_large_blue", "crystal_ship_large_green_elite",
        "crystal_ship_large_green", "crystal_ship_large_red_elite", "crystal_ship_large_red", "crystal_ship_large_yellow_elite", "crystal_ship_medium_blue",
        "crystal_ship_medium_blue_elite", "crystal_ship_medium_green", "crystal_ship_medium_green_elite", "crystal_ship_medium_red", "crystal_ship_medium_red_elite",
        "crystal_ship_medium_yellow", "crystal_ship_medium_yellow_elite", "crystal_ship_small_blue", "crystal_ship_small_blue_elite", "crystal_ship_small_green",
        "crystal_ship_small_green_elite", "crystal_ship_small_red", "crystal_ship_small_red_elite", "crystal_ship_small_yellow", "crystal_ship_small_yellow_elite",
        "crystal_station_large", "destroyer", "dimensional_horror", "dimensional_portal_ed", "enclave_station", "enigmatic_cache", "eventship_[01-07]", "final_core_ai",
        "galleon", "graygoo_factory", "graygoo_interdictor", "graygoo_mothership", "hive_asteroid", "homebase", "ion_cannon", "large_ship_ai", "large_ship_carrier_swarm",
        "large_ship_ed", "large_ship_fallen_empire", "large_ship_swarm", "leviathan_01_elder_tiyanki", "leviathan_01_scavenger_bot", "leviathan_01_voidspawn", "leviathan_01_voidspawn",
        "marauder_corvette", "marauder_cruiser", "marauder_destroyer", "marauder_galleon", "marauder_station", "marauder_void_dwelling", "massive_ship_fallen_empire", "medium_ship_ed",
        "military_station_large_ai", "military_station_large_fallen_empire", "military_station_large", "military_station_medium", "military_station_small_ai", "military_station_small",
        "military_station_small_fallen_empire", "military_station_small", "mining_station", "nanite_space_dragon_baby", "nomad_destroyer", "npc_warship_01", "observation_station",
        "passenger_liner", "pirate_corvette", "pirate_cruiser", "pirate_destroyer", "pirate_station", "primitive_space_station", "probe", "psionic_avatar", "queen_swarm",
        "research_station", "science", "sensor_station_01", "sentinel_station", "shroud_manifestation", "small_ship_ai", "small_ship_ed", "small_ship_fallen_empire", "small_ship_swarm",
        "space_amoeba", "space_amoeba_mother", "space_cloud", "space_dragon_baby", "space_dragon_blue", "space_dragon_red", "space_whale_[1-4", "sphere", "sponsored_colonizer",
        "starbase_ai", "starbase_caravaneer", "starbase_citadel", "starbase_exd", "starbase_fe_outpost", "starbase_gatebuilders", "starbase_marauder", "starbase_outpost", "starbase_starfortres",
        "starbase_starhold", "starbase_starport", "starbase_swarm", "station_generic_01", "station_1", "station_m", "station_s", "station_xl", "station_xs", "stellarite", "titan", "transport",
        "transport_ship_ai", "transport_ship_swarm", "warped_consciousness", "wraith_01_blue", "wraith_01_red", "wraith_01_yellow"
    ];

    public static readonly string[] ShipClasses = ["battleship, colonizer, corvette, cruiser, destroyer, military_station, science"];
    public static readonly string[] StationClasses = ["military_station", "mining_station", "observation_station", "research_station", "starbase", "ion_cannon"];

    //Resources

    public static readonly string[] RareResources = ["exotic_gases", "rare_crystals", "sr_dark_matter", "sr_living_metal", "sr_zro", "volatile_motes", "nanites"];
    public static readonly string[] GeneralResources = ["alloys", "consumer_goods", "energy", "minerals", "food"];
    public static readonly string[] ScienceResources = ["physics_research", "society_research", "engineering_research"];
    public static readonly string[] AllResources;

    static ModifierNamesData() =>
        AllResources = RareResources.Concat(GeneralResources).ToArray();
}
