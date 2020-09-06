using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ORIAServices
{
    public interface ISCMSRepository
    {
        static AvatarManager AvatarManager { get; }
        Task AddSequence(Sequence sequence);
        Task Update(Sequence sequence);
        //Task Delete(string id);
        Task<Sequence> GetSequence(string id);
        Task<IEnumerable<Sequence>> GetAllSequences();
        Task<Phase> GetPhase(string id);
        Task<IEnumerable<Phase>> GetAllPhases();
        Task<Contact> GetContact(string id);
        Task<IEnumerable<Contact>> GetAllContacts(bool loadPhase = false);
        Task<IEnumerable<Contact>> GetAllContacts(int SequenceNo, int PhaseNo, bool loadPhase = false);
        Task<Contract> GetContract(string id);
        Task<IEnumerable<Contract>> GetAllContracts();
        //Task<Delivery> GetDelivery(string id, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadMaterial = true, bool loadFile = true);
        //Task<IEnumerable<Delivery>> GetAllDeliveries(bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadMaterial = true, bool loadFile = true);
        //Task<IEnumerable<Delivery>> GetAllDeliveries(int sequenceNo, int phaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadMaterial = true, bool loadFile = true);
        Task<Delivery> GetDelivery(string id, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true);
        Task<IEnumerable<Delivery>> GetAllDeliveries(bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true);
        Task<IEnumerable<Delivery>> GetAllDeliveries(int sequenceNo, int phaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true);

        Task<DeliveryItem> GetDeliveryItem(string id);
        Task<IEnumerable<DeliveryItem>> GetAllDeliveryItems();
        Task<Drawing> GetDrawing(string id);
        Task<IEnumerable<Drawing>> GetAllDrawings(bool loadPhase = false, bool loadFile = true);
        Task<IEnumerable<Drawing>> GetAllDrawings(int SequenceNo, int PhaseNo, bool loadPhase = false, bool loadFile = true);
        Task<File> GetFile(string id);
        Task<IEnumerable<File>> GetAllFiles();
        Task<Handover> GetHandover(string id);
        Task<IEnumerable<Handover>> GetAllHandovers();
        Task<Link> GetLink(string id);
        Task<IEnumerable<Link>> GetAllLinks();
        Task<Log> GetLog(string id);
        Task<IEnumerable<Log>> GetAllLogs();
        Task<Material> GetMaterial(string id);
        Task<IEnumerable<Material>> GetAllMaterials();
        Task<Note> GetNote(string id);
        Task<IEnumerable<Note>> GetAllNotes();
        Task<Trigger> GetTrigger(string id);
        Task<IEnumerable<Trigger>> GetAllTriggers();
    }

}
