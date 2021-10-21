using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using EOSNewYork.EOSCore.Serialization;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class SCMSRepository : ISCMSRepository
    {
        MongoDbContext db = new MongoDbContext();
        private AvatarManager AvatarManager => Program.AvatarManager;

        public async Task<OASISResult<Sequence>> GetSequence(string id)
        {
            var response = new OASISResult<Sequence>();
            try
            {
                FilterDefinition<Sequence> filter = Builders<Sequence>.Filter.Eq("Id", id); 
                response.Result = await db.Sequence.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<OASISResult<IEnumerable<Sequence>>> GetAllSequences()
        {
            var response = new OASISResult<IEnumerable<Sequence>>();
            try
            {
                response.Result = await db.Sequence.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<IEnumerable<Sequence>> GetSequencesBySequenceNo(int SequenceNo)
        {
            try
            {
                FilterDefinition<Sequence> sequenceFilter = Builders<Sequence>.Filter.Eq("SequenceNo", SequenceNo);
                return await db.Sequence.Find(sequenceFilter).ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Phase>> GetPhase(string id)
        {
            var response = new OASISResult<Phase>();
            try
            {
                FilterDefinition<Phase> filter = Builders<Phase>.Filter.Eq("Id", id);
                response.Result = await db.Phase.Find(filter).FirstOrDefaultAsync();
            }
            catch(Exception ex)
            {
                response.IsError = true;
                response.Message = ex.Message;
                response.Exception = ex;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<IEnumerable<Phase>> GetPhasesByPhaseNo(int phaseNo)
        {
            try
            {
                FilterDefinition<Phase> phaseFilter = Builders<Phase>.Filter.Eq("PhaseNo", phaseNo.ToString());
                return await db.Phase.Find(phaseFilter).ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Phase>>> GetAllPhases()
        {
            var response = new OASISResult<IEnumerable<Phase>>();
            try
            {
                response.Result = await db.Phase.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<OASISResult<Contract>> GetContract(string id)
        {
            var response = new OASISResult<Contract>();
            try
            {
                FilterDefinition<Contract> filter = Builders<Contract>.Filter.Eq("Id", id);
                response.Result = await db.Contract.Find(filter).FirstOrDefaultAsync();
            }
            catch(Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<OASISResult<IEnumerable<Contract>>> GetAllContracts()
        {
            var response = new OASISResult<IEnumerable<Contract>>();
            try
            {
                response.Result = await db.Contract.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<OASISResult<Contact>> GetContact(string id)
        {
            var response = new OASISResult<Contact>();
            try
            {
                FilterDefinition<Contact> filter = Builders<Contact>.Filter.Eq("Id", id);
                response.Result = await db.Contact.Find(filter).FirstOrDefaultAsync();
            }
            catch(Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContacts(bool loadPhase = false)
        {
            var response = new OASISResult<IEnumerable<Contact>>();
            try
            {
                var contacts = await db.Contact.AsQueryable().ToListAsync();

                if (loadPhase)
                {
                    foreach (Contact contact in contacts.AsQueryable())
                    {
                        var phase = await GetPhase(contact.PhaseId);
                        contact.Phase = phase.Result;
                    }
                }
                response.Result = LoadUserDataIntoContacts(contacts);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }
        
        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContacts(int SequenceNo, int PhaseNo, bool loadPhase = false)
        {
            var response = new OASISResult<IEnumerable<Contact>>();
            try
            {
                List<Contact> filteredContacts = new List<Contact>();
                var contacts = await db.Contact.AsQueryable().ToListAsync();
                var filteredPhases = await GetPhasesByPhaseNo(PhaseNo);
                var filteredSequences = await GetSequencesBySequenceNo(SequenceNo);

                foreach (Contact contact in contacts.AsQueryable())
                {
                    foreach (Phase phase in filteredPhases)
                    {
                        if (phase.Id == contact.PhaseId)
                        {
                            if (loadPhase)
                                contact.Phase = phase;

                            foreach (Sequence sequence in filteredSequences)
                            {
                                if (phase.SequenceId == sequence.Id)
                                {
                                    filteredContacts.Add(contact);
                                    break;
                                }
                            }
                        }
                    }
                }

                response.Result = LoadUserDataIntoContacts(filteredContacts);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }
        
        private List<Contact> LoadUserDataIntoContacts(List<Contact> contacts)
        {
            // TODO: Be good if can find global way of caching the AvatarManager because expensive to start up the providers each time.
            // Just want one persisted/cached but more tricky in web so may need to put it into a cache...
            
            IEnumerable<IAvatar> avatars = AvatarManager.LoadAllAvatars();

            //TODO: Need to change Contact fields in Mongo to match new ones (like Created/Modified/Deleted, etc since User/Profile renamed to Avatar.
            foreach (Contact contact in contacts)
            {
                foreach (IAvatar avatar in avatars)
                {

                    if (contact.AvatarId == avatar.Id.ToString())
                    {
                        contact.FirstName = avatar.FirstName;
                        contact.LastName = avatar.LastName;
                        contact.CreatedDate = avatar.CreatedDate;
                        contact.DeletedDate = avatar.DeletedDate;
                        contact.Email = avatar.Email;
                        contact.Landline = avatar.LastName;
                        contact.Password = avatar.Password;
                        contact.Title = avatar.Title;
                        contact.CreatedDate = avatar.CreatedDate;
                        contact.DeletedDate = avatar.DeletedDate;
                        contact.Email = avatar.Email;
                        contact.Landline = avatar.LastName;
                        contact.Password = avatar.Password;
                        contact.Title = avatar.Title;
                        contact.Username = avatar.Username;
                        contact.AvatarType = avatar.AvatarType;
                        contact.Version = avatar.Version;
                        //TODO: Change to use Avatar EVERYWHERE ASAP...
                        contact.CreatedByUserId = avatar.CreatedByAvatarId.ToString();
                        contact.DeletedByUserId = avatar.DeletedByAvatarId.ToString();
                        contact.ModifiedByUserId = avatar.ModifiedByAvatarId.ToString();
                        contact.ModifiedDate = avatar.ModifiedDate;
                        break;
                    }
                }
            }
            
            return contacts;
        }

        public Task AddSequence(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }

        public Task Update(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }
        
        public async Task<OASISResult<Delivery>> GetDelivery(string id, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true)
        {
            var response = new OASISResult<Delivery>();
            try
            {
                FilterDefinition<Delivery> filter = Builders<Delivery>.Filter.Eq("Id", id);
                Delivery delivery = await db.Delivery.Find(filter).FirstOrDefaultAsync();

                if (loadDeliveryItems)
                    delivery.DeliveryItems = GetDeliveryItemsForDelivery(delivery.Id).Result.ToList();

                if (loadSignedByUser)
                {
                    //TODO: Fix BUG in MongoDBOASIS with being able to return async methods ASAP!
                    IAvatar avatar = await AvatarManager.LoadAvatarAsync(Guid.Parse(delivery.SignedByUserId));

                    if (avatar != null)
                        delivery.SignedByUserFullName = avatar.FullName;
                }

                if (loadSentToPhase)
                {
                    var sentToPhase = await GetPhase(delivery.SentToPhaseId);
                    delivery.SentToPhase = sentToPhase.Result;
                }

                if (delivery.SentToPhase != null)
                {
                    var sequence = await GetSequence(delivery.SentToPhase.SequenceId);
                    delivery.SentToPhase.Sequence = sequence.Result;
                }

                if (delivery.DeliveryItems != null)
                {
                    foreach (var deliveryItem in delivery.DeliveryItems.Where(deliveryItem => loadFile))
                    {
                        var file = await GetFile(deliveryItem.FileId);
                        deliveryItem.File = file.Result;
                    }
                }

                response.Result = delivery;
            }
            catch(Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<IEnumerable<Delivery>>> GetAllDeliveries(bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true,  bool loadFile = true)
        {
            var response = new OASISResult<IEnumerable<Delivery>>();
            try
            {
                var deliveries = await db.Delivery.AsQueryable().ToListAsync();

                for (int i = 0; i < deliveries.Count; i++)
                {
                    var delivery = await GetDelivery(deliveries[i].Id, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadFile);
                    deliveries[i] = delivery.Result;
                }

                response.Result = deliveries;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.Exception = ex;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }
        
        public async Task<OASISResult<IEnumerable<Delivery>>> GetAllDeliveries(int sequenceNo, int phaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true)
        {
            var response = new OASISResult<IEnumerable<Delivery>>();
            try
            {
                List<Delivery> filteredDeliveries = new List<Delivery>();
                var deliveries = await db.Delivery.AsQueryable().ToListAsync();
                var filteredPhases = await GetPhasesByPhaseNo(phaseNo);
                var filteredSequences = await GetSequencesBySequenceNo(sequenceNo);

                for (int i=0; i < deliveries.Count(); i++)
                {
                    foreach (Phase phase in filteredPhases)
                    {
                        if (phase.Id == deliveries[i].PhaseId)
                        {
                            foreach (Sequence sequence in filteredSequences)
                            {
                                if (phase.SequenceId == sequence.Id)
                                {
                                    var delivery = await GetDelivery(deliveries[i].Id, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadFile);
                                    deliveries[i] = delivery.Result;
                                    filteredDeliveries.Add(deliveries[i]);
                                    break;
                                }
                            }
                        }
                    }
                }

                response.Result = filteredDeliveries;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }

            return response;
        }

        public async Task<OASISResult<DeliveryItem>> GetDeliveryItem(string id)
        {
            var response = new OASISResult<DeliveryItem>();
            try
            {
                FilterDefinition<DeliveryItem> filter = Builders<DeliveryItem>.Filter.Eq("Id", id);
                response.Result = await db.DeliveryItem.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.Exception = ex;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<OASISResult<IEnumerable<DeliveryItem>>> GetAllDeliveryItems()
        {
            var response = new OASISResult<IEnumerable<DeliveryItem>>();
            try
            {
                response.Result = await db.DeliveryItem.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.Message = ex.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, ex.Message);
            }
            return response;
        }

        public async Task<IEnumerable<DeliveryItem>> GetDeliveryItemsForDelivery(string deliveryId)
        {
            try
            {
                FilterDefinition<DeliveryItem> sequenceFilter = Builders<DeliveryItem>.Filter.Eq("DeliveryId", deliveryId);
                return await db.DeliveryItem.Find(sequenceFilter).ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Drawing>> GetDrawing(string id)
        {
            FilterDefinition<Drawing> filter = Builders<Drawing>.Filter.Eq("Id", id);
            return await db.Drawing.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<OASISResult<IEnumerable<Drawing>>> GetAllDrawings(bool loadPhase = false, bool loadFile = true)
        {
            try
            {
                var drawings = await db.Drawing.AsQueryable().ToListAsync();

                if (loadPhase || loadFile)
                {
                    foreach (Drawing drawing in drawings)
                    {
                        if (loadPhase)
                            drawing.Phase = await GetPhase(drawing.PhaseId);

                        if (loadFile)
                            drawing.File = await GetFile(drawing.FileId);
                    }
                }
                
                return drawings;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Drawing>>> GetAllDrawings(int SequenceNo, int PhaseNo, bool loadPhase = false, bool loadFile= true)
        {
            try
            {
                List<Drawing> filteredDrawings = new List<Drawing>();

                var filteredPhases = await GetPhasesByPhaseNo(PhaseNo);
                var filteredSequences = await GetSequencesBySequenceNo(SequenceNo);
                var drawings = await db.Drawing.AsQueryable().ToListAsync();
                //var drawings = await GetAllDrawings();

                foreach (Drawing drawing in drawings.AsQueryable())
                {
                    foreach (Phase phase in filteredPhases)
                    {
                        if (phase.Id == drawing.PhaseId)
                        {
                            if (loadPhase)
                                drawing.Phase = phase;

                            foreach (Sequence sequence in filteredSequences)
                            {
                                if (phase.SequenceId == sequence.Id)
                                {
                                    filteredDrawings.Add(drawing);
                                    break;
                                }
                            }
                        }
                    }

                    if (loadFile)
                        drawing.File = await GetFile(drawing.FileId);
                }

                // Load the child File objects.
                /*
                if (loadFile)
                {
                    foreach (Drawing drawing in filteredDrawings)
                        drawing.File = await GetFile(drawing.FileId);
                }*/


                //Use to pull back the whole Phase object too but that adds a lot more to the payload send back so we only want the File object
                /*
                var drawings = db.MongoDbBEB.GetCollection<Drawing>("Drawing");

                var resultOfJoin = drawings.Aggregate();

                if (includeFileObject)
                    resultOfJoin = (IAggregateFluent<Drawing>)resultOfJoin.Lookup("File", "FileId", "_id", @as: "File").Unwind("File");

                if (includePhaseObject)
                    resultOfJoin = (IAggregateFluent<Drawing>)resultOfJoin.Lookup("Phase", "PhaseId", "_id", @as: "Phase").Unwind("Phase");
                
                var drawingsExtended = resultOfJoin.As<Drawing>().ToList();
                */

                /*
                var resultOfJoin = drawings.Aggregate()
                   // .Lookup("Phase", "PhaseId", "_id", @as: "Phase")
                    .Lookup("File", "FileId", "_id", @as: "File")
                   //.Lookup("Phase.Sequence", "Phase.SequenceId", "_id", @as: "Sequence")  //TODO: Need to find out why this doesn't work?!
                 //  .Unwind("Phase")
                   .Unwind("File")
                    //.Unwind("Sequence")
                    .As<Drawing>()
                    .ToList();
                    */

                /*
                foreach (Drawing drawing in resultOfJoin)
                {
                    if (drawing.Phase.PhaseNo == PhaseNo)
                    {
                        foreach (Sequence sequence in filteredSequences)
                        {
                            if (drawing.Phase.SequenceId == sequence.Id)
                            {
                                filteredDrawings.Add(drawing);
                                break;
                            }
                        }
                    }

                    //if (contact.Phase.PhaseNo == PhaseNo && contact.Phase.Sequence.SequenceNo == SequenceNo.ToString())
                    //    filteredContacts.Add(contact);
                }
                
                if (!includePhaseObject)
                {
                    FilterDefinition<Phase> phaseFilter = Builders<Phase>.Filter.Eq("PhaseNo", PhaseNo);
                    var filteredPhases = await db.Phase.Find(phaseFilter).ToListAsync();

                    foreach (Drawing drawing in drawingsExtended)
                    {
                        foreach (Phase phase in filteredPhases)
                        {
                            if (phase.Id == drawing.PhaseId)
                            {
                                foreach (Sequence sequence in filteredSequences)
                                {
                                    if (phase.SequenceId == sequence.Id)
                                    {
                                        filteredDrawings.Add(drawing);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //If Phase has been loaded already (later we will try and get the join above to also load sequences)
                    foreach (Drawing drawing in drawingsExtended)
                    {
                        if (drawing.Phase.PhaseNo == PhaseNo)
                        {
                            foreach (Sequence sequence in filteredSequences)
                            {
                                if (drawing.Phase.SequenceId == sequence.Id)
                                {
                                    filteredDrawings.Add(drawing);
                                    break;
                                }
                            }
                        }   
                    }
                }
                */

                // Load the child File objects.
                //foreach (Drawing drawing in filteredDrawings)
                //  drawing.File = await GetFile(drawing.FileId);

                return filteredDrawings;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<File>> GetFile(string id)
        {
            try
            {
                FilterDefinition<File> filter = Builders<File>.Filter.Eq("Id", id);
                return await db.File.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<File>>> GetAllFiles()
        {
            try
            {
                return await db.File.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Handover>> GetHandover(string id)
        {
            try
            {
                FilterDefinition<Handover> filter = Builders<Handover>.Filter.Eq("Id", id);
                return await db.Handover.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Handover>>> GetAllHandovers()
        {
            try
            {
                return await db.Handover.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Link>> GetLink(string id)
        {
            try
            {
                FilterDefinition<Link> filter = Builders<Link>.Filter.Eq("Id", id);
                return await db.Link.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Link>>> GetAllLinks()
        {
            try
            {
                return await db.Link.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Log>> GetLog(string id)
        {
            try
            {
                FilterDefinition<Log> filter = Builders<Log>.Filter.Eq("Id", id);
                return await db.Log.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Log>>> GetAllLogs()
        {
            try
            {
                return await db.Log.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Material>> GetMaterial(string id)
        {
            try
            {
                FilterDefinition<Material> filter = Builders<Material>.Filter.Eq("Id", id);
                return await db.Material.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Material>>> GetAllMaterials()
        {
            try
            {
                return await db.Material.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Note>> GetNote(string id)
        {
            try
            {
                FilterDefinition<Note> filter = Builders<Note>.Filter.Eq("Id", id);
                return await db.Note.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Note>>> GetAllNotes()
        {
            try
            {
                return await db.Note.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<Trigger>> GetTrigger(string id)
        {
            try
            {
                FilterDefinition<Trigger> filter = Builders<Trigger>.Filter.Eq("Id", id);
                return await db.Trigger.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Trigger>>> GetAllTriggers()
        {
            try
            {
                return await db.Trigger.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
