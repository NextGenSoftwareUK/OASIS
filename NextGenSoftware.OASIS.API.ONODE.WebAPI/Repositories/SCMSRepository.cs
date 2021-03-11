using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class SCMSRepository : ISCMSRepository
    {
        MongoDbContext db = new MongoDbContext();
        //private AvatarManager _avatarManager;

        public AvatarManager AvatarManager
        {
            get
            {
                return Program.AvatarManager;

                //if (_avatarManager == null)
                //{
                //    _avatarManager = new AvatarManager();
                //    _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;
                //}

                //return _avatarManager;
            }
        }

        //private void _avatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        //{
        //    //TODO: Log and handle errors here.
        //}

        //public async Task Add(Avatar Avatar)
        //{
        //    try
        //    {
        //        await db.Avatar.InsertOneAsync(Avatar);
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        //private AvatarManager GetAvatarManager()
        //{
        //    if (AvatarManager.Instance.CurrentOASISStorageProvider == null)
        //        AvatarManager.Instance.SetOASISStorageProvider(new HoloOASIS("ws://localhost:8888")); //Default to HoloOASIS Provider.

        //    return AvatarManager.Instance;
        //}

        public async Task<Sequence> GetSequence(string id)
        {
            try
            {
                FilterDefinition<Sequence> filter = Builders<Sequence>.Filter.Eq("Id", id);
                return await db.Sequence.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Sequence>> GetAllSequences()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Sequence.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
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

        public async Task<Phase> GetPhase(string id)
        {
            try
            {
                FilterDefinition<Phase> filter = Builders<Phase>.Filter.Eq("Id", id);
                return await db.Phase.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
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

        public async Task<IEnumerable<Phase>> GetAllPhases()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Phase.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Contract> GetContract(string id)
        {
            try
            {
                FilterDefinition<Contract> filter = Builders<Contract>.Filter.Eq("Id", id);
                return await db.Contract.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Contract>> GetAllContracts()
        {
            try
            {
                return await db.Contract.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Contact> GetContact(string id)
        {
            try
            {
                FilterDefinition<Contact> filter = Builders<Contact>.Filter.Eq("Id", id);
                return await db.Contact.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Contact>> GetAllContacts(bool loadPhase = false)
        {
            try
            {
                var contacts = await db.Contact.AsQueryable().ToListAsync();

                if (loadPhase)
                {
                    foreach (Contact contact in contacts.AsQueryable())
                        contact.Phase = await GetPhase(contact.PhaseId);
                }

                contacts = LoadUserDataIntoContacts(contacts);
                return contacts;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        public async Task<IEnumerable<Contact>> GetAllContacts(int SequenceNo, int PhaseNo, bool loadPhase = false)
        {
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

                filteredContacts = LoadUserDataIntoContacts(filteredContacts);

                /*
                //var contacts = await db.Contact.Find({"VIP": true, "Country": "Germany"});
                List<Contact> filteredContacts = new List<Contact>();
                //FilterDefinition<Contact> filter = Builders<Contact>.Filter.Eq("SequenceNo", SequenceNo);

            //    var contacts = await db.Contact.Find(filter).ToListAsync();

              //  string connectionString = "mongodb://localhost:27017";
              //  var client = new MongoClient(connectionString);

              //  var db = client.GetDatabase("test");
                var contacts = db.MongoDbBEB.GetCollection<Contact>("Contact");
                var resultOfJoin = contacts.Aggregate()
                    .Lookup("Phase", "PhaseId", "_id", @as: "Phase")
                    //.Lookup("Phase.Sequence", "Phase.SequenceId", "_id", @as: "Sequence")  //TODO: Need to find out why this doesn't work?!
                   .Unwind("Phase")
                    //.Unwind("Sequence")
                    .As<Contact>()
                    .ToList();

     
                foreach (Contact contact in resultOfJoin)
                {
                    if (contact.Phase.PhaseNo == PhaseNo)
                    {
                        foreach (Sequence sequence in filteredSequences)
                        {
                            if (contact.Phase.SequenceId == sequence.Id)
                            {
                                filteredContacts.Add(contact);
                                break;
                            }
                        }
                    }

                    //if (contact.Phase.PhaseNo == PhaseNo && contact.Phase.Sequence.SequenceNo == SequenceNo.ToString())
                    //    filteredContacts.Add(contact);
                }
                */


                /*
                var users = db.User.AsQueryable().ToListAsync();

                foreach (Contact contact in filteredContacts)
                {
                    foreach (User user in users.Result)
                    {
                        if (contact.UserId == user.Id)
                        {
                            contact.FirstName = user.FirstName;
                            contact.LastName = user.LastName;
                            contact.Address = user.Address;
                            contact.Country = user.Country;
                            contact.County = user.County;
                            contact.CreatedByUserId = user.CreatedByUserId;
                            contact.CreatedDate = user.CreatedDate;
                            contact.DeletedByUserId = user.DeletedByUserId;
                            contact.DeletedDate = user.DeletedDate;
                            contact.Email = user.Email;
                            contact.DOB = user.DOB;
                            contact.Landline = user.LastName;
                            contact.Mobile = user.Mobile;
                            contact.ModifledByUserId = user.ModifledByUserId;
                            contact.ModifledDate = user.ModifledDate;
                            contact.Password = user.Password;
                            contact.Postcode = user.Postcode;
                            contact.Title = user.Title;
                            contact.Town = user.Town;
                            contact.Username = user.Username;
                            contact.UserType = user.UserType;
                            contact.Version = user.Version;
                            break;
                        }
                    }
                }*/

                //return await db.Sequence.Find(_ => true).ToListAsync();
                //return await db.Contact.AsQueryable().ToListAsync();
                //return contacts.Result;
                return filteredContacts;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        private List<Contact> LoadUserDataIntoContacts(List<Contact> contacts)
        {
            //var users = db.Avatar.AsQueryable().ToListAsync();            

            // TODO: Be good if can find global way of caching the AvatarManager because expensive to start up the providers each time.
            // Just want one persisted/cached but more tricky in web so may need to put it into a cache...

            //OASISProviderManager.SetOASISSettings()
            // OASISProviderManager.GetAndActivateProvider();
            
            //var users = AvatarManager.LoadAllAvatarsAsync();
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
                        contact.Address = avatar.Address;
                        contact.Country = avatar.Country;
                        contact.County = avatar.County;
                        contact.CreatedDate = avatar.CreatedDate;
                        contact.DeletedDate = avatar.DeletedDate;
                        contact.Email = avatar.Email;
                        contact.DOB = avatar.DOB;
                        contact.Landline = avatar.LastName;
                        contact.Mobile = avatar.Mobile;
                        contact.Password = avatar.Password;
                        contact.Postcode = avatar.Postcode;
                        contact.Title = avatar.Title;
                        contact.Town = avatar.Town;
                        contact.Username = avatar.Username;
                        contact.AvatarType = avatar.AvatarType;
                        contact.Version = avatar.Version;

                        //contact.CreatedByAvatarId = avatar.CreatedByAvatarId;
                        //contact.DeletedByAvatarId = avatar.DeletedByAvatarId;
                        //contact.ModifiedByAvatarId = avatar.ModifiedByAvatarId;
                        //contact.ModifiedDate = avatar.ModifiedDate;

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

        /*
        public async Task Update(Avatar Avatar)
        {
            try
            {
                await db.Avatar.ReplaceOneAsync(filter: g => g.Id == Avatar.Id, replacement: Avatar);
            }
            catch
            {
                throw;
            }
        }
        public async Task Delete(string id)
        {
            try
            {
                FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Eq("Id", id);
                await db.Avatar.DeleteOneAsync(data);
            }
            catch
            {
                throw;
            }
        }*/

        public Task AddSequence(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }

        public Task Update(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }

        //public async Task<Delivery> GetDelivery(string id, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadMaterial = true, bool loadFile = true)
        public async Task<Delivery> GetDelivery(string id, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true)
        {
            try
            {
                FilterDefinition<Delivery> filter = Builders<Delivery>.Filter.Eq("Id", id);
                Delivery delivery = await db.Delivery.Find(filter).FirstOrDefaultAsync();

                if (loadDeliveryItems)
                    delivery.DeliveryItems = GetDeliveryItemsForDelivery(delivery.Id).Result.ToList();

                if (loadSignedByUser)
                {
                    //Avatar user = await GetAvatar(delivery.SignedByUserId);
                    
                    //TODO: Fix BUG in MongoDBOASIS with being able to return async methods ASAP!
                    //IAvatar user = await AvatarManager.LoadAvatarAsync(Guid.Parse(delivery.SignedByUserId));
                    IAvatar avatar = AvatarManager.LoadAvatar(Guid.Parse(delivery.SignedByUserId));

                    if (avatar != null)
                        delivery.SignedByUserFullName = avatar.FullName;
                }

                if (loadSentToPhase)
                    delivery.SentToPhase = await GetPhase(delivery.SentToPhaseId);

                if (delivery.SentToPhase != null)
                    delivery.SentToPhase.Sequence = await GetSequence(delivery.SentToPhase.SequenceId);

                if (delivery.DeliveryItems != null)
                {
                    foreach (DeliveryItem deliveryItem in delivery.DeliveryItems)
                    {
                        if (loadFile)
                            deliveryItem.File = await GetFile(deliveryItem.FileId);

                        /*
                        if (loadMaterial)
                        {
                            Material material = await GetMaterial(deliveryItem.MaterialId);

                            if (material != null)
                            {
                                deliveryItem.Material = material;

                                if (loadFile)
                                    material.File = await GetFile(material.FileId);
                            }
                        }*/
                    }
                }

                return delivery;
            }
            catch
            {
                throw;
            }
        }

        //public async Task<IEnumerable<Delivery>> GetAllDeliveries(bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadMaterial = true, bool loadFile = true)
        public async Task<IEnumerable<Delivery>> GetAllDeliveries(bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true,  bool loadFile = true)
        {
            try
            {
                var deliveries = await db.Delivery.AsQueryable().ToListAsync();
               
                for (int i = 0; i < deliveries.Count; i++)
                    deliveries[i] = await GetDelivery(deliveries[i].Id, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadFile);
                //deliveries[i] = await GetDelivery(deliveries[i].Id, loadDeliveryItems,  loadSignedByUser, loadSentToPhase, loadMaterial, loadFile);

                return deliveries;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //public async Task<IEnumerable<Delivery>> GetAllDeliveries(int sequenceNo, int phaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadMaterial = true, bool loadFile = true)
        public async Task<IEnumerable<Delivery>> GetAllDeliveries(int sequenceNo, int phaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true)
        {
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
                                    //deliveries[i] = await GetDelivery(deliveries[i].Id, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadMaterial, loadFile);
                                    deliveries[i] = await GetDelivery(deliveries[i].Id, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadFile);
                                    filteredDeliveries.Add(deliveries[i]);
                                    break;
                                }
                            }
                        }
                    }
                }

                return filteredDeliveries;

                // var deliveries = db.GetCollection<Delivery>("Instances");


                //var resultOfJoin = deliveries.Result.Aggregate()
                //    .Lookup("Templates", "TemplateId", "_id", @as: "Template")
                //    .Unwind("Template")
                //    .As<Instance>()
                //    .ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DeliveryItem> GetDeliveryItem(string id)
        {
            try
            {
                FilterDefinition<DeliveryItem> filter = Builders<DeliveryItem>.Filter.Eq("Id", id);
                return await db.DeliveryItem.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<DeliveryItem>> GetAllDeliveryItems()
        {
            try
            {
                return await db.DeliveryItem.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
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

        /*
        public async Task<Avatar> GetAvatar(string id)
        {
            FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
            return await db.Avatar.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Avatar>> GetAllAvatars()
        {
            try
            {
                return await db.Avatar.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }*/

        public async Task<Drawing> GetDrawing(string id)
        {
            FilterDefinition<Drawing> filter = Builders<Drawing>.Filter.Eq("Id", id);
            return await db.Drawing.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Drawing>> GetAllDrawings(bool loadPhase = false, bool loadFile = true)
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

        public async Task<IEnumerable<Drawing>> GetAllDrawings(int SequenceNo, int PhaseNo, bool loadPhase = false, bool loadFile= true)
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

        public async Task<File> GetFile(string id)
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

        public async Task<IEnumerable<File>> GetAllFiles()
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

        public async Task<Handover> GetHandover(string id)
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

        public async Task<IEnumerable<Handover>> GetAllHandovers()
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

        public async Task<Link> GetLink(string id)
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

        public async Task<IEnumerable<Link>> GetAllLinks()
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

        public async Task<Log> GetLog(string id)
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

        public async Task<IEnumerable<Log>> GetAllLogs()
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

        public async Task<Material> GetMaterial(string id)
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

        public async Task<IEnumerable<Material>> GetAllMaterials()
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

        public async Task<Note> GetNote(string id)
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

        public async Task<IEnumerable<Note>> GetAllNotes()
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

        public async Task<Trigger> GetTrigger(string id)
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

        public async Task<IEnumerable<Trigger>> GetAllTriggers()
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
