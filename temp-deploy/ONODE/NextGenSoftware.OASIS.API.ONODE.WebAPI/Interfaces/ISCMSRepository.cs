using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface ISCMSRepository
    {
        static AvatarManager AvatarManager { get; }
        Task AddSequence(Sequence sequence);
        Task Update(Sequence sequence);
        //Task Delete(string id);
        Task<OASISResult<Sequence>> GetSequence(string id);
        Task<OASISResult<IEnumerable<Sequence>>> GetAllSequences();
        Task<OASISResult<Phase>> GetPhase(string id);
        Task<OASISResult<IEnumerable<Phase>>> GetAllPhases();
        Task<OASISResult<Contact>> GetContact(string id);
        Task<OASISResult<IEnumerable<Contact>>> GetAllContacts(bool loadPhase = false);
        Task<OASISResult<IEnumerable<Contact>>> GetAllContacts(int SequenceNo, int PhaseNo, bool loadPhase = false);
        Task<OASISResult<Contract>> GetContract(string id);
        Task<OASISResult<IEnumerable<Contract>>> GetAllContracts();
        Task<OASISResult<Delivery>> GetDelivery(string id, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true);
        Task<OASISResult<IEnumerable<Delivery>>> GetAllDeliveries(bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true);
        Task<OASISResult<IEnumerable<Delivery>>> GetAllDeliveries(int sequenceNo, int phaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true);
        Task<OASISResult<DeliveryItem>> GetDeliveryItem(string id);
        Task<OASISResult<IEnumerable<DeliveryItem>>> GetAllDeliveryItems();
        Task<OASISResult<Drawing>> GetDrawing(string id);
        Task<OASISResult<IEnumerable<Drawing>>> GetAllDrawings(bool loadPhase = false, bool loadFile = true);
        Task<OASISResult<IEnumerable<Drawing>>> GetAllDrawings(int SequenceNo, int PhaseNo, bool loadPhase = false, bool loadFile = true);
        Task<OASISResult<File>> GetFile(string id);
        Task<OASISResult<IEnumerable<File>>> GetAllFiles();
        Task<OASISResult<Handover>> GetHandover(string id);
        Task<OASISResult<IEnumerable<Handover>>> GetAllHandovers();
        Task<OASISResult<Link>> GetLink(string id);
        Task<OASISResult<IEnumerable<Link>>> GetAllLinks();
        Task<OASISResult<Log>> GetLog(string id);
        Task<OASISResult<IEnumerable<Log>>> GetAllLogs();
        Task<OASISResult<Material>> GetMaterial(string id);
        Task<OASISResult<IEnumerable<Material>>> GetAllMaterials();
        Task<OASISResult<Note>> GetNote(string id);
        Task<OASISResult<IEnumerable<Note>>> GetAllNotes();
        Task<OASISResult<Trigger>> GetTrigger(string id);
        Task<OASISResult<IEnumerable<Trigger>>> GetAllTriggers();
    }
}
