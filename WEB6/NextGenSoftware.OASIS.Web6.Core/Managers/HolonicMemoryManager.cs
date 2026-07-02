using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Manages the Holonic BRAID fractal memory hierarchy - Session → Agent → User → Group → Neighbourhood →
    /// District → City → County → Country → Continent → Earth. Every level is a holon that is simultaneously
    /// a whole in itself and a part of the level above it. Membrane rules (per-field, consent-governed) decide
    /// exactly what memory is allowed to propagate from a child holon up to its parent - the default is private;
    /// nothing propagates without an explicit rule. This is how genuine bottom-up collective intelligence forms
    /// without becoming surveillance.
    /// </summary>
    public class HolonicMemoryManager : OASISManager
    {
        // The Earth holon is a global singleton - every Holonic BRAID deployment shares the same well-known id.
        public static readonly Guid EarthHolonId = new Guid("00000000-0000-0000-0000-00000000EA47");

        public HolonicMemoryManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }

        public HolonicMemoryManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA) { }

        private static HolonType ToHolonType(HolonicMemoryLevel level)
        {
            return level switch
            {
                HolonicMemoryLevel.Session => HolonType.ReasoningSession,
                HolonicMemoryLevel.Agent => HolonType.HolonicMemoryAgentHolon,
                HolonicMemoryLevel.User => HolonType.HolonicMemoryUserHolon,
                HolonicMemoryLevel.Group => HolonType.HolonicMemoryGroupHolon,
                HolonicMemoryLevel.Neighbourhood => HolonType.HolonicMemoryNeighbourhoodHolon,
                HolonicMemoryLevel.District => HolonType.HolonicMemoryDistrictHolon,
                HolonicMemoryLevel.City => HolonType.HolonicMemoryCityHolon,
                HolonicMemoryLevel.County => HolonType.HolonicMemoryCountyHolon,
                HolonicMemoryLevel.Country => HolonType.HolonicMemoryCountryHolon,
                HolonicMemoryLevel.Continent => HolonType.HolonicMemoryContinentHolon,
                _ => HolonType.HolonicMemoryEarthHolon,
            };
        }

        /// <summary>Gets the single planetary Earth holon, creating it if this is the very first call anywhere.</summary>
        public async Task<OASISResult<HolonicMemoryHolonDto>> GetOrCreateEarthHolonAsync()
        {
            OASISResult<HolonicMemoryHolonDto> result = new OASISResult<HolonicMemoryHolonDto>();
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(EarthHolonId, false);

            if (!loadResult.IsError && loadResult.Result != null)
            {
                result.Result = MapToDto(loadResult.Result);
                return result;
            }

            Holon holon = new Holon(EarthHolonId) { Name = "Earth", Description = "The planetary Holonic BRAID collective intelligence holon." };
            holon.HolonType = HolonType.HolonicMemoryEarthHolon;
            WriteDtoToMetaData(holon, new HolonicMemoryHolonDto { Id = EarthHolonId, Level = HolonicMemoryLevel.Earth, Name = "Earth" });

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(holon, AvatarId, false);

            if (saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Earth holon. Reason: {saveResult.Message}");
                return result;
            }

            result.Result = MapToDto(saveResult.Result);
            return result;
        }

        /// <summary>Finds an existing holon at the given level/name/parent, or creates a new one.</summary>
        public async Task<OASISResult<HolonicMemoryHolonDto>> GetOrCreateHolonAsync(HolonicMemoryLevel level, string name, Guid parentHolonId)
        {
            OASISResult<HolonicMemoryHolonDto> result = new OASISResult<HolonicMemoryHolonDto>();
            HolonType holonType = ToHolonType(level);

            OASISResult<IEnumerable<IHolon>> loadAllResult = await Data.LoadAllHolonsAsync(holonType);

            if (loadAllResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading {level} holons. Reason: {loadAllResult.Message}");
                return result;
            }

            IHolon existing = loadAllResult.Result?.FirstOrDefault(h =>
                string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase) &&
                h.MetaData != null &&
                h.MetaData.TryGetValue("ParentHolonId", out object p) &&
                Guid.TryParse(p?.ToString(), out Guid pid) && pid == parentHolonId);

            if (existing != null)
            {
                result.Result = MapToDto(existing);
                return result;
            }

            Holon holon = new Holon(holonType) { Name = name, Description = $"Holonic BRAID {level} holon for '{name}'." };
            WriteDtoToMetaData(holon, new HolonicMemoryHolonDto { Level = level, Name = name, ParentHolonId = parentHolonId });

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(holon, AvatarId, false);

            if (saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating {level} holon. Reason: {saveResult.Message}");
                return result;
            }

            result.Result = MapToDto(saveResult.Result);
            return result;
        }

        /// <summary>Sets the membrane rule that governs what this holon is allowed to propagate upward to its parent.</summary>
        public async Task<OASISResult<bool>> SetMembraneRuleAsync(Guid holonId, MembraneRule rule)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(holonId, false);

            if (loadResult.IsError || loadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Holon {holonId} not found.");
                return result;
            }

            loadResult.Result.MetaData["MembraneRule"] = JsonSerializer.Serialize(rule);
            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(loadResult.Result, AvatarId, false);

            result.Result = !saveResult.IsError;
            return result;
        }

        /// <summary>Records a new memory item at the given holon. Does not propagate it anywhere by itself - call PropagateAsync for that.</summary>
        public async Task<OASISResult<bool>> RecordMemoryAsync(Guid holonId, HolonicMemoryItem item)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(holonId, false);

            if (loadResult.IsError || loadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Holon {holonId} not found.");
                return result;
            }

            HolonicMemoryHolonDto dto = MapToDto(loadResult.Result);
            dto.MemoryItems.Add(item);
            WriteDtoToMetaData(loadResult.Result, dto);

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(loadResult.Result, AvatarId, false);
            result.Result = !saveResult.IsError;
            return result;
        }

        /// <summary>
        /// Propagates whatever memory items are permitted by the child holon's membrane rule up to its parent holon
        /// (a single hop - call again at the parent level to continue propagation further up the hierarchy).
        /// The default is private: any field not explicitly listed in FieldsAllowedToPropagate is never propagated.
        /// </summary>
        public async Task<OASISResult<int>> PropagateAsync(Guid childHolonId)
        {
            OASISResult<int> result = new OASISResult<int>();
            OASISResult<IHolon> childLoadResult = await Data.LoadHolonAsync(childHolonId, false);

            if (childLoadResult.IsError || childLoadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Holon {childHolonId} not found.");
                return result;
            }

            HolonicMemoryHolonDto child = MapToDto(childLoadResult.Result);

            if (child.ParentHolonId == Guid.Empty || child.MemoryItems.Count == 0)
            {
                result.Result = 0;
                return result;
            }

            MembraneRule rule = child.MembraneRule ?? new MembraneRule();
            List<HolonicMemoryItem> permitted = child.MemoryItems.Where(item => IsPermittedToPropagate(item, rule)).ToList();

            if (permitted.Count == 0)
            {
                result.Result = 0;
                return result;
            }

            OASISResult<IHolon> parentLoadResult = await Data.LoadHolonAsync(child.ParentHolonId, false);

            if (parentLoadResult.IsError || parentLoadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Parent holon {child.ParentHolonId} not found.");
                return result;
            }

            HolonicMemoryHolonDto parent = MapToDto(parentLoadResult.Result);

            foreach (HolonicMemoryItem item in permitted)
            {
                parent.MemoryItems.Add(rule.AnonymisedAggregateOnly
                    ? new HolonicMemoryItem { FieldName = item.FieldName, Value = "[anonymised-aggregate]", Tags = item.Tags }
                    : item);
            }

            WriteDtoToMetaData(parentLoadResult.Result, parent);
            await Data.SaveHolonAsync(parentLoadResult.Result, AvatarId, false);

            result.Result = permitted.Count;
            return result;
        }

        private static bool IsPermittedToPropagate(HolonicMemoryItem item, MembraneRule rule)
        {
            if (rule.FieldsAllowedToPropagate == null || !rule.FieldsAllowedToPropagate.Contains(item.FieldName, StringComparer.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(rule.TriggerCondition))
                return true;

            return item.Tags != null && item.Tags.Any(t => string.Equals(t, rule.TriggerCondition, StringComparison.OrdinalIgnoreCase));
        }

        private static void WriteDtoToMetaData(IHolon holon, HolonicMemoryHolonDto dto)
        {
            holon.MetaData["Level"] = dto.Level.ToString();
            holon.MetaData["ParentHolonId"] = dto.ParentHolonId.ToString();
            holon.MetaData["MemoryItems"] = JsonSerializer.Serialize(dto.MemoryItems);

            if (!holon.MetaData.ContainsKey("MembraneRule"))
                holon.MetaData["MembraneRule"] = JsonSerializer.Serialize(dto.MembraneRule ?? new MembraneRule());
        }

        private static HolonicMemoryHolonDto MapToDto(IHolon holon)
        {
            HolonicMemoryHolonDto dto = new HolonicMemoryHolonDto
            {
                Id = holon.Id,
                Name = holon.Name,
                Level = holon.MetaData.TryGetValue("Level", out object l) && Enum.TryParse(l?.ToString(), true, out HolonicMemoryLevel level) ? level : HolonicMemoryLevel.Session,
                ParentHolonId = holon.MetaData.TryGetValue("ParentHolonId", out object p) && Guid.TryParse(p?.ToString(), out Guid pid) ? pid : Guid.Empty
            };

            if (holon.MetaData.TryGetValue("MemoryItems", out object mi) && mi != null)
                dto.MemoryItems = JsonSerializer.Deserialize<List<HolonicMemoryItem>>(mi.ToString()) ?? new List<HolonicMemoryItem>();

            if (holon.MetaData.TryGetValue("MembraneRule", out object mr) && mr != null)
                dto.MembraneRule = JsonSerializer.Deserialize<MembraneRule>(mr.ToString()) ?? new MembraneRule();

            return dto;
        }
    }
}
