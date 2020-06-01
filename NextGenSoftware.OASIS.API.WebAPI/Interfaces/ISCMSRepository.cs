using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public interface ISCMSRepository
    {
        Task AddSequence(Sequence sequence);
        Task Update(Sequence sequence);
        Task Delete(string id);
        Task<Sequence> GetSequence(string id);
        Task<IEnumerable<Sequence>> GetAllSequences();
        Task<Phase> GetPhase(string id);
        Task<IEnumerable<Phase>> GetAllPhases();
        Task<Contact> GetContact(string id);
        Task<IEnumerable<Contact>> GetAllContacts();
        Task<Contract> GetContract(string id);
        Task<IEnumerable<Contract>> GetAllContracts();
        Task<Delivery> GetDelivery(string id);
        Task<IEnumerable<Delivery>> GetAllDeliveries();
        Task<DeliveryItem> GetDeliveryItem(string id);
        Task<IEnumerable<DeliveryItem>> GetAllDeliveryItems();
        Task<Drawing> GetDrawing(string id);
        Task<IEnumerable<Drawing>> GetAllDrawings();
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
